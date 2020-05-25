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
            if (x1 == x3 || y1 == y3)
            {
                return;
            }
            if ((y1 > y3 && x1 < x3) || (y1 < y3 && x1 > x3))
            {
                base.AddVertex(x1, y3);
                base.AddVertex(x3, y3);
                base.AddVertex(x3, y1);
                base.AddVertex(x1, y1);
            }
            else
            {
                base.AddVertex(x3, y1);
                base.AddVertex(x3, y3);
                base.AddVertex(x1, y3);
                base.AddVertex(x1, y1);
            }
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
            //MessageBox.Show($"{y1}, {y3}");
            if ((y1 > y3 && x1 < x3) || (y1 < y3 && x1 > x3))
            {
                base.AddVertex(x1, y3);
                base.AddVertex(x3, y3);
                base.AddVertex(x3, y1);
                base.AddVertex(x1, y1);
            }
            else
            {
                base.AddVertex(x3, y1);
                base.AddVertex(x3, y3);
                base.AddVertex(x1, y3);
                base.AddVertex(x1, y1);
            }
        }
    }
}
