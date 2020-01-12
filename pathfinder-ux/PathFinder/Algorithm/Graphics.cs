using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.Algorithm
{
    class Point
    {
        public object Info { get; set; }

        public Point(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public double X { get; private set; }
        public double Y { get; private set; }
    }

    class Line
    {
        public double A { get; private set; }
        public double B { get; private set; }
        public double C { get; private set; }

        public Line(double a, double b, double c)
        {
            A = a;
            B = b;
            C = c;
        }
        public object Info { get; set; }

        public double ComputeX(double Y)
        {
            return -(B * Y + C) / A;
        }

        public double ComputeY(double X)
        {
            return -(A * X + C) / B;
        }

        public int AtLineSide(double X, double Y)
        {
            return Math.Sign(A * X + B * Y + C);
        }
    }

    class Edge
    {
        public double XA { get; private set; }
        public double XB { get; private set; }
        public double YA { get; private set; }
        public double YB { get; private set; }

        public Point PA 
        { 
            get { return new Point(XA, YA); } 
            set { XA = value.X; YA = value.Y; }
        }

        public Line L { get; private set; }

        public Point PB 
        { 
            get { return new Point(XB, YB); }
            set { XB = value.X; YB = value.Y; }
        }

        public Edge(double xa, double ya, double xb, double yb)
        {
            XA = xa;
            YA = ya;
            XB = xb;
            YB = yb;
            var A = PA.Y - PB.Y;
            var B = PB.X - PA.X;
            var C = PA.X * PB.Y - PB.X * PA.Y;
            L = new Line(A, B, C);
        }

        public Edge(Point pa, Point pb)
        {
            PA = pa;
            PB = pb;
        }

        public object Info { get; set; }
    }

    class Rectangle
    {
        public double Left { get; private set; }
        public double Right { get; private set; }
        public double Top { get; private set; }
        public double Bottom { get; private set; }

        private const int cTOP = 0b_1000;
        private const int cBOTTOM = 0b_0100;
        private const int cRIGHT = 0b_0010;
        private const int cLEFT = 0b_0001;
        private const int cINSIDE = 0b_0000;

        public Point LeftTop
        {
            get { return new Point(Left, Top); }
        }

        public Point RightBottom
        {
            get { return new Point(Right, Bottom); }
        }

        public Rectangle(double left, double right, double top, double bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public int EncodePoint(Point p)
        {
            int val = cINSIDE;
            if (p.X < Left)
            {
                val = val | cLEFT;
            }
            else if (p.X > Right)
            {
                val = val | cRIGHT;
            }
            if (p.Y < Top)
            {
                val = val | cTOP;
            }
            else if (p.Y > Bottom)
            {
                val = val | cBOTTOM;
            }
            return val;
        }

        public Edge ClipByWindow(Edge e)
        {
            Point pA = e.PA;
            Point pB = e.PB;
            int cA = EncodePoint(pA);
            int cB = EncodePoint(pB);

            if (cB == cINSIDE)
            {
                // [一] 两端点都在框内, 则线段在框内.
                if (cA == cINSIDE)
                {
                    return e;
                }

                // [三] 如果A在框外而B在框内, 则交换A、B两个符号. 写代码更方便.
                else
                {
                    int _ = cA; cA = cB; cB = _;
                    Point __ = pA; pA = pB; pB = __;
                }
            }

            // [二] 两端点在框的同侧, 则线段不经过任何边.
            // 什么叫「同侧」? 举个例子, 
            // 「左上」和「右上」都带有「上」的属性, 则左上和右上是同侧的.
            if ((cA & cB) != 0)
            {
                return null;
            }

            Line line = e.L;

            // [三] A点在框内, B点在框外. B点有8种可能的位置, 需分类讨论.
            // 注意前面已经考虑了B在里A在外, 和AB都在里的情况
            if (cA == cINSIDE)
            {
                var cLR = cB & 0b_11;
                var cTB = cB & 0b_1100;
                // [1~3] 点B在左侧的3种情况
                if (cLR == cLEFT) 
                {
                    // [1] 一定和左边有交点
                    if (cTB == cINSIDE) 
                    {
                        pB = new Point(Left, line.ComputeY(Left));
                    }

                    // [2] 点B落在左上角, 故可能和左边或上边有交点
                    else if (cTB == cTOP) 
                    {
                        // [2.1] 和左边交于上延长线. 故必然和上边相交.
                        if (line.ComputeY(Left) < Top) 
                        {
                            pB = new Point(line.ComputeX(Top), Top);
                        }

                        // [2.2] 和左边交于左边
                        else
                        {
                            pB = new Point(Left, line.ComputeY(Left));
                        }
                    }

                    // [3] 点B落在左下角, 可能和左边或下边有交点
                    else
                    {
                        // [3.1] 和左边交于下延长线. 故必然和下边相交.
                        if (line.ComputeY(Left) > Bottom) 
                        {
                            pB = new Point(line.ComputeX(Bottom), Bottom);
                        }

                        // [3.2] 和左边交于左边
                        else
                        {
                            pB = new Point(Left, line.ComputeY(Left));
                        }
                    }
                }

                // [4~6] 点B在右侧的3种情况
                else if (cLR == cRIGHT) 
                {
                    // [4] 一定和右边有交点
                    if (cTB == cINSIDE) 
                    {
                        pB = new Point(Right, line.ComputeY(Right));
                    }

                    // [5] 点B落在右上角, 可能和右边或上边有交点
                    else if (cTB == cTOP)
                    {
                        // [5.1] 和右边交于上延长线. 故必然和上边相交.
                        if (line.ComputeY(Right) < Top) 
                        {
                            pB = new Point(line.ComputeX(Top), Top);
                        }
                        // [5.2] 和右边交于右边
                        else
                        {
                            pB = new Point(Right, line.ComputeY(Right));
                        }
                    }

                    // [6] 点B落在右下角, 可能和右边或下边有交点
                    else if (cTB == cBOTTOM) 
                    {
                        // [6.1] 和右边交于下延长线. 故必然和下边相交.
                        if (line.ComputeY(Right) > Bottom)
                        {
                            pB = new Point(line.ComputeX(Bottom), Bottom);
                        }
                        // [6.2] 和右边交于右边
                        else
                        {
                            pB = new Point(Right, line.ComputeY(Right));
                        }
                    }
                }

                // [7~8] cLR == cINSIDE, 即点B在正上或正下方
                else
                {
                    // [7] 只和上边有交点
                    if (cTB == cTOP) 
                    {
                        pB = new Point(line.ComputeX(Top), Top);
                    }
                    // [8] 只和下边有交点
                    else if (cTB == cBOTTOM) 
                    {
                        pB = new Point(line.ComputeX(Bottom), Bottom);
                    }
                }
            }

            // [四] A, B两点都在框外.
            else
            {
                int slt = line.AtLineSide(Left, Top);
                int srt = line.AtLineSide(Right, Top);
                int slb = line.AtLineSide(Left, Bottom);
                int srb = line.AtLineSide(Right, Bottom);
                int s = slt + srt + slb + srb;
                int abs = Math.Abs(s);

                // [1] 四点同侧, 则线段不经过任何边.
                if (abs == 4)
                {
                    return null;
                }

                // [2] 符号和的绝对值为3, 则线段经过一个顶点.
                else if (abs == 3)
                {
                    if (slt == 0)
                    {
                        pA = pB = new Point(Left, Top);
                    }
                    else if (srt == 0)
                    {
                        pA = pB = new Point(Right, Top);
                    }
                    else if (slb == 0)
                    {
                        pA = pB = new Point(Left, Bottom);
                    }
                    else if (srb == 0)
                    {
                        pA = pB = new Point(Right, Bottom);
                    }
                }

                // [3] 当符号和为2时,
                else if (abs == 2)
                {
                    // [3.1] 线段和矩形某一边重合
                    if ((slt & srt & slb & srb) == 0)
                    {
                        // [3.1.1] 和上边重合
                        if (slt == 0 && srt == 0)
                        {
                            pA = new Point(Left, Top);
                            pB = new Point(Right, Top);
                        }
                        // [3.1.2] 和右边重合
                        if (srt == 0 && srb == 0)
                        {
                            pA = new Point(Right, Top);
                            pB = new Point(Right, Bottom);
                        }
                        // [3.1.3] 和下边重合
                        if (srb == 0 && slb == 0)
                        {
                            pA = new Point(Right, Bottom);
                            pB = new Point(Left, Bottom);
                        }
                        // [3.1.4] 和左边重合
                        if (slb == 0 && slt == 0)
                        {
                            pA = new Point(Left, Bottom);
                            pB = new Point(Left, Top);
                        }
                    }
                    // [3.2] 3负1正. 线段切割矩形的某一角
                    else
                    {
                        // [3.2] 3正1负. 转化为3负1正
                        if (s == 2)
                        {
                            slt = -slt;
                            srt = -srt;
                            slb = -slb;
                            srb = -srb;
                        }

                        // [3.2.1] 左上角和另外三个顶点不在直线同一侧.
                        // i.e. 线段切割矩形的左上角.
                        if (slt > 0)
                        {
                            pA = new Point(Left, line.ComputeY(Left));
                            pB = new Point(line.ComputeX(Top), Top);
                        }
                        // [2.2.2] 切割矩形的右上角
                        else if (srt > 0)
                        {
                            pA = new Point(Right, line.ComputeY(Right));
                            pB = new Point(line.ComputeX(Top), Top);
                        }
                        // [2.2.3] 切割矩形的右下角
                        else if (srb > 0)
                        {
                            pA = new Point(Right, line.ComputeY(Right));
                            pB = new Point(line.ComputeX(Bottom), Bottom);
                        }
                        // [2.2.4] 切割矩形的左下角
                        else if (slb > 0)
                        {
                            pA = new Point(Left, line.ComputeY(Left));
                            pB = new Point(line.ComputeX(Bottom), Bottom);
                        }
                    }
                }

                // [3] 符号和的绝对值为1, 则经过一条边和边对面一个顶点
                else if (abs == 1)
                {
                    // [3.1] 经过左上角
                    if (slt == 0)
                    {
                        pA = new Point(Left, Top);
                        // 经过右边 (右下角和左下角在直线的同一侧)
                        if (srb == slb)
                        {
                            pB = new Point(Right, line.ComputeY(Right));
                        }
                        // 经过下边 (右下角和右上角在直线的同一侧)
                        else
                        {
                            pB = new Point(line.ComputeX(Bottom), Bottom);
                        }
                    }
                    // [3.2] 经过右上角
                    else if (srt == 0)
                    {
                        pA = new Point(Right, Top);
                        // 经过左边 (左下角和右下角在直线同一侧)
                        if (slb == srb)
                        {
                            pB = new Point(Left, line.ComputeY(Left));
                        }
                        // 经过下边 (左下角和左上角在直线同一侧)
                        else
                        {
                            pB = new Point(line.ComputeX(Bottom), Bottom);
                        }
                    }
                    // [3.3] 经过右下角
                    else if (srb == 0)
                    {
                        pA = new Point(Right, Bottom);
                        // 经过左边 (左上角和右上角在直线同一侧)
                        if (slt == srt)
                        {
                            pB = new Point(Left, line.ComputeY(Left));
                        }
                        // 经过上边 (左上角和左下角在直线同一侧)
                        else
                        {
                            pB = new Point(line.ComputeX(Top), Top);
                        }
                    }
                    // [3.4] 经过左下角
                    else
                    {
                        pA = new Point(Left, Bottom);
                        // 经过上边 (右上角和右下角在直线同一侧)
                        if (srt == srb)
                        {
                            pB = new Point(line.ComputeX(Top), Top);
                        }
                        // 经过右边 (右上角和左上角在直线同一侧)
                        else
                        {
                            pB = new Point(Right, line.ComputeY(Right));
                        }
                    }
                }

                // [4] 符号和的绝对值为0, 则0, 0, -, + 或 +, +, -, -
                else
                {
                    // [4.1] 和对角线重合
                    if (slt == 0)
                    {
                        pA = new Point(Left, Top);
                        pB = new Point(Right, Bottom);
                    }
                    // [4.2] 和反对角线重合
                    else if (srt == 0)
                    {
                        pA = new Point(Right, Top);
                        pB = new Point(Left, Bottom);
                    }
                    // [4.3] 经过左右两边 (左上角和右上角在直线同一侧)
                    else if (slt == srt)
                    {
                        pA = new Point(Left, line.ComputeY(Left));
                        pB = new Point(Right, line.ComputeY(Right));
                    }
                    // [4.4] 经过上下两边 (左上角和左下角在直线同一侧)
                    else
                    {
                        pA = new Point(line.ComputeX(Top), Top);
                        pB = new Point(line.ComputeX(Bottom), Bottom);
                    }
                }
            }

            return new Edge(pA, pB);
        }
    }

    class Polygon
    {
        private Point head;
        private double lastX;
        private double lastY;
        private List<Edge> edges;
        
        public Polygon(double headX, double headY)
        {
            head = new Point(lastX = headX, lastY = headY);
            edges = new List<Edge>();
        }

        public void AddPoint(double X, double Y)
        {
            edges.Add(new Edge(lastX, lastY, lastX = X, lastY = Y));
        }
    }

}
