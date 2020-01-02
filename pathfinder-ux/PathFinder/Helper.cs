using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;

namespace PathFinder
{
    public static class Helper
    {
        /// <summary>
        /// 获取鼠标的像素坐标
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void GetMousePosition(ref int x, ref int y)
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            x = w32Mouse.X;
            y = w32Mouse.Y;
        }

        /// <summary>
        /// 获取框架式UI元素的像素宽高(或横纵坐标)
        /// </summary>
        /// <param name="elem"></param>
        /// <param name="x_wpf"></param>
        /// <param name="y_wpf"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void GetPixelXY(FrameworkElement elem, double x_wpf, double y_wpf, ref int x, ref int y)
        {
            var src = PresentationSource.FromVisual(elem);
            Matrix tran = src.CompositionTarget.TransformToDevice;
            Vector vec = new Vector { X = x_wpf, Y = y_wpf };
            Vector px_vec = tran.Transform(vec);
            x = (int)px_vec.X;
            y = (int)px_vec.Y;
        }

        /// <summary>
        /// Add/Reset BMP file header to <pre>bgra_data</pre>.
        /// Idea from: https://stackoverflow.com/a/44023462
        /// See data sheet: https://en.wikipedia.org/wiki/BMP_file_format#Example_2
        /// </summary>
        /// <param name="bgra_data"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        public static void InitBgraHeader(byte[] bgra_data, int w, int h)
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

        public static readonly int BMP_BGRA_DATA_OFFSET = 122;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        public static void FillWith<T>(this T[] a, T val)
        {
            if (a == null) { throw new ArgumentNullException("a"); }
            int len = a.Length;
            if (len == 0) return;
            a[0] = val;
            int cnt;
            for (cnt = 1; cnt <= len / 2; cnt *= 2)
            {
                Array.Copy(a, 0, a, cnt, len - cnt);
            }
            Array.Copy(a, 0, a, cnt, len - cnt);
        }
        
    }
}
