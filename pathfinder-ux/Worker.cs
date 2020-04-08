using PathFinder.Algorithm;
using PathFinder.Scene;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace PathFinder
{

    class Worker
    {
        #region 这些对象为UI线程所持有
        private Canvas canvasMain;
        private Canvas canvasThumb;
        private ImageBrush imageMain;
        private ImageBrush imageThumb;
        #endregion

        // UI和工作线程的通信队列
        private MessageQueue msgq;

        #region 这些成员对象为工作线程所持有
        private Thread th;
        private double thumbMaxW;
        private double thumbMaxH;
        private double thumbDensity;
        /// 每period毫秒合成1帧画面.
        /// 如果合成第i帧所消耗的时间超过了period, 则等到第i帧合成完毕才开始合成第i+1帧.
        /// 如果合成时间小于period, 则工作线程忙等到period才合成第i+1帧.
        private long period;

        private Slide slide;
        private PolylineAnnotation poly;
        #endregion

        public Worker(Canvas canvasMain, Canvas canvasThumb, 
            ImageBrush imageMain, ImageBrush imageThumb, 
            int fps, double thumbMaxW, double thumbMaxH)
        {
            this.canvasMain = canvasMain;
            this.canvasThumb = canvasThumb;
            this.imageMain = imageMain;
            this.imageThumb = imageThumb;
            period = (long)(1000.0 / fps);
            msgq = MessageQueue.GetInstance();
            slide = new Slide();
            poly = new PolylineAnnotation();
            th = new Thread(new ThreadStart(Work));
            (this.thumbMaxW, this.thumbMaxH) = (thumbMaxW, thumbMaxH);
        }

        public void Start()
        {
            th.Start();
        }

        public void Stop()
        {
            th.Abort();
        }

        private void Work()
        {
            Viewport v0 = new Viewport();
            Viewport v1 = new Viewport();
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
                    if (act is ViewportMessage)
                    {
                        if (act is MoveMessage)
                        {
                            var a = act as MoveMessage;
                            dX += a.dXScreen;
                            dY += a.dYScreen;
                        }
                        else if (act is ZoomMessage)
                        {
                            var a = act as ZoomMessage;
                            v1.Zoom(a.nScroll, a.XScreen, a.YScreen);
                        }
                        else if (act is ResizeMessage)
                        {
                            var a = act as ResizeMessage;
                            v1.Resize(a.WScreen, a.HScreen);
                        }
                        else if (act is ThumbJumpMessage)
                        {
                            var a = act as ThumbJumpMessage;
                            v1.X = a.CenterXScreen * thumbDensity - v1.ToActualPixel(v1.OutW) / 2;
                            v1.Y = a.CenterYScreen * thumbDensity - v1.ToActualPixel(v1.OutH) / 2;
                        }
                    }
                    else if (act is PolylineMessage)
                    {
                        drew = true;
                        if (act is BulletMessage)
                        {
                            var a = act as BulletMessage;
                            double x = v1.X + v1.ToActualPixel(a.X);
                            double y = v1.Y + v1.ToActualPixel(a.Y);
                            poly.Bullet(x, y, a.IdV);
                        }
                        else if (act is StickMessage)
                        {
                            var a = act as StickMessage;
                            double x = v1.X + v1.ToActualPixel(a.X);
                            double y = v1.Y + v1.ToActualPixel(a.Y);
                            poly.Stick(x, y, a.IdV1, a.IdV2);
                        }
                    }
                    else if (act is FileMessage)
                    {
                        if (act is LoadSlideMessage)
                        {
                            var a = act as LoadSlideMessage;
                            slide.Open(a.Path, v1);
                            v1.OutW = a.WCanvas;
                            v1.OutH = a.HCanvas;
                            canvasThumb.Dispatcher.Invoke(() =>
                            {
                                using (var ms = new System.IO.MemoryStream(slide.LoadThumbJPG()))
                                {
                                    var bi = new BitmapImage();
                                    bi.BeginInit();
                                    bi.CacheOption = BitmapCacheOption.OnLoad;
                                    bi.StreamSource = ms;
                                    bi.EndInit();
                                    UniformToFill(v1.SlideW, v1.SlideH, 
                                        out double thumbW, out double thumbH);
                                    (canvasThumb.Width, canvasThumb.Height) = (thumbW, thumbH);
                                    canvasThumb.UpdateLayout();
                                    imageThumb.ImageSource = bi;
                                    //Console.WriteLine($"actualWH=({canvasThumb.ActualWidth},{canvasThumb.ActualHeight})");
                                }
                            }, System.Windows.Threading.DispatcherPriority.Send);
                        }
                        else if (act is CloseSlideMessage)
                        {
                            //poly.Close();
                            slide.Close();
                        }
                    }
                }

                v1.Move(dX, dY);

                // 只有画面的几何信息发生变化才合成新的帧.
                if (v1.GetHashCode() != v0.GetHashCode() || drew)
                {
                    byte[] img = slide.LoadRegionBMP(v1);
                    List<DisplayParameter> items = poly.LoadRegionShapes(v1);
                    canvasMain.Dispatcher.Invoke(() =>
                    {
                        #region 重新绘制矢量物体
                        canvasMain.Children.Clear();
                        foreach (var item in items)
                        {
                            if (item is BulletParameter)
                            {
                                var bp = item as BulletParameter;
                                DrawBullet(bp, v1);
                            }
                            else if (item is StickParameter)
                            {
                                var sp = item as StickParameter;
                                DrawStick(sp, v1);
                            }
                        }
                        canvasMain.UpdateLayout();
                        #endregion

                        #region 重新绘制位图
                        BitmapImage bi = null;
                        using (var ms = new System.IO.MemoryStream(img))
                        {
                            bi = new BitmapImage();
                            bi.BeginInit();
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.StreamSource = ms;
                            bi.EndInit();
                        }
                        imageMain.ImageSource = bi;
                        #endregion
                    }, System.Windows.Threading.DispatcherPriority.Send);
                    canvasThumb.Dispatcher.Invoke(() =>
                    {
                        canvasThumb.Children.Clear();
                        DrawOnThumb(canvasThumb, v1);
                    });
                    v0.CopyFrom(v1);
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

        /// <summary>
        /// 计算缩略图的输出尺寸，模拟WPF的UniformToFill
        /// </summary>
        /// <param name="iw"></param>
        /// <param name="ih"></param>
        /// <param name="ow"></param>
        /// <param name="oh"></param>
        private void UniformToFill(double iw, double ih, 
            out double ow, out double oh)
        {
            thumbDensity = 1;
            // 如果缩略图比缩略窗口更宽
            if (thumbMaxW * ih < thumbMaxH * iw)
            {
                ow = thumbMaxW;
                thumbDensity = iw / ow;
                oh = ih / thumbDensity;
            }
            // 如果缩略图比缩略窗口更长
            else if (thumbMaxW * ih > thumbMaxH * iw)
            {
                oh = thumbMaxH;
                thumbDensity = ih / oh;
                ow = oh / thumbDensity;
            }
            // 如果缩略图和缩略窗口比例相同
            else
            {
                (ow, oh) = (thumbMaxW, thumbMaxH);
                thumbDensity =ih / oh;
            }

        }

        private void DrawOnThumb(Canvas c, Viewport sg)
        {
            // 计算缩略图框的边界
            double lft = sg.X / thumbDensity;
            double top = sg.Y / thumbDensity;
            double rgt = (sg.X + sg.ToActualPixel(sg.OutW)) / thumbDensity;
            double btm = (sg.Y + sg.ToActualPixel(sg.OutH)) / thumbDensity;

            //Console.WriteLine($"{lft:0.0}, {top:0.0}, {rgt:0.0}, {btm:0.0}");

            double diameter = 12;
            // 绘制缩略图框
            if (rgt - lft >= diameter && btm - top >= diameter)
            {
                Rectangle rect = new Rectangle();
                rect.SetValue(Canvas.LeftProperty, lft);
                rect.SetValue(Canvas.TopProperty, top);
                rect.Width = rgt - lft;
                rect.Height = btm - top;
                rect.Fill = new SolidColorBrush(Color.FromArgb(127, 127, 127, 127));
                rect.Stroke = new SolidColorBrush(Color.FromRgb(217, 83, 79));
                rect.StrokeThickness = 2;
                c.Children.Add(rect);
            }
            else
            {
                Ellipse bullet = new Ellipse();
                bullet.Height = diameter;
                bullet.Width = diameter;
                bullet.Fill = new SolidColorBrush(Color.FromArgb(127, 127, 127, 127));
                bullet.Stroke = new SolidColorBrush(Color.FromRgb(217, 83, 79));
                bullet.StrokeThickness = 2;
                bullet.SetValue(Canvas.LeftProperty, (lft + rgt - diameter) / 2);
                bullet.SetValue(Canvas.TopProperty, (top + btm - diameter) / 2);
                c.Children.Add(bullet);
            }
        }

        private void DrawBullet(BulletParameter bp, Viewport v)
        {
            double x = v.ToDisplayPixel(bp.x - v.X);
            double y = v.OutH - v.ToDisplayPixel(bp.y - v.Y);

            Ellipse bullet = new Ellipse();
            bullet.SetValue(Canvas.LeftProperty, x - 8);
            bullet.SetValue(Canvas.TopProperty, y - 8);
            bullet.SetValue(Canvas.ZIndexProperty, 1);
            bullet.Height = 16;
            bullet.Width = 16;
            bullet.Fill = new SolidColorBrush(Color.FromRgb(246, 246, 233));
            bullet.Stroke = new SolidColorBrush(Color.FromRgb(217, 83, 79));
            bullet.StrokeThickness = 4;

            bullet.DataContext = bp.id;
            canvasMain.Children.Add(bullet);
        }

        private void DrawStick(StickParameter sp, Viewport v)
        {
            //Console.WriteLine($"StickParameter {sp.x1} {sp.y1} {sp.x2} {sp.y2}");
            double x1 = v.ToDisplayPixel(sp.x1 - v.X);
            double y1 = v.OutH - v.ToDisplayPixel(sp.y1 - v.Y);
            double x2 = v.ToDisplayPixel(sp.x2 - v.X);
            double y2 = v.OutH - v.ToDisplayPixel(sp.y2 - v.Y);

            //Console.WriteLine($"DrawStick #{sp.id} at ({x1:.0},{y1:.0})-({x2:.0},{y2:.0})");

            Line l = new Line();
            (l.X1, l.Y1, l.X2, l.Y2) = (x1, y1, x2, y2);
            l.SetValue(Canvas.ZIndexProperty, 0);
            l.StrokeThickness = 4;
            l.Stroke = new SolidColorBrush(Color.FromRgb(51, 51, 51));

            l.DataContext = sp.id;
            canvasMain.Children.Add(l);
        }
    }
}
