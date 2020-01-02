using OpenSlideNET;
using PathFinder.DataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.Layer
{
    class ImageLayer : ILayer
    {
        private byte[] bmpBuf;
        private OpenSlideImage slide;

        public ImageLayer()
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
        /// <returns>切片的宽高 (slideW, slideH) </returns>
        public void Open(string path, object info)
        {
            if (info is MapGeometry)
            {
                slide = OpenSlideImage.Open(path);
                MapGeometry mg = info as MapGeometry;
                mg.W = (int)slide.Width;
                mg.H = (int)slide.Height;
                mg.NumL = slide.LevelCount;
                for (int i = 0; i < mg.NumL; ++i)
                {
                    mg.SetScaleAtLevel(i, slide.GetLevelDownsample(i));
                }
            }
            else throw new InvalidOperationException("info is not MapGeometry type.");
        }
    }
}
