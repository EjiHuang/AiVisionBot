using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MainView.View
{
    /// <summary>
    /// CaptureMarkView.xaml 的交互逻辑
    /// </summary>
    public partial class CaptureMarkView : Window
    {
        private double x;
        private double y;
        private double width;
        private double height;
        private bool isMouseDown = false;
        public Bitmap bitmap;


        public CaptureMarkView()
        {
            InitializeComponent();
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = true;
            x = e.GetPosition(null).X;
            y = e.GetPosition(null).Y;
        }

        private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                double curx = e.GetPosition(null).X;
                double cury = e.GetPosition(null).Y;

                System.Windows.Shapes.Rectangle r = new System.Windows.Shapes.Rectangle
                {
                    Stroke = System.Windows.Media.Brushes.White,
                    Fill = System.Windows.Media.Brushes.White,
                    StrokeThickness = 1,
                    Width = Math.Abs(curx - x),
                    Height = Math.Abs(cury - y)
                };

                cnv.Children.Clear();
                cnv.Children.Add(r);
                Canvas.SetLeft(r, Math.Min(x, curx));
                Canvas.SetTop(r, Math.Min(y, cury));

                if (e.LeftButton == MouseButtonState.Released)
                {
                    cnv.Children.Clear();
                    Hide();
                    width = Math.Abs(curx - x);
                    height = Math.Abs(cury - y);

                    bitmap = ScreenShotMaker.CaptureScreen(width, height, Math.Min(x, curx) - 7, Math.Min(y, cury) - 7);

                    x = y = 0;
                    isMouseDown = false;
                    Close();
                }
            }
        }
    }

    partial class ScreenShotMaker
    {
        public static Bitmap CaptureScreen(double width, double height, double x = 0, double y = 0)
        {
            int ix, iy, iw, ih;
            ix = Convert.ToInt32(x);
            iy = Convert.ToInt32(y);
            iw = Convert.ToInt32(width);
            ih = Convert.ToInt32(height);
            if (iw <= 0) iw = 1;
            if (ih <= 0) ih = 1;
            Bitmap image = new Bitmap(iw, ih, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(image);
            graphics.CopyFromScreen(ix, iy, 0, 0, new System.Drawing.Size(iw, ih), CopyPixelOperation.SourceCopy);
            return image;
        }

        public static Bitmap SaveScreen(double width, double height, double x = 0, double y = 0)
        {
            int ix, iy, iw, ih;
            ix = Convert.ToInt32(x);
            iy = Convert.ToInt32(y);
            iw = Convert.ToInt32(width);
            ih = Convert.ToInt32(height);
            if (iw <= 0) iw = 1;
            if (ih <= 0) ih = 1;
            Bitmap myImage = new Bitmap(iw, ih);
            try
            {

                Graphics gr1 = Graphics.FromImage(myImage);
                IntPtr dc1 = gr1.GetHdc();
                IntPtr dc2 = NativeMethods.GetWindowDC(NativeMethods.GetForegroundWindow());
                NativeMethods.BitBlt(dc1, ix, iy, iw, ih, dc2, ix, iy, 13369376);
                gr1.ReleaseHdc(dc1);
            }
            catch { }
            return myImage;
        }

        internal class NativeMethods
        {

            [DllImport("user32.dll")]
            public extern static IntPtr GetDesktopWindow();
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hwnd);
            [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            public static extern IntPtr GetForegroundWindow();
            [DllImport("gdi32.dll")]
            public static extern UInt64 BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, System.Int32 dwRop);

        }
    }
}
