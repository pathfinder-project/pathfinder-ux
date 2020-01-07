using System;
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

        private BrowsingState brs;
        private DrawingState drs;

        private int x0 = 0;
        private int x1 = 0;

        private int y0 = 0;
        private int y1 = 0;

        public PathFinderMainWindow()
        {
            aq = ActionQueue.Singleton();
            InitializeComponent();
            brs = BrowsingState.NoSlide;
            drs = DrawingState.NotDrawing;
            blender = new SceneBlender(canvas, canvasBg, 15);
        }

        /// <summary>
        /// 按住菜单栏拖动。方案来自 https://stackoverflow.com/a/7418629
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menu_main_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;
            // 以下代码是为了支持最大化状态下的拖动, 但会导致拖动后鼠标指针漂移.
            //if (WindowState == WindowState.Maximized)
            //    WindowState = WindowState.Normal;
                // TODO: 窗口移动到鼠标底下
            this.DragMove();
        }

        private void menu_main_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (WindowState != WindowState.Maximized)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (brs == BrowsingState.Moving)
            {
                var act = new Move();
                Helper.GetMousePosition(ref x1, ref y1);
                act.dX = x0 - x1;
                act.dY = y0 - y1;
                aq.Submit(act);
                x0 = x1;
                y0 = y1;
            }
        }

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //int x = 0, y = 0;
            //Helper.GetMousePosition(ref x, ref y);
            //Console.WriteLine("mx={0}, my={1}", x, y);
            if (brs == BrowsingState.Free)
            {
                canvas.Cursor = Cursors.ScrollAll;
                brs = BrowsingState.Moving;
                Helper.GetMousePosition(ref x0, ref y0);
            } 
        }

        private void canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (brs == BrowsingState.Moving)
            {
                brs = BrowsingState.Free;
                canvas.Cursor = Cursors.Hand;
            }  
        }

        private void canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (brs == BrowsingState.Moving)
            {
                brs = BrowsingState.Free;
                canvas.Cursor = Cursors.Hand;
            }
        }

        private void canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (brs == BrowsingState.Free)
            {
                var act = new Resize();
                Helper.ToScreenPixel(canvas, canvas.ActualWidth, canvas.ActualHeight, ref act.W, ref act.H);
                aq.Submit(act);
            }
        }

        private void canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 是与canvas左上角的WPF距离向量
            if (brs == BrowsingState.Free)
            {
                int scroll = e.Delta;

                var act = new Zoom();
                act.nScroll = scroll;
                var p = e.GetPosition(canvas);

                // GetPosition得到的坐标是以「左下角」为原点的.
                // 需要换算为以左上角为原点.
                Helper.ToScreenPixel(canvas, p.X, canvas.ActualHeight - p.Y, ref act.X, ref act.Y);
                aq.Submit(act);
            }
        }

        private void menu_file_open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                var act = new LoadSlide();
                act.Path = dlg.FileName;
                Helper.ToScreenPixel(canvas, canvas.ActualWidth, canvas.ActualHeight, ref act.W, ref act.H);
                Console.WriteLine("W={0}, H={1}", act.W, act.H);
                aq.Submit(act);
                blender.Start();
                brs = BrowsingState.Free;
                canvas.Cursor = Cursors.Hand;
            }
        }

        private void menu_file_close_Click(object sender, RoutedEventArgs e)
        {
            blender.Stop();
            brs = BrowsingState.NoSlide;
            canvas.Cursor = Cursors.Arrow;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            blender.Stop();
        }

    }
}
