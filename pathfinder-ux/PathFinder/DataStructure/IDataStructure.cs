using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.DataStructure
{
    interface IDataStructure
    {
        void Init();
        void CopyValuesFrom(IDataStructure d);
    }
}
