using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.DataStructure
{
    class PixelCoordinate: IDataStructure
    {
        public int X { get; set; }
        public int Y { get; set; }

        public PixelCoordinate()
        {
            Init();
        }

        public void Clone(IDataStructure d)
        {
            if (d is PixelCoordinate)
            {
                var mpc = d as PixelCoordinate;
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
