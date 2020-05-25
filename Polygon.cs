using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using cg_proj2.enums;

namespace cg_proj2
{
    public class Polygon : IShape
    {
        public List<Point> Vertices { get; set; }
        public Point InitialPoint { get; set; }
        public Color ColorPoly { get; set; }
        public int PolyThickness { get; set; }
        public bool IsFilled { get; set; }
        public bool IsClipping { get; set; }
        public Color FillColor { get; set; }
        protected static int selectedVertexIndex;
        public Polygon(int x, int y, Color color, int _thickness)
        {
            InitialPoint = new Point(x, y);
            this.ColorPoly = color;
            PolyThickness = _thickness;
            Vertices = new List<Point>();
            Vertices.Add(InitialPoint);
        }

        public bool AddVertex(int x, int y)
        {
            if (InitialPoint == null)
            {
                return false;
            }
            int count = Vertices.Count;
            if (count >= 3)
            {
                int Vx = Math.Abs((int)x - (int)InitialPoint.X);
                int Vy = Math.Abs((int)y - (int)InitialPoint.Y);
                double len = Math.Sqrt(Math.Pow(Vy, 2) + Math.Pow(Vx, 2));
                if (len <= 10)
                {
                    Vertices.Add(InitialPoint);
                    MainWindow.DrawLine((int)Vertices[count - 1].X, (int)Vertices[count - 1].Y, (int)InitialPoint.X, (int)InitialPoint.Y, PolyThickness, ColorPoly);
                    return true;
                }
            }
            Vertices.Add(new Point(x, y));
            MainWindow.DrawLine((int)Vertices[count - 1].X, (int)Vertices[count - 1].Y, x, y, PolyThickness, ColorPoly);
            return false;
        }
        public void DeleteShape()
        {
            if (Vertices.Where(x => Vertices.Contains(x)).Count() >= 2)
            {
                for (int i = 1; i < Vertices.Count; i++)
                {
                    MainWindow.DrawLine((int)Vertices[i - 1].X, (int)Vertices[i - 1].Y, (int)Vertices[i].X, (int)Vertices[i].Y, PolyThickness, ColorPoly, true);
                }
            }
            if (IsFilled)
            {
                EdgeTableFill(FillColor, true);
            }
        }

        public void DrawShape()
        {
            if (Vertices.Where(x => Vertices.Contains(x)).Count() >= 2)
            {
                //MessageBox.Show(Vertices.Count.ToString());
                for (int i = 1; i < Vertices.Count; i++)
                {
                    MainWindow.DrawLine((int)Vertices[i - 1].X, (int)Vertices[i - 1].Y, (int)Vertices[i].X, (int)Vertices[i].Y, PolyThickness, ColorPoly);
                }
            }
            if (IsFilled)
            {
                EdgeTableFill(FillColor);
            }
        }

        public bool WasClicked(int x, int y)
        {
            int Vx, Vy;
            double len;
            foreach (Point pt in Vertices)
            {
                Vx = Math.Abs((int)x - (int)pt.X);
                Vy = Math.Abs((int)y - (int)pt.Y);
                len = Math.Sqrt(Math.Pow(Vy, 2) + Math.Pow(Vx, 2));
                //MessageBox.Show(len.ToString());
                //MessageBox.Show(Vertices.Count.ToString());
                if (len < 10)
                {
                    selectedVertexIndex = Vertices.IndexOf(pt);
                    return true;
                }
            }
            return false;
        }

        public virtual void MovePolygon(int x, int y, PolyMoveModes mode)
        {
            if (mode == PolyMoveModes.ByVertex)
            {
                DeleteShape();
                if (Vertices[selectedVertexIndex] == InitialPoint)
                {
                    InitialPoint = new Point(x, y);
                    Vertices[0] = Vertices[Vertices.Count - 1] = InitialPoint;
                }
                else
                {
                    Vertices[selectedVertexIndex] = new Point(x, y);
                }
                DrawShape();
            }
            else if (mode == PolyMoveModes.WholePoly)
            {
                int Vx, Vy;
                List<Point> tmp = new List<Point>();
                Vx = ((int)x - (int)Vertices[selectedVertexIndex].X);
                Vy = ((int)y - (int)Vertices[selectedVertexIndex].Y);
                foreach (Point pt in Vertices.ToList())
                {

                    if (pt.X + Vx > MainWindow.actualWidth || pt.X + Vx < 0 ||
                        pt.Y + Vy > MainWindow.actualHeight || pt.Y + Vy < 0)
                    {
                        MessageBox.Show("Poly would be out of bounds");
                        return;
                    }
                    else
                    {
                        tmp.Add(new Point(pt.X + Vx, pt.Y + Vy));
                    }
                }
                DeleteShape();
                Vertices = tmp;
                InitialPoint = tmp[0];
                DrawShape();
            }
        }

        public void ReColour(Color color)
        {
            if (color == ColorPoly)
            {
                return;
            }
            DeleteShape();
            ColorPoly = color;
            DrawShape();
        }

        public void Resize(int _thickness)
        {
            DeleteShape();
            PolyThickness = _thickness;
            DrawShape();
        }

        private double CrossProduct(double Ax, double Ay, double Bx, double By, double Cx, double Cy)
        {
            double BAx = Ax - Bx;
            double BAy = Ay - By;
            double BCx = Cx - Bx;
            double BCy = Cy - By;
            return (BAx * BCy - BAy * BCx);
        }

        public bool CheckIfConvex()
        {
            bool gotNegative = false;
            bool gotPositive = false;
            int n = Vertices.Count;
            int B, C;
            double crossProduct;
            for (int A = 0; A < n; A++)
            {
                B = (A + 1) % n;
                C = (B + 1) % n;
                crossProduct = CrossProduct(Vertices[A].X, Vertices[A].Y, Vertices[B].X, Vertices[B].Y, Vertices[C].X, Vertices[C].Y);
                if (crossProduct < 0)
                {
                    gotNegative = true;
                }
                else if (crossProduct > 0)
                {
                    gotPositive = true;
                }
                if (gotNegative && gotPositive) return false;
            }
            return true;
        }

        private double PolygonArea()
        {
            List<Point> tmp = new List<Point>();
            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                tmp.Add(Vertices[i]);
            }
            int n = Vertices.Count - 1;
            Point[] points = new Point[n + 1];
            tmp.CopyTo(points, 0);
            points[n] = Vertices[0];

            double area = 0;
            for (int i = 0; i < n; i++)
            {
                area += (points[i + 1].X - points[i].X) * (points[i + 1].Y + points[i].Y) / 2;
            }
            return area;
        }

        private Point FindCentroid()
        {
            List<Point> tmp = new List<Point>();
            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                tmp.Add(Vertices[i]);
            }
            int n = tmp.Count;
            Point[] points = new Point[n + 1];
            tmp.CopyTo(points, 0);
            points[n] = tmp[0];

            double X = 0;
            double Y = 0;
            double tmpDbl;
            for (int i = 0; i < n; i++)
            {
                tmpDbl = points[i].X * points[i + 1].Y - points[i + 1].X * points[i].Y;
                X = X + (points[i].X + points[i + 1].X) * tmpDbl;
                Y = Y + (points[i].Y + points[i + 1].Y) * tmpDbl;
            }

            double polygon_area = PolygonArea();
            X = X / (6 * polygon_area);
            Y = Y / (6 * polygon_area);

            if (X < 0)
            {
                X = -X;
                Y = -Y;
            }

            return new Point(X, Y);
        }

        public void SortClockwise()
        {
            if (this is Rectangle)
            {
                return;
            }
            Point center = FindCentroid();
            List<Point> tmp = new List<Point>();
            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                tmp.Add(Vertices[i]);
            }
            Vertices = new List<Point>();
            tmp.OrderBy(x => Math.Atan2(x.X - center.X, x.Y - center.Y)).ToList();
            Vertices = tmp;
            Vertices.Add(Vertices[0]);
        }

        internal class EdgeBucket
        {
            internal int YMax { get; set; }
            internal double XOfYMin { get; set; }
            internal double M { get; set; }
            internal EdgeBucket(Point p0, Point p1)
            {
                YMax = (int)p1.Y;
                XOfYMin = p0.X;
                M = (p1.X - p0.X) / (p1.Y - p0.Y);
            }
        }

        internal class EdgeTable
        {
            internal int NumOfBuckets;
            internal List<EdgeBucket> Buckets { get; set; }
            
            internal EdgeTable()
            {
                Buckets = new List<EdgeBucket>();
                NumOfBuckets = Buckets.Count;
            }

            internal void RemoveElems(int val)
            {
                foreach (EdgeBucket edgeBucket in Buckets.ToList())
                {
                    if(edgeBucket.YMax == val)
                    {
                        Buckets.Remove(edgeBucket);
                    }
                }
            }

            internal void Increment()
            {
                foreach(EdgeBucket edge in Buckets.ToList())
                {
                    edge.XOfYMin = edge.XOfYMin + edge.M;
                }
            }
        }

        public void EdgeTableFill(Color? color, bool del = false)
        {
            EdgeTable activeEdgeTable = new EdgeTable();
            List<Point> vertices = new List<Point>();
            for (int i = 0; i < Vertices.Count - 1; i++)
            {
                vertices.Add(Vertices[i]);
            }

            List<int> indexList = Enumerable.Range(0, vertices.Count).OrderBy(x => vertices[x].Y).ToList();
            int tmpIndex = 0;
            int beg = indexList[0];
            int yMin = (int)vertices.Min(x => x.Y), yMax = (int)vertices.Max(x => x.Y);
            while (yMin < yMax)
            {
                if (vertices[beg].Y == yMin)
                {
                    if (vertices[(beg + 1) % vertices.Count].Y > vertices[beg].Y)
                    {
                        activeEdgeTable.Buckets.Add(new EdgeBucket(vertices[beg], vertices[(beg + 1 + vertices.Count) % vertices.Count]));
                    }
                    if (vertices[(beg - 1 + vertices.Count) % vertices.Count].Y > vertices[beg].Y)
                    {
                        activeEdgeTable.Buckets.Add(new EdgeBucket(vertices[beg], vertices[(beg - 1 + vertices.Count) % vertices.Count]));
                    }
                    beg = indexList[++tmpIndex];
                }
               
                activeEdgeTable.Buckets = activeEdgeTable.Buckets.OrderBy(x => x.XOfYMin).ToList();
                for (int i = 0; i < activeEdgeTable.Buckets.Count - 1; i++)
                {
                    EdgeBucket start = activeEdgeTable.Buckets[i], end = activeEdgeTable.Buckets[i + 1];
                    MainWindow.DrawLine((int)start.XOfYMin, yMin, (int)end.XOfYMin, yMin, 1, color == null ? ColorPoly : (Color)color, del);
                }
                yMin++;
                activeEdgeTable.RemoveElems(yMin);
                activeEdgeTable.Increment();
            }
            IsFilled = true;
            FillColor = (Color)color;
        }
    }
}
