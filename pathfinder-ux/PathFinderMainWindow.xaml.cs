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
    using id_ida_idb = ValueTuple<uint, uint, uint>;

    public partial class PathFinderMainWindow : Window
    {
        private readonly int Idle = -1;
        private readonly int Browsing = 0;
        private readonly int Polyline = 2;

        private Worker blender;
        private MessageQueue aq;

        private int operation;

        private uint pid;
        private uint pdc;

        #region 用于拖动画布的状态
        private bool dragging_slide;

        private double x0s;
        private double x1s;
        private double y0s;
        private double y1s;
        #endregion

        #region 用于拖动节点
        private bool dragging_vertex;

        private double x1v;
        private double y1v;
        private double x0v;
        private double y0v;
        #endregion

        private uint idv_snake_prev;
        private uint idv_drag;

        private Stack<Cursor> cursorHistory;


        public PathFinderMainWindow()
        {
            this.DataContext = this;
            aq = MessageQueue.GetInstance();
            InitializeComponent();
            double thumbMaxWidth = (double)this.Resources["thumbMaxWidth"];
            double thumbMaxHeight = (double)this.Resources["thumbMaxHeight"];
            //Console.WriteLine($"{thumbMaxWidth}, {thumbMaxHeight}");
            blender = new Worker(CanvasMain, CanvasThumb, CanvasMainImage, CanvasThumbImage,
                15, thumbMaxWidth, thumbMaxHeight);
            CanvasBackground.Visibility = Visibility.Hidden;
            pid = 0;
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


        #region 主画布
        private void CanvasMain_MouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(CanvasMain);

            if (dragging_slide)
            {
                DragSlide(p);
            }
            else if (dragging_vertex)
            {
                DragBullet(idv_drag, p);
            }
        }

        private void CanvasMain_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(CanvasMain);
            x1s = p.X;
            y1s = CanvasMain.ActualHeight - p.Y;
            object t = WhatUnderCursor(p);

            if (operation == Browsing)
            {
                StartDragSlide(p);
                PushCursor(Cursors.ScrollAll);
            }
            else if (operation == Polyline)
            {
                #region 在空白处点击，则创建新折线
                if (t is Canvas)
                {
                    #region 首次在空白处点击，创建新点
                    if (idv_snake_prev == 0)
                    {
                        pdc = pdc + 1;
                        var act = new VertexMessage();
                        (act.X, act.Y) = (x1s, y1s);
                        pid = pid + 1;
                        act.IdV = idv_snake_prev = pid;
                        aq.Submit(act);
                    }
                    #endregion

                    #region 连续第>=2次在空白处点击，创建新点并与上一个点连成新边
                    else
                    {
                        pdc = pdc + 1;
                        var act = new EdgeMessgae();
                        (act.X, act.Y) = (x1s, y1s);
                        pid = pid + 1;
                        act.IdV1 = idv_snake_prev;
                        act.IdV2 = idv_snake_prev = pid;
                        aq.Submit(act);
                    }
                    #endregion
                }
                #endregion

                #region 在节点上点击，则从节点接续折线
                else if (t is Ellipse)
                {
                    #region 在节点上点击，检查节点是否已有2条边
                    var bullet = t as Ellipse;
                    var ctx = (id_ida_idb)bullet.DataContext;
                    uint idv = ctx.Item1, ida = ctx.Item2, idb = ctx.Item3;
                    if (ida == 0 || idb == 0)
                    {
                        if (idv_snake_prev == 0)
                        {
                            idv_snake_prev = idv;
                        }
                        else
                        {
                            #region 如果当前点已满
                            if (ida != 0 && idb != 0)
                            {
                                idv_snake_prev = 0;
                            }
                            #endregion
                            else 
                            {
                                var act = new EdgeMessgae();
                                (act.X, act.Y) = (x1s, y1s);
                                act.IdV1 = idv_snake_prev;
                                act.IdV2 = idv;
                                aq.Submit(act);
                                #region 如果相连后当前点会满
                                if ((ida == 0) ^ (idb == 0))
                                {
                                    idv_snake_prev = 0;
                                }
                                #endregion
                            }
                            #endregion
                        }
                    }
                    #endregion
                }
                #endregion
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
                    if (e.ClickCount == 1)
                    {
                        idv_drag = ((id_ida_idb)bullet.DataContext).Item1;
                        StartDragVertex(p);
                        PushCursor(Cursors.Hand);
                    }
                    else if (e.ClickCount == 2)
                    {
                        var a = new DeleteVertexMessage();
                        var vab = (id_ida_idb)bullet.DataContext;
                        a.IdV = vab.Item1;
                        aq.Submit(a);
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
            else if (dragging_vertex)
            {
                dragging_vertex = false;
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

        private void StartDragVertex(Point p)
        {
            x1v = p.X;
            y1v = CanvasMain.ActualHeight - p.Y;
            x0v = x1v;
            y0v = y1v;
            dragging_vertex = true;
        }

        private void MoveBullet(uint idv, double dx, double dy)
        {
            var act = new MoveVertexMessgae();
            act.dXScreen = dx;
            act.dYScreen = dy;
            act.IdV = idv;
            aq.Submit(act);
        }

        private void DragBullet(uint idv, Point p)
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
            dragging_vertex = false;

            x0s = x1s = y0s = y1s = 0;
            x0v = x1v = y0v = y1v = 0;
            cursorHistory.Clear();
            PopCursor();

            idv_snake_prev = 0;
            idv_drag = 0;
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
