using PathFinder.Controller;
using PathFinder.Algorithm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PathFinder.Scene
{

    class PolylineScene
    {
        private Dictionary<long, Polyline> polylines;

        public PolylineScene()
        {
            Clear();
        }

        public void AddPoint(long id, double x, double y)
        {
            if (!polylines.ContainsKey(id))
            {
                polylines.Add(id, new Polyline());
            }
            var pl = polylines[id];
            pl.AddPoint(x, y);
        }

        public void MakePolygon(long id)
        {
            if (polylines.ContainsKey(id))
            {
                var pl = polylines[id];
                var head = pl.Points[0];
                pl.AddPoint(head.Item1, head.Item2);
            }
        }

        public void RemovePolygon(long polygonId)
        {
            polylines.Remove(polygonId);
        }

        public void Clear()
        {
            polylines = new Dictionary<long, Polyline>();
        }

        public void Close()
        {
            
        }

        public List<Drawable> LoadRegionShapes(SceneGeometry sg)
        {
            double left, top, right, bottom;
            (left, top, right, bottom) = (sg.X, sg.Y, sg.X + sg.ToActualPixel(sg.OutW), sg.Y + sg.ToActualPixel(sg.OutH));
            //Console.WriteLine($"Loading w={right - left}(from {left} to {right}), h={bottom - top}(from {top} to {bottom})");
            var toShow = new List<Drawable>();

            foreach (var _ in polylines)
            {
                var id = _.Key;
                var pl = _.Value;

                if (pl.NumPoints == 1)
                {
                    double x, y;
                    (x, y) = (pl.Points[0].Item1, pl.Points[0].Item2);

                    if (Graphics.EncodePoint(left, top, right, bottom, x, y) == 0)
                    {
                        toShow.Add(new Point(x - left, y - top, id));
                    }
                }
                else if (pl.NumPoints > 1)
                {
                    for (int i = 0; i < pl.NumPoints - 1; i = i + 1)
                    {
                        double xa, ya, xb, yb;
                        (xa, ya, xb, yb) = (pl.Points[i].Item1, pl.Points[i].Item2, pl.Points[i + 1].Item1, pl.Points[i + 1].Item2);

                        Graphics.ClipByWindow(
                            left, top, right, bottom,
                            xa, ya, xb, yb,
                            out double x1, out double y1, out double x2, out double y2
                        );
                        //Console.WriteLine($"Rectangle({left - left:0.0}, {top - top:0.0}, {right - left:0.0}, {bottom - top:0.0}) " +
                        //    $"Cut({xa - left:0.0}, {ya - top:0.0} -> {xb - left:0.0}, {yb - top:0.0}), " +
                        //    $"Yield({x1 - left:0.0}, {y1 - top:0.0} -> {x2 - left:0.0}, {y2 - top:0.0})");

                        if (!double.IsNaN(x1))
                        {
                            if (x1 == y1 && x2 == y2)
                            {
                                toShow.Add(new Point(x1 - left, y1 - top, id));
                            }
                            else
                            {
                                toShow.Add(new LineSegment(x1 - left, y1 - top, x2 - left, y2 - top, id));
                            }
                        }
                    }
                }
            }

            return toShow;
        }

        public void Open(string path, object info)
        {
            
        }

        public void Save(string path, object info)
        {

        }
    }
}
