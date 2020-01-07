using PathFinder.DataStructure;
using PathFinder.Layer;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PathFinder.Controller
{
    using Actions;
    using Polygon = System.Collections.Generic.List<PixelCoordinate>;

    class SceneBlender
    {
        // 每period毫秒合成1帧画面.
        // 如果合成第i帧所消耗的时间超过了period, 则等到第i帧合成完毕才开始合成第i+1帧.
        // 如果合成时间小于period, 则工作线程忙等到period才合成第i+1帧.
        private long period;

        private Canvas canvas;
        private ActionQueue msgq;
        private ImageBrush canvasBg;

        private SlideLayer bg;
        private AnnotationLayer fg;
        private Thread worker;

        public SceneBlender(Canvas canvas, ImageBrush canvasBg, int fps)
        {
            this.canvas = canvas;
            this.canvasBg = canvasBg;
            period = (long)(1000.0 / fps);
            msgq = ActionQueue.Singleton();
            bg = new SlideLayer();
            fg = new AnnotationLayer();
            worker = new Thread(new ThreadStart(Work));
        }

        public void Start()
        {
            worker.Start();
        }

        public void Stop()
        {
            worker.Abort();
        }

        private void Work()
        {
            SceneGeometry sg0 = new SceneGeometry();
            SceneGeometry sg1 = new SceneGeometry();
            Polygon polygonEmbryo = null;

            while (true)
            {
                long beg = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // 当前帧开始的时间
                int dX = 0, dY = 0;

                // 获取画面的几何信息, 保存thread local
                var pendingActions = msgq.CheckOut();
                if (pendingActions.Length == 0)
                {
                    long end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    long delta = end - beg;
                    int ms = (int)(period - delta);
                    Thread.Sleep(ms);
                    continue; // 注意不是执行剩余的循环体, 而是开始新的循环.
                }

                // 「打草稿」阶段
                foreach (var act in pendingActions)
                {
                    
                    if (act is GeometryAction)
                    {
                        if (act is Move)
                        {
                            var a = act as Move;
                            dX += a.dX;
                            dY += a.dY;
                        }
                        else if (act is Zoom)
                        {
                            var a = act as Zoom;
                            sg1.Zoom(a.nScroll, a.X, a.Y);
                        }
                        else if (act is Resize)
                        {
                            var a = act as Resize;
                            sg1.Resize(a.W, a.H);
                        }
                    }
                    else if (act is DrawingAction)
                    {
                        if (act is DrawPolygonFirstV)
                        {
                            var a = act as DrawPolygonFirstV;
                            polygonEmbryo = new Polygon();
                            polygonEmbryo.Add(new PixelCoordinate() { 
                                X = sg1.X + sg1.ToActualPixel(a.X), 
                                Y = sg1.Y + sg1.ToActualPixel(a.Y) });
                        }
                        else if (act is DrawPolygonV)
                        {
                            var a = act as DrawPolygonFirstV;
                            polygonEmbryo.Add(new PixelCoordinate()
                            {
                                X = sg1.X + sg1.ToActualPixel(a.X),
                                Y = sg1.Y + sg1.ToActualPixel(a.Y)
                            });
                        }
                        else if (act is DrawPolygonFinish)
                        {
                            fg.AddPolygon(polygonEmbryo);
                            polygonEmbryo = null;
                        }
                    }
                    else if (act is FileAction)
                    {
                        if (act is LoadSlide)
                        {
                            var a = act as LoadSlide;
                            bg.Open(a.Path, sg1);
                            sg1.W = a.W;
                            sg1.H = a.H;
                        }
                        else if (act is CloseSlide)
                        {
                            bg.Close();
                        }
                    }
                }

                sg1.Move(dX, dY);

                // 只有画面的几何信息发生变化才合成新的帧.
                if (sg1.GetHashCode() != sg0.GetHashCode())
                {
                    // 先只做image layer的合成
                    // 做好了再做annotation layer
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
                    sg0.Clone(sg1);
                }
                
                
                // 如果end-beg小于period, 则忙等到period
                for (long end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // 当前帧结束的时间
                          end - beg < period;
                          end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    Thread.Sleep(1); // 降低cpu占用率. 改善阅览体验.
                }
            }
        }

    }
}
