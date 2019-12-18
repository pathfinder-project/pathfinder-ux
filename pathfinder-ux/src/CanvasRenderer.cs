using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenSlideNET;


namespace pathfinder_ux
{

    class CanvasRenderer: IDisposable
    {
        /// <summary>
        /// 初始化一个Canvas渲染器
        /// </summary>
        /// <param name="imageBrush"></param>
        public CanvasRenderer(ImageBrush imageBrush)
        {
            bmpBuf = new byte[Helper.BMP_BGRA_DATA_OFFSET + 4096 * 4096 * 4];
            // 除以96.0是因为
            // 1 WPF长度 = 1/96 英寸.
            dpiFactor = (double)dpi / 96.0;

            // 设定每period一帧, 即1000/period fps.
            period = 100;

            // 绑定画刷
            this.imageBrush = imageBrush;

            // 设定并运行定时器
            timer = new System.Timers.Timer(period);
            timer.AutoReset = true;
            timer.Enabled = false;
            timer.Elapsed += RenderFrame;
        }

        private void RenderFrame(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (slide == null)
            {
                return;
            }
            #region 计算左上角像素坐标 & 长度 & 宽度
            lock (this)
            {
                int dx = (int)Math.Round((P0.X - P.X) * dpiFactor * levelShrinkFactors[currLevel]);
                int dy = (int)Math.Round((P.Y - P0.Y) * dpiFactor * levelShrinkFactors[currLevel]);
                P0 = P;
                LeftTopPixelX += dx;
                LeftTopPixelY += dy;
            }
            #endregion

            #region 加载图像 (执行于工作线程)
            Helper.init_bgra_header(bmpBuf, PixelW, PixelH);
            // bmpBuf是工作线程和UI线程的共享内存.
            this.slide.DangerousReadRegion(currLevel,
                LeftTopPixelX, LeftTopPixelY,
                PixelW, PixelH,
                ref this.bmpBuf[Helper.BMP_BGRA_DATA_OFFSET]);
            BitmapImage bi = null;
            #endregion

            // !! WPF不允许后台线程更新UI.
            // !! System.Tmers.Timer.Elapsed回调函数执行在工作线程T里, 不是UI线程.
            // !! Dispatcher.Invoke的回调函数是在「创建Dispatcher的线程」里执行的. 此处为UI线程.
            // !! 程序员需要在Dispatcher.Invoke里调用或实现「UI线程和工作线程的同步机制」
            imageBrush.Dispatcher.Invoke(() => {
                #region 同步UI线程和工作线程. i.e. 更新UI (执行于UI线程)
                using (var ms = new System.IO.MemoryStream(bmpBuf))
                {
                    bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = ms;
                    bi.EndInit();
                }
                imageBrush.ImageSource = bi;
                #endregion
            }, System.Windows.Threading.DispatcherPriority.DataBind);
            return;
        }

        // 当鼠标左键按下时,
        // 记录鼠标位置, 初始化鼠标上一次位置
        public void OnMouseLeftPress(System.Windows.Point p)
        {
            lock (this)
            {
                P0 = P = p;
            }
        }

        // 当鼠标拖动时,
        // 更新鼠标位置
        public void OnMouseDrag(System.Windows.Point p)
        {
            lock (this)
            {
                P = p;
            }
        }

        // 当鼠标滚轮有动作时, 
        // (1) 更新缩放级别; (2) 调整左上角像素坐标.
        public void OnMouseScroll(System.Windows.Point p, int scroll)
        {

            if (slide == null) return;
            if (scroll == 0) return;
            else
            {
                int sign = scroll > 0 ? 1 : -1;
                lock (this)
                {
                    int prevLevel = currLevel;
                    currLevel = Math.Max(Math.Min(numLevels - 1, currLevel + sign), 0);
                    if (prevLevel == currLevel) return;
                    double scale = levelShrinkFactors[prevLevel] / levelShrinkFactors[currLevel];
                    int dx = (int)Math.Round((scale - 1) * (p.X * dpiFactor * levelShrinkFactors[currLevel]));
                    int dy = (int)Math.Round((scale - 1) * (p.Y * dpiFactor * levelShrinkFactors[currLevel]));
                    LeftTopPixelX += dx;
                    LeftTopPixelY += dy;
                }
            }
        }

        // 当画布尺寸变化时, 
        // 记录画布尺寸
        public void OnCanvasResize(double h, double w)
        {
            lock (this)
            {
                canvasH = h;
                canvasW = w;
            }
        }

        public void LoadSlide(string slidePath)
        {
            string absPath = System.IO.Path.GetFullPath(slidePath);
            bool isSameFile = object.Equals(absPath, this.slidePath);

            // 如果是同一个文件, 就不要重复打开.
            if (isSameFile)
            {
                return;
            }

            // 如果不是同一个文件, 就妥善关闭已打开的文件, 再打开新文件
            else
            {
                CloseSlide();
                this.slidePath = absPath;
                slide = OpenSlideImage.Open(slidePath);
                leftTopX = (int)slide.Width / 2;
                leftTopY = (int)slide.Height / 2;

                // 缩放相关
                currLevel = 0;
                numLevels = slide.LevelCount;

                // 取得每一级缩小的倍率. 留用.
                levelShrinkFactors = new double[numLevels];
                for (int sf = 0; sf < numLevels; sf++)
                {
                    levelShrinkFactors[sf] = slide.GetLevelDownsample(sf);
                }

                timer.Start();
            }
        }

        // 关闭
        public void CloseSlide()
        {
            timer.Stop();

            if (slide != null)
            {
                slide.Dispose(); // 关闭切片对象
                slide = null; // 提示GC
            }
            imageBrush.ImageSource = null;
            this.slidePath = null;
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Dispose();
            CloseSlide();
        }

        // 切片的路径
        public string Path {
            get { return slidePath; }
            set { LoadSlide(value); }
        }

        // 切片刷新的帧率
        public double FPS { get { return 1000.0 / period; } }

        // 图片加载出来的高度, 以像素计
        public int PixelH { get { return (int)Math.Round(canvasH * dpiFactor); } }

        // 图片加载出来的宽度, 以像素计
        public int PixelW { get { return (int)Math.Round(canvasW * dpiFactor); } }

        // 切片的0级(最大倍率)宽度, 以像素计
        public int SlidePixelW { get { return (int)slide.Width; } }

        // 切片的0级(最大倍率)高度, 以像素计
        public int SlidePixelH { get { return (int)slide.Height; } }

        // Canvas左上角的纵坐标(行编号).
        // 对应于`slide`中, 0级(最大)倍率的坐标.
        private int LeftTopPixelY
        {
            get
            {
                return this.leftTopY;
            }
            set
            {
                int maxAvailable = SlidePixelH - (int)Math.Round(canvasH * dpiFactor * levelShrinkFactors[currLevel]);
                this.leftTopY = Math.Max(Math.Min(value, maxAvailable), 0);
            }
        }

        // Canvas左上角的横坐标 (列编号).
        // 对应于`slide`中, 0级(最大)倍率的坐标.
        private int LeftTopPixelX
        {
            get 
            { 
                return this.leftTopX; 
            }
            set
            {
                int maxAvailable = SlidePixelW - (int)Math.Round(canvasW * dpiFactor * levelShrinkFactors[currLevel]);
                this.leftTopX = Math.Max(Math.Min(value, maxAvailable), 0);
            }
        }

        // 带有BMP文件头的图像数据, 通道序BGRA, 维序未知.
        private byte[] bmpBuf;

        // OpenSlide.NET提供的数字切片对象
        private OpenSlideImage slide;

        // 待读取区域的左上角在0级缩放下的横坐标.
        private int leftTopX;

        // 待读取区域的左上角在0级缩放下的纵坐标
        private int leftTopY;

        // Canvas所显示内容的强制性DPI. 屏幕的每英寸对应`dpi`这么多的像素数.
        private readonly int dpi = 120;

        // 根据DPI换算出来的, WPF长度与像素长度的比值.
        private double dpiFactor;

        // 切片的路径
        private string slidePath;

        // 切片刷新的周期(毫秒)
        private int period;

        // 刷新切片的计时器
        // API文档: https://docs.microsoft.com/en-us/dotnet/api/system.timers.timer?view=netframework-4.5
        // C#还有一个System.Threading.Timer, 但接口说明比System.Timers.Timer复杂多了,
        // 因此我不想用 (懒  死  了).
        private System.Timers.Timer timer;

        // 前端画布的画刷.
        private ImageBrush imageBrush;

        // 鼠标当前的位置
        private System.Windows.Point P;

        // 上一帧(i.e 计时器上一次触发) 或 计时器刚开始时的
        // 鼠标X, Y坐标. 单位是WPF长度.
        private System.Windows.Point P0;

        // 当前切片有多少级缩小
        private int numLevels;

        // 当前缩小级别
        private int currLevel;

        // 当前缩小倍率
        private double[] levelShrinkFactors;

        // Canvas的WPF高度(竖直跨度)
        private double canvasH;

        // Canvas的WPF宽度(水平跨度)
        private double canvasW;
    }
}
