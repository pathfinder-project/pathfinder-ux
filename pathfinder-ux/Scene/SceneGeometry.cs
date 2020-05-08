using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.Scene
{
    class SceneGeometry
    {
        private static readonly long BUFLEN = 255;

        // 视野左上角横坐标. 像素. 0级缩放.
        public double X;

        // 视野左上角纵坐标. 像素. 0级缩放.
        public double Y;

        // 缩放级别.
        public int L;

        // 缩放级别数.
        public int NumL;

        // 输出宽度. 像素. L级缩放.
        public double OutW;

        // 输出高度. 像素. L级缩放.
        public double OutH;

        // 切片宽度. 像素. 0级缩放.
        public double SlideW;

        // 切片高度. 像素. L级缩放.
        public double SlideH;

        private double[] pixelDensity;

        public SceneGeometry()
        {
            Init();
        }

        public SceneGeometry(SceneGeometry sg)
        {
            CopyFrom(sg);
        }

        public override int GetHashCode()
        {
            int p1 = 29, p2 = 31, p3 = 233;

            p1 = p1 * p2 + L % p3;
            p1 = p1 * p2 + (int)OutH % p3;
            p1 = p1 * p2 + (int)OutW % p3;
            p1 = p1 * p2 + (int)X % p3;
            p1 = p1 * p2 + (int)Y % p3;

            return p1;
        }

        public void Init()
        {
            (X, Y, L, NumL, OutW, OutH, SlideW, SlideH) 
                = (0, 0, 0, 0, 0, 0, 0, 0);
            if (pixelDensity == null)
            {
                pixelDensity = new double[BUFLEN];
            }
            pixelDensity.FillWith(-1);
        }

        public void CopyFrom(SceneGeometry sg)
        {
            (X, Y, L, NumL, OutW, OutH, SlideW, SlideH) = 
                (sg.X, sg.Y, sg.L, sg.NumL, 
                sg.OutW, sg.OutH, sg.SlideW, sg.SlideH);
            if (pixelDensity == null)
            {
                pixelDensity = new double[BUFLEN];
            }
            Array.Copy(sg.pixelDensity, pixelDensity, BUFLEN);
        }

        public void Update(SceneGeometry sg)
        {
            X = sg.X; Y = sg.Y;
            L = sg.L; OutW = sg.L; OutH = sg.OutH;
        }

        /// <summary>
        /// 初始化各级缩放的像素密度
        /// </summary>
        /// <param name="level">缩放级别的序号</param>
        /// <param name="density">屏幕的1个像素对应该级别density^density这么多像素.</param>
        public void SetDensityAtLevel(int level, double density)
        {
            pixelDensity[level] = density;
        }

        /// <summary>
        /// 移动左上角坐标, 使得画布在屏幕上移动(dx, dy)像素.
        /// </summary>
        /// <param name="dx">屏幕像素数</param>
        /// <param name="dy">屏幕像素数</param>
        public void Move(double dx, double dy)
        {
            double x0 = X, y0 = Y;
            X += ToActualPixel(dx);
            //X += dx;
            Y += ToActualPixel(dy);
            //Y += dy;

            //// 修正超出范围的坐标
            //var padX = ToActualPixel(OutW / 2.0);
            //var padY = ToActualPixel(OutH / 2.0);
            //X = Math.Max(Math.Min(X, SlideW - padX), 0);
            //Y = Math.Max(Math.Min(Y, SlideH - padY), 0);
        }

        /// <summary>
        /// 把屏幕上的像素值换算成切片上的像素值.
        /// </summary>
        /// <param name="pScreen">屏幕上的像素值</param>
        /// <returns></returns>
        public double ToActualPixel(double pScreen)
        {
            return pScreen * pixelDensity[L];
        }

        public double ToDisplayPixel(double pActual)
        {
            return pActual / pixelDensity[L];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n">滚轮滚动数</param>
        /// <param name="xm">鼠标横坐标. 像素. 屏幕.</param>
        /// <param name="ym">鼠标纵坐标. 像素. 屏幕.</param>
        public void Zoom(int n, double xm, double ym)
        {
            int sign = Math.Sign(n);
            int srcL = L;
            int dstL = Math.Max(Math.Min(NumL - 1, L + sign), 0);

            // 如果从最大继续放大, 或从最小继续缩小, 则不要再缩放了
            if (srcL != dstL)
            {
                L = dstL;
                double d0 = pixelDensity[srcL]; // 原倍率的像素密度
                double d1 = pixelDensity[dstL]; // 新倍率的像素密度

                /**
                 * 以下两条式子, 以X1的计算为例, 是这么得到的:
                 * 在d0密度下, 鼠标横坐标xm对应的实际位置Xm是
                 * X + xm*d0 = Xm          (1)
                 * 在d1密度下, 要想让Xm仍然出现在屏幕的xm处, 需要满足
                 * X1 + xm*d1 = Xm         (2)
                 * 由(1)(2)得到
                 * X1 = X + xm*(d0 - d1)   (3)
                 */
                X = X + xm * (d0 - d1);
                Y = Y + ym * (d0 - d1);

                // 修正超出范围的坐标
                //var padX = ToActualPixel(W / 2.0);
                //var padY = ToActualPixel(H / 2.0);
                //X = Math.Max(Math.Min(X, SlideW - padX), -padX);
                //Y = Math.Max(Math.Min(Y, SlideH - padY), -padY);
            }
        }

        public void Resize(double w, double h)
        {
            OutW = w; OutH = h;
        }
    }
}
