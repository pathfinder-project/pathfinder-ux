using OpenSlideNET;
using PathFinder.Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.Layer
{
    class SlideLayer : ILayer
    {
        private byte[] bmpBuf;
        private OpenSlideImage slide;

        public SlideLayer()
        {
            bmpBuf = new byte[Helper.BMP_BGRA_DATA_OFFSET + 4096 * 4096 * 4];
        }

        public Task<object> AsyncLoadRegion(SceneGeometry sg)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            slide.Dispose();
        }

        public void Dispose()
        {
            Close();
        }

        public object LoadRegion(SceneGeometry sg)
        {
            Helper.InitBgraHeader(bmpBuf, sg.W, sg.H);
            slide.DangerousReadRegion(sg.L, sg.X, sg.Y, sg.W, sg.H, ref bmpBuf[Helper.BMP_BGRA_DATA_OFFSET]);
            return bmpBuf;
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
                throw new InvalidOperationException("info is not MapGeometry type.");
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

        }
    }
}
