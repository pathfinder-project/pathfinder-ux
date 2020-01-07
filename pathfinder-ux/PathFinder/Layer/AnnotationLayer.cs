using PathFinder.DataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.Layer
{
    using Polygon = List<PixelCoordinate>;
    class AnnotationLayer : ILayer
    {
        private List<Polygon> polygons;

        public AnnotationLayer()
        {
            polygons = new List<Polygon>();
        }

        public void AddPolygon(Polygon p)
        {
            polygons.Add(p);
        }

        public void Clear()
        {
            polygons.Clear();
        }

        public Task<object> AsyncLoadRegion(SceneGeometry sg)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public object LoadRegion(SceneGeometry sg)
        {
            throw new NotImplementedException();
        }

        public void Open(string path, object info)
        {
            throw new NotImplementedException();
        }
    }
}
