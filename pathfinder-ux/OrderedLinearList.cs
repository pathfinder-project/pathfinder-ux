using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.Algorithm
{
    /// <summary>
    /// 一种线性表, (1) 插入时自动按升序排序; (2) 允许重复元素
    /// </summary>
    class OrderedLinearList<T>
    {
        private List<T> payload;

        public OrderedLinearList()
        {
            payload = new List<T>();
        }

        public void Add(T val)
        {
            int pos = payload.BinarySearch(val);
            // 对于C#的二分搜索的返回值的意义, Array.BinarySearch的文档讲得很清楚, 如下
            // https://docs.microsoft.com/en-us/dotnet/api/system.array.binarysearch?view=netframework-4.8
            // 而List<T>.BinarySearch讲得有些晦涩.
            pos = pos >= 0 ? pos : ~pos;
            payload.Insert(pos, val);
        }

        public void Remove(T val)
        {
            int pos = payload.BinarySearch(val);
            if (pos >= 0)
            {
                payload.RemoveAt(pos);
            }
        }

        public bool Contains(T val)
        {
            return payload.BinarySearch(val) >= 0;
        }

        public void Clear()
        {
            payload.Clear();
        }

        /// <summary>
        /// 找出所有满足<pre>L <= x < R</pre> 的 <pre>x</pre>
        /// </summary>
        /// <param name="L"></param>
        /// <param name="R"></param>
        /// <returns></returns>
        public T[] FindWithin(T L, T R)
        {
            int il = payload.BinarySearch(L), ir = payload.BinarySearch(R);
            // 经实测, 二分查找不保证找到重复元素首次出现的位置. 因而需要向前找到首次出现.
            while (il > 0)
            {
                if (object.Equals(payload[il - 1], L))
                {
                    il -= 1;
                }
                else break;
            }
            // 经实测, 二分查找不保证找到重复元素首次出现的位置. 因而需要向前找到首次出现.
            while (ir >= il)
            {
                if (object.Equals(payload[ir - 1], R))
                {
                    ir -= 1;
                }
                else break;
            }
            int count = ir - il;
            return payload.GetRange(il, count).ToArray();
        }

        public T[] Values { get { return payload.ToArray(); } }
    }
}
