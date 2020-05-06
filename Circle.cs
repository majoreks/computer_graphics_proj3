using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;

namespace cg_proj2
{
    public class Circle : IShape
    {
        public int R { get; set; }
        public Point Centre { get; set; }
        private static bool changeRadius;

        public Circle(int r, double x, double y)
        {
            Centre = new Point(x, y);
            R = r;
        }

        public void DeleteShape()
        {
            MainWindow.MidpointCircle(R, (int)Centre.X, (int)Centre.Y, true);
        }

        public void DrawShape()
        {
            MainWindow.MidpointCircle(R, (int)Centre.X, (int)Centre.Y);
        }

        public bool WasClicked(int x, int y)
        {
            int Vx = Math.Abs((int)x - (int)Centre.X);
            int Vy = Math.Abs((int)y - (int)Centre.Y);
            double len = Math.Sqrt(Math.Pow(Vy, 2) + Math.Pow(Vx, 2));
            //MessageBox.Show(len.ToString());
            if(len<=R+10 && len>=R-10)
            {
                changeRadius = true;
                return true;
            }
            else if (len<=10 && len>=0)
            {
                changeRadius = false;
                return true;
            }
            else 
            {
                return false;
            }
        }
        
        public bool MoveCircle(int x, int y)
        {
            if (changeRadius)
            {
                int Vx = Math.Abs((int)x - (int)Centre.X);
                int Vy = Math.Abs((int)y - (int)Centre.Y);
                double len = Math.Sqrt(Math.Pow(Vy, 2) + Math.Pow(Vx, 2));
                if (Centre.X + len > MainWindow.actualWidth || Centre.X - len < 0 || 
                    Centre.Y + len > MainWindow.actualHeight || Centre.Y - len < 0)
                {
                    return false;
                }
                DeleteShape();
                R = (int)len;
            }
            else
            {
                if (x + R > MainWindow.actualWidth || x - R < 0 
                    || y + R > MainWindow.actualHeight || y - R < 0)
                {
                    return false;
                }
                DeleteShape();
                Centre = new Point(x, y);
                //Centre.X = x;
                //Centre.Y = y;
            }
            DrawShape();
            return true;
        }

        public (Point, double) GetInfo()
        {
            return (Centre, R);
        }

    }
}
