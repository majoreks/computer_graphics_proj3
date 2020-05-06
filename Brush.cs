using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace cg_proj2
{
    class Brush : IShape
    {
        public int Thickness { get; set; }
        public Point BrushCentre { get; set; }
        public Color ColorBrush { get; set; }
        public Brush(int thickness, int x, int y)
        {
            Thickness = thickness;
            BrushCentre = new Point(x, y);
        }
        public void DeleteShape()
        {
            MainWindow.DrawBrush((int)BrushCentre.X, (int)BrushCentre.Y, ColorBrush, true);
        }

        public void DrawShape()
        {
            MainWindow.DrawBrush((int)BrushCentre.X, (int)BrushCentre.Y, ColorBrush);
        }

        public bool WasClicked(int x, int y)
        {
            int Vx = Math.Abs((int)x - (int)BrushCentre.X);
            int Vy = Math.Abs((int)y - (int)BrushCentre.Y);
            double len = Math.Sqrt(Math.Pow(Vy, 2) + Math.Pow(Vx, 2));
            if(len<Thickness)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public bool MoveBrush(int x, int y)
        {
            if (x + Thickness > MainWindow.actualWidth || x - Thickness < 0
                    || y + Thickness > MainWindow.actualHeight || y - Thickness < 0)
            {
                return false;
            }
            DeleteShape();
            BrushCentre = new Point(x, y);
            DrawShape();
            return true;
        }

        public void ReColour(Color color)
        {
            if (color == ColorBrush)
            {
                return;
            }
            DeleteShape();
            ColorBrush = color;
            DrawShape();
        }
    }
}
