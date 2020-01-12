using PathFinder.Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.Layer
{
    interface ILayer: IDisposable
    {
        void Open(string path, object info);
        void Close();
        object LoadRegion(SceneGeometry sg);
        Task<object> AsyncLoadRegion(SceneGeometry sg);
    }
}
