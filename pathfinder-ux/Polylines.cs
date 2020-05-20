using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder
{
    class Vertex
    {
        public double x;
        public double y;
        public uint idl { get; set; }
        public uint idr { get; set; }

        public uint id { get; set; }

        public Vertex(uint id)
        {
            this.id = id;
            this.idl = this.idr = 0;
        }

        /// <summary>
        /// 把自己和顶点v连起来
        /// </summary>
        /// <param name="v"></param>
        public static bool Connect(Vertex v1, Vertex v2)
        {
            if (v1.id == v2.id)
            {
                return false;
            }
            else if (v1.idl == 0 && v2.idr == 0)
            {
                v1.idl = v2.id;
                v2.idr = v1.id;
                return true;
            }
            else if (v1.idr == 0 && v2.idl == 0)
            {
                v1.idr = v2.id;
                v2.idl = v1.id;
                return true;
            }
            else
            {
                return false;
            }
            //Console.WriteLine($"v1[{v1.id}, {v1.idl}, {v1.idr}], v2[{v2.id}, {v2.idl}, {v2.idr}]");
        }

        /// <summary>
        /// 把自己和邻居断开
        /// </summary>
        public void DisconnectWith(Vertex v)
        {
            if (v.id == idl)
            {
                idl = v.idr = 0;
            }
            else if (v.id == idr)
            {
                idr = v.idl = 0;
            }
        }
    }
}
