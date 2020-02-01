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
    class SlideScene
    {
        private byte[] imgBmpBuf;
        private byte[] thumbJpgBuf;
        private OpenSlideImage slide;

        public SlideScene()
        {
            imgBmpBuf = new byte[Helper.BMP_BGRA_DATA_OFFSET + 4096 * 4096 * 4];
        }

        /// <summary>
        /// 根据sg所描述的位置、大小和倍率, 读取一个区域
        /// </summary>
        /// <param name="sg"></param>
        /// <returns></returns>
        public byte[] LoadRegionBMP(SceneGeometry sg)
        {
            Helper.InitBgraHeader(imgBmpBuf, (int)sg.OutW, (int)sg.OutH);
            slide.DangerousReadRegion(sg.L, (int)sg.X, (int)sg.Y, (int)sg.OutW, (int)sg.OutH, ref imgBmpBuf[Helper.BMP_BGRA_DATA_OFFSET]);
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
            if (!(info is SceneGeometry))
            {
                throw new InvalidOperationException("Info is not MapGeometry type.");
            }
            if (OpenSlideImage.DetectFormat(path) == null)
            {
                throw new InvalidOperationException($"<<{path}>> is NOT a digital slide!");
            }
            slide = OpenSlideImage.Open(path);
            SceneGeometry sg = info as SceneGeometry;
            sg.Init();
            sg.SlideW = (int)slide.Width;
            sg.SlideH = (int)slide.Height;
            sg.L = 0;
            sg.X = sg.SlideW / 2;
            sg.Y = sg.SlideH / 2;
            sg.NumL = slide.LevelCount;
            for (int i = 0; i < sg.NumL; ++i)
            {
                sg.SetDensityAtLevel(i, slide.GetLevelDownsample(i));
            }
            thumbJpgBuf = slide.GetThumbnailAsJpeg(1024, 90);
        }

        public void Close()
        {
            if (!slide.IsDisposed)
            {
                slide.Dispose();
            }
            imgBmpBuf = new byte[Helper.BMP_BGRA_DATA_OFFSET + 4096 * 4096 * 4];
        }
    }
}
