using PathFinder.Controller;
using PathFinder.Algorithm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PathFinder.Layer
{

    class PolygonLayer : ILayer
    {
        private List<Polygon> polygons;

        public PolygonLayer()
        {
            Clear();
        }

        public Polygon AddPolygon()
        {
            var polygon = new Polygon();
            polygons.Add(polygon);
            return polygon;
        }

        public void RemovePolygon(Polygon p)
        {
            polygons.Remove(p);
        }

        public void Clear()
        {
            polygons = new List<Polygon>();
        }

        public Task<object> AsyncLoadRegion(SceneGeometry sg)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            
        }

        public void Dispose()
        {
            Close();
        }

        public object LoadRegion(SceneGeometry sg)
        {
            var toShow = new List<object>();
            var rect = new Rectangle(
                sg.X,
                sg.Y,
                sg.X + sg.ToActualPixel(sg.W),
                sg.Y + sg.ToActualPixel(sg.H)
                );
            foreach (var polygon in polygons)
            {
                Point p = polygon.head;
                p.Info = polygon;
                toShow.Add(p);
                foreach (var edge in p.edges)
                {

                }
            }
        }

        public void Open(string path, object info)
        {
            
        }

        public void Save(string path, object info)
        {

        }
    }
}
