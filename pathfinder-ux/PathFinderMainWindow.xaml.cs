﻿using System;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Input;

using PathFinder.Controller;
using PathFinder.Controller.Actions;

namespace PathFinder
{
    enum BrowsingState
    {
        NoSlide,
        Locked,
        Free,
        Moving,
    }

    enum DrawingState
    {
        NotDrawing,
        DrawingPolygon,
    }

    public partial class PathFinderMainWindow : Window
    {
        private SceneBlender blender;
        private ActionQueue aq;

        private BrowsingState bs;
        private DrawingState ds;

        private double x0 = 0;
        private double x1 = 0;

        private double y0 = 0;
        private double y1 = 0;

        public PathFinderMainWindow()
        {
            aq = ActionQueue.Singleton();
            InitializeComponent();
            bs = BrowsingState.NoSlide;
            ds = DrawingState.NotDrawing;
            blender = new SceneBlender(CanvasMain, canvasBg, 15);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            blender.Stop();
        }

        /// <summary>
        /// 按住菜单栏拖动。方案来自 https://stackoverflow.com/a/7418629
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuMain_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;
            // 以下代码是为了支持最大化状态下的拖动, 但会导致拖动后鼠标指针漂移.
            //if (WindowState == WindowState.Maximized)
            //    WindowState = WindowState.Normal;
                // TODO: 窗口移动到鼠标底下
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
                var act = new LoadSlide();
                act.Path = dlg.FileName;
                (act.W, act.H) = (CanvasMain.ActualWidth, CanvasMain.ActualHeight);
                aq.Submit(act);
                blender.Start();
                bs = BrowsingState.Free;
                CanvasMain.Cursor = Cursors.Hand;
            }
        }

        private void MenuFileClose_Click(object sender, RoutedEventArgs e)
        {
            blender.Stop();
            bs = BrowsingState.NoSlide;
            CanvasMain.Cursor = Cursors.Arrow;
        }

        private void CanvasMain_MouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(CanvasMain);
            if (bs == BrowsingState.Moving)
            {
                var act = new Move();
                (x1, y1) = (p.X, CanvasMain.ActualHeight - p.Y);
                (act.dX, act.dY) = (x0 - x1, y0 - y1);
                aq.Submit(act);
                (x0, y0) = (x1, y1);
            }
        }

        private void CanvasMain_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var p = e.GetPosition(CanvasMain);
            (x1, y1) = (p.X, CanvasMain.ActualHeight - p.Y);

            if (bs == BrowsingState.Free)
            {
                CanvasMain.Cursor = Cursors.ScrollAll;
                bs = BrowsingState.Moving;
                (x0, y0) = (x1, y1);
            } 
            else if (bs == BrowsingState.Locked)
            {
                if (e.ClickCount == 1)
                {
                    if (ds == DrawingState.DrawingPolygon)
                    {
                        var act = new DrawPolygonV();
                        act.dps = DrawPolygonState.PlacingVertex;
                        (act.X, act.Y) = (x1, y1);
                        aq.Submit(act);
                    }
                }
                else if (e.ClickCount == 2)
                {
                    if (ds == DrawingState.DrawingPolygon)
                    {
                        var act = new DrawPolygonV();
                        act.dps = DrawPolygonState.TryFinish;
                        (act.X, act.Y) = (x1, y1);
                        aq.Submit(act);
                    }

                    ds = DrawingState.NotDrawing;
                    bs = BrowsingState.Free;
                    CanvasMain.Cursor = Cursors.Hand;
                }
            }
        }

        private void CanvasMain_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (bs == BrowsingState.Moving)
            {
                bs = BrowsingState.Free;
                CanvasMain.Cursor = Cursors.Hand;
            }  
        }

        private void CanvasMain_MouseLeave(object sender, MouseEventArgs e)
        {
            if (bs == BrowsingState.Moving)
            {
                bs = BrowsingState.Free;
                CanvasMain.Cursor = Cursors.Hand;
            }
        }

        private void CanvasMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double w, h;
            (w, h) = (CanvasMain.ActualWidth, CanvasMain.ActualHeight);
            if (bs != BrowsingState.NoSlide)
            {
                var act = new Resize();
                (act.W, act.H) = (w, h);
                aq.Submit(act);
            }
        }

        private void CanvasMain_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // GetPosition得到的坐标是以「左下角」为原点的.
            // 需要换算为以左上角为原点.
            var p = e.GetPosition(CanvasMain);
            (x1, y1) = (p.X, CanvasMain.ActualHeight - p.Y);

            // 是与canvas左上角的WPF距离向量
            if (bs == BrowsingState.Free)
            {
                int scroll = e.Delta;

                var act = new Zoom();
                act.nScroll = scroll;
                (act.X, act.Y) = (x1, y1);
                aq.Submit(act);
            }
        }

        private void ButtonLeftPanelDrawPolygon_Click(object sender, RoutedEventArgs e)
        {
            if (bs == BrowsingState.Free)
            {
                bs = BrowsingState.Locked;
                ds = DrawingState.DrawingPolygon;
                CanvasMain.Cursor = Cursors.Pen;
            }
            else if (ds != DrawingState.NotDrawing)
            {
                bs = BrowsingState.Free;
                ds = DrawingState.NotDrawing;
                CanvasMain.Cursor = Cursors.Hand;
            }
        }
    }
}
