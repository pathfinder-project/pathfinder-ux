using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.DataStructure
{
    class IntTuple: IDataStructure
    {
        public int X;
        public int Y;

        public IntTuple()
        {
            Init();
        }

        public void CopyValuesFrom(IDataStructure d)
        {
            if (d is IntTuple)
            {
                var mpc = d as IntTuple;
                X = mpc.X;
                Y = mpc.Y;
            }
            else throw new InvalidOperationException("Object d is not MousePixelCoordinate type");
        }

        public void Init()
        {
            X = Y = 0;
        }
    }
}
