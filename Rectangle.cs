using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using cg_proj2.enums;

namespace cg_proj2
{
    public class Rectangle : Polygon
    {
        public Rectangle(int x1, int y1, int x3, int y3, Color color, int _thickness) : base(x1, y1, color, _thickness)
        {
            //Point M = new Point((x3 + x1) / 2, (y3 + y1) / 2);
            //double r = Math.Sqrt(Math.Pow(x3 - x1, 2) + Math.Pow(y3 - y1, 2)) / 2;
            //Random random = new Random();
            //int x2 = random.Next((int)(M.X - r), (int)(M.X+r));
            //double c = Math.Pow(x2 - M.X, 2) - r * r;
            //double d = 4 * M.Y * M.Y - 4 * (M.Y * M.Y + c);
            //int y2;
            //if (d>0)
            //{
            //    y2 = (int)(2 * M.Y + Math.Sqrt(d)) / 2;
            //}
            //else
            //{
            //    y2 = (int)M.Y;
            //}
            //int x4 = (int)(2 * M.X - x2), y4 = (int)(2 * M.Y - y2);
            //base.AddVertex(x2, y2);
            //base.AddVertex(x3, y3);
            //base.AddVertex(x4, y4);
            //base.AddVertex(x1, y1);
            if (x1 == x3 || y1 == y3)
            {
                return;
            }
            base.AddVertex(x3, y1);
            base.AddVertex(x3, y3);
            base.AddVertex(x1, y3);
            base.AddVertex(x1, y1);
        }

        public override void MovePolygon(int x, int y, PolyMoveModes mode)
        {
            if (mode == PolyMoveModes.WholePoly)
            {
                base.MovePolygon(x, y, mode);
            }
            else if (mode == PolyMoveModes.ByVertex)
            {
                base.DeleteShape();
                Point newInitialVertex = new Point(x, y);
                Point tmpVertex = base.Vertices[(selectedVertexIndex + 2) % 4 == 0 ? 1 : (selectedVertexIndex + 2) % 4];
                Func((int)newInitialVertex.X, (int)newInitialVertex.Y, (int)tmpVertex.X, (int)tmpVertex.Y);
            }
        }

        private void Func(int x1, int y1, int x3, int y3)
        {
            if (x1 == x3 || y1 == y3)
            {
                return;
            }
            InitialPoint = new Point(x1, y1);
            Vertices = new List<Point>();
            Vertices.Add(InitialPoint);
            base.AddVertex(x3, y1);
            base.AddVertex(x3, y3);
            base.AddVertex(x1, y3);
            base.AddVertex(x1, y1);
        }
    }
}
