using OpenSlideNET;
using PathFinder.Algorithm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// <param name="sg"></param>
        /// <returns></returns>
        public byte[] LoadRegionBMP(Viewport sg)
        {
            Helper.InitBgraHeader(imgBmpBuf, (int)sg.OutW, (int)sg.OutH);
            img.DangerousReadRegion(sg.L, (int)sg.X, (int)sg.Y, (int)sg.OutW, (int)sg.OutH, ref imgBmpBuf[Helper.BMP_BGRA_DATA_OFFSET]);
            return imgBmpBuf;
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
