using PathFinder.Algorithm;
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
        public double W;
        public double H;

        public string Path { get; set; }
    }

    class CloseSlide : FileAction
    {

    }

    class Move : GeometryAction
    {
        public double dX;
        public double dY;
    }

    class Zoom : GeometryAction
    {
        public int nScroll;
        public double X;
        public double Y;
    }

    class ThumbJumpAction : GeometryAction
    {
        public double CenterX;
        public double CenterY;
    }

    class Resize : GeometryAction
    {
        public double W;
        public double H;
    }

    enum DrawPolygonState
    {
        ForceCancel = -1,
        PlacingVertex = 0,
        TryFinish = 1,
    }

    class DrawPolygonV : DrawingAction
    {
        public double X;
        public double Y;
        public DrawPolygonState dps;
        public long Id;
    }
}
