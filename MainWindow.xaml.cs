#nullable enable
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
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;

namespace cg_proj2
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static int BRUSH_THICKNESS = 6;

        static WriteableBitmap writeable;
        static int brushThickness = BRUSH_THICKNESS;
        //static WriteableBitmap writeableBitmap;
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
        static PolyMoveModes polyMode = PolyMoveModes.WholePoly;
        static RightClickModes rightClickMode = RightClickModes.Move;
        static RightClickModes lastRightClickMode;
        static PolyLineResize polyResize = PolyLineResize.Whole;
        static Modes lastMode;
        static Polygon poly;
        static Polygon polyToClip;
        static Polygon polyClipping;
        static Color color;
        static int thickness;
        static Dictionary<Polygon, List<Polygon>> clippingPolys;
        static Dictionary<Polygon, List<Line>> clippedLines;
        static private Test dataContext = new Test("CG_proj3", mode.ToString(), rightClickMode.ToString(), polyMode.ToString(), 1, polyResize.ToString());
        public class Test : INotifyPropertyChanged
        {
            private string title;
            private string rightClickMode;
            private string drawingMode;
            private string polyMoveMode;
            private int brushThickness;
            private string polyResize;
            public Test(string str, string drawingStr, string rightStr, string polyStr, int _brushThickness, string xd)
            {
                title = str;
                rightClickMode = rightStr;
                drawingMode = drawingStr;
                polyMoveMode = polyStr;
                brushThickness = _brushThickness;
                polyResize = xd;
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

            public string PolyResize
            {
                get { return $"Poly resize mode: {polyResize}"; }
                set { polyResize = value; NotifyPropertyChanged("PolyResize"); }

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
            string str1 = "Since the controls are rather not very intuitive:\n\n";
            string str2 = "I recommend loading simple_polygons.json or drawing simple polygon using controls\n";
            string str3 = "Then to enable clipping: click on Clipping mode\n";
            string str4 = "And now right click vertex of a polygon to grab it and draw the clipping rectangle/polygon (select which one by using menu -> drawing)\n";
            string str5 = "Now to test moving, uncheck the Clipping mode and move things around via selecting Move option from right click menu (it's default)\n\n";
            string str6 = "Filling is just, select fill in the right click menu and select polygon, it'll get filled with the colour currently selected by user\n";
            string str7 = "Flood fill is similar, select Flood Fill from right click menu and click on a region";
            string msg = str1 + str2 + str3 + str4 + str5 + str6 + str7;
            MessageBox.Show(msg);
            lastMode = mode;
            shapes = new List<IShape>();
            this.DataContext = dataContext;
            clippingPolys = new Dictionary<Polygon, List<Polygon>>();
            clippedLines = new Dictionary<Polygon, List<Line>>();
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
                //writeableBitmap.Lock();
                //MessageBox.Show("xd");
                unsafe
                {
                    IntPtr pBackBuffer = writeable.BackBuffer;

                    pBackBuffer += row * writeable.BackBufferStride;
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

                //writeableBitmap.AddDirtyRect(new Int32Rect(column, row, 1, 1));
            }
            finally
            {
                //writeableBitmap.Unlock();
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
                //writeableBitmap.Lock();
                //MessageBox.Show(n.ToString());
                unsafe
                {
                    IntPtr pBackBuffer = writeable.BackBuffer;
                    byte* pBuff = (byte*)pBackBuffer.ToPointer();
                    for (int i = 0; i < n; i++)
                    {
                        x = _x - (mid - i);
                        for (int j = 0; j < n; j++)
                        {
                            y = _y - (mid - j);
                            if (shape[i, j])
                            {
                                pBuff[4 * x + (y * writeable.BackBufferStride)] = b;
                                pBuff[4 * x + (y * writeable.BackBufferStride) + 1] = g;
                                pBuff[4 * x + (y * writeable.BackBufferStride) + 2] = r;
                                //pBuff[4 * x + (y * writeableBitmap.BackBufferStride) + 3] = 255;

                            }
                        }
                    }

                }

                //writeable.AddDirtyRect(new Int32Rect(0, 0, actualWidth, actualHeight));
            }
            finally
            {
                //writeableBitmap.Unlock();
            }
        }

        static private int MyGetPixel(int x, int y)
        {
            int column = x;
            int row = y;
            try
            {

                unsafe
                {
                    IntPtr pBackBuffer = writeable.BackBuffer;

                    pBackBuffer += row * writeable.BackBufferStride;
                    pBackBuffer += column * 4;
                    //*((int*)pBackBuffer)
                    //MessageBox.Show((*((int*)pBackBuffer)).ToString());
                    return *((int*)pBackBuffer);

                }
            }
            finally
            {
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
                    if (clippingPolys.ContainsKey(tmpPoly))
                    {
                        DeleteClippedLines(tmpPoly);
                        tmpPoly.MovePolygon(x, y, polyMode);
                        foreach (Polygon polygon in clippingPolys[tmpPoly])
                        {
                            CyrusBeck(polygon, tmpPoly);
                        }
                    }
                    else if (clippingPolys.ContainsClippingPoly(tmpPoly))
                    {
                        //MessageBox.Show("XDDDDDD");
                        DeleteClippedLinesSingle(tmpPoly);
                        tmpPoly.MovePolygon(x, y, polyMode);
                        CyrusBeck(tmpPoly, clippingPolys.FindPolyKey(tmpPoly));
                    }
                    else
                    {
                        tmpPoly.MovePolygon(x, y, polyMode);
                    }

                    i.Cursor = Cursors.Arrow;
                    mode = lastMode;
                    movingShape = null;
                    polyToClip = null;
                }

                RedrawShapes();

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
                    //Point[] xddd = CyrusBeckLine((shapes[0] as Polygon), new Point[] { new Point(p1.X, p1.Y), new Point(p2.X, p2.Y) });
                    //MessageBox.Show($"{xddd.Length}");
                    //DrawLine((int)xddd[0].X, (int)xddd[0].Y, (int)xddd[1].X, (int)xddd[1].Y, 3, Colors.Red);
                    //Line? tmp;
                    //tmp = CyrusBeckLine(shapes[0] as Polygon, (int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y);
                    //CyrusBeck(shapes[0] as Polygon, (int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y);
                    //MessageBox.Show(CyrusBeckLine(shapes[0] as Polygon, (int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y)!=null ? ");
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
            else if (rightClickMode != RightClickModes.PickPolyToClip || polyToClip != null)
            {
                if (mode == Modes.DrawPolygons)
                {
                    //MyGetPixel(x, y);
                    if (counter == 0)
                    {
                        i.Cursor = Cursors.Cross;
                        poly = new Polygon(x, y, polyToClip == null ? color : Colors.White, polyToClip == null ? thickness : 1);
                        shapes.Add(poly);
                        counter++;
                    }
                    else
                    {
                        if (poly.AddVertex(x, y))
                        {
                            i.Cursor = Cursors.Arrow;
                            counter = 0;
                            if ((mode == Modes.ClippingByPolygon || mode == Modes.ClippingByRectangle) && polyToClip != null)
                            {
                                if (!((shapes[shapes.Count - 1] as Polygon).CheckIfConvex()))
                                {
                                    poly = null;
                                    shapes[shapes.Count - 1].DeleteShape();
                                    shapes.Remove(shapes[shapes.Count - 1]);
                                    RedrawShapes();
                                    return;
                                }
                                (shapes[shapes.Count - 1] as Polygon).SortClockwise();

                                CyrusBeck(shapes[shapes.Count - 1] as Polygon, polyToClip);
                                (shapes[shapes.Count - 1] as Polygon).IsClipping = true;
                                List<Polygon> tmpList;
                                if (!clippingPolys.ContainsKey(polyToClip))
                                {
                                    clippingPolys.Add(polyToClip, new List<Polygon>() { shapes[shapes.Count - 1] as Polygon });
                                }
                                else
                                {
                                    clippingPolys[polyToClip].Add(shapes[shapes.Count - 1] as Polygon);
                                }
                                shapes.Remove(shapes[shapes.Count - 1]);
                                polyToClip = null;
                                //clippingPolys.Add(polyToClip, shapes[shapes.Count - 1] as Polygon);
                            }
                            poly = null;
                            (shapes[shapes.Count - 1] as Polygon).SortClockwise();
                            return;
                        }
                    }

                }
                else if (mode == Modes.DrawRectangles)
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
                        shapes.Add(new Rectangle((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, polyToClip == null ? color : Colors.White, polyToClip == null ? thickness : 1));
                        if ((mode == Modes.ClippingByPolygon || mode == Modes.ClippingByRectangle) && polyToClip != null)
                        {
                            CyrusBeck(shapes[shapes.Count - 1] as Polygon, polyToClip);
                            (shapes[shapes.Count - 1] as Polygon).IsClipping = true;
                            if (!clippingPolys.ContainsKey(polyToClip))
                            {
                                clippingPolys.Add(polyToClip, new List<Polygon>() { shapes[shapes.Count - 1] as Polygon });
                            }
                            else
                            {
                                clippingPolys[polyToClip].Add(shapes[shapes.Count - 1] as Polygon);
                            }
                            shapes.Remove(shapes[shapes.Count - 1]);

                        }
                        p1 = new Point();
                        p2 = new Point();
                        i.Cursor = Cursors.Arrow;
                        polyToClip = null;
                    }
                }
            }

        }

        private void DeleteClippedLines(Polygon key)
        {
            if (clippingPolys.ContainsKey(key))
            {
                foreach (Polygon polygon in clippingPolys[key])
                {
                    DeleteClippedLinesSingle(polygon);
                }

            }
        }

        private void DeleteClippedLinesSingle(Polygon polygon)
        {
            foreach (Line line in clippedLines[polygon].ToArray())
            {
                line.DeleteShape();
                clippedLines[polygon].Remove(line);
            }
            clippedLines.Remove(polygon);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            i = this.img;
            RenderOptions.SetBitmapScalingMode(i, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(i, EdgeMode.Aliased);
            writeable = new WriteableBitmap(
                900,
                900,
                96,
                96,
                PixelFormats.Bgr32,
                null);

            i.Source = writeable;
            i.Stretch = Stretch.None;
            i.HorizontalAlignment = HorizontalAlignment.Left;
            i.VerticalAlignment = VerticalAlignment.Top;
            i.MouseLeftButtonDown +=
                new MouseButtonEventHandler(i_MouseLeftButtonDown);
            i.MouseRightButtonDown +=
                new MouseButtonEventHandler(i_MouseRightButtonDown);
            actualHeight = (int)writeable.Height;
            actualWidth = (int)writeable.Width;
            //writeable = writeableBitmap;
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
            //Rectangle rect = new Rectangle(0, 0, writeable.Width, writeable.Height);
            writeable.Lock();
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
                }
                else
                {
                    SymmetricLineENE(x0, y0, x1, y1, shape, clr, del); // ok
                }
            }
            else
            {
                if (dy > -dx)
                {
                    SymmetricLineESE(x0, y0, x1, y1, shape, clr, del); // not ok
                }
                else
                {
                    SymmetricLineSSE(x0, y0, x1, y1, shape, clr, del); // not ok

                }
            }
            writeable.AddDirtyRect(new Int32Rect(0, 0, actualWidth, actualHeight));
            writeable.Unlock();
            i.Source = writeable;
            return true;
        }

        static public void MidpointCircle(int R, int offestX, int offsetY, Color clr, int _thickness, bool del = false)
        {
            writeable.Lock();
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
            writeable.AddDirtyRect(new Int32Rect(0, 0, actualWidth, actualHeight));
            writeable.Unlock();
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
                if (tmpShape is Polygon)
                {
                    if ((tmpShape as Polygon).IsClipping)
                    {
                        if (clippedLines.ContainsKey(tmpShape as Polygon))
                        {
                            foreach (Line line in clippedLines[tmpShape as Polygon])
                            {
                                line.DeleteShape();
                            }
                            clippedLines.Remove(tmpShape as Polygon);
                        }

                        var myKey = clippingPolys.DeleteFromList(tmpShape as Polygon);
                        //clippingPolys.Remove(myKey);
                        tmpShape.DeleteShape();
                        myKey.DrawShape();
                    }
                    else
                    {
                        if (clippingPolys.ContainsKey(tmpShape as Polygon))
                        {
                            foreach (Polygon polygon in clippingPolys[tmpShape as Polygon])
                            {
                                foreach (Line line in clippedLines[polygon])
                                {
                                    line.DeleteShape();
                                }
                                clippedLines.Remove(polygon);
                                polygon.DeleteShape();
                            }
                            clippingPolys.Remove(tmpShape as Polygon);
                        }
                        tmpShape.DeleteShape();
                    }
                }
                else
                {
                    tmpShape.DeleteShape();
                }
                shapes.Remove(tmpShape);
                RedrawShapes();
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
                    if (shapes.Count > 1)
                    {
                        RedrawShapes();
                    }
                }
                else if (rightClickMode == RightClickModes.PickPolyToClip)
                {
                    if (xd == null || !(xd is Polygon))
                    {
                        polyToClip = null;
                        i.Cursor = Cursors.Arrow;
                        return;
                    }
                    if (xd is Rectangle)
                    {
                        MessageBox.Show("Not possible");
                    }
                    else
                    {
                        polyToClip = xd as Polygon;
                        i.Cursor = Cursors.Cross;
                    }

                }
                else if (rightClickMode == RightClickModes.PolyFill)
                {
                    if (xd == null || !(xd is Polygon))
                    {
                        return;
                    }
                    (xd as Polygon).EdgeTableFill(color);
                }
                else if (rightClickMode == RightClickModes.FloodFill)
                {
                    //(int)e.GetPosition(i).X, (int)e.GetPosition(i).Y
                    int x = (int)e.GetPosition(i).X, y = (int)e.GetPosition(i).Y;
                    int pixelColor = MyGetPixel(x, y);
                    Color clr = new Color();
                    clr.R = (byte)((pixelColor & 0x00ff0000) >> 16);
                    clr.G = (byte)((pixelColor & 0x0000ff00) >> 8);
                    clr.B = (byte)(pixelColor & 0x000000ff);
                    FloodFillNR(x, y, clr, color);
                    //MessageBox.Show($"{red}, {green}, {blue}");
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
            }

            if (clippingPolys.Count == 0)
            {
                return null;
            }
            else
            {
                foreach (List<Polygon> list in clippingPolys.Values)
                {
                    foreach (Polygon poly in list)
                    {
                        if (poly.WasClicked(x, y))
                        {
                            return poly;
                        }
                    }
                }
            }

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
            }
            return null;
        }

        // remove all shapes
        private void RemoveAllShapes()
        {
            foreach (IShape shape in shapes.ToList())
            {
                shape.DeleteShape();
                shapes.Remove(shape);
            }
            if (clippingPolys.Count > 0)
            {
                foreach (List<Polygon> list in clippingPolys.Values)
                {
                    foreach (Polygon poly in list)
                    {
                        poly.DeleteShape();
                    }
                }
                clippingPolys.Clear();
            }
            if (clippedLines.Count > 0)
            {
                foreach (List<Line> list in clippedLines.Values)
                {
                    foreach (Line line in list)
                    {
                        line.DeleteShape();
                    }
                }
                clippedLines.Clear();
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
            if (clippingPolys.Count > 0)
            {
                foreach (List<Polygon> list in clippingPolys.Values)
                {
                    foreach (Polygon poly in list)
                    {
                        poly.DeleteShape();
                        poly.DrawShape();
                    }
                }
            }
            if (clippedLines.Count > 0)
            {
                foreach (List<Line> list in clippedLines.Values)
                {
                    foreach (Line line in list)
                    {
                        line.DeleteShape();
                        line.DrawShape();
                    }
                }
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
            string str = "Left click: Drawing";
            string str2 = "Right click: Depends on the mode which can be set in Options->Right click";
            string str3 = "Middle click: Deleting given shape";
            MessageBox.Show($"{str}\n{str2}\n{str3}", "Functionalities", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if ((bool)clippingModeCheckBox.IsChecked)
            {
                SetMode(Modes.ClippingByPolygon);
            }
            else
            {
                SetMode(Modes.DrawPolygons);
            }
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
                JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
                var json = JsonConvert.SerializeObject(shapes, Formatting.Indented, settings);
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Title = "Save shapes to JSON";
                dlg.FileName = "shapes"; // Default file name
                dlg.DefaultExt = ".json"; // Default file extension
                dlg.Filter = "JSON | *.json"; // Filter files by extension
                //MessageBox.Show(json);
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
                JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
                var json = System.IO.File.ReadAllText(filename);
                //MessageBox.Show(json);
                RemoveAllShapes();
                try
                {
                    shapes = JsonConvert.DeserializeObject<List<IShape>>(json, settings);
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

        private void SetPolyResizeMode(PolyLineResize _mode)
        {
            polyResize = _mode;
            dataContext.PolyResize = _mode.ToString();
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

        private void MenuItem_Click_15(object sender, RoutedEventArgs e)
        {
            SetPolyResizeMode(PolyLineResize.Whole);
        }

        private void MenuItem_Click_16(object sender, RoutedEventArgs e)
        {
            SetPolyResizeMode(PolyLineResize.NextEdge);

        }

        private void MenuItem_Click_17(object sender, RoutedEventArgs e)
        {
            SetPolyResizeMode(PolyLineResize.PrevEdge);
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            rightClickMenu.IsEnabled = true;
            polyMoveMenu.IsEnabled = true;
            drawCirclesMenuItem.IsEnabled = true;
            drawLinesMenuItem.IsEnabled = true;
            mode = lastMode;
            SetMode(lastMode);
            SetRightClickMode(lastRightClickMode);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (mode == Modes.Moving)
            {
                clippingModeCheckBox.IsChecked = false;
                i.Cursor = Cursors.Arrow;
                return;
            }
            lastMode = mode;
            lastRightClickMode = rightClickMode;
            rightClickMenu.IsEnabled = false;
            polyMoveMenu.IsEnabled = false;
            drawCirclesMenuItem.IsEnabled = false;
            drawLinesMenuItem.IsEnabled = false;
            //SetRightClickMode(RightClickModes.Move);
            SetRightClickMode(RightClickModes.PickPolyToClip);
            SetMode(Modes.ClippingByRectangle);
        }

        private void MenuItem_Click_18(object sender, RoutedEventArgs e)
        {
            if ((bool)clippingModeCheckBox.IsChecked)
            {
                SetMode(Modes.ClippingByRectangle);
            }
            else
            {
                SetMode(Modes.DrawRectangles);
            }
        }

        private int DotProduct(Point p0, Point p1)
        {
            return (int)(p0.X * p1.X + p0.Y * p1.Y);
        }

        private Line? CyrusBeckLine(Polygon clippingPoly, int x1, int y1, int x2, int y2)
        {
            //if (!(clippingPoly.CheckIfConvex()))
            //{
            //    MessageBox.Show("Poly not convex");
            //    return null;
            //}
            Point[] lineToClip = new Point[] { new Point(x1, y1), new Point(x2, y2) };
            List<Point> vertices = clippingPoly.Vertices;
            int n = vertices.Count - 1;
            Point[] normal = new Point[n];

            for (int i = 0; i < n; i++)
            {
                normal[i].Y = vertices[(i + 1) % n].X - vertices[i].X;
                normal[i].X = vertices[i].Y - vertices[(i + 1) % n].Y;
            }

            Point P1toP0 = new Point(lineToClip[1].X - lineToClip[0].X, lineToClip[1].Y - lineToClip[0].Y);
            Point[] P0toPEi = new Point[n];

            for (int i = 0; i < n; i++)
            {

                P0toPEi[i].X = vertices[i].X - lineToClip[0].X;

                P0toPEi[i].Y = vertices[i].Y - lineToClip[0].Y;
            }

            int[] num = new int[n], denom = new int[n];
            for (int i = 0; i < n; i++)
            {
                num[i] = DotProduct(normal[i], P0toPEi[i]);
                denom[i] = DotProduct(normal[i], P1toP0);
            }

            double[] t = new double[n];
            List<double> tmpE = new List<double>(), tmpL = new List<double>();
            for (int i = 0; i < n; i++)
            {

                t[i] = (double)(num[i]) / (float)(denom[i]);

                if (denom[i] > 0)
                    tmpE.Add(t[i]);
                else
                    tmpL.Add(t[i]);
            }

            double[] tmp = new double[2];
            tmpE.Add(0);
            tmp[0] = tmpE.Max();

            tmpL.Add(1);
            tmp[1] = tmpL.Min();

            if (tmp[0] > tmp[1])
            {
                return null;
            }
            return new Line((int)((double)lineToClip[0].X + (double)P1toP0.X * (double)tmp[0]), (int)((double)lineToClip[0].Y + (double)P1toP0.Y * (double)tmp[0]),
                (int)((double)lineToClip[0].X + (double)P1toP0.X * (double)tmp[1]), (int)((double)lineToClip[0].Y + (double)P1toP0.Y * (double)tmp[1]),
                Colors.Red, 3);
        }

        private void CyrusBeck(Polygon clippingPoly, Polygon toClipPoly)
        {
            //clippingPoly.SortClockwise();
            //toClipPoly.SortClockwise();
            //MessageBox.Show("XD");
            List<Line> tmp = new List<Line>();
            Line tmpLine;
            for (int i = 1; i < toClipPoly.Vertices.Count; i++)
            {
                //MainWindow.DrawLine((int)toClipPoly.Vertices[i - 1].X, (int)toClipPoly.Vertices[i - 1].Y, (int)toClipPoly.Vertices[i].X, (int)toClipPoly.Vertices[i].Y, PolyThickness, ColorPoly);
                tmpLine = CyrusBeckLine(clippingPoly, (int)toClipPoly.Vertices[i - 1].X, (int)toClipPoly.Vertices[i - 1].Y, (int)toClipPoly.Vertices[i].X, (int)toClipPoly.Vertices[i].Y);
                if (tmpLine != null)
                {
                    //MessageBox.Show("XD");
                    tmpLine.LineThickness = toClipPoly.PolyThickness + 2;
                    tmp.Add(tmpLine);
                    tmpLine.DrawShape();
                }
            }
            clippedLines.Add(clippingPoly, tmp);
        }

        private void MenuItem_Click_19(object sender, RoutedEventArgs e)
        {
            SetRightClickMode(RightClickModes.PolyFill);
        }

        private void FloodFillNR(int x0, int y0, Color targetColor, Color replacementColor)
        {
            writeable.Lock();
            Stack<Point> stack = new Stack<Point>();
            stack.Push(new Point(x0, y0));
            while (stack.Count > 0)
            {
                Point p = stack.Pop();
                int x = (int)p.X, y = (int)p.Y;
                if (x < 0 || x >= actualWidth || y < 0 || y >= actualHeight)
                {
                    continue;
                }
                DrawPixel(x, y, replacementColor);
                if (CheckCondFFNR(x + 1, y, targetColor)) stack.Push(new Point(x + 1, y));
                if (CheckCondFFNR(x - 1, y, targetColor)) stack.Push(new Point(x - 1, y));
                if (CheckCondFFNR(x, y + 1, targetColor)) stack.Push(new Point(x, y + 1));
                if (CheckCondFFNR(x, y - 1, targetColor)) stack.Push(new Point(x, y - 1));
            }

            writeable.AddDirtyRect(new Int32Rect(0, 0, actualWidth, actualHeight));
            writeable.Unlock();
            i.Source = writeable;
        }

        private bool CheckCondFFNR(int x, int y, Color targetColor)
        {
            int pixelColor = MyGetPixel(x, y);
            Color clr = new Color();
            clr.R = (byte)((pixelColor & 0x00ff0000) >> 16);
            clr.G = (byte)((pixelColor & 0x0000ff00) >> 8);
            clr.B = (byte)(pixelColor & 0x000000ff);
            if (targetColor == clr)
            {
                return true;
            }
            return false;
        }

        private void MenuItem_Click_20(object sender, RoutedEventArgs e)
        {
            SetRightClickMode(RightClickModes.FloodFill);
        }
    }
}
