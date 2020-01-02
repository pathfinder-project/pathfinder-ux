using PathFinder.DataStructure;
using PathFinder.Layer;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PathFinder
{
    class Scene : IDisposable
    {
        // 每period毫秒合成1帧画面.
        // 如果合成第i帧所消耗的时间超过了period, 则等到第i帧合成完毕才开始合成第i+1帧.
        // 如果合成时间小于period, 则工作线程忙等到period才合成第i+1帧.
        private long period;

        private Canvas canvas;
        private ImageBrush canvasBg;
        private List<ILayer> layers;
        private SceneGeometry sg;
        private MapGeometry mg;
        private Mutex mutex;
        private Thread worker;

        private IntTuple P0;
        private IntTuple P;

        public Scene(Canvas canvas, ImageBrush canvasBg, int fps)
        {
            this.canvas = canvas;
            this.canvasBg = canvasBg;
            period = (long)(1000.0 / fps);
            layers = new List<ILayer>();
            sg = new SceneGeometry();
            mg = new MapGeometry();
            P0 = new IntTuple();
            P = new IntTuple();
            mutex = new Mutex();
            worker = new Thread(new ThreadStart(Work));
        }

        private void Work()
        {
            SceneGeometry sg0 = new SceneGeometry(); // 上一帧的几何信息
            SceneGeometry sg1 = new SceneGeometry(); // 当前帧的几何信息
            while (true)
            {
                long beg = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // 当前帧开始的时间

                // 获取画面的几何信息, 保存thread local
                mutex.WaitOne();
                sg.X -= (int)Math.Round((P.X - P0.X) * mg.GetScaleAtLevel(sg.L));
                sg.Y += (int)Math.Round((P.Y - P0.Y) * mg.GetScaleAtLevel(sg.L));
                P0.CopyValuesFrom(P);
                var paddingX = (int)Math.Round(sg.W * mg.GetScaleAtLevel(sg.L) / 2);
                var paddingY = (int)Math.Round(sg.H * mg.GetScaleAtLevel(sg.L) / 2);
                sg.X = Math.Max(Math.Min(sg.X, mg.W - paddingX), 0);
                sg.Y = Math.Max(Math.Min(sg.Y, mg.H - paddingY), 0);
                sg1.CopyValuesFrom(sg);
                mutex.ReleaseMutex();

                // 只有画面的几何信息发生变化才合成新的帧.
                if (sg1 != sg0)
                {
                    // 先只做image layer的合成
                    // 做好了再做annotation layer
                    var bg = layers[0] as ImageLayer;
                    byte[] img = bg.LoadRegion(sg1) as byte[];
                    canvasBg.Dispatcher.Invoke(() => {
                        BitmapImage bi = null;
                        #region 同步UI线程和工作线程. i.e. 更新UI (执行于UI线程)
                        using (var ms = new System.IO.MemoryStream(img))
                        {
                            bi = new BitmapImage();
                            bi.BeginInit();
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.StreamSource = ms;
                            bi.EndInit();
                        }
                        canvasBg.ImageSource = bi;
                        #endregion
                    }, System.Windows.Threading.DispatcherPriority.DataBind);
                    sg0.CopyValuesFrom(sg1); // 保存几何信息的历史
                }
                
                // 如果end-beg小于period, 则忙等到period
                for (long end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // 当前帧结束的时间
                          end - beg < period;
                          end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    Thread.Sleep(1);
                }
            }
        }

        public void LoadSlide(string path)
        {
            ImageLayer im = new ImageLayer();
            im.Open(path, mg);
            layers.Add(im);
            sg.X = mg.W / 2;
            sg.Y = mg.H / 2;
            worker.Start();
        }

        public void Dispose()
        {
            CloseSlide();
        }
        
        private void CloseSlide()
        {
            worker.Abort();
            sg.Init();
            mg.Init();
            foreach (ILayer layer in layers)
            {
                layer.Dispose();
            }
            layers.Clear();
        }

        public void BeginDrag()
        {
            mutex.WaitOne();
            Helper.GetMousePosition(ref P0.X, ref P0.Y);
            P.CopyValuesFrom(P0);
            mutex.ReleaseMutex();
        }

        public void DuringDrag()
        {
            mutex.WaitOne();
            Helper.GetMousePosition(ref P.X, ref P.Y);
            mutex.ReleaseMutex();
        }

        /// <summary>
        /// 缩放. 保持中心点不动.
        /// </summary>
        /// <param name="n"></param>
        public void Zoom(int n)
        {
            mutex.WaitOne();
            int sign = Math.Sign(n);
            int srcL = sg.L;
            int dstL = Math.Max(Math.Min(mg.NumL - 1, sg.L + sign), 0);

            // 如果从最大继续放大, 或从最小继续缩小, 则不要再缩放了
            if (srcL != dstL)
            {
                sg.L = dstL;
                double srcScale = mg.GetScaleAtLevel(srcL);
                double dstScale = mg.GetScaleAtLevel(dstL);

                int srcA = (int)(sg.W * srcScale / 2);
                int srcB = (int)(sg.H * srcScale / 2);
                int dstA = (int)(sg.W * dstScale / 2); // 目标倍率下, 长半轴(宽度的一半)在0级缩放下的长度.
                int dstB = (int)(sg.H * dstScale / 2); // 短半轴(高度的一半)在0级缩放下的长度.
                int centerX = sg.X + srcA;
                int centerY = sg.Y + srcB;
                sg.X = centerX - dstA;
                sg.Y = centerY - dstB;
            }

            mutex.ReleaseMutex();
        }

        public void Resize(double wpfW, double wpfH)
        {
            mutex.WaitOne();
            Helper.GetPixelXY(canvas, wpfW, wpfH, ref sg.W, ref sg.H);
            mutex.ReleaseMutex();
        }
    }
}
