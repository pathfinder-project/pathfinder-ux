using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.DataStructure
{
    class MapGeometry: IDataStructure
    {
        // 0级宽度
        public int W;

        // 0级高度
        public int H;

        public int NumL;
        private double[] scales;

        public MapGeometry()
        {
            Init();
        }

        public void CopyValuesFrom(IDataStructure d)
        {
            throw new NotImplementedException();
        }

        public void Init()
        {
            W = H = NumL = 0;
            if (scales == null) scales = new double[256];
            scales.FillWith(-1);
        }

        public double GetScaleAtLevel(int level)
        {
            return scales[level];
        }

        public void SetScaleAtLevel(int level, double scale)
        {
            scales[level] = scale;
        }
    }
}
