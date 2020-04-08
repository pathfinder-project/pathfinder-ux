using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PathFinder
{

    public partial class PathFinderMainWindow : Window
    {
        private readonly int Idle = -1;
        private readonly int Browsing = 0;
        private readonly int Polyline = 1;

        int operation;
        bool dragging;

        uint pid;
        uint pdc;
        double x0;
        double x1;
        double y0;
        double y1;

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
                    /**
                     * TODO
                     * 如果无锚点，则拖动视野
                     * 如果有锚点，则拖动锚点
                     * 如果有锚定边，则拖动锚定边
                     */
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


        /// <summary>
        /// 定义鼠标左键点击画布、顶点的行为
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanvasMain_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(CanvasMain);

            if (operation == Browsing)
            {
                StartDrag(p);
                dragging = true;
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
                        var act = new BulletMessage();
                        (act.X, act.Y) = (x1, y1);
                        pid = pid + 1;
                        act.IdV = pid;
                        aq.Submit(act);
                    }
                    else if (pdc >= 1)
                    {
                        pdc = pdc + 1;
                        var act = new StickMessage();
                        (act.X, act.Y) = (x1, y1);
                        pid = pid + 1;
                        act.IdV1 = pid - 1;
                        act.IdV2 = pid;
                        aq.Submit(act);
                    }
                }
                else if (t is Ellipse)
                {

                }
                else if (t is Line)
                {

                }
            }
        }

        private void CanvasMain_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragging = false;
        }

        private void CanvasMain_MouseLeave(object sender, MouseEventArgs e)
        {

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
            operation = Browsing;
        }

        private void ButtonToolbarPolyline_Click(object sender, RoutedEventArgs e)
        {
            operation = Polyline;
        }
        #endregion


        #region 快捷键
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Init();
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

            pid = 0;
            pdc = 0;

            x0 = 0;
            x1 = 0;

            y0 = 0;
            y1 = 0;
        }
        #endregion
    }
}
