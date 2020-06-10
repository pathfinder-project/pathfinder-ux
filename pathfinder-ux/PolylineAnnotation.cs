using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

#pragma warning disable CA2100 // 检查 SQL 查询是否存在安全漏洞

namespace PathFinder
{

    class V
    {
        public int idv;
        public int? prev;
        public int? next;
        public double x;
        public double y;
        public int head;
        public int tail;

        public V(int idv, int? prev, int? next, double x, double y, int head, int tail)
        {
            this.idv = idv;
            this.prev = prev;
            this.next = next;
            this.x = x;
            this.y = y;
            this.head = head;
            this.tail = tail;
        }
    }

    class E
    {
        public double xa;
        public double ya;
        public double xb;
        public double yb;

        public E(double xa, double ya, double xb, double yb)
        {
            this.xa = xa;
            this.ya = ya;
            this.xb = xb;
            this.yb = yb;
        }
    }
    
    class BoundingBox
    {
        public double x1;
        public double y1;
        public double x2;
        public double y2;
    }

    class PolylineAnnotation
    {
        private SQLiteConnection conn;
        private readonly string init_sql =
            @"PRAGMA foreign_keys = 1;
            create table vertex(
                idv integer primary key default 1,
                prev integer default null,
                next integer default null,
                x real default 0,
                y real default 0,
                head integer not null,
                tail integer not null,
                swap integer default null,
                foreign key (prev) references vertex(idv) on delete set null on update cascade,
                foreign key (next) references vertex(idv) on delete set null on update cascade,
                foreign key (head) references vertex(idv) on delete restrict on update cascade,
                foreign key (tail) references vertex(idv) on delete restrict on update cascade
            );
            create view edge (prev, next, xa, ya, xb, yb) as
                select min(va.idv, vb.idv) as _prev, max(va.idv, vb.idv) as _next, va.x, va.y, vb.x, vb.y
                from vertex va, vertex vb
                where vb.prev = va.idv or va.next = vb.idv
                group by _prev, _next;
            ";

        /// <summary>
        /// Create a brand-new annotation.
        /// </summary>
        public PolylineAnnotation()
        {
            conn = new SQLiteConnection("Data Source=:memory:");
            conn.Open();
            
            ExecuteSql(init_sql);
        }

        /// <summary>
        /// Load an annotation from file.
        /// </summary>
        /// <param name="path"></param>
        public PolylineAnnotation(string path)
        {

        }

        /// <summary>
        /// 新增一个节点
        /// </summary>
        /// <param name="idb">前端分配给新增节点的id</param>
        /// <param name="x">新增节点在最大倍率下的x坐标</param>
        /// <param name="y">新增节点在最大倍率下的y坐标</param>
        /// <param name="ida">新增节点的前驱节点，不是头就是尾。如果存在前驱节点，则和它相连</param>
        public void AddVertex(int idb, double x, double y, int? ida)
        {
            string ins = $"insert into vertex (idv, x, y, head, tail) values ({idb}, {x}, {y}, {idb}, {idb}); ";
            ExecuteSql(ins);
            if (ida != null) // 如果存在前驱节点
            {
                var va = QueryVertex(ida.Value);
                var vb = new V(idb, null, null, x, y, idb, idb);
                if (ida == va.head)
                {
                    RevertChain(va.head, va.tail);
                }
                ConnectVertex(ida.Value, idb);
            }
        }

        /// <summary>
        /// 连接两个已存在的节点
        /// 只可能是头连尾、尾连头、头连头、尾连尾四种情况
        /// </summary>
        /// <param name="ida">A链的头或尾节点</param>
        /// <param name="idb">B链的头或尾节点</param>
        public void ConnectVertex(int ida, int idb)
        {
            var va = QueryVertex(ida);
            var vb = QueryVertex(idb);

            // A头连B尾（也包括头连孤、孤连尾、孤连孤）
            if (va.head == ida && vb.tail == idb)
            {
                ida = vb.idv;
                idb = va.idv;
            }

            // A头连B头
            else if (va.head == ida && vb.head == idb)
            {
                RevertChain(va.head, va.tail);
            }

            // A尾连B尾
            else if (va.tail == ida && vb.tail == idb)
            {
                RevertChain(vb.head, vb.tail);
            }

            // 化归成  A尾连B头  的情况
            va = QueryVertex(ida);
            vb = QueryVertex(idb);
            ConnectATailToBHead(va, vb);
        }

        /// <summary>
        /// 把A链的尾和B链的头连起来。得到新链的头是A头，尾是B尾
        /// </summary>
        /// <param name="headb">A链上的任意节点</param>
        /// <param name="heada">B链上的任意节点</param>
        private void ConnectATailToBHead(V a, V b)
        {
            string sql = $"update vertex set next={b.head} where idv={a.tail};"; // 连接A尾和B头
            sql += $"update vertex set prev={a.tail} where idv={b.head};"; // 连接B头和A尾
            sql += $"update vertex set tail={b.tail} where head={a.head};"; // 更新A链的尾节点为B尾
            sql += $"update vertex set head={a.head} where head={b.head};"; // 更新B链的头节点为A头
            ExecuteSql(sql);
        }

        /// <summary>
        /// 反转一条链
        /// </summary>
        /// <param name="head"></param>
        /// <param name="tail"></param>
        private void RevertChain(int head, int tail)
        {
            string sql = $"update vertex set swap=next, next=prev where head={head};";
            ExecuteSql(sql);
            sql = $"update vertex set prev=swap, head={tail}, tail={head} where head={head};";
            ExecuteSql(sql);
        }

        /// <summary>
        /// 将某节点平移指定的偏移量
        /// </summary>
        /// <param name="idv">节点id</param>
        /// <param name="dx">最大倍率下的x偏移分量</param>
        /// <param name="dy">最大倍率下的y偏移分量</param>
        public void MoveVertex(int idv, double dx, double dy)
        {
            var _ = QueryVertex(idv);
            double x = _.x + dx;
            double y = _.y + dy;
            string sql = $"update vertex set x={x}, y={y} where idv={idv};";
            ExecuteSql(sql);
        }

        public void DeleteVertex(int idv)
        {
            var v = QueryVertex(idv);

            if (v.head != v.tail)
            {
                if (v.head == idv) // 删除头节点
                {
                    ExecuteSql($"update vertex set head={v.next.Info()} where head={idv};");
                }
                else if (v.tail == idv) // 删除尾节点
                {
                    ExecuteSql($"update vertex set tail={v.prev.Info()} where tail={idv};");
                }
                else // 删除中继节点
                {
                    var h = QueryVertex(v.head);
                    var t = QueryVertex(v.tail);

                    if (h.prev == t.idv && t.next == h.idv) // 环
                    {
                        ExecuteSql($"update vertex set head={v.next.Info()}, tail={v.prev.Info()} where head={h.idv};");
                    }
                    else // 链
                    {
                        int i = h.idv;
                        while (i != idv)
                        {
                            var a = QueryVertex(i);
                            ExecuteSql($"update vertex set tail={v.prev.Info()} where idv={i};");
                            i = a.next.Value;
                        }
                        i = v.next.Value;
                        while (i != t.idv)
                        {
                            var b = QueryVertex(i);
                            ExecuteSql($"update vertex set head={v.next.Info()} where idv={i};");
                            i = b.next.Value;
                        }
                        ExecuteSql($"update vertex set head={v.next.Info()} where idv={v.tail};");
                    }
                }
            }
            ExecuteSql($"delete from vertex where idv={idv};"); // 删除孤立节点
        }

        public void LastId(out int? idv)
        {
            idv = null;
            var q = conn.CreateCommand();
            q.CommandText = $"select last_insert_rowid()";
            using (var res = q.ExecuteReader())
            {
                while (res.Read())
                {
                    idv = res.GetInt32(0);
                    break;
                }
            }
        }

        private V QueryVertex(int idv)
        {
            V v = null;
            var q = conn.CreateCommand();
            q.CommandText = $"select * from vertex where idv={idv};";
            using (var res = q.ExecuteReader())
            {
                while (res.Read())
                {
                    v = new V(
                        idv,
                        res.GetInt32OrNull(1),
                        res.GetInt32OrNull(2),
                        res.GetDouble(3),
                        res.GetDouble(4),
                        res.GetInt32(5),
                        res.GetInt32(6)
                    );
                    break;
                }
            }
            return v;
        }

        public void QueryChain(int idv, out List<double> x, out List<double> y, out BoundingBox bb)
        {
            var _ = QueryVertex(idv);
            x = new List<double>();
            y = new List<double>();
            bb = new BoundingBox();
            int? id = _.head;
            int? guard = id;
            do
            {
                var v = QueryVertex(id.Value);
                x.Add(v.x);
                y.Add(v.y);
                id = v.next;
            } while (id != null && id != guard);
            bb.x1 = x.Min();
            bb.y1 = y.Min();
            bb.x2 = x.Max();
            bb.y2 = y.Max();
        }

        private void ListAll(out List<V> vertices, out List<E> edges)
        {
            var q = conn.CreateCommand();
            q.CommandText = $"select * from vertex;";
            vertices = new List<V>();
            using (var res = q.ExecuteReader())
            {
                while (res.Read())
                {
                    var v = new V(
                        res.GetInt32(0), 
                        res.GetInt32OrNull(1), 
                        res.GetInt32OrNull(2), 
                        res.GetDouble(3), 
                        res.GetDouble(4),
                        res.GetInt32(5),
                        res.GetInt32(6)
                    );
                    vertices.Add(v);
                }
            }

            var q2 = conn.CreateCommand();
            q2.CommandText = $"select * from edge;";
            edges = new List<E>();
            using (var res = q2.ExecuteReader())
            {
                while (res.Read())
                {
                    var e = new E(
                        res.GetDouble(2),
                        res.GetDouble(3),
                        res.GetDouble(4),
                        res.GetDouble(5));
                    edges.Add(e);
                }
            }
        }

        private void ExecuteSql(string sql)
        {
            var stmt = conn.CreateCommand();
            stmt.CommandText = sql;
            stmt.ExecuteNonQuery();
        }

        public List<Object> LoadRegionShapes(Viewport vp, double margin = 2)
        {
            double left, top, right, bottom;
            left = vp.X - margin;
            top = vp.Y - margin;
            right = vp.X + vp.ToActualPixel(vp.OutW) + margin;
            bottom = vp.Y + vp.ToActualPixel(vp.OutH) + margin;

            var toShow = new List<Object>();
            ListAll(out List<V> vertices, out List<E> edges);

            foreach (var v in vertices)
            {
                double x = v.x, y = v.y;
                if (EncodePoint(left, top, right, bottom, x, y) == cINSIDE)
                {
                    toShow.Add(v);
                }
            }

            foreach (var e in edges)
            {
                double xa = e.xa, ya = e.ya, xb = e.xb, yb = e.yb;
                ClipByWindow(
                    left, top, right, bottom,
                    xa, ya, xb, yb,
                    out double x1, out double y1, out double x2, out double y2
                );
                if (!double.IsNaN(x1))
                {
                    if (x1 != x2 || y1 != y2)
                    {
                        toShow.Add(new E(x1, y1, x2, y2));
                    }
                }
            }

            return toShow;
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
        /// TODO: 改良「碰撞检测」部分，先用「跨立法」检测是否与矩形四边相交，再求交点
        /// 参照 https://www.cnblogs.com/Duahanlang/archive/2013/05/11/3073434.html
        /// 和  https://www.cnblogs.com/Duahanlang/archive/2013/05/11/3073434.html
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
}
#pragma warning restore CA2100 // 检查 SQL 查询是否存在安全漏洞
