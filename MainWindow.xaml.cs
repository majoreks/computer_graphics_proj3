using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Contexts;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using cg_proj2.enums;
using System.Runtime.InteropServices;

namespace cg_proj2
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static int BRUSH_THICKNESS = 6;

        static int brushThickness = BRUSH_THICKNESS;
        static WriteableBitmap writeableBitmap;
        //static Window w;
        static Image i;
        static int counter = 0;
        static Point p1 = new Point();
        static Point p2 = new Point();
        static public int actualWidth;
        static public int actualHeight;
        static List<IShape> shapes;
        static IShape movingShape;
        static Modes mode = Modes.DrawLines;
        static PolyMoveModes polyMode = PolyMoveModes.ByVertex;
        static RightClickModes rightClickMode = RightClickModes.Move;
        static Modes lastMode;
        static Polygon poly;
        static Color color;
        static int thickness;
        static private Test dataContext = new Test("CG_proj3", mode.ToString(), rightClickMode.ToString(), polyMode.ToString(), 1);
        public class Test : INotifyPropertyChanged
        {
            private string title;
            private string rightClickMode;
            private string drawingMode;
            private string polyMoveMode;
            private int brushThickness;
            public Test(string str, string drawingStr, string rightStr, string polyStr, int _brushThickness)
            {
                title = str;
                rightClickMode = rightStr;
                drawingMode = drawingStr;
                polyMoveMode = polyStr;
                brushThickness = _brushThickness;
            }
            public string Title
            {
                get { return title; }
                set { title = value; NotifyPropertyChanged("Title"); }
            }
            public string RightClickMode
            {
                get { return $"Right click mode: {rightClickMode}"; }
                set { rightClickMode = value; NotifyPropertyChanged("RightClickMode"); }
            }
            public string DrawingMode
            {
                get { return $"Drawing mode: {drawingMode}"; }
                set { drawingMode = value; NotifyPropertyChanged("DrawingMode"); }
            }
            public string PolyMoveMode
            {
                get { return $"Moving polygons mode: {polyMoveMode}"; }
                set { polyMoveMode = value; NotifyPropertyChanged("PolyMoveMode"); }
            }

            public int BrushThickness
            {
                get { return brushThickness; }
                set { brushThickness = value; }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            public void NotifyPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            // write to console helper class

            internal static class ConsoleAllocator
            {
                [DllImport(@"kernel32.dll", SetLastError = true)]
                static extern bool AllocConsole();

                [DllImport(@"kernel32.dll")]
                static extern IntPtr GetConsoleWindow();

                [DllImport(@"user32.dll")]
                static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

                const int SwHide = 0;
                const int SwShow = 5;


                public static void ShowConsoleWindow()
                {
                    var handle = GetConsoleWindow();

                    if (handle == IntPtr.Zero)
                    {
                        AllocConsole();
                    }
                    else
                    {
                        ShowWindow(handle, SwShow);
                    }
                }

                public static void HideConsoleWindow()
                {
                    var handle = GetConsoleWindow();

                    ShowWindow(handle, SwHide);
                }
            }

        }
        public MainWindow()
        {
            lastMode = mode;
            shapes = new List<IShape>();
            this.DataContext = dataContext;
            InitializeComponent();
        }

        static private bool[,] FillShape(int n)
        {
            int cond;
            bool[,] shape = new bool[n, n];
            int mid = (int)Math.Floor((double)n / 2);
            for (int x = 0; x < n; x++)
            {
                cond = Math.Abs(x - mid) - 1;
                for (int y = 0; y < n; y++)
                {
                    if (y >= cond && y <= n - cond - 1)
                    {
                        //Console.Write("# ");
                        shape[x, y] = true;
                    }
                    else
                    {
                        //Console.Write("O ");
                        shape[x, y] = false;
                    }
                }

                //Console.Write("\n");
            }
            return shape;
        }

        static private void SetTitle(string str)
        {
            dataContext.Title = str;
        }
        static public void DrawPixel(int x, int y, Color clr, bool del = false)
        {
            int column = x;
            int row = y;
            try
            {
                writeableBitmap.Lock();
                //MessageBox.Show("xd");
                unsafe
                {
                    IntPtr pBackBuffer = writeableBitmap.BackBuffer;

                    pBackBuffer += row * writeableBitmap.BackBufferStride;
                    pBackBuffer += column * 4;
                    int color_data;
                    if (del)
                    {
                        color_data = 0 << 16;
                        color_data |= 0 << 8;
                        color_data |= 0 << 0;
                    }
                    else
                    {
                        color_data = clr.R << 16; // R
                        color_data |= clr.G << 8; // G
                        color_data |= clr.B << 0; // B
                    }


                    *((int*)pBackBuffer) = color_data;
                }

                writeableBitmap.AddDirtyRect(new Int32Rect(column, row, 1, 1));
            }
            finally
            {
                writeableBitmap.Unlock();
            }
        }

        static public void DrawPixel(int _x, int _y, Color clr, bool[,] shape, bool del = false)
        {
            byte r, g, b;
            if (del)
            {
                r = 0; // R
                g = 0; // G
                b = 0; // B
            }
            else
            {
                r = clr.R; // R
                g = clr.G; // G
                b = clr.B; // B
            }
            int n = (int)Math.Sqrt(shape.Length);
            int mid = (int)Math.Floor((double)n / 2);
            //int column = _x;
            //int row = _y;
            int x, y;
            try
            {
                writeableBitmap.Lock();
                //MessageBox.Show(n.ToString());
                unsafe
                {
                    IntPtr pBackBuffer = writeableBitmap.BackBuffer;
                    byte* pBuff = (byte*)pBackBuffer.ToPointer();
                    for (int i = 0; i < n; i++)
                    {
                        x = _x - (mid - i);
                        for (int j = 0; j < n; j++)
                        {
                            y = _y - (mid - j);
                            if (shape[i, j])
                            {
                                pBuff[4 * x + (y * writeableBitmap.BackBufferStride)] = b;
                                pBuff[4 * x + (y * writeableBitmap.BackBufferStride) + 1] = g;
                                pBuff[4 * x + (y * writeableBitmap.BackBufferStride) + 2] = r;
                                //pBuff[4 * x + (y * writeableBitmap.BackBufferStride) + 3] = 255;

                            }
                        }
                    }

                }

                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, actualWidth, actualHeight));
            }
            finally
            {
                writeableBitmap.Unlock();
            }
        }

        static private int MyGetPixel(int x, int y)
        {
            int column = x;
            int row = y;
            try
            {
                writeableBitmap.Lock();

                unsafe
                {
                    IntPtr pBackBuffer = writeableBitmap.BackBuffer;

                    pBackBuffer += row * writeableBitmap.BackBufferStride;
                    pBackBuffer += column * 4;
                    //*((int*)pBackBuffer)
                    //MessageBox.Show((*((int*)pBackBuffer)).ToString());
                    return *((int*)pBackBuffer);

                }
            }
            finally
            {
                writeableBitmap.Unlock();
            }
        }
        static void i_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

        }
        private void i_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int x, y;
            x = (int)e.GetPosition(i).X;
            y = (int)e.GetPosition(i).Y;
            if (mode == Modes.Moving)
            {
                if (movingShape is Circle)
                {
                    Circle tmpCricle = movingShape as Circle;
                    bool res = tmpCricle.MoveCircle(x, y);
                    if (!res)
                    {
                        MessageBox.Show("Circle will be out of bounds");
                        mode = lastMode;
                        i.Cursor = Cursors.Arrow;
                        return;
                    }
                    mode = lastMode;
                    i.Cursor = Cursors.Arrow;
                    movingShape = null;
                }
                else if (movingShape is Line)
                {
                    Line tmpLine = movingShape as Line;
                    tmpLine.MoveLine(x, y);
                    mode = lastMode;
                    i.Cursor = Cursors.Arrow;
                    movingShape = null;
                }
                else if (movingShape is Brush)
                {
                    Brush tmpBrush = movingShape as Brush;
                    tmpBrush.MoveBrush(x, y);
                    mode = lastMode;
                    i.Cursor = Cursors.Arrow;
                    movingShape = null;
                }
                else if (movingShape is Polygon)
                {
                    Polygon tmpPoly = movingShape as Polygon;
                    tmpPoly.MovePolygon(x, y, polyMode);
                    i.Cursor = Cursors.Arrow;
                    mode = lastMode;
                    movingShape = null;
                }
                if (shapes.Count > 1)
                {
                    RedrawShapes();
                }
            }
            else if (mode == Modes.DrawLines)
            {
                i.Cursor = Cursors.Cross;
                if (counter == 0)
                {
                    p1.X = x;
                    p1.Y = y;
                    counter++;
                }
                else if (counter == 1)
                {

                    p2.X = x;
                    p2.Y = y;
                    counter = 0;
                    DrawLine((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, thickness, color);
                    shapes.Add(new Line((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, color, thickness));
                    p1 = new Point();
                    p2 = new Point();
                    i.Cursor = Cursors.Arrow;
                }
                //MessageBox.Show($"{counter}, {p1.ToString()}, {p2.ToString()}");
            }
            else if (mode == Modes.DrawCircles)
            {
                i.Cursor = Cursors.Cross;
                if (counter == 0)
                {
                    p1.X = x;
                    p1.Y = y;
                    counter++;
                }
                else if (counter == 1)
                {

                    p2.X = x;
                    p2.Y = y;
                    counter = 0;
                    int Vx = (int)p2.X - (int)p1.X;
                    int Vy = (int)p2.Y - (int)p1.Y;
                    double len = Math.Sqrt(Math.Pow(Vy, 2) + Math.Pow(Vx, 2));
                    if (p1.X + len > actualWidth || p1.X - len < 0 || p1.Y + len > actualHeight || p1.Y - len < 0)
                    {
                        MessageBox.Show("Circle out of bounds");
                        i.Cursor = Cursors.Arrow;
                        p1 = new Point();
                        p2 = new Point();
                        return;
                    }
                    MidpointCircle((int)len, (int)p1.X, (int)p1.Y, color, thickness);
                    shapes.Add(new Circle((int)len, (int)p1.X, (int)p1.Y, color, thickness));
                    p1 = new Point();
                    p2 = new Point();
                    i.Cursor = Cursors.Arrow;
                }
            }
            else if (mode == Modes.DrawPolygons)
            {
                //MyGetPixel(x, y);
                if (counter == 0)
                {
                    i.Cursor = Cursors.Cross;
                    poly = new Polygon(x, y, color, thickness);
                    shapes.Add(poly);
                    counter++;
                }
                else
                {
                    if (poly.AddVertex(x, y))
                    {
                        i.Cursor = Cursors.Arrow;
                        counter = 0;
                        poly = null;
                        return;
                    }
                }

            }
            else if (mode == Modes.DrawBrush)
            {
                //DrawBrush(x, y, color);
                //mouseDown = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            i = this.img;
            RenderOptions.SetBitmapScalingMode(i, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(i, EdgeMode.Aliased);
            writeableBitmap = new WriteableBitmap(
                900,
                900,
                96,
                96,
                PixelFormats.Bgr32,
                null);

            i.Source = writeableBitmap;
            i.Stretch = Stretch.None;
            i.HorizontalAlignment = HorizontalAlignment.Left;
            i.VerticalAlignment = VerticalAlignment.Top;
            i.MouseLeftButtonDown +=
                new MouseButtonEventHandler(i_MouseLeftButtonDown);
            i.MouseRightButtonDown +=
                new MouseButtonEventHandler(i_MouseRightButtonDown);
            actualHeight = (int)writeableBitmap.Height;
            actualWidth = (int)writeableBitmap.Width;
        }

        static void SymmetricLineENE(int x1, int y1, int x2, int y2, bool[,] shape, Color clr, bool del)
        {
            //MessageBox.Show($"P1: {x1}, {y1}; P2: {x2}, {y2}");
            int dx = x2 - x1;
            int dy = y2 - y1;
            int d = 2 * dy - dx;
            int dE = 2 * dy;
            int dNE = 2 * (dy - dx);
            int xf = x1, yf = y1;
            int xb = x2, yb = y2;
            DrawPixel(xf, yf, clr, shape, del);
            DrawPixel(xb, yb, clr, shape, del);
            while (xf < xb)
            {
                ++xf;
                --xb;
                if (d < 0) d += dE;
                else
                {
                    d += dNE;
                    ++yf;
                    --yb;
                }
                DrawPixel(xf, yf, clr, shape, del);
                DrawPixel(xb, yb, clr, shape, del);
            }
        }

        static void SymmetricLineNNE(int x1, int y1, int x2, int y2, bool[,] shape, Color clr, bool del)
        {
            //MessageBox.Show($"P1: {x1}, {y1}; P2: {x2}, {y2}");
            int dx = x2 - x1;
            int dy = y2 - y1;
            int d = 2 * dx - dy;
            int dN = 2 * dx;
            int dNE = 2 * (dx - dy);
            int xf = x1, yf = y1;
            int xb = x2, yb = y2;
            DrawPixel(xf, yf, clr, shape, del);
            DrawPixel(xb, yb, clr, shape, del);
            while (yf < yb)
            {

                if (d < 0)
                {
                    d += dN;
                    ++yf;
                    --yb;
                }

                else
                {
                    d += dNE;
                    ++yf;
                    --yb;
                    ++xf;
                    --xb;
                }
                DrawPixel(xf, yf, clr, shape, del);
                DrawPixel(xb, yb, clr, shape, del);
            }
        }

        static void SymmetricLineESE(int x1, int y1, int x2, int y2, bool[,] shape, Color clr, bool del)
        {
            //MessageBox.Show($"P1: {x1}, {y1}; P2: {x2}, {y2}");
            int dx = x2 - x1;
            int dy = y2 - y1;
            int d = 2 * dy + dx;
            int dE = 2 * dy;
            int dSE = 2 * (dy + dx);
            int xf = x1, yf = y1;
            int xb = x2, yb = y2;
            DrawPixel(xf, yf, clr, shape, del);
            DrawPixel(xb, yb, clr, shape, del);
            while (xf < xb)
            {
                ++xf;
                --xb;
                if (d > 0) d += dE;
                else
                {
                    d += dSE;
                    --yf;
                    ++yb;
                }
                DrawPixel(xf, yf, clr, shape, del);
                DrawPixel(xb, yb, clr, shape, del);
            }
        }

        static void SymmetricLineSSE(int x1, int y1, int x2, int y2, bool[,] shape, Color clr, bool del)
        {
            //MessageBox.Show($"P1: {x1}, {y1}; P2: {x2}, {y2}");
            int dx = x2 - x1;
            int dy = y2 - y1;
            int d = 2 * dx + dy;
            int dS = 2 * dx;
            int dSE = 2 * (dy + dx);
            int xf = x1, yf = y1;
            int xb = x2, yb = y2;
            DrawPixel(xf, yf, clr, shape, del);
            DrawPixel(xb, yb, clr, shape, del);
            while (yf > yb)
            {

                if (d < 0)
                {
                    d += dS;
                    --yf;
                    ++yb;
                }
                else
                {
                    d += dSE;
                    ++xf;
                    --xb;
                    --yf;
                    ++yb;
                }
                DrawPixel(xf, yf, clr, shape, del);
                DrawPixel(xb, yb, clr, shape, del);
            }
        }

        static public bool DrawLine(int x0, int y0, int x1, int y1, int _thickness, Color clr, bool del = false)
        {
            bool[,] shape = FillShape(_thickness);
            if (x0 > x1)
            {
                int tempx = x0;
                int tempy = y0;
                x0 = x1;
                x1 = tempx;
                y0 = y1;
                y1 = tempy;
            }
            int dy = y1 - y0;
            int dx = x1 - x0;
            if (y1 >= y0)
            {
                if (dy > dx)
                {
                    SymmetricLineNNE(x0, y0, x1, y1, shape, clr, del); // not ok
                    return true;
                }
                else
                {
                    SymmetricLineENE(x0, y0, x1, y1, shape, clr, del); // ok
                    return true;
                }
            }
            else
            {
                if (dy > -dx)
                {
                    SymmetricLineESE(x0, y0, x1, y1, shape, clr, del); // not ok
                    return true;
                }
                else
                {
                    SymmetricLineSSE(x0, y0, x1, y1, shape, clr, del); // not ok
                    return true;

                }
            }

        }

        static public void MidpointCircle(int R, int offestX, int offsetY, Color clr, int _thickness, bool del = false)
        {
            int dE = 3;
            int dSE = 5 - 2 * R;
            int d = 1 - R;
            int x = 0;
            int y = R;
            bool[,] shape = FillShape(_thickness);
            DrawPixel(x + offestX, y + offsetY, clr, shape, del);
            DrawPixel(-x + offestX, y + offsetY, clr, shape, del);
            DrawPixel(x + offestX, -y + offsetY, clr, shape, del);
            DrawPixel(-x + offestX, -y + offsetY, clr, shape, del);
            DrawPixel(y + offestX, x + offsetY, clr, shape, del);
            DrawPixel(-y + offestX, x + offsetY, clr, shape, del);
            DrawPixel(y + offestX, -x + offsetY, clr, shape, del);
            DrawPixel(-y + offestX, -x + offsetY, clr, shape, del);
            while (y > x)
            {
                if (d < 0)
                {
                    d += dE;
                    dE += 2;
                    dSE += 2;
                }
                else
                {
                    d += dSE;
                    dE += 2;
                    dSE += 4;
                    --y;
                }
                ++x;
                DrawPixel(x + offestX, y + offsetY, clr, shape, del);
                DrawPixel(-x + offestX, y + offsetY, clr, shape, del);
                DrawPixel(x + offestX, -y + offsetY, clr, shape, del);
                DrawPixel(-x + offestX, -y + offsetY, clr, shape, del);
                DrawPixel(y + offestX, x + offsetY, clr, shape, del);
                DrawPixel(-y + offestX, x + offsetY, clr, shape, del);
                DrawPixel(y + offestX, -x + offsetY, clr, shape, del);
                DrawPixel(-y + offestX, -x + offsetY, clr, shape, del);
            }
        }

        // midpoitn circle fill
        /*
        static public void MidpointCircleFill(int R, int offestX, int offsetY, bool del = false)
        {
            int dE = 3;
            int dSE = 5 - 2 * R;
            int d = 1 - R;
            int x = 0;
            int y = R;
            DrawPixel(x + offestX, y + offsetY, del);
            DrawPixel(-x + offestX, y + offsetY, del);
            DrawPixel(x + offestX, -y + offsetY, del);
            DrawPixel(-x + offestX, -y + offsetY, del);
            DrawPixel(y + offestX, x + offsetY, del);
            DrawPixel(-y + offestX, x + offsetY, del);
            DrawPixel(y + offestX, -x + offsetY, del);
            DrawPixel(-y + offestX, -x + offsetY, del);
            int counter;
            while (y > x)
            {
                counter = 0;
                if (d < 0)
                {
                    d += dE;
                    dE += 2;
                    dSE += 2;
                }
                else
                {
                    d += dSE;
                    dE += 2;
                    dSE += 4;
                    --y;
                }
                ++x;
                DrawPixel(x + offestX, y + offsetY, del);
                DrawPixel(-x + offestX, y + offsetY, del);
                DrawPixel(x + offestX, -y + offsetY, del);
                DrawPixel(-x + offestX, -y + offsetY, del);
                DrawPixel(y + offestX, x + offsetY, del);
                DrawPixel(-y + offestX, x + offsetY, del);
                DrawPixel(y + offestX, -x + offsetY, del);
                DrawPixel(-y + offestX, -x + offsetY, del);
                while(counter<R)
                {
                    DrawPixel(x + offestX - R, y + offsetY - R, del);
                    DrawPixel(-x + offestX - R, y + offsetY - R, del);
                    DrawPixel(x + offestX - R, -y + offsetY - R, del);
                    DrawPixel(-x + offestX - R, -y + offsetY - R, del);
                    DrawPixel(y + offestX - R, x + offsetY - R, del);
                    DrawPixel(-y + offestX - R, x + offsetY - R, del);
                    DrawPixel(y + offestX - R, -x + offsetY - R, del);
                    DrawPixel(-y + offestX - R, -x + offsetY - R, del);
                }
            }
        }
        */


        // couldnt get this to work
        static void DrawHalfCircle(int centerX, int centerY, int xdX, int xdY, int r, Color clr)
        {
            int d = 1 - r;
            int x = 0;
            int y = r;
            if (Sign(centerX, centerY, centerX + xdX, centerY + xdY, centerX - x, centerY + y)) DrawPixel(centerX - x, centerY + y, clr);
            if (Sign(centerX, centerY, centerX + xdX, centerY + xdY, centerX - x, centerY - y)) DrawPixel(centerX - x, centerY - y, clr);
            if (Sign(centerX, centerY, centerX + xdX, centerY + xdY, centerX + x, centerY + y)) DrawPixel(centerX + x, centerY + y, clr);
            if (Sign(centerX, centerY, centerX + xdX, centerY + xdY, centerX + x, centerY - y)) DrawPixel(centerX + x, centerY - y, clr);

            while (y > x)
            {
                if (d < 0)
                {
                    d += 2 * x + 3;
                }
                else
                {
                    d += 2 * x - 2 * y + 5;
                    --y;
                }
                ++x;
                if (Sign(centerX, centerY, centerX + xdX, centerY + xdY, centerX - x, centerY + y)) DrawPixel(centerX - x, centerY + y, clr);
                if (Sign(centerX, centerY, centerX + xdX, centerY + xdY, centerX - x, centerY - y)) DrawPixel(centerX - x, centerY - y, clr);
                if (Sign(centerX, centerY, centerX + xdX, centerY + xdY, centerX + x, centerY + y)) DrawPixel(centerX + x, centerY + y, clr);
                if (Sign(centerX, centerY, centerX + xdX, centerY + xdY, centerX + x, centerY - y)) DrawPixel(centerX + x, centerY - y, clr);

            }
        }

        static bool Sign(int Dx, int Dy, int Ex, int Ey, int Fx, int Fy)
        {
            return ((Ex - Dx) * (Fy - Dy) - (Ey - Dy) * (Fx - Dx)) < 0 ? true : false;
        }



        private void img_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                counter = 0;
                i.Cursor = Cursors.Arrow;
                IShape tmpShape = FindClosestShape((int)e.GetPosition(i).X, (int)e.GetPosition(i).Y);
                if (tmpShape == null)
                {
                    return;
                }
                tmpShape.DeleteShape();
                shapes.Remove(tmpShape);
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                if (counter != 0)
                {
                    i.Cursor = Cursors.Arrow;
                    counter = 0;
                    if (mode == Modes.DrawPolygons)
                    {
                        shapes[shapes.Count - 1].DeleteShape();
                    }
                    if (shapes.Count > 1)
                    {
                        RedrawShapes();
                    }
                }
                if (mode == Modes.Moving)
                {
                    mode = lastMode;
                    i.Cursor = Cursors.Arrow;
                    movingShape = null;
                    return;
                }
                IShape xd = FindClosestShape((int)e.GetPosition(i).X, (int)e.GetPosition(i).Y);
                if (rightClickMode == RightClickModes.Move)
                {
                    movingShape = xd;
                    if (movingShape != null)
                    {
                        lastMode = mode;
                        i.Cursor = Cursors.Hand;
                        mode = Modes.Moving;
                    }
                    //if (shapes.Count>0)
                    //{
                    //    RedrawShapes();
                    //}
                }
                else if (rightClickMode == RightClickModes.Colour)
                {
                    if (xd == null)
                    {
                        return;
                    }
                    xd.ReColour(color);
                }
                else if (rightClickMode == RightClickModes.Resize)
                {
                    if (xd == null)
                    {
                        return;
                    }
                    xd.Resize(thickness);
                    if (shapes.Count>1)
                    {
                        RedrawShapes();
                    }
                }
            }
        }

        private static IShape FindClosestShape(int x, int y)
        {
            if (shapes.Count == 0)
            {
                return null;
            }
            else
            {
                foreach (IShape shape in shapes)
                {

                    if (shape.WasClicked(x, y))
                    {
                        return shape;
                    }
                }
                return null;
            }
        }

        // remove all shapes
        private void RemoveAllShapes()
        {
            foreach (IShape shape in shapes.ToList())
            {
                shape.DeleteShape();
                shapes.Remove(shape);
            }
        }

        // clear menu item
        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            RemoveAllShapes();
        }
        private void RedrawShapes()
        {
            foreach (IShape shape in shapes)
            {
                shape.DeleteShape();
                shape.DrawShape();
            }
        }

        private static void FloodFill(int x, int y, Color clr, bool del = false)
        {
            if (x < 0 || x >= actualWidth) return;
            if (y < 0 || y >= actualHeight) return;
            if (del)
            {
                if (MyGetPixel(x, y) != 0)
                {
                    DrawPixel(x, y, clr, del);
                    FloodFill(x + 1, y, clr, del);
                    FloodFill(x, y + 1, clr, del);
                    FloodFill(x - 1, y, clr, del);
                    FloodFill(x, y - 1, clr, del);
                }
            }
            else
            {
                //bool cond = FindClosestShape(x, y) is Brush ? true : false;
                if (MyGetPixel(x, y) == 0)
                {
                    DrawPixel(x, y, clr);
                    FloodFill(x + 1, y, clr);
                    FloodFill(x, y + 1, clr);
                    FloodFill(x - 1, y, clr);
                    FloodFill(x, y - 1, clr);
                }

            }
        }

        //public static void DrawBrush(int x, int y, Color clr, bool del = false)
        //{
        //    if (x + brushThickness > actualWidth || x - brushThickness < 0 ||
        //        y + brushThickness > actualHeight || y - brushThickness < 0)
        //    {
        //        MessageBox.Show("Brush out of bounds");
        //        i.Cursor = Cursors.Arrow;
        //        return;
        //    }
        //    MidpointCircle(brushThickness, x, y, clr, del);
        //    FloodFill(x, y, clr, del);
        //    shapes.Add(new Brush(brushThickness, x, y));
        //}

        // info IMPLEMENT
        private void MenuItem_Click_8(object sender, RoutedEventArgs e)
        {
            string str = "XD";
            MessageBox.Show(str, "Functionalities", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // drawing mode menu
        // pick drawing brush
        private void MenuItem_Click_7(object sender, RoutedEventArgs e)
        {
            SetMode(Modes.DrawBrush);
        }
        // pick drawing circles
        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            SetMode(Modes.DrawCircles);
        }
        // pick drawing elipses
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetMode(Modes.DrawElipse);
        }
        // pick drawing lines
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            SetMode(Modes.DrawLines);
        }
        // pick drawing polygons
        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            SetMode(Modes.DrawPolygons);
        }
        private void SetMode(Modes _mode)
        {
            mode = _mode;
            dataContext.DrawingMode = _mode.ToString();
            counter = 0;
        }

        // loading & saving
        // save shapes
        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            if (shapes != null)
            {
                var json = JsonConvert.SerializeObject(shapes);
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Title = "Save shapes to JSON";
                dlg.FileName = "shapes"; // Default file name
                dlg.DefaultExt = ".json"; // Default file extension
                dlg.Filter = "JSON | *.json"; // Filter files by extension
                if (dlg.ShowDialog() == true)
                {
                    string filename = dlg.FileName;
                    using (StreamWriter file = File.CreateText(filename))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        file.Write(json);
                    }
                }
            }
        }

        // load shapes
        private void MenuItem_Click_6(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select shapes JSON";
            op.Filter = "JSON | *.json"; // Filter files by extension
            if (op.ShowDialog() == true)
            {
                var filename = op.FileName;
                var json = System.IO.File.ReadAllText(filename);
                //MessageBox.Show(json);
                RemoveAllShapes();
                try
                {
                    shapes = JsonConvert.DeserializeObject<List<IShape>>(json);
                    RedrawShapes();
                }
                catch
                {
                    MessageBox.Show(e.ToString() + "Unexpected error while loading shapes from JSON");
                }

            }
        }

        // poly move menu
        // by vertex
        private void MenuItem_Click_11(object sender, RoutedEventArgs e)
        {
            SetPolyMoveMode(PolyMoveModes.ByVertex);
        }

        // as a whole
        private void MenuItem_Click_12(object sender, RoutedEventArgs e)
        {
            SetPolyMoveMode(PolyMoveModes.WholePoly);
        }

        private void SetPolyMoveMode(PolyMoveModes _mode)
        {
            polyMode = _mode;
            dataContext.PolyMoveMode = _mode.ToString();
        }

        // move
        private void MenuItem_Click_9(object sender, RoutedEventArgs e)
        {
            SetRightClickMode(RightClickModes.Move);
        }

        // colour
        private void MenuItem_Click_10(object sender, RoutedEventArgs e)
        {
            SetRightClickMode(RightClickModes.Colour);
            //MessageBox.Show("not implemented");
        }

        private void SetRightClickMode(RightClickModes _mode)
        {
            rightClickMode = _mode;
            dataContext.RightClickMode = _mode.ToString();
        }

        private void colourPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            color = (Color)colourPicker.SelectedColor;
            //MessageBox.Show(color.ToString());
        }

        private void MenuItem_Click_13(object sender, RoutedEventArgs e)
        {
            try
            {
                RedrawShapes();
            }
            catch
            {
                MessageBox.Show(e.ToString() + " Unexpected error while redrawing shapes");
            }
        }

        private void IntegerUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            thickness = dataContext.BrushThickness;
        }

        private void MenuItem_Click_14(object sender, RoutedEventArgs e)
        {
            SetRightClickMode(RightClickModes.Resize);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //MessageBox.Show(sender.ToString());
            //MainWindow xd = sender as MainWindow;
            //SetTitle($"{xd.ActualWidth}, {xd.ActualHeight}");

        }
    }

}
