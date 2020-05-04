using System;

namespace PathFinder
{

    abstract class Message { }

    #region 文件消息
    abstract class FileMessage : Message { }

    class LoadSlideMessage : FileMessage
    {
        public double WCanvas;
        public double HCanvas;
        public string Path { get; set; }
    }

    class CloseSlideMessage : FileMessage
    {

    }
    #endregion

    #region 视野消息
    abstract class ViewportMessage : Message { }

    class MoveMessage : ViewportMessage
    {
        public double dXScreen;
        public double dYScreen;
    }

    class ZoomMessage : ViewportMessage
    {
        public int nScroll;
        public double XScreen;
        public double YScreen;
    }

    class ThumbJumpMessage : ViewportMessage
    {
        public double CenterXScreen;
        public double CenterYScreen;
    }

    class ResizeMessage : ViewportMessage
    {
        public double WScreen;
        public double HScreen;
    }
    #endregion

    #region 画笔消息
    abstract class PolylineMessage : Message { }

    class VertexMessage : PolylineMessage
    {
        public double X;
        public double Y;
        public uint IdV;
    }

    class EdgeMessgae : PolylineMessage
    {
        public double X;
        public double Y;
        public uint IdV1;
        public uint IdV2;
    }

    class MoveVertexMessgae : PolylineMessage
    {
        public uint IdV;
        public double dXScreen;
        public double dYScreen;
    }

    class DeleteVertexMessage: PolylineMessage
    {
        public uint IdV;
    }

    
    #endregion
}
