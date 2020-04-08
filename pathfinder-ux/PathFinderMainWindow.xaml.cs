using System;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Shapes;

namespace PathFinder
{

    public partial class PathFinderMainWindow : Window
    {
        Worker blender;
        MessageQueue aq;

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

            operation = Idle;
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
                Init();
                CanvasMain.Cursor = Cursors.Hand;
                CanvasBackground.Visibility = Visibility.Visible;
            }
        }

        private void MenuFileClose_Click(object sender, RoutedEventArgs e)
        {
            blender.Stop();
            operation = Idle;
            CanvasMain.Cursor = Cursors.Arrow;
        }
    }
}
