using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using OpenSlideNET;
using System.Drawing;

namespace pathfinder_ux
{
    class View
    {
        public View()
        {
            slide = OpenSlideImage.Open(@"C:\ki-67\1713365\Ki-67_H_10.mrxs");
        }

        public int VisionW { get; set; }
        public int VisionH { get; set; }
        private byte[] bgra_data; // BMP Bgra data.
        private OpenSlideImage slide;
        private int lefttop_x;
        private int lefttop_y;

        public int MaxWidth { get { return (int)slide.Width; } }

        public int MaxHeight { get { return (int)slide.Height; } }

        public int LeftTopX
        {
            get { return this.lefttop_x; }
            set
            {
                if (value < 0)
                    this.lefttop_x = 0;
                else if (value > MaxWidth - VisionW)
                    this.lefttop_x = MaxWidth - VisionW;
                else
                    this.lefttop_x = value;
            }
        }

        public int LeftTopY
        {
            get { return this.lefttop_y; }
            set
            {
                if (value < 0)
                    this.lefttop_y = 0;
                else if (value > MaxHeight - VisionH)
                    this.lefttop_y = MaxHeight - VisionH;
                else
                    this.lefttop_y = value;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public BitmapImage LoadVision()
        {
            Helper.init_bgra_header(bgra_data, VisionW, VisionH);
            this.slide.DangerousReadRegion(0,
                this.LeftTopX, this.LeftTopY,
                VisionW, VisionH,
                ref this.bgra_data[Helper.BMP_BGRA_DATA_OFFSET]);
            BitmapImage bi = null;
            using (var ms = new System.IO.MemoryStream(bgra_data))
            {
                bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.StreamSource = ms;
                bi.EndInit();
            }
            return bi;
        }

        //public void LoadVision(WriteableBitmap wb)
        //{
        //    try
        //    {
        //        wb.Lock();
        //        unsafe
        //        {
        //            this.slide.ReadRegionInternal(0,
        //                this.LeftTopX,
        //                this.LeftTopY,
        //                VisionW, VisionH,
        //                wb.BackBuffer.ToPointer());
        //        }
        //    }
        //    finally
        //    {
        //        wb.Unlock();
        //    }
        //}

        public void ReallocBgraData()
        {
            bgra_data = null;
            bgra_data = new byte[Helper.BMP_BGRA_DATA_OFFSET + VisionW * VisionH * 4];
        }
    }
}
