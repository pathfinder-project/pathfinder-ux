using PathFinder.Algorithm;
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
    using System.Windows.Shapes;
    using LineSegment = Layer.LineSegment;

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
        private PolylineLayer fg;
        private Thread worker;

        public SceneBlender(Canvas canvas, ImageBrush canvasBg, int fps)
        {
            this.canvas = canvas;
            this.canvasBg = canvasBg;
            period = (long)(1000.0 / fps);
            msgq = ActionQueue.Singleton();
            bg = new SlideLayer();
            fg = new PolylineLayer();
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
            bool drew = false;
            while (true)
            {
                long beg = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // 当前帧开始的时间
                double dX = 0, dY = 0;

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
                        drew = true;
                        if (act is DrawPolygonV)
                        {
                            var a = act as DrawPolygonV;
                            if (a.dps == DrawPolygonState.PlacingVertex)
                            {
                                double x = sg1.X + sg1.ToActualPixel(a.X);
                                double y = sg1.Y + sg1.ToActualPixel(a.Y);
                                fg.AddPoint(a.Id, x, y);
                            }
                            else if (a.dps == DrawPolygonState.TryFinish)
                            {
                                double x = sg1.X + sg1.ToActualPixel(a.X);
                                double y = sg1.Y + sg1.ToActualPixel(a.Y);
                                fg.AddPoint(a.Id, x, y);
                                fg.MakePolygon(a.Id);
                            }
                            else if (a.dps == DrawPolygonState.ForceCancel)
                            {
                                fg.RemovePolygon(a.Id);
                            }
                        }
                    }
                    else if (act is FileAction)
                    {
                        if (act is LoadSlide)
                        {
                            var a = act as LoadSlide;
                            bg.Open(a.Path, sg1);
                            sg1.OutW = a.W;
                            sg1.OutH = a.H;
                        }
                        else if (act is CloseSlide)
                        {
                            fg.Close();
                            bg.Close();
                        }
                    }
                }

                sg1.Move(dX, dY);

                // 只有画面的几何信息发生变化才合成新的帧.
                if (sg1.GetHashCode() != sg0.GetHashCode() || drew)
                    //if (true)
                {
                    // 先只做image layer的合成
                    // 做好了再做annotation layer
                    byte[] img = bg.LoadRegion(sg1) as byte[];
                    List<Drawable> items = fg.LoadRegion(sg1) as List<Drawable>;
                    Console.WriteLine($"Need to draw {items.Count} items");
                    canvasBg.Dispatcher.Invoke(() => 
                    {
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
                    }, System.Windows.Threading.DispatcherPriority.Send);
                    canvas.Dispatcher.Invoke(() =>
                    {
                        canvas.Children.Clear();
                        foreach (var item in items)
                        {
                            if (item is Point)
                            {
                                var p = item as Point;
                                DrawBullet(p.x, p.y, canvas, sg1);
                            }
                            else if (item is LineSegment)
                            {
                                var ls = item as LineSegment;
                                DrawLineSegment(ls.xa, ls.ya, ls.xb, ls.yb, canvas, sg1);
                            }
                        }
                        canvas.UpdateLayout();
                    }, System.Windows.Threading.DispatcherPriority.Send);
                    sg0.CopyFrom(sg1);
                    drew = false;
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

        private void DrawBullet(double x, double y, Canvas c, SceneGeometry sg)
        {
            Ellipse bullet = new Ellipse();
            bullet.Height = 16;
            bullet.Width = 16;
            bullet.Fill = new SolidColorBrush(Color.FromRgb(246, 246, 233));
            bullet.Stroke = new SolidColorBrush(Color.FromRgb(217, 83, 79));
            bullet.StrokeThickness = 4;
            double imx = sg.ToDisplayPixel(x);
            double imy = sg.ToDisplayPixel(y);
            double imy_fixed = sg.OutH - imy;
            bullet.SetValue(Canvas.LeftProperty, imx - 8);
            bullet.SetValue(Canvas.TopProperty, imy_fixed - 8);
            bullet.SetValue(Canvas.ZIndexProperty, 1);
            c.Children.Add(bullet);
        }

        private void DrawLineSegment(double x1, double y1, double x2, double y2, Canvas c, SceneGeometry sg)
        {
            Line l = new Line();
            DrawBullet(x1, y1, c, sg);
            DrawBullet(x2, y2, c, sg);
            x1 = sg.ToDisplayPixel(x1);
            y1 = sg.ToDisplayPixel(y1);
            x2 = sg.ToDisplayPixel(x2);
            y2 = sg.ToDisplayPixel(y2);
            double y1_fixed = sg.OutH - y1;
            double y2_fixed = sg.OutH - y2;
            (l.X1, l.Y1, l.X2, l.Y2) = (x1, y1_fixed, x2, y2_fixed);
            l.SetValue(Canvas.ZIndexProperty, 0);
            l.StrokeThickness = 4;
            l.Stroke = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            canvas.Children.Add(l);
        }
    }
}
