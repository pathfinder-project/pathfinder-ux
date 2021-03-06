﻿using PathFinder.Scene;
using System;
using System.Collections.Generic;
using System.IO;
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
        private TextBlock scoreBoard;
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

        #region 量化相关
        private Dictionary<int, Ki67Task> quantTasks;
        #endregion

        public Worker(Canvas canvasMain, Canvas canvasThumb, TextBlock scoreBoard,
            ImageBrush imageMain, ImageBrush imageThumb, 
            int fps, double thumbMaxW, double thumbMaxH)
        {
            this.canvasMain = canvasMain;
            this.canvasThumb = canvasThumb;
            this.imageMain = imageMain;
            this.imageThumb = imageThumb;
            this.scoreBoard = scoreBoard;
            period = (long)(1000.0 / fps);
            msgq = MessageQueue.GetInstance();
            slide = new Slide();
            poly = new PolylineAnnotation();
            (this.thumbMaxW, this.thumbMaxH) = (thumbMaxW, thumbMaxH);
            quantTasks = new Dictionary<int, Ki67Task>();
            th = new Thread(new ThreadStart(Work));
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
            Viewport viewport_prev = new Viewport();
            Viewport viewport_curr = new Viewport();
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
                            viewport_curr.Zoom(a.nScroll, a.XScreen, a.YScreen);
                        }
                        else if (act is ResizeMessage)
                        {
                            var a = act as ResizeMessage;
                            viewport_curr.Resize(a.WScreen, a.HScreen);
                        }
                        else if (act is ThumbJumpMessage)
                        {
                            var a = act as ThumbJumpMessage;
                            viewport_curr.X = a.CenterXScreen * thumbDensity - viewport_curr.ToActualPixel(viewport_curr.OutW) / 2;
                            viewport_curr.Y = a.CenterYScreen * thumbDensity - viewport_curr.ToActualPixel(viewport_curr.OutH) / 2;
                        }
                    }
                    else if (act is PolylineMessage)
                    {
                        drew = true;
                        if (act is AddVertexMessage)
                        {
                            var a = act as AddVertexMessage;
                            double x = viewport_curr.X + viewport_curr.ToActualPixel(a.x);
                            double y = viewport_curr.Y + viewport_curr.ToActualPixel(a.y);
                            poly.AddVertex(a.idv, x, y, a.prev);
                        }
                        else if (act is ConnectVertexMessage)
                        {
                            var a = act as ConnectVertexMessage;
                            poly.ConnectVertex(a.idv1, a.idv2);
                        }
                        else if (act is MoveVertexMessgae)
                        {
                            var a = act as MoveVertexMessgae;
                            double dx = viewport_curr.ToActualPixel(a.dx);
                            double dy = viewport_curr.ToActualPixel(a.dy);
                            poly.MoveVertex(a.idv, dx, dy);
                        }
                        else if (act is DeleteVertexMessage)
                        {
                            var a = act as DeleteVertexMessage;
                            poly.DeleteVertex(a.idv);
                        }
                    }
                    else if (act is ComputeMessage)
                    {
                        if (act is Ki67Message)
                        {
                            var a = act as Ki67Message;
                            if (!quantTasks.ContainsKey(a.idv))
                            {
                                poly.QueryChain(a.idv, out List<double> x, out List<double> y, out BoundingBox bb);
                                byte[] data = slide.LoadRegionBMP(bb);

                                //using (var fs = new FileStream(@"C:\Users\winston\Pictures\aaa.bmp", FileMode.Create))
                                //{
                                //    fs.Write(data, 0, data.Length);
                                //}

                                var task = new Ki67Task(a.idv, data, x, y);

                                quantTasks.Add(a.idv, task);
                                task.Start();
                            }
                        }
                    }
                    else if (act is FileMessage)
                    {
                        if (act is LoadSlideMessage)
                        {
                            var a = act as LoadSlideMessage;
                            slide.Open(a.Path, viewport_curr);
                            viewport_curr.OutW = a.WCanvas;
                            viewport_curr.OutH = a.HCanvas;
                            canvasThumb.Dispatcher.Invoke(() =>
                            {
                                using (var ms = new System.IO.MemoryStream(slide.LoadThumbJPG()))
                                {
                                    var bi = new BitmapImage();
                                    bi.BeginInit();
                                    bi.CacheOption = BitmapCacheOption.OnLoad;
                                    bi.StreamSource = ms;
                                    bi.EndInit();
                                    UniformToFill(viewport_curr.SlideW, viewport_curr.SlideH, 
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

                viewport_curr.Move(dX, dY);

                #region 摆放前端对象
                byte[] img = slide.LoadRegionBMP(viewport_curr);
                List<Object> items = poly.LoadRegionShapes(viewport_curr);
                canvasMain.Dispatcher.Invoke(() =>
                {
                    #region 重新绘制矢量物体
                    canvasMain.Children.Clear();
                    foreach (var item in items)
                    {
                        if (item is V)
                        {
                            var v = item as V;
                            DrawVertex(v, viewport_curr);
                        }
                        else if (item is E)
                        {
                            var e = item as E;
                            DrawEdge(e, viewport_curr);
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
                    DrawOnThumb(canvasThumb, viewport_curr);
                }, System.Windows.Threading.DispatcherPriority.Send);
                viewport_prev.CopyFrom(viewport_curr);

                var scores = new Dictionary<int, double>();
                var to_remove = new List<int>();
                foreach (var _ in quantTasks)
                {
                    var t = _.Value;
                    if (!double.IsNaN(t.Result))
                    {
                        scores.Add(t.HeadId, t.Result);
                        to_remove.Add(t.HeadId);
                    }
                }

                foreach (var _ in to_remove)
                {
                    quantTasks.Remove(_);
                }

                scoreBoard.Dispatcher.Invoke(() =>
                {
                    foreach (var _ in scores)
                    {
                        scoreBoard.Text = $"PolygonId: {_.Key}. Ki67={(int)(_.Value * 100)}%\r\n";
                    }
                }, System.Windows.Threading.DispatcherPriority.Send);
                
                // 如果end-beg小于period, 则忙等到period
                for (long end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // 当前帧结束的时间
                          end - beg < period;
                          end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    Thread.Sleep(1); // 降低cpu占用率. 改善阅览体验.
                }
                #endregion
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

        private void DrawVertex(V v, Viewport vp)
        {
            double x = vp.ToDisplayPixel(v.x - vp.X);
            double y = vp.OutH - vp.ToDisplayPixel(v.y - vp.Y);
            double r = 12, d = r * 2;
            Ellipse bullet = new Ellipse();
            bullet.SetValue(Canvas.LeftProperty, x - r);
            bullet.SetValue(Canvas.TopProperty, y - r);
            bullet.SetValue(Canvas.ZIndexProperty, 1);
            bullet.Height = d;
            bullet.Width = d;
            bullet.Fill = new SolidColorBrush(Color.FromRgb(246, 246, 233));
            bullet.Stroke = new SolidColorBrush(Color.FromRgb(217, 83, 79));
            bullet.StrokeThickness = 3;

            
            //Label label = new Label();
            //string hint = null;
            //if (v.idv == v.head && v.idv != v.tail)
            //{
            //    hint = $"头【{v.idv}】\n前={v.prev.Info()}，后={v.next.Info()}\n尾={v.tail}";
            //}
            //else if (v.idv == v.tail && v.idv != v.head)
            //{
            //    hint = $"尾【{v.idv}】\n前={v.prev.Info()}，后={v.next.Info()}\n头={v.head}";
            //}
            //else if (v.idv == v.head && v.idv == v.tail)
            //{
            //    hint = $"孤立【{v.idv}】\n前={v.prev.Info()}，后={v.next.Info()}\n头={v.head}，尾={v.tail}";
            //}
            //else
            //{
            //    hint = $"中继【{v.idv}】\n前={v.prev.Info()}，后={v.next.Info()}\n头={v.head}，尾={v.tail}";
            //}
            //label.Content = hint;
            //label.SetValue(Canvas.LeftProperty, x);
            //label.SetValue(Canvas.TopProperty, y);
            //label.SetValue(Canvas.ZIndexProperty, 1);
            //var rotate = new RotateTransform(180);
            //var flip = new ScaleTransform(-1, 1);
            //var tf = new TransformGroup();
            //tf.Children.Add(rotate);
            //tf.Children.Add(flip);
            //label.RenderTransform = tf;
            //canvasMain.Children.Add(label);

            bullet.DataContext = v;
            canvasMain.Children.Add(bullet);
        }

        private void DrawEdge(E e, Viewport vp)
        {
            //Console.WriteLine($"StickParameter {sp.x1} {sp.y1} {sp.x2} {sp.y2}");
            double x1 = vp.ToDisplayPixel(e.xa - vp.X);
            double y1 = vp.OutH - vp.ToDisplayPixel(e.ya - vp.Y);
            double x2 = vp.ToDisplayPixel(e.xb - vp.X);
            double y2 = vp.OutH - vp.ToDisplayPixel(e.yb - vp.Y);

            //Console.WriteLine($"DrawStick #{sp.id} at ({x1:.0},{y1:.0})-({x2:.0},{y2:.0})");

            Line l = new Line();
            (l.X1, l.Y1, l.X2, l.Y2) = (x1, y1, x2, y2);
            l.SetValue(Canvas.ZIndexProperty, 0);
            l.StrokeThickness = 3;
            l.Stroke = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            
            canvasMain.Children.Add(l);
        }
    }
}

