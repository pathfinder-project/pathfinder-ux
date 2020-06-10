using OpenSlideNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PathFinder.Scene
{
    class Slide
    {
        private byte[] imgBmpBuf;
        private byte[] thumbJpgBuf;
        private OpenSlideImage img;

        public Slide()
        {
            imgBmpBuf = new byte[Helper.BMP_BGRA_DATA_OFFSET + 4096 * 4096 * 4];
        }

        /// <summary>
        /// 根据sg所描述的位置、大小和倍率, 读取一个区域
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        public byte[] LoadRegionBMP(Viewport view)
        {
            Helper.InitBgraHeader(imgBmpBuf, (int)view.OutW, (int)view.OutH);
            img.DangerousReadRegion(view.L, (int)view.X, (int)view.Y, (int)view.OutW, (int)view.OutH, ref imgBmpBuf[Helper.BMP_BGRA_DATA_OFFSET]);
            return imgBmpBuf;
        }

        public byte[] LoadRegionBMP(BoundingBox bb)
        {
            int x = (int)bb.x1;
            int y = (int)bb.y1;
            int w = (int)(bb.x2 - bb.x1 + 1);
            int h = (int)(bb.y2 - bb.y1 + 1);
            byte[] buf = new byte[Helper.BMP_BGRA_DATA_OFFSET + w * h * 4];
            Helper.InitBgraHeader(buf, w, h);
            img.DangerousReadRegion(0, x, y, w, h, ref buf[Helper.BMP_BGRA_DATA_OFFSET]);
            return buf;
        }

        public byte[] LoadThumbJPG()
        {
            return thumbJpgBuf;
        }

        /// <summary>
        /// 打开一张扫描切片
        /// </summary>
        /// <param name="path"></param>
        /// <param name="info">输出. 封装了切片信息</param>
        /// <returns>切片的宽高 (slideW, slideH) </returns>
        public void Open(string path, object info)
        {
            if (!(info is Viewport))
            {
                throw new InvalidOperationException("Info is not MapGeometry type.");
            }
            if (OpenSlideImage.DetectFormat(path) == null)
            {
                throw new InvalidOperationException($"<<{path}>> is NOT a digital slide!");
            }
            img = OpenSlideImage.Open(path);
            Viewport sg = info as Viewport;
            sg.Init();
            sg.SlideW = (int)img.Width;
            sg.SlideH = (int)img.Height;
            sg.L = 0;
            sg.X = sg.SlideW / 2;
            sg.Y = sg.SlideH / 2;
            sg.NumL = img.LevelCount;
            for (int i = 0; i < sg.NumL; ++i)
            {
                sg.SetDensityAtLevel(i, img.GetLevelDownsample(i));
            }
            thumbJpgBuf = img.GetThumbnailAsJpeg(1024, 90);
        }

        public void Close()
        {
            if (!img.IsDisposed)
            {
                img.Dispose();
            }
            imgBmpBuf = new byte[Helper.BMP_BGRA_DATA_OFFSET + 4096 * 4096 * 4];
        }
    }
}
