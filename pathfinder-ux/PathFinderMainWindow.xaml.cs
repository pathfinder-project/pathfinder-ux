using System;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;

namespace PathFinder
{

    public partial class PathFinderMainWindow : Window
    {
        private int operation;

        private readonly int Idle = -1;
        private readonly int Browsing = 0;
        private readonly int Polyline = 2;

        private Worker blender;
        private MessageQueue aq;

        #region 用于拖动画布的状态
        private bool dragging_slide;

        private double x0s;
        private double x1s;
        private double y0s;
        private double y1s;
        #endregion

        #region 用于拖动节点
        private int? idv_drag;
        private double x1v;
        private double y1v;
        private double x0v;
        private double y0v;
        #endregion

        #region 用于创建节点
        private int? idv_prev;
        private int idv_pool = 1;
        #endregion


        private Stack<Cursor> cursorHistory;

        public PathFinderMainWindow()
        {
            this.DataContext = this;
            aq = MessageQueue.GetInstance();
            InitializeComponent();
            double thumbMaxWidth = (double)this.Resources["thumbMaxWidth"];
            double thumbMaxHeight = (double)this.Resources["thumbMaxHeight"];
            //Console.WriteLine($"{thumbMaxWidth}, {thumbMaxHeight}");
            blender = new Worker(CanvasMain, CanvasThumb, Ki67ScoreBoard, CanvasMainImage, CanvasThumbImage, 
                15, thumbMaxWidth, thumbMaxHeight);
            CanvasBackground.Visibility = Visibility.Hidden;
            operation = Idle;
            
            cursorHistory = new Stack<Cursor>();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            blender.Stop();
        }

        private void MenuMain_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // 按住菜单栏拖动。方案来自 https://stackoverflow.com/a/7418629

            if (e.ChangedButton != MouseButton.Left)
                return;
            this.DragMove();
        }

        private void MenuMain_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (WindowState != WindowState.Maximized)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }

        private void MenuFileOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                var act = new LoadSlideMessage();
                act.Path = dlg.FileName;
                (act.WCanvas, act.HCanvas) = (CanvasMain.ActualWidth, CanvasMain.ActualHeight);
                aq.Submit(act);
                blender.Start();
                Init();
                CanvasBackground.Visibility = Visibility.Visible;
            }
        }

        private void MenuFileClose_Click(object sender, RoutedEventArgs e)
        {
            Init();
            operation = Idle;
            blender.Stop();
        }

        private void CanvasMain_MouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(CanvasMain);

            if (dragging_slide)
            {
                DragSlide(p);
            }
            else if (idv_drag != null)
            {
                DragVertex(idv_drag.Value, p);
            }
        }

        /// <summary>
        /// 按下鼠标左键。包含拖动切片、创建节点、连接节点操作。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanvasMain_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(CanvasMain);
            x1s = p.X;
            y1s = CanvasMain.ActualHeight - p.Y;
            object t = WhatUnderCursor(p);

            if (operation == Browsing)
            {
                if (e.ClickCount == 1)
                {
                    StartDragSlide(p);
                    PushCursor(Cursors.ScrollAll);
                }
                else if (e.ClickCount == 2)
                {
                    if (t is Ellipse)
                    {
                        var bullet = t as Ellipse;
                        var ctx = (V)bullet.DataContext;
                        var act = new Ki67Message();
                        act.idv = ctx.idv;
                        aq.Submit(act);
                    }
                }
            }
            else if (operation == Polyline)
            {
                // 在空白处点击，则创建新点
                // 首次点击空白处——创建新点
                // 第n次连续点击空白处——创建新点，并与上次创建的点相连
                if (t is Canvas)
                {
                    var act = new AddVertexMessage();
                    (act.x, act.y) = (x1s, y1s);
                    act.idv = idv_pool;
                    act.prev = idv_prev;
                    aq.Submit(act);
                    idv_prev = idv_pool;
                    idv_pool += 1;
                }
                
                // 在节点上点击，则尝试从节点开始绘制折线
                else if (t is Ellipse)
                {
                    var bullet = t as Ellipse;
                    var v = (V)bullet.DataContext;

                    // 点击的节点<=1度
                    if (v.prev == null || v.next == null)
                    {
                        // 首次点击
                        if (idv_prev == null)
                        {
                            idv_prev = v.idv;
                        }
                        // 再次点击
                        else
                        {
                            var act = new ConnectVertexMessage();
                            act.idv1 = idv_prev.Value;
                            act.idv2 = v.idv;
                            aq.Submit(act);
                            // 如果相连后当前点会满（即相连之前恰好为1度）
                            if ((v.prev == null) ^ (v.next == null))
                            {
                                idv_prev = null;
                            }
                            else
                            {
                                idv_prev = v.idv;
                            }
                        }
                    }

                    // 若点击的节点=2度，则放弃idv_prev
                    else
                    {
                        idv_prev = null;
                    }
                }
            }
        }

        private void CanvasMain_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dragging_slide)
            {
                dragging_slide = false;
                PopCursor();
            }
        }

        private void CanvasMain_MouseLeave(object sender, MouseEventArgs e)
        {
            if (dragging_slide)
            {
                dragging_slide = false;
                PopCursor();
            }
        }

        /// <summary>
        /// 右键按下，可以处理拖动切片、拖动节点、删除节点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanvasMain_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(CanvasMain);

            if (operation == Browsing)
            {
                StartDragSlide(p);
                PushCursor(Cursors.ScrollAll);
            }
            else if (operation == Polyline)
            {
                object t = WhatUnderCursor(p);

                if (t is Canvas)
                {
                    StartDragSlide(p);
                    PushCursor(Cursors.ScrollAll);
                }
                else if (t is Ellipse)
                {
                    var bullet = t as Ellipse;
                    var v = (V)bullet.DataContext;

                    if (e.ClickCount == 1)
                    {
                        idv_drag = v.idv;
                        x1v = p.X;
                        y1v = CanvasMain.ActualHeight - p.Y;
                        x0v = x1v;
                        y0v = y1v; 
                        PushCursor(Cursors.Hand);
                    }
                    else if (e.ClickCount == 2)
                    {
                        var a = new DeleteVertexMessage();
                        a.idv = v.idv;
                        aq.Submit(a);
                        idv_prev = null;
                    }
                }
                
            }
        }

        private void CanvasMain_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dragging_slide)
            {
                dragging_slide = false;
                PopCursor();
            }
            if (idv_drag != null)
            {
                idv_drag = null;
                PopCursor();
            }
        }

        private void CanvasMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (operation != Idle)
            {
                double w = CanvasMain.ActualWidth;
                double h = CanvasMain.ActualHeight;
                var act = new ResizeMessage();
                (act.WScreen, act.HScreen) = (w, h);
                aq.Submit(act);
            }
        }

        private void CanvasMain_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // GetPosition得到的坐标是以「左下角」为原点的.
            // 需要换算为以左上角为原点.
            var p = e.GetPosition(CanvasMain);
            (x1s, y1s) = (p.X, CanvasMain.ActualHeight - p.Y);

            int scroll = e.Delta;

            var act = new ZoomMessage();
            act.nScroll = scroll;
            (act.XScreen, act.YScreen) = (x1s, y1s);
            aq.Submit(act);
        }

        private void Bullet_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (operation == Browsing && e.ClickCount == 2)
            {
                var bullet = sender as Ellipse;
                
            }
        }


        #region 缩略图画布
        private void CanvasThumb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (operation == Browsing)
            {
                var p = e.GetPosition(CanvasThumb);
                var act = new ThumbJumpMessage();
                (act.CenterXScreen, act.CenterYScreen) = (p.X, p.Y);
                aq.Submit(act);
            }
        }
        #endregion


        #region 工具栏按钮
        private void ButtonToolbarBrowsing_Click(object sender, RoutedEventArgs e)
        {
            Init();
        }

        private void ButtonToolbarPolyline_Click(object sender, RoutedEventArgs e)
        {
            operation = Polyline;
            PushCursor(Cursors.Pen);
        }
        #endregion


        #region 快捷键
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            #region 松开ESC键
            if (e.Key == Key.Escape)
            {
                Init();
            }
            #endregion
        }
        #endregion

        #region 基础操作
        private void MoveSlide(double dx, double dy)
        {
            var act = new MoveMessage();
            act.dXScreen = dx;
            act.dYScreen = dy;
            aq.Submit(act);
        }

        private void StopDrag()
        {
            dragging_slide = false;
        }

        private void DragSlide(Point p)
        {
            (x1s, y1s) = (p.X, CanvasMain.ActualHeight - p.Y);
            MoveSlide(x0s - x1s, y0s - y1s);
            (x0s, y0s) = (x1s, y1s);
        }

        private void StartDragSlide(Point p)
        {
            x1s = p.X;
            y1s = CanvasMain.ActualHeight - p.Y;
            x0s = x1s;
            y0s = y1s;
            dragging_slide = true;
        }

        private void MoveBullet(int idv, double dx, double dy)
        {
            var act = new MoveVertexMessgae();
            act.dx = dx;
            act.dy = dy;
            act.idv = idv;
            aq.Submit(act);
        }

        private void DragVertex(int idv, Point p)
        {
            (x1v, y1v) = (p.X, CanvasMain.ActualHeight - p.Y);
            MoveBullet(idv, x1v - x0v, y1v - y0v);
            (x0v, y0v) = (x1v, y1v);
        }
        #endregion

        #region 基础功能性操作
        private object WhatUnderCursor(Point p)
        {
            // Inspired by https://stackoverflow.com/a/7011895
            HitTestResult target = VisualTreeHelper.HitTest(CanvasMain, p);
            var t = target.VisualHit;
            return t;
        }

        private void Init()
        {
            operation = Browsing;
            dragging_slide = false;

            x0s = x1s = y0s = y1s = 0;
            x0v = x1v = y0v = y1v = 0;
            cursorHistory.Clear();
            PopCursor();

            idv_prev = null;
            idv_drag = null;
        }

        private void PushCursor(Cursor c)
        {
            if (cursorHistory.Count == 0 || cursorHistory.Peek() != c)
            {
                cursorHistory.Push(c);
                CanvasMain.Cursor = c;
            }
        }

        private void PopCursor()
        {
            if (cursorHistory.Count > 0)
            {
                cursorHistory.Pop();
            }
            if (cursorHistory.Count > 0)
            {
                CanvasMain.Cursor = cursorHistory.Peek();
            }
            else
            {
                CanvasMain.Cursor = Cursors.Arrow;
            }
            
        }
        #endregion
    }
}
