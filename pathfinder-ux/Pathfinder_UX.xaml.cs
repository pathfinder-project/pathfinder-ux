using System;
using System.Collections.Generic;
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
        private View view;
        private bool isMouseLeftPressed = false;
        private System.Windows.Point previousPosition = new System.Windows.Point();

        public Pathfinder_UX()
        {
            InitializeComponent();
            view = new View();
            view.LeftTopX = view.MaxWidth / 2;
            view.LeftTopY = view.MaxHeight / 4;
            ChangeScreen();
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

        private void workspace_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseLeftPressed)
            {
                System.Windows.Point currentPosition = e.GetPosition(workspace);
                // Change 1: Multiply a correction ratio 0.8
                // The value is human-tweaked.
                view.LeftTopX -= (int)((currentPosition.X - previousPosition.X)*0.8);
                view.LeftTopY += (int)((currentPosition.Y - previousPosition.Y)*0.8);

                // Change 2: Update previousPosition
                // If omitted, and if you are dragging to the same direction,
                // you will feel that the image is out of your control.
                previousPosition = currentPosition;
                if (view.LeftTopY < 0)
                    view.LeftTopY = 0;
                UpdateWorkspaceBackground();
            }
            /**
             * 我现在只能写出这种精度的拖动。完全对准的拖动还是实现不出来。
             * 精度提高的原因是两处改动，已经注释在代码里。
             * 以后有精力慢慢实现吧。
             */
        }

        private void workspace_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            previousPosition = e.GetPosition(workspace);
            isMouseLeftPressed = true;
        }

        private void workspace_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isMouseLeftPressed = false;
        }

        private void ChangeScreen()
        {
            int h = 0, w = 0;
            Helper.CurrentScreenHeightWidth(ref h, ref w);
            view.VisionH = h;
            view.VisionW = w;
            view.ReallocBgraData();
            UpdateWorkspaceBackground();
        }

        private void UpdateWorkspaceBackground()
        {
            workspace_bg.ImageSource = view.LoadVision();
        }
    }
}
