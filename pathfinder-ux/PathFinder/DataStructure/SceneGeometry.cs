using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.DataStructure
{
    class SceneGeometry: IDataStructure
    {
        // 视野左上角横坐标. 像素. 0级缩放.
        public int X;

        // 视野左上角纵坐标. 像素. 0级缩放.
        public int Y;

        // 缩放级别.
        public int L;

        // 视野宽度. 像素.
        public int W;

        // 视野高度. 像素.
        public int H;

        public SceneGeometry()
        {
            Init();
        }

        public void CopyValuesFrom(IDataStructure d)
        {
            if (d is SceneGeometry)
            {
                SceneGeometry sg2 = d as SceneGeometry;
                X = sg2.X;
                Y = sg2.Y;
                L = sg2.L;
                W = sg2.W;
                H = sg2.H;
            }
            else throw new InvalidOperationException("Object d is not SceneGeometry type.");
        }

        public static bool operator ==(SceneGeometry sg1, SceneGeometry sg2)
        {
            if (ReferenceEquals(sg1, null)) return false;
            if (ReferenceEquals(sg2, null)) return false;
            if (sg1.X != sg2.X) return false;
            if (sg1.Y != sg2.Y) return false;
            if (sg1.L != sg2.L) return false;
            if (sg1.W != sg2.W) return false;
            if (sg1.H != sg2.H) return false;
            return true;
        }

        public static bool operator !=(SceneGeometry sg1, SceneGeometry sg2)
        {
            return !(sg1 == sg2);
        }

        public void Init()
        {
            X = Y = L = W = H = 0;
        }
    }
}
