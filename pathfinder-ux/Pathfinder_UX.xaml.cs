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

namespace pathfinder_ux
{
    /// <summary>
    /// Pathfinder_UX.xaml 的交互逻辑
    /// </summary>
    public partial class Pathfinder_UX : Window
    {
        private CanvasRenderer renderer;
        private bool dragging = false;

        public Pathfinder_UX()
        {
            InitializeComponent();
            renderer = new CanvasRenderer(canvasBg);
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
                renderer.OnMouseDrag(e.GetPosition(canvas));
            }
        }

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragging = true;
            renderer.OnMouseLeftPress(e.GetPosition(canvas));
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
            renderer.OnCanvasResize(canvas.ActualHeight, canvas.ActualWidth);
        }

        private void canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            renderer.OnCanvasResize(canvas.ActualHeight, canvas.ActualWidth);
        }

        private void canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 是与canvas左上角的WPF距离向量
            System.Windows.Point P = e.GetPosition(canvas);
            int scroll = e.Delta;
            renderer.OnMouseScroll(P, scroll);
        }

        private void menu_file_open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                string path = dlg.FileName;
                renderer.Path = path;
            }
        }

        private void menu_file_close_Click(object sender, RoutedEventArgs e)
        {
            renderer.Dispose();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            renderer.Dispose();

            // 即使renderer停止计时, 仍可能有至少1帧正在渲染;
            // 因为计时器只是到时间把任务投放到线程池中, 然后就不管了.
            // 干脆强行退出, 以避免程序报错.
            Environment.Exit(0);
        }

    }
}
