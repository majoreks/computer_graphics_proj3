using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;

namespace cg_proj2
{
    class Line : IShape
    {
        public Point P0 { get; set; }
        public Point P1 { get; set; }
        public Color ColorLine { get; set; }
        private static Point pLastClicked;

        public Line(int x0, int y0, int x1, int y1, Color color)
        {
            P0 = new Point(x0, y0);
            P1 = new Point(x1, y1);
            this.ColorLine = color;
        }
        public void DeleteShape()
        {
            MainWindow.DrawLine((int)P0.X, (int)P0.Y, (int)P1.X, (int)P1.Y, ColorLine, true);
        }

        public void DrawShape()
        {
            MainWindow.DrawLine((int)P0.X, (int)P0.Y, (int)P1.X, (int)P1.Y, ColorLine);
        }

        public bool WasClicked(int x, int y)
        {
            int Vx = Math.Abs((int)x - (int)P0.X);
            int Vy = Math.Abs((int)y - (int)P0.Y);
            double len = Math.Sqrt(Math.Pow(Vy, 2) + Math.Pow(Vx, 2));
            //MessageBox.Show(len.ToString());
            if (len <= 10)
            {
                pLastClicked = P0;
                return true;
            }
            else
            {
                Vx = Math.Abs((int)x - (int)P1.X);
                Vy = Math.Abs((int)y - (int)P1.Y);
                len = Math.Sqrt(Math.Pow(Vy, 2) + Math.Pow(Vx, 2));
                if (len <= 10)
                {
                    pLastClicked = P1;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void MoveLine(int x, int y)
        {
            DeleteShape();
            if (pLastClicked==P1)
            {
                P1 = new Point(x, y);
            }
            else
            {
                P0 = new Point(x, y);
            }
            DrawShape();
        }

        public void ReColour(Color color)
        {
            if (color == ColorLine)
            {
                return;
            }
            DeleteShape();
            ColorLine = color;
            DrawShape();
        }
    }
}
