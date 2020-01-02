using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenSlideNET;

namespace PathFinder
{

    public partial class PathFinderMainWindow : Window
    {
        private Scene scene;
        private bool dragging = false;

        public PathFinderMainWindow()
        {
            InitializeComponent();
            scene = new Scene(canvas, canvasBg, 15);
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
            if (dragging)
            {
                scene.DuringDrag();
            }
        }

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragging = true;
            scene.BeginDrag();
        }

        private void canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragging = false;
        }

        private void canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private void canvas_Loaded(object sender, RoutedEventArgs e)
        {
            scene.Resize(canvas.ActualWidth, canvas.ActualHeight);
        }

        private void canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            scene.Resize(canvas.ActualWidth, canvas.ActualHeight);
        }

        private void canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 是与canvas左上角的WPF距离向量
            int scroll = e.Delta;
            scene.Zoom(scroll);
        }

        private void menu_file_open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                string path = dlg.FileName;
                scene.LoadSlide(path);
            }
        }

        private void menu_file_close_Click(object sender, RoutedEventArgs e)
        {
            scene.Dispose();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //renderer.Dispose();

            // 即使renderer停止计时, 仍可能有至少1帧正在渲染;
            // 因为计时器只是到时间把任务投放到线程池中, 然后就不管了.
            // 干脆强行退出, 以避免程序报错.
            scene.Dispose();
            Environment.Exit(0);
        }

    }
}
