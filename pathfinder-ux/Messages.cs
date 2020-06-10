using System;

namespace PathFinder
{
    using Ctx = Tuple<int, int?, int?>;

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

    class AddVertexMessage : PolylineMessage
    {
        public double x; // 屏幕上的横坐标
        public double y; // 屏幕上的纵坐标
        public int idv; // 由前端分配的id
        public int? prev = null; // 上一个点。如果非null，则把新点和上一个点连起来
    }

    class ConnectVertexMessage : PolylineMessage
    {
        public int idv1;
        public int idv2;
    }

    class MoveVertexMessgae : PolylineMessage
    {
        public int idv;
        public double dx; // 屏幕上的x偏移量
        public double dy; // 屏幕上的y偏移量
    }

    class DeleteVertexMessage: PolylineMessage
    {
        public int idv;
    }


    #endregion

    #region 计算消息
    abstract class ComputeMessage : Message { }

    class Ki67Message : ComputeMessage 
    {
        public int idv;
    }
    #endregion
}
