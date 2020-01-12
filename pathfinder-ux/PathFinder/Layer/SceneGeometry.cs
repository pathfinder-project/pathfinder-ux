using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathFinder.Layer
{
    class SceneGeometry
    {
        private static readonly long BUFLEN = 255;

        // 视野左上角横坐标. 像素. 0级缩放.
        public int X { get; set; }

        // 视野左上角纵坐标. 像素. 0级缩放.
        public int Y { get; set; }

        // 缩放级别.
        public int L { get; set; }

        // 缩放级别数
        public int NumL { get; set; }

        // 视野宽度. 像素.
        public int W { get; set; }

        // 视野高度. 像素.
        public int H { get; set; }

        // 切片宽度. 像素.
        public int SlideW { get; set; }

        // 切片高度. 像素.
        public int SlideH { get; set; }

        private double[] pixelDensity;

        public SceneGeometry()
        {
            Init();
        }

        public SceneGeometry(SceneGeometry sg)
        {
            Clone(sg);
        }

        public override int GetHashCode()
        {
            int p1 = 29, p2 = 31, p3 = 233;

            p1 = p1 * p2 + L % p3;
            p1 = p1 * p2 + H % p3;
            p1 = p1 * p2 + W % p3;
            p1 = p1 * p2 + X % p3;
            p1 = p1 * p2 + Y % p3;

            return p1;
        }

        public void Init()
        {
            X = Y = L = NumL = W = H = SlideW = SlideH = 0;
            if (pixelDensity == null)
            {
                pixelDensity = new double[BUFLEN];
            }
            pixelDensity.FillWith(-1);
        }

        public void Clone(SceneGeometry sg)
        {
            X = sg.X; Y = sg.Y; L = sg.L; NumL = sg.NumL;
            W = sg.W; H = sg.H;
            SlideW = sg.SlideW; SlideH = sg.SlideH;
            if (pixelDensity == null)
            {
                pixelDensity = new double[BUFLEN];
            }
            Array.Copy(sg.pixelDensity, pixelDensity, BUFLEN);
        }

        public void Update(SceneGeometry sg)
        {
            X = sg.X; Y = sg.Y;
            L = sg.L; W = sg.L; H = sg.H;
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
        public void Move(int dx, int dy)
        {
            int x0 = X, y0 = Y;
            X += ToActualPixel(dx);
            //X += dx;
            Y += ToActualPixel(dy);
            //Y += dy;

            // 修正超出范围的坐标
            var padX = ToActualPixel(W / 2.0);
            var padY = ToActualPixel(H / 2.0);
            X = Math.Max(Math.Min(X, SlideW - padX), 0);
            Y = Math.Max(Math.Min(Y, SlideH - padY), 0);
        }

        /// <summary>
        /// 把屏幕上的像素值换算成切片上的像素值.
        /// </summary>
        /// <param name="pScreen">屏幕上的像素值</param>
        /// <returns></returns>
        public int ToActualPixel(int pScreen)
        {
            return (int)(pScreen * pixelDensity[L]);
        }

        /// <summary>
        /// 把屏幕上的像素值换算成切片上的像素值.
        /// </summary>
        /// <param name="pScreen">屏幕上的像素值</param>
        /// <returns></returns>
        public int ToActualPixel(double pScreen)
        {
            return (int)(pScreen * pixelDensity[L]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="n">滚轮滚动数</param>
        /// <param name="xm">鼠标横坐标. 像素. 屏幕.</param>
        /// <param name="ym">鼠标纵坐标. 像素. 屏幕.</param>
        public void Zoom(int n, int xm, int ym)
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
                X = (int)(X + xm * (d0 - d1));
                Y = (int)(Y + ym * (d0 - d1));

                // 修正超出范围的坐标
                //var padX = ToActualPixel(W / 2.0);
                //var padY = ToActualPixel(H / 2.0);
                //X = Math.Max(Math.Min(X, SlideW - padX), -padX);
                //Y = Math.Max(Math.Min(Y, SlideH - padY), -padY);
            }
        }

        public void Resize(int w, int h)
        {
            W = w; H = h;
        }
    }
}
