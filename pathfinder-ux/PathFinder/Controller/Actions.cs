using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.Controller.Actions
{

    abstract class Action
    {
        long SN { get; set; }
        ActionPriority Priority { get; set; }
    }

    enum ActionPriority
    {
        IO = 0,
        UI = 1,
        Draw = 2,
    }

    abstract class FileAction : Action
    {
    }
    
    abstract class GeometryAction : Action
    {

    }

    abstract class DrawingAction : Action
    {

    }

    class LoadSlide : FileAction
    {
        public int W;
        public int H;

        public string Path { get; set; }
    }

    class CloseSlide : FileAction
    {

    }

    class Move : GeometryAction
    {
        public int dX;
        public int dY;
    }

    class Zoom : GeometryAction
    {
        public int nScroll;
        public int X;
        public int Y;
    }

    class Resize : GeometryAction
    {
        public int W;
        public int H;
    }

    class DrawPolygonV : DrawingAction
    {
        public int X;
        public int Y;
    }

    class DrawPolygonFirstV : DrawingAction
    {
        public int X;
        public int Y;
    }

    class DrawPolygonFinish: DrawingAction
    {

    }
}
