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
        private readonly int Idle = -1;
        private readonly int Browsing = 0;
        private readonly int Polyline = 1;

        private Worker blender;
        private MessageQueue aq;

        private int operation;
        private bool dragging;
        private bool deleting;

        private uint pid;
        private uint pdc;
        private double x0;
        private double x1;
        private double y0;
        private double y1;

        private Ellipse toggled;

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
            if (!dragging)
            {
                return;
            }
            var p = e.GetPosition(CanvasMain);
            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (operation == Browsing)
                {
                    DragSlide(p);
                }
                else if (operation == Polyline)
                {
                    if (toggled == null) // 右键拖动背景
                    {
                        DragSlide(p);
                    }
                    else // 右键拖动顶点
                    {
                        uint idv = (uint)(toggled.DataContext);
                        DragBullet(idv, p);
                    }
                }
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (operation == Browsing)
                {
                    DragSlide(p);
                }
            }
        }

        private void CanvasMain_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(CanvasMain);

            if (operation == Browsing)
            {
                StartDrag(p);
                PushCursor(Cursors.ScrollAll);
            }
            else if (operation == Polyline)
            {
                x1 = p.X;
                y1 = CanvasMain.ActualHeight - p.Y;
                object t = WhatUnderCursor(p);

                // 在空白处点击，则创建新节点
                if (t is Canvas)
                {
                    // 开启新的连续画点
                    if (pdc == 0)
                    {
                        pdc = pdc + 1;
                        var act = new VertexMessage();
                        (act.X, act.Y) = (x1, y1);
                        pid = pid + 1;
                        act.IdV = pid;
                        aq.Submit(act);
                    }
                    else if (pdc >= 1)
                    {
                        pdc = pdc + 1;
                        var act = new EdgeMessgae();
                        (act.X, act.Y) = (x1, y1);
                        pid = pid + 1;
                        act.IdV1 = pid - 1;
                        act.IdV2 = pid;
                        aq.Submit(act);
                    }
                }
                else if (t is Ellipse)
                {
                    var bullet = t as Ellipse;
                    if (deleting)
                    {
                        var act = new DeleteVertexMessage();
                        act.IdV = (uint)bullet.DataContext;
                        aq.Submit(act);
                    }
                }
                else if (t is Line)
                {

                }
            }
        }

        private void CanvasMain_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dragging)
            {
                StopDrag();
                PopCursor();
            }
        }

        private void CanvasMain_MouseLeave(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                StopDrag();
                toggled = null;
                PopCursor();
            }
        }

        private void CanvasMain_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(CanvasMain);

            if (operation == Browsing)
            {
                StartDrag(p);
                PushCursor(Cursors.ScrollAll);
            }
            else if (operation == Polyline)
            {
                object t = WhatUnderCursor(p);
                if (t is Canvas)
                {
                    StartDrag(p);
                    PushCursor(Cursors.ScrollAll);
                }
                else if (t is Ellipse)
                {
                    toggled = t as Ellipse;
                    StartDrag(p);
                    PushCursor(Cursors.Hand);
                }
            }
        }

        private void CanvasMain_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dragging)
            {
                StopDrag();
                toggled = null;
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
            (x1, y1) = (p.X, CanvasMain.ActualHeight - p.Y);

            int scroll = e.Delta;

            var act = new ZoomMessage();
            act.nScroll = scroll;
            (act.XScreen, act.YScreen) = (x1, y1);
            aq.Submit(act);
        }
        #endregion


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
            if (e.Key == Key.Escape)
            {
                Init();
            }
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                deleting = !deleting;
            }
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
            dragging = false;
        }

        private void DragSlide(Point p)
        {
            (x1, y1) = (p.X, CanvasMain.ActualHeight - p.Y);
            MoveSlide(x0 - x1, y0 - y1);
            (x0, y0) = (x1, y1);
        }

        private void StartDrag(Point p)
        {
            (x1, y1) = (p.X, CanvasMain.ActualHeight - p.Y);
            (x0, y0) = (x1, y1);
            dragging = true;
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
            (x1, y1) = (p.X, CanvasMain.ActualHeight - p.Y);
            MoveBullet(idv, x1 - x0, y1 - y0);
            (x0, y0) = (x1, y1);
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
            dragging = false;

            //pid = 0;
            pdc = 0;

            x0 = x1 = y0 = y1 = 0;
            cursorHistory.Clear();
            PopCursor();

            toggled = null;
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
