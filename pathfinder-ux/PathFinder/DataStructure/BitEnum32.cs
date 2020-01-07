using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.DataStructure
{

    class BitEnum32 : IDataStructure
    {
        private uint bits;

        public uint Value { get { return bits; } }

        public void Add(params uint[] state)
        {
            foreach (var s in state)
            {
                bits |= s;
            }
        }

        public void Set(uint state)
        {
            bits = state;
        }

        public void Unset(params uint[] state)
        {
            foreach (var s in state)
            {
                bits &= ~s;
            }
        }

        public bool Has(uint state)
        {
            return (bits & state) == state;
        }

        public bool Is(uint state)
        {
            return bits == state;
        }

        public bool BelongTo(uint state)
        {
            return (bits & state) == bits;
        }

        public bool HasAll(params uint[] state)
        {
            uint u = 0;
            foreach (var s in state)
            {
                u |= s;
            }
            return (u & bits) == u;
        }

        public bool HasAny(params uint[] state)
        {
            foreach (var s in state)
            {
                if (Has(s))
                    return true;
            }
            return false;
        }

        public void Clone(IDataStructure d)
        {
            var s = d as BitEnum32;
            bits = s.bits;
        }

        public void Init() { bits = 0; }
    }
}
