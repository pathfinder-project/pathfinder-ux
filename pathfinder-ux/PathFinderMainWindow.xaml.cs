﻿using System;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Input;


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
        Worker blender;
        MessageQueue aq;

        BrowsingState bs;
        int pdc; // Polygon Dot Count
        uint pid = 0;
        
        double x0 = 0;
        double x1 = 0;
        
        double y0 = 0;
        double y1 = 0;

        public PathFinderMainWindow()
        {
            this.DataContext = this;
            aq = MessageQueue.GetInstance();
            InitializeComponent();
            bs = BrowsingState.NoSlide;
            double thumbMaxWidth = (double)this.Resources["thumbMaxWidth"];
            double thumbMaxHeight = (double)this.Resources["thumbMaxHeight"];
            //Console.WriteLine($"{thumbMaxWidth}, {thumbMaxHeight}");
            blender = new Worker(CanvasMain, CanvasThumb, CanvasMainImage, CanvasThumbImage,
                15, thumbMaxWidth, thumbMaxHeight);
            CanvasBackground.Visibility = Visibility.Hidden;
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
                bs = BrowsingState.Free;
                CanvasMain.Cursor = Cursors.Hand;
                
                CanvasBackground.Visibility = Visibility.Visible;
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
                var act = new MoveMessage();
                (x1, y1) = (p.X, CanvasMain.ActualHeight - p.Y);
                (act.dXScreen, act.dYScreen) = (x0 - x1, y0 - y1);
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
                    if (pdc == 0)
                    {
                        pdc++;
                        var act = new BulletMessage();
                        (act.X, act.Y) = (x1, y1);
                        pid += 1;
                        act.IdV = pid;
                        aq.Submit(act);
                    }
                    else if (pdc >= 1)
                    {
                        pdc++;
                        var act = new StickMessage();
                        (act.X, act.Y) = (x1, y1);
                        pid += 1;
                        act.IdV1 = pid - 1;
                        act.IdV2 = pid;
                        aq.Submit(act);
                    }
                }
                else if (e.ClickCount == 2)
                {
                    pdc = 0;
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

            // 是与canvas左上角的WPF距离向量
            if (bs == BrowsingState.Free)
            {
                int scroll = e.Delta;

                var act = new ZoomMessage();
                act.nScroll = scroll;
                (act.XScreen, act.YScreen) = (x1, y1);
                aq.Submit(act);
            }
        }

        private void ButtonLeftPanelDrawPolygon_Click(object sender, RoutedEventArgs e)
        {
            if (bs == BrowsingState.Free)
            {
                bs = BrowsingState.Locked;
                CanvasMain.Cursor = Cursors.Pen;
            }
            else
            {
                bs = BrowsingState.Free;
                pdc = 0;
                CanvasMain.Cursor = Cursors.Hand;
            }
        }

        private void CanvasThumb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (bs == BrowsingState.Free)
            {
                bs = BrowsingState.Moving;
                var p = e.GetPosition(CanvasThumb);
                var act = new ThumbJumpMessage();
                (act.CenterXScreen, act.CenterYScreen) = (p.X, p.Y);
                Console.WriteLine($"Click ({p.X:0.0},{p.Y:0.0}) on CanvasThumb");
                aq.Submit(act);
                bs = BrowsingState.Free;
            }
        }
    }
}
