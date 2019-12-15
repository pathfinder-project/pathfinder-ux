using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;

namespace pathfinder_ux
{
    public static class Helper
    {
        /// <summary>
        /// 
        /// References: 
        /// * Difference between ref and out: https://stackoverflow.com/a/388467
        /// * Getting the resolution of CURRENT screen: https://stackoverflow.com/a/2902734
        /// </summary>
        /// <param name="window"></param>
        /// <param name="Height">Output height</param>
        /// <param name="Width">Output width</param>
        /// 
        public static void CurrentScreenHeightWidth(ref int Height, ref int Width)
        {
            Height = (int)SystemParameters.VirtualScreenHeight;
            Width = (int)SystemParameters.VirtualScreenWidth;
        }

        /// <summary>
        /// Add/Reset BMP file header to <pre>bgra_data</pre>.
        /// Idea from: https://stackoverflow.com/a/44023462
        /// See data sheet: https://en.wikipedia.org/wiki/BMP_file_format#Example_2
        /// </summary>
        /// <param name="bgra_data"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        public static void init_bgra_header(byte[] bgra_data, int w, int h)
        {
            int img_byte_len = w * h * 4;
            #region BMP header (14 Bytes)
            // 0~1: "BM"
            // <byte>
            bgra_data[0] = 0x42;
            bgra_data[1] = 0x4d;

            // 2~5: File size.
            // =byte_len+122 <int>
            Array.Copy(BitConverter.GetBytes(img_byte_len + 122), 0, bgra_data, 2, 4);

            // 6~7: Application specific.
            // 8~9: Application specific.
            // =0 <int>
            Array.Copy(BitConverter.GetBytes(0), 0, bgra_data, 6, 4);

            // 10~13: Offset where the pixel array can be found.
            // =122 <int>
            Array.Copy(BitConverter.GetBytes(122), 0, bgra_data, 10, 4);
            #endregion

            #region DIB header (108 Bytes)
            // 14~17: Offset where the pixel array can be found.
            // =108 <short>
            Array.Copy(BitConverter.GetBytes(108), 0, bgra_data, 14, 4);

            // 18~21: Width (The distance between left and right border)
            // =w <int>
            Array.Copy(BitConverter.GetBytes(w), 0, bgra_data, 18, 4);

            // 22~25: Height (The distance between top and bottom border)
            // =h <int>
            Array.Copy(BitConverter.GetBytes(h), 0, bgra_data, 22, 4);

            // 26~27: Number of color planes being used.
            // =1 <short>
            Array.Copy(BitConverter.GetBytes(1), 0, bgra_data, 26, 2);

            // 28~29: Number of bits per pixel.
            // =32 <short>
            Array.Copy(BitConverter.GetBytes(32), 0, bgra_data, 28, 2);

            // 30~33: BI_BITFIELDS, no pixel array compression used.
            // =3 <int>
            Array.Copy(BitConverter.GetBytes(3), 0, bgra_data, 30, 4);

            // 34~37: Size of the raw bitmap data.
            // =byte_len <int>
            Array.Copy(BitConverter.GetBytes(img_byte_len), 0, bgra_data, 34, 4);

            // 38~41: Horizontal print resolution (unit: pixels/meter).
            // =2835 <int>
            Array.Copy(BitConverter.GetBytes(2835), 0, bgra_data, 38, 4);

            // 42~45: Vertical print resolution (unit: pixels/meter).
            // =2835 <int>
            Array.Copy(BitConverter.GetBytes(2835), 0, bgra_data, 42, 4);

            // 46~49: Number of colors in the palette.
            // =0 <int>
            Array.Copy(BitConverter.GetBytes(0), 0, bgra_data, 46, 4);

            // 50~53: Number of important colors (0 means all colors are important).
            // =0 <int>
            Array.Copy(BitConverter.GetBytes(0), 0, bgra_data, 50, 4);

            // 54~57: Red channel bit mask (Big endian).
            // =0x00ff0000 <int>
            Array.Copy(BitConverter.GetBytes(0x00ff0000), 0, bgra_data, 54, 4);

            // 58~61: Green channel bit mask (to Big endian).
            // =0x0000ff00 <int>
            Array.Copy(BitConverter.GetBytes(0x0000ff00), 0, bgra_data, 58, 4);

            // 62~65: Blue channel bit mask (Big endian).
            // =0x000000ff <int>
            Array.Copy(BitConverter.GetBytes(0x000000ff), 0, bgra_data, 62, 4);

            // 66~69: Alpha channel bit mask (Big endian).
            // =0xff000000 <int>
            Array.Copy(BitConverter.GetBytes(0xff000000), 0, bgra_data, 66, 4);

            // 70~73: little endian "Win ".
            Array.Copy(BitConverter.GetBytes(0x57696e20), 0, bgra_data, 70, 4);

            // 74~109: Unused for LCS "Win " or "sRGB".
            // 110~113: Red Gamma (unused)
            // 114~117: Green Gamma (unused)
            // 118~121: Blue Gamma (unused)
            Array.Clear(bgra_data, 74, 48);
            #endregion
        }

        public static int BMP_BGRA_DATA_OFFSET = 122;

        /// <summary>
        /// 获取一个WPF控件的宽高，以像素计。
        /// NOTE: WPF默认获取设备无关长度（每单位1/96英寸）。需要转化为像素。
        /// </summary>
        /// <param name="elem">要获取宽高的控件</param>
        /// <returns></returns>
        public static Size devIndepLen2px(UIElement elem)
        {
            Matrix transformToDevice;
            var source = PresentationSource.FromVisual(elem);
            if (source != null)
                transformToDevice = source.CompositionTarget.TransformToDevice;
            else
                using (var source2 = new HwndSource(new HwndSourceParameters()))
                    transformToDevice = source2.CompositionTarget.TransformToDevice;

            if (elem.DesiredSize == new Size())
                elem.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            return (Size)transformToDevice.Transform((Vector)elem.DesiredSize);
        }

        #region Utilities of Mouse cursor position
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
        public static Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }
        #endregion
    }
}
