using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using cg_proj2.enums;

namespace cg_proj2
{
    class Polygon : IShape
    {
        public List<Point> Vertices {get; set;}
        public Point InitialPoint { get; set; }
        private static int selectedVertexIndex;
        public Polygon(int x, int y)
        {
            InitialPoint = new Point(x, y);
            Vertices = new List<Point>();
            Vertices.Add(InitialPoint);
        }

        public bool AddVertex(int x, int y)
        {
            if(InitialPoint==null)
            {
                return false;
            }
            int count = Vertices.Count;
            if (count >= 3)
            {
                int Vx = Math.Abs((int)x - (int)InitialPoint.X);
                int Vy = Math.Abs((int)y - (int)InitialPoint.Y);
                double len = Math.Sqrt(Math.Pow(Vy, 2) + Math.Pow(Vx, 2));
                if(len<=10)
                {
                    Vertices.Add(InitialPoint);
                    MainWindow.DrawLine((int)Vertices[count - 1].X, (int)Vertices[count - 1].Y, (int)InitialPoint.X, (int)InitialPoint.Y);
                    return true;
                }
            }
            Vertices.Add(new Point(x, y));
            MainWindow.DrawLine((int)Vertices[count - 1].X, (int)Vertices[count - 1].Y, x, y);
            return false;
        }
        public void DeleteShape()
        {
            if (Vertices.Where(x => Vertices.Contains(x)).Count() >= 2)
            {
                for (int i = 1; i < Vertices.Count; i++)
                {
                    MainWindow.DrawLine((int)Vertices[i - 1].X, (int)Vertices[i - 1].Y, (int)Vertices[i].X, (int)Vertices[i].Y, true);
                }
            }
        }

        public void DrawShape()
        {
            if(Vertices.Where(x=>Vertices.Contains(x)).Count()>=2)
            {
                //MessageBox.Show(Vertices.Count.ToString());
                for(int i=1; i<Vertices.Count; i++)
                {
                    MainWindow.DrawLine((int)Vertices[i - 1].X, (int)Vertices[i - 1].Y, (int)Vertices[i].X, (int)Vertices[i].Y);
                }
            }
        }

        public bool WasClicked(int x, int y)
        {
            int Vx, Vy;
            double len;
            foreach(Point pt in Vertices)
            {
                Vx = Math.Abs((int)x - (int)pt.X);
                Vy = Math.Abs((int)y - (int)pt.Y);
                len = Math.Sqrt(Math.Pow(Vy, 2) + Math.Pow(Vx, 2));
                //MessageBox.Show(len.ToString());
                //MessageBox.Show(Vertices.Count.ToString());
                if (len<10)
                {
                    selectedVertexIndex = Vertices.IndexOf(pt);

                    return true;
                }
            }
            return false;
        }

        public void MovePolygon(int x, int y, PolyMoveModes mode)
        {
            if(mode==PolyMoveModes.ByVertex)
            {
                DeleteShape();
                if (Vertices[selectedVertexIndex] == InitialPoint)
                {
                    InitialPoint = new Point(x, y);
                    Vertices[0] = Vertices[Vertices.Count-1] = InitialPoint;
                }
                else
                {
                    Vertices[selectedVertexIndex] = new Point(x, y);
                }
                DrawShape();
            }
            else if(mode==PolyMoveModes.WholePoly)
            {
                int Vx, Vy;
                List<Point> tmp = new List<Point>();
                Vx = ((int)x - (int)Vertices[selectedVertexIndex].X);
                Vy = ((int)y - (int)Vertices[selectedVertexIndex].Y);
                foreach (Point pt in Vertices.ToList())
                {

                    if(pt.X + Vx > MainWindow.actualWidth|| pt.X + Vx < 0 ||
                        pt.Y + Vy > MainWindow.actualHeight || pt.Y + Vy < 0)
                    {
                        MessageBox.Show("XD");
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
    }
}
