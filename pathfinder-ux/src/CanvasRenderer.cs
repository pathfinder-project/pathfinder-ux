using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenSlideNET;


namespace pathfinder_ux
{
    class CanvasRenderer
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

            // 设定每67ms一帧, 即15fps.
            period = 67;

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
            // 因为timer事件执行在另一个线程里(不是UI线程), WPF不允许线程直接更新UI.
            imageBrush.Dispatcher.Invoke(delegate
            {
                imageBrush.ImageSource = _render();
            });
        }

        private BitmapImage _render() 
        {
            if (slide == null) return null;
            #region 计算左上角像素坐标 & 长度 & 宽度
            lock (this)
            {
                int dx = (int)Math.Round((P0.X - P.X) * dpiFactor);
                int dy = (int)Math.Round((P.Y - P0.Y) * dpiFactor);
                P0 = P;
                LeftTopPixelX += dx;
                LeftTopPixelY += dy;
            }
            #endregion

            #region 加载图像
            //bmpBuf = new byte[FileLength];
            Helper.init_bgra_header(bmpBuf, PixelW, PixelH);
            //MessageBox.Show(string.Format("PixelH={0}, PixelW={1}", PixelH, PixelW));
            this.slide.DangerousReadRegion(ScaleLevel,
                LeftTopPixelX, LeftTopPixelY,
                PixelW, PixelH,
                ref this.bmpBuf[Helper.BMP_BGRA_DATA_OFFSET]);
            BitmapImage bi = null;
            using (var ms = new System.IO.MemoryStream(bmpBuf))
            {
                bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.StreamSource = ms;
                bi.EndInit();
            }
            #endregion
            return bi;
        }

        public void Render()
        {
            imageBrush.ImageSource = _render();
        }

        public void MouseLeftPress(System.Windows.Point p)
        {
            P0 = P = p;
            timer.Start();
        }

        public void MouseLeftRelease()
        {
            timer.Stop();
        }

        public void LoadSlide(string slidePath)
        {
            string absPath = Path.GetFullPath(slidePath);
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
                Render();
            }
        }

        // 关闭
        public void CloseSlide()
        {
            if (slide != null)
            {
                slide.Dispose(); // 关闭切片对象
                slide = null; // 提示GC
            }
            this.slidePath = null;
        }

        // 切片的路径
        public string SlidePath {
            get { return slidePath; }
            set { LoadSlide(value); }
        }

        // 切片刷新的帧率
        public double FPS { get { return 1000.0 / period; } }

        // Canvas的WPF高度(竖直跨度)
        public double CanvasH { get; set; }

        // Canvas的WPF宽度(水平跨度)
        public double CanvasW { get; set; }

        // 需要加载的图片高度, 以像素计
        public int PixelH { get { return (int)Math.Round(CanvasH * dpiFactor); } }

        // 需要加载的图片宽度, 以像素计
        public int PixelW { get { return (int)Math.Round(CanvasW * dpiFactor); } }

        // 切片的0级(最大倍率)宽度, 以像素计
        public int SlidePixelW { get { return (int)slide.Width; } }

        // 切片的0级(最大倍率)高度, 以像素计
        public int SlidePixelH { get { return (int)slide.Height; } }

        // 鼠标当前的位置
        public System.Windows.Point P { get; set; }

        // Canvas左上角的纵坐标(行编号).
        // 对应于`slide`中, 0级(最大)倍率的坐标.
        public int LeftTopPixelY
        {
            get
            {
                return this.leftTopY;
            }
            private set // UI不能直接设置左上角坐标, 因此改成private set
            {
                if (value < 0)
                    this.leftTopY = 0;
                else if (value > SlidePixelH - PixelH)
                    this.leftTopY = SlidePixelH - PixelH;
                else
                    this.leftTopY = value;
            }
        }

        // Canvas左上角的横坐标 (列编号).
        // 对应于`slide`中, 0级(最大)倍率的坐标.
        private int LeftTopPixelX
        {
            get {
                return this.leftTopX;
            }
            set // UI不能直接设置左上角坐标, 因此改成private set
            {
                if (value < 0)
                    this.leftTopX = 0;
                else if (value > SlidePixelW - PixelH)
                    this.leftTopX = SlidePixelW - PixelH;
                else
                    this.leftTopX = value;
            }
        }

        // BMP缓冲区有效大小.
        public int FileLength { get { return Helper.BMP_BGRA_DATA_OFFSET + PixelH * PixelW * 4; } }

        // 数字切片的缩放级别
        public int ScaleLevel { get; set; }

        // 带有BMP文件头的图像数据, 通道序BGRA, 维序未知.
        private byte[] bmpBuf;

        // OpenSlide.NET提供的数字切片对象
        private OpenSlideImage slide;

        // Canvas左上角的横坐标 (列编号).
        // 对应于`slide`中, 0级(最大)倍率的坐标.
        private int leftTopX;

        // Canvas左上角的纵坐标 (行编号).
        // 对应于`slide`中, 0级(最大)倍率的坐标.
        private int leftTopY;

        // Canvas所显示内容的强制性DPI. 屏幕的每英寸对应`dpi`这么多的像素数.
        private int dpi = 120;

        // 根据DPI换算出来的, WPF长度与像素长度的比值.
        private double dpiFactor;

        // 切片的路径
        private string slidePath;

        // 切片刷新的周期(毫秒)
        private int period;

        // 刷新切片的计时器
        // API文档: https://docs.microsoft.com/en-us/dotnet/api/system.timers.timer?view=netframework-4.5
        // C#还有一个System.Threading.Timer, 但接口说明比System.Timers.Timer复杂多了,
        // 因此我不想用.
        private System.Timers.Timer timer;

        // 前端界面的画刷.
        private ImageBrush imageBrush;

        // 上一帧(i.e 计时器上一次触发) 或 计时器刚开始时的
        // 鼠标X, Y坐标. 单位是WPF长度.
        private System.Windows.Point P0;
    }
}
