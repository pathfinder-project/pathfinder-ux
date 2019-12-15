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
        /// 单击按钮关闭窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void button_maximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState != WindowState.Maximized)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }

        private void button_minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
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
            lock (renderer)
            {
                renderer.P = e.GetPosition(canvas);
            }
        }

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            renderer.MouseLeftPress(e.GetPosition(canvas));
        }

        private void canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            renderer.MouseLeftRelease();
        }

        private void canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            renderer.MouseLeftRelease();
        }

        private void canvas_Loaded(object sender, RoutedEventArgs e)
        {
            renderer.CanvasH = canvas.ActualHeight;
            renderer.CanvasW = canvas.ActualWidth;
            renderer.SlidePath = @"C:\ki-67\1713365\Ki-67_H_10.mrxs";
        }

        private void canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            renderer.CanvasH = canvas.ActualHeight;
            renderer.CanvasW = canvas.ActualWidth;
            renderer.Render();
        }
    }
}
