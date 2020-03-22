using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PathFinder.Scene
{
    abstract class Drawable
    {
        public object info { get; set; }
    }

    class Image : Drawable
    {
        public byte[] buf; 
    }

    class Point : Drawable
    {
        public double x { get; }
        public double y { get; }

        public Point(double x, double y)
        {
            (this.x, this.y) = (x, y);
        }

        public Point(double x, double y, object info)
        {
            (this.x, this.y, this.info) = (x, y, info);
        }
    }

    class LineSegment : Drawable
    {
        public double xa { get; }
        public double ya { get; }
        public double xb { get; }
        public double yb { get; }

        public LineSegment(double xa, double ya, double xb, double yb)
        {
            (this.xa, this.ya, this.xb, this.yb) = (xa, ya, xb, yb);
        }

        public LineSegment(double xa, double ya, double xb, double yb, object info)
        {
            (this.xa, this.ya, this.xb, this.yb, this.info) = (xa, ya, xb, yb, info);
        }
    }
}
