using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;

namespace cg_proj2
{
    public class Circle : IShape
    {
        public int R { get; set; }
        public Point Centre { get; set; }
        public Color ColorCircle { get; set; }
        public int ThicknessCircle { get; set; }

        private static bool changeRadius;

        public Circle(int r, double x, double y, Color color, int _thickness)
        {
            Centre = new Point(x, y);
            R = r;
            this.ColorCircle = color;
            ThicknessCircle = _thickness;
        }

        public void DeleteShape()
        {
            MainWindow.MidpointCircle(R, (int)Centre.X, (int)Centre.Y, ColorCircle, ThicknessCircle , true);
        }

        public void DrawShape()
        {
            MainWindow.MidpointCircle(R, (int)Centre.X, (int)Centre.Y, ColorCircle, ThicknessCircle);
        }

        public bool WasClicked(int x, int y)
        {
            int Vx = Math.Abs((int)x - (int)Centre.X);
            int Vy = Math.Abs((int)y - (int)Centre.Y);
            double len = Math.Sqrt(Math.Pow(Vy, 2) + Math.Pow(Vx, 2));
            //MessageBox.Show(len.ToString());
            if(len<=R+10+ThicknessCircle && len>=R-10-ThicknessCircle)
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
                if (Centre.X + len + ThicknessCircle > MainWindow.actualWidth || Centre.X - len - ThicknessCircle < 0 || 
                    Centre.Y + len + ThicknessCircle > MainWindow.actualHeight || Centre.Y - len - ThicknessCircle < 0)
                {
                    return false;
                }
                DeleteShape();
                R = (int)len;
            }
            else
            {
                if (x + R + ThicknessCircle > MainWindow.actualWidth || x - R - ThicknessCircle < 0 
                    || y + R + ThicknessCircle > MainWindow.actualHeight || y - R - ThicknessCircle < 0)
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

        public void ReColour(Color color)
        {
            if (color == ColorCircle)
            {
                return;
            }
            DeleteShape();
            ColorCircle = color;
            DrawShape();
        }

        public void Resize(int _thickness)
        {
            DeleteShape();
            ThicknessCircle = _thickness;
            DrawShape();
        }
    }
}
