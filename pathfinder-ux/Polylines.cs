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
        private uint idv;
        private Vertex a;
        private Vertex b;

        public uint id
        {
            get { return idv; }
            set
            {
                idv = value;
            }
        }

        public Vertex(uint id)
        {
            this.idv = id;
        }

        public bool IsHead()
        {
            return (a == null) ^ (b == null);
        }

        public void GetNeighbours(out uint ia, out uint ib)
        {
            ia = ib = 0;
            if (a != null)
            {
                ia = a.idv;
                Console.WriteLine($"ia={ia}");
            }
            if (b != null)
            {
                ib = b.idv;
                Console.WriteLine($"ib={ib}");
            }
        }

        /// <summary>
        /// 把自己和顶点v连起来
        /// </summary>
        /// <param name="v"></param>
        public bool ConnectWith(Vertex v)
        {
            if (v.idv < idv && a == null && v.b == null)
            {
                a = v;
                v.b = this;
                return true;
            }
            else if (v.idv > idv && b == null && v.a == null)
            {
                b = v;
                v.a = this;
                return true;
            }
            return false;
        }

        public void ForceConnectWith(Vertex v)
        {
            if (v.idv < idv)
            {
                a = v;
                v.b = this;
            }
            else if (v.idv > idv)
            {
                b = v;
                v.a = this;
            }
        }

        /// <summary>
        /// 把自己和邻居断开
        /// </summary>
        public void DisconnectWith(Vertex v)
        {
            if (v == a)
            {
                a = null;
                v.b = null;
            }
            else if (v == b)
            {
                b = null;
                v.a = null;
            }
        }
    }
}
