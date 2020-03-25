using PathFinder.Algorithm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PathFinder.Scene
{

    class PolylineAnnotation
    {
        private Dictionary<uint, Vertex> V;
        private HashSet<ulong> E;

        public PolylineAnnotation()
        {
            V = new Dictionary<uint, Vertex>();
            E = new HashSet<ulong>();
            Clear();
        }

        public void Bullet(double x_slide, double y_slide, uint idv)
        {
            if (idv == 0)
            {
                return;
            }
            Vertex v = new Vertex(idv);
            v.x = x_slide;
            v.y = y_slide;
            V.Add(idv, v);
        }

        public void Stick(double x2_slide, double y2_slide, 
            uint idv1, uint idv2)
        {
            if ((idv1 == 0) && (idv2 == 0))
            {
                return;
            }
            Vertex v1 = V[idv1], v2 = null;
            if (V.ContainsKey(idv2))
            {
                v2 = V[idv2];
            }
            else
            {
                v2 = new Vertex(idv2);
            }
            v2.x = x2_slide;
            v2.y = y2_slide;
            V.Add(idv2, v2);
            EncodeEdgeId(idv1, idv2, out ulong ide);
            E.Add(ide);
        }

        public void InsertVertex(double x_slide, double y_slide,
            Guid idv, Guid ide0, Guid ide1, Guid ide2)
        {

        }

        public void MoveVertex(double x_slide, double y_slide, 
            Guid idv)
        {

        }

        public void EraseVertex(Guid idv)
        {

        }
        
        private void EncodeEdgeId(uint idv1, uint idv2, out ulong ide)
        {
            ulong _idv1 = (ulong)idv1;
            ulong _idv2 = (ulong)idv2;
            if (idv1 > idv2)
            {
                ulong _ = _idv1;
                _idv1 = _idv2;
                _idv2 = _;
            }
            ide = (_idv1 << 32) | _idv2;
        }

        private void DecodeEdgeId(ulong ide, out uint idv1, out uint idv2)
        {
            idv1 = (uint)(ide >> 32);
            idv2 = (uint)(ide & 0x0000_0000_ffff_ffff);
        }

        public List<DisplayParameter> LoadRegionShapes(Viewport vp, double margin = 2)
        {
            double left, top, right, bottom;
            left = vp.X - margin;
            top = vp.Y - margin;
            right = vp.X + vp.ToActualPixel(vp.OutW) + margin;
            bottom = vp.Y + vp.ToActualPixel(vp.OutH) + margin;
            var toShow = new List<DisplayParameter>();

            foreach (var v in V.Values)
            {
                double x = v.x, y = v.y;
                uint idv = v.id;
                if (EncodePoint(left, top, right, bottom, x, y) == cINSIDE)
                {
                    toShow.Add(new BulletParameter(idv, x, y));
                }
            }

            foreach (var ide in E)
            {
                DecodeEdgeId(ide, out uint idv1, out uint idv2);
                Vertex v1 = V[idv1], v2 = V[idv2];
                double xa = v1.x, ya = v1.y, xb = v2.x, yb = v2.y;
                ClipByWindow(
                    left, top, right, bottom,
                    xa, ya, xb, yb,
                    out double x1, out double y1, out double x2, out double y2
                );
                if (!double.IsNaN(x1))
                {
                    if (x1 != x2 || y1 != y2)
                    {
                        toShow.Add(new StickParameter(ide, x1, y1, x2, y2));
                    }
                }
            }

            return toShow;
        }

        public void Clear()
        {

        }

        public void Open(string path, object info)
        {
            
        }

        public void Save(string path, object info)
        {

        }

        private const int cLEFT = 0b_0001;
        private const int cRIGHT = 0b_0010;
        private const int cBOTTOM = 0b_0100;
        private const int cTOP = 0b_1000;
        private const int cINSIDE = 0b_0000;

        /// <summary>
        /// 计算点(x, y)相对于矩形(left, top, right, bottom)的位置关系.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int EncodePoint(double left, double top, double right, double bottom, double x, double y)
        {
            int val = cINSIDE;
            if (x < left)
            {
                val = val | cLEFT;
            }
            else if (x > right)
            {
                val = val | cRIGHT;
            }
            if (y < top)
            {
                val = val | cTOP;
            }
            else if (y > bottom)
            {
                val = val | cBOTTOM;
            }
            return val;
        }

        public static int GetLineSide(double A, double B, double C, double x, double y)
        {
            return Math.Sign(A * x + B * y + C);
        }

        public static void SolveCoordinate(double A, double B, double C, out double x, double y)
        {
            x = -(B * y + C) / A;
        }

        public static void SolveCoordinate(double A, double B, double C, double x, out double y)
        {
            y = -(A * x + C) / B;
        }

        /// <summary>
        /// 从两点(xa, ya), (xb, yb)得到直线的一般方程Ax+By+C=0;
        /// </summary>
        /// <param name="xa"></param>
        /// <param name="ya"></param>
        /// <param name="xb"></param>
        /// <param name="yb"></param>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        public static void GetLineGeneralForm(
            double xa, double ya, double xb, double yb,
            out double A, out double B, out double C)
        {
            A = ya - yb;
            B = xb - xa;
            C = xa * yb - xb * ya;
        }

        /// <summary>
        /// 计算线段(xa, ya, xb, yb)在矩形(left, top, right, bottom)内部或边界上的部分, 即矩形对线段的裁剪.
        /// 裁剪出来的线段记为(x1, y1, x2, y2).
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        /// <param name="xa"></param>
        /// <param name="ya"></param>
        /// <param name="xb"></param>
        /// <param name="yb"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        public static void ClipByWindow(
            double left, double top, double right, double bottom,
            double xa, double ya, double xb, double yb,
            out double x1, out double y1, out double x2, out double y2)
        {
            x1 = y1 = x2 = y2 = double.NaN;
            int ca = EncodePoint(left, top, right, bottom, xa, ya);
            int cb = EncodePoint(left, top, right, bottom, xb, yb);

            if (cb == cINSIDE)
            {
                // [一] 两端点都在框内, 则线段在框内.
                if (ca == cINSIDE)
                {
                    (x1, y1, x2, y2) = (xa, ya, xb, yb);
                    return;
                }

                // [三] 如果A在框外而B在框内, 则交换A、B的坐标和编码. 写代码更方便.
                else
                {
                    int _ = ca; ca = cb; cb = _;
                    double tmp = xa; xa = xb; xb = tmp;
                    tmp = ya; ya = yb; yb = tmp;
                }
            }

            // [二] 两端点在框的同侧, 则线段不经过任何边.
            // 什么叫「同侧」? 举个例子, 
            // 「左上」和「右上」都带有「上」的属性, 则左上和右上是同侧的.
            if ((ca & cb) != 0)
            {
                return;
            }

            GetLineGeneralForm(xa, ya, xb, yb, out double A, out double B, out double C);

            // [三] A点在框内, B点在框外. B点有8种可能的位置, 需分类讨论.
            // 注意前面已经考虑了B在里A在外, 和AB都在里的情况
            if (ca == cINSIDE)
            {
                (x1, y1) = (xa, ya);

                var cLR = cb & 0b_11;
                var cTB = cb & 0b_1100;
                // [1] 点B在左侧
                if (cLR == cLEFT)
                {
                    // [1.1] 和左边相交
                    SolveCoordinate(A, B, C, x2 = left, out y2);

                    // [1.2] 和上边相交.
                    if (y2 < top)
                    {
                        SolveCoordinate(A, B, C, out x2, y2 = top);
                    }
                    // [1.3] 和下边相交.
                    else if (y2 > bottom)
                    {
                        SolveCoordinate(A, B, C, out x2, y2 = bottom);
                    }
                }

                // [2] 点B在右侧.
                else if (cLR == cRIGHT)
                {
                    // [2.1] 和右边相交
                    x2 = right;
                    SolveCoordinate(A, B, C, x2 = right, out y2);

                    // [2.2] 和上边相交
                    if (y2 < top)
                    {
                        SolveCoordinate(A, B, C, out x2, y2 = top);
                    }

                    // [2.3] 和下边相交
                    else if (y2 > bottom)
                    {
                        SolveCoordinate(A, B, C, out x2, y2 = bottom);
                    }
                }

                // [3] 点B在正上方. 和上边相交
                else if (cTB == cTOP)
                {
                    SolveCoordinate(A, B, C, out x2, y2 = top);
                }

                // [4] 点B在正下方. 和下边相交
                else if (cTB == cBOTTOM)
                {
                    SolveCoordinate(A, B, C, out x2, y2 = bottom);
                }
            }

            // [四] A, B两点都在框外.
            else
            {
                int slt = GetLineSide(A, B, C, left, top);
                int srt = GetLineSide(A, B, C, right, top);
                int slb = GetLineSide(A, B, C, left, bottom);
                int srb = GetLineSide(A, B, C, right, bottom);
                int s = slt + srt + slb + srb;
                int abs = Math.Abs(s);

                // [1] 四点同侧, 则线段不经过任何边.
                if (abs == 4)
                {
                    x1 = y1 = x2 = y2 = double.NaN;
                    return;
                }

                // [2] 符号和的绝对值为3, 则线段经过一个顶点.
                else if (abs == 3)
                {
                    if (slt == 0)
                    {
                        (x1, y1, x2, y2) = (left, top, left, top);
                    }
                    else if (srt == 0)
                    {
                        (x1, y1, x2, y2) = (right, top, right, top);
                    }
                    else if (slb == 0)
                    {
                        (x1, y1, x2, y2) = (left, bottom, left, bottom);
                    }
                    else if (srb == 0)
                    {
                        (x1, y1, x2, y2) = (right, bottom, right, bottom);
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
                            (x1, y1, x2, y2) = (left, top, right, top);
                        }
                        // [3.1.2] 和右边重合
                        if (srt == 0 && srb == 0)
                        {
                            (x1, y1, x2, y2) = (right, top, right, bottom);
                        }
                        // [3.1.3] 和下边重合
                        if (srb == 0 && slb == 0)
                        {
                            (x1, y1, x2, y2) = (right, bottom, left, bottom);
                        }
                        // [3.1.4] 和左边重合
                        if (slb == 0 && slt == 0)
                        {
                            (x1, y1, x2, y2) = (left, bottom, left, top);
                        }
                    }
                    // [3.2] 3负1正. 线段切割矩形的某一角
                    else
                    {
                        // [3.2] 3正1负. 转化为3负1正
                        if (s == 2)
                        {
                            (slt, srt, srb, slb) = (-slt, -srt, -srb, -slb);
                        }

                        // [3.2.1] 左上角和另外三个顶点不在直线同一侧.
                        // i.e. 线段切割矩形的左上角.
                        if (slt > 0)
                        {
                            SolveCoordinate(A, B, C, x1 = left, out y1);
                            SolveCoordinate(A, B, C, out x2, y2 = top);
                        }
                        // [2.2.2] 切割矩形的右上角
                        else if (srt > 0)
                        {
                            SolveCoordinate(A, B, C, x1 = right, out y1);
                            SolveCoordinate(A, B, C, out x2, y2 = top);
                        }
                        // [2.2.3] 切割矩形的右下角
                        else if (srb > 0)
                        {
                            SolveCoordinate(A, B, C, x1 = right, out y1);
                            SolveCoordinate(A, B, C, out x2, y2 = bottom);
                        }
                        // [2.2.4] 切割矩形的左下角
                        else if (slb > 0)
                        {
                            SolveCoordinate(A, B, C, x1 = left, out y1);
                            SolveCoordinate(A, B, C, out x2, y2 = bottom);
                        }
                    }
                }

                // [3] 符号和的绝对值为1, 则经过一条边和边对面一个顶点
                else if (abs == 1)
                {
                    // [3.1] 经过左上角
                    if (slt == 0)
                    {
                        (x1, y1) = (left, top);
                        // 经过右边 (右下角和左下角在直线的同一侧)
                        if (srb == slb)
                        {
                            SolveCoordinate(A, B, C, x2 = right, out y2);
                        }
                        // 经过下边 (右下角和右上角在直线的同一侧)
                        else
                        {
                            SolveCoordinate(A, B, C, out x2, y2 = bottom);
                        }
                    }
                    // [3.2] 经过右上角
                    else if (srt == 0)
                    {
                        (x1, y1) = (right, top);
                        // 经过左边 (左下角和右下角在直线同一侧)
                        if (slb == srb)
                        {
                            SolveCoordinate(A, B, C, x2 = left, out y2);
                        }
                        // 经过下边 (左下角和左上角在直线同一侧)
                        else
                        {
                            SolveCoordinate(A, B, C, out x2, y2 = bottom);
                        }
                    }
                    // [3.3] 经过右下角
                    else if (srb == 0)
                    {
                        (x1, y1) = (right, bottom);
                        // 经过左边 (左上角和右上角在直线同一侧)
                        if (slt == srt)
                        {
                            SolveCoordinate(A, B, C, x2 = left, out y2);
                        }
                        // 经过上边 (左上角和左下角在直线同一侧)
                        else
                        {
                            SolveCoordinate(A, B, C, out x2, y2 = top);
                        }
                    }
                    // [3.4] 经过左下角
                    else
                    {
                        (x1, y1) = (left, bottom);
                        // 经过上边 (右上角和右下角在直线同一侧)
                        if (srt == srb)
                        {
                            SolveCoordinate(A, B, C, out x2, y2 = top);
                        }
                        // 经过右边 (右上角和左上角在直线同一侧)
                        else
                        {
                            SolveCoordinate(A, B, C, x2 = right, out y2);
                        }
                    }
                }

                // [4] 符号和的绝对值为0, 则0, 0, -, + 或 +, +, -, -
                else
                {
                    // [4.1] 和对角线重合
                    if (slt == 0)
                    {
                        (x1, y1, x2, y2) = (left, top, right, bottom);
                    }
                    // [4.2] 和反对角线重合
                    else if (srt == 0)
                    {
                        (x1, y1, x2, y2) = (right, top, left, bottom);
                    }
                    // [4.3] 经过左右两边 (左上角和右上角在直线同一侧)
                    else if (slt == srt)
                    {
                        SolveCoordinate(A, B, C, x1 = left, out y1);
                        SolveCoordinate(A, B, C, x2 = right, out y2);
                    }
                    // [4.4] 经过上下两边 (左上角和左下角在直线同一侧)
                    else
                    {
                        SolveCoordinate(A, B, C, out x1, y1 = top);
                        SolveCoordinate(A, B, C, out x2, y2 = bottom);
                    }
                }
            }
        }
    }

    abstract class DisplayParameter { }

    class BulletParameter : DisplayParameter
    {
        public uint id;
        public double x;
        public double y;

        public BulletParameter(uint id, double x, double y)
        {
            this.id = id;
            this.x = x;
            this.y = y;
        }
    }

    class StickParameter : DisplayParameter
    {
        public ulong id;
        public double x1;
        public double y1;
        public double x2;
        public double y2;

        public StickParameter(ulong id, double x1, double y1, double x2, double y2)
        {
            this.id = id;
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
        }
    }
}
