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
        public uint ida { get; set; }
        public uint idb { get; set; }

        public uint id { get; set; }

        public Vertex(uint id)
        {
            this.id = id;
            this.ida = this.idb = 0;
        }

        /// <summary>
        /// 把自己和顶点v连起来
        /// </summary>
        /// <param name="v"></param>
        public bool ConnectWith(Vertex v)
        {
            if (v.id < id && ida == 0 && v.idb == 0)
            {
                ida = v.id;
                v.idb = id;
                return true;
            }
            else if (v.id > id && idb == 0 && v.ida == 0)
            {
                idb = v.id;
                v.ida = id;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 把自己和邻居断开
        /// </summary>
        public void DisconnectWith(Vertex v)
        {
            if (v.id == ida)
            {
                ida = v.idb = 0;
            }
            else if (v.id == idb)
            {
                idb = v.ida = 0;
            }
        }
    }
}
