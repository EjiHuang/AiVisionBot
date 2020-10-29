using CaptureCore;
using Composition.WindowsRuntimeHelpers;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using GameOverlay.Windows;
using ImageProcessor;
using MainView.Framework;
using MainView.Model;
using MainView.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.UI.Composition;

namespace MainView.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            Initialize();
        }

        #region public properties

        /// <summary>
        /// Process list collection.
        /// </summary>
        public ObservableCollection<ProcessModel> ProcessInfoes { get; } = new ObservableCollection<ProcessModel>();

        /// <summary>
        /// Process index.
        /// </summary>
        public int ProcessIndex
        {
            get => GetProperty(() => ProcessIndex);
            set => SetProperty(() => ProcessIndex, value);
        }

        /// <summary>
        /// Basic sample application.
        /// </summary>
        public BasicApplication Capturer
        {
            get => GetProperty(() => Capturer);
            set => SetProperty(() => Capturer, value);
        }

        /// <summary>
        /// Used to display the current frame image.
        /// </summary>
        public WriteableBitmap CurrentFrameImage
        {
            get => GetProperty(() => CurrentFrameImage);
            set => SetProperty(() => CurrentFrameImage, value);
        }

        /// <summary>
        /// App status bar text.
        /// </summary>
        public string StatusText
        {
            get => GetProperty(() => StatusText);
            set => SetProperty(() => StatusText, value);
        }

        /// <summary>
        /// Is enable image recognition method?
        /// </summary>
        public bool EnableImageRecognition
        {
            get => GetProperty(() => EnableImageRecognition);
            set => SetProperty(() => EnableImageRecognition, value);
        }

        /// <summary>
        /// The positive samples save folder's path.
        /// </summary>
        public string PositiveSamplesSavePath
        {
            get => GetProperty(() => PositiveSamplesSavePath);
            set => SetProperty(() => PositiveSamplesSavePath, value);
        }

        /// <summary>
        /// The negative samples save folder's path.
        /// </summary>
        public string NegativeSamplesSavePath
        {
            get => GetProperty(() => NegativeSamplesSavePath);
            set => SetProperty(() => NegativeSamplesSavePath, value);
        }

        #endregion

        #region private properties

        /// <summary>
        /// Save current frame?
        /// </summary>
        private bool _isSaveFrame = false;

        /// <summary>
        /// Current process list.
        /// </summary>
        private static List<EnumProcessHelper.ProcessInfo> _currentProcesses;

        /// <summary>
        /// Game.Overlay.Net sticky window.
        /// </summary>
        private StickyWindow _stickyWindow;

        /// <summary>
        /// Game.Overlay.Net sticky window's graphics.
        /// </summary>
        private GameOverlay.Drawing.Graphics _gfx;

        #endregion

        #region public methods

        [AsyncCommand]
        public void RefreshProcessListCommand(object sender)
        {
            var btn = sender as Button;
            var img = UIHelper.FindChild<System.Windows.Controls.Image>(btn, "Img_refresh");

            //btn.IsEnabled = false;

            var storyboard = UIHelper.AnimateRotate(img, true);
            storyboard.Begin();

            Task.Run(async () =>
            {
                // Get process list, maybe longer time.
                _currentProcesses = GetProcessList();
                foreach (var p in _currentProcesses)
                {
                    p.ProcessIcon?.Freeze();
                }

                // Run in ui thread.
                await ThreadHelper.UIThread;
                {
                    // Clear list.
                    if (ProcessInfoes.Count > 0)
                        ProcessInfoes.Clear();
                    // Update list.
                    foreach (var p in _currentProcesses)
                    {
                        ProcessInfoes.Add(new ProcessModel
                        {
                            ProcessName = p.ProcessName + ".exe",
                            ProcessId = p.ProcessId,
                            ProcessWindowTitle = p.ProcessWindowTitle,
                            ProcessIcon = p.ProcessIcon,
                            MainWindowHandle = p.MainWindowHandle
                        });
                    }

                    storyboard.Remove();
                    ProcessIndex = 0;
                }
            });

            //btn.IsEnabled = true;
        }

        [AsyncCommand]
        public void StartCaptureCommand(object sender)
        {
            ToggleButton btn = sender as ToggleButton;
            if (btn.IsChecked == true)
            {
                // Start capture.
                if (ProcessIndex >= 0)
                {
                    var p = ProcessInfoes[ProcessIndex];
                    Debug.WriteLine($"Selected: {p.ProcessName}");
                    StopCapture();
                    var hwnd = p.MainWindowHandle;
                    try
                    {
                        StartHwndCapture(hwnd);

                        // Init sticky window.
                        _gfx = new GameOverlay.Drawing.Graphics
                        {
                            MeasureFPS = true,
                            PerPrimitiveAntiAliasing = true,
                            TextAntiAliasing = true,
                            UseMultiThreadedFactories = false,
                            VSync = true,
                            WindowHandle = IntPtr.Zero
                        };
                        _stickyWindow = new StickyWindow(hwnd, _gfx)
                        {
                            IsTopmost = true,
                            IsVisible = true,
                            FPS = 30
                        };
                        _stickyWindow.Create();
                    }
                    catch (Exception e)
                    {
                        if (IntPtr.Size == 8)
                        {
                            Debug.WriteLine($"Hwnd 0x{hwnd.ToInt64():X16} is not valid for capture!");
                        }
                        else
                        {
                            Debug.WriteLine($"Hwnd 0x{hwnd.ToInt32():X8} is not valid for capture!");
                        }
                        Debug.WriteLine(e.ToString());
                        Debug.WriteLine(e.StackTrace);
                        ProcessInfoes.Remove(p);
                    }
                }
            }
            else
            {
                // Stop capture.
                Debug.WriteLine("OnStopCapturing");
                StopCapture();

                //btn.Content = "Start capture";
            }
        }

        [AsyncCommand]
        public void SaveCurrentFrameCommand()
        {
            _isSaveFrame = true;
        }

        [AsyncCommand]
        public void StartCallbackCommand(object sender)
        {
            ToggleButton btn = sender as ToggleButton;
            if (btn.IsChecked == true)
            {
                // Registher the GetOneFrameToBitmap delegate.
                Capturer.capture_core.GetOneFrameFromBitmapEvent += ReceiveOneFrameCallback;
            }
            else
            {
                // Unregisther the GetOneFrameToBitmap delegate.
                Capturer.capture_core.GetOneFrameFromBitmapEvent -= ReceiveOneFrameCallback;
            }
        }

        [AsyncCommand]
        public void ImageRecognitionCommand()
        {
            new ImageRecognitionEditorView().Show();
        }

        [AsyncCommand]
        public void MouseCapturerCommand(object sender)
        {
            var btn = sender as Button;
            var isP = true;
            if (btn.Content.ToString() == "Negative Capturer")
            {
                isP = false;
            }

            Task.Run(() =>
            {
                var rnd = new Random();
                // Init overlay window.
                var overlayWindow = new OverlayWindow(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight)
                {
                    IsTopmost = true,
                    IsVisible = true
                };
                var gfx = new GameOverlay.Drawing.Graphics
                {
                    MeasureFPS = true,
                    Height = overlayWindow.Height,
                    PerPrimitiveAntiAliasing = true,
                    TextAntiAliasing = true,
                    UseMultiThreadedFactories = false,
                    VSync = true,
                    Width = overlayWindow.Width,
                    WindowHandle = IntPtr.Zero
                };
                overlayWindow.Create();
                gfx.WindowHandle = overlayWindow.Handle;
                gfx.Setup();

                GameOverlay.Drawing.SolidBrush greenBrush = gfx.CreateSolidBrush(GameOverlay.Drawing.Color.Green);
                GameOverlay.Drawing.SolidBrush redBrush = gfx.CreateSolidBrush(GameOverlay.Drawing.Color.Red);

                var width = 200;
                var height = 300;

                while (true)
                {
                    var mousePos = GetMousePosition();
                    //overlayWindow.X = mousePos.X - width / 2;
                    //overlayWindow.Y = mousePos.Y - height / 2;

                    gfx.BeginScene();
                    gfx.ClearScene();

                    Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Resize the gfx draw.
                        if (Keyboard.IsKeyDown(Key.Left))
                        {
                            if (width <= 0) width = 0;
                            else width -= 1;
                        }
                        if (Keyboard.IsKeyDown(Key.Right))
                        {
                            width += 1;
                        }
                        if (Keyboard.IsKeyDown(Key.Up))
                        {
                            height += 1;
                        }
                        if (Keyboard.IsKeyDown(Key.Down))
                        {
                            if (height <= 0) height = 0;
                            else height -= 1;
                        }
                    }));
                    //width = width < 0 ? 0 : width;
                    //height = height < 0 ? 0 : height;
                    var left = mousePos.X - width / 2;
                    var up = mousePos.Y - height / 2;

                    gfx.DrawText(gfx.CreateFont("Arial", 14), redBrush, new GameOverlay.Drawing.Point(left, up - 18),
                        $"Size: {width}x{height}  Pos:[{left},{up}]");
                    gfx.DrawRectangle(greenBrush, left, up, left + width, up + height, 2);

                    gfx.EndScene();

                    // Save the sample.
                    if (KeyboardInterceptor.IsKeyToggled(KeyboardInterceptor.Keys.Home))
                    {
                        var path = isP == true ? PositiveSamplesSavePath : NegativeSamplesSavePath;

                        var i = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories).Length;
                        var fileName = $"{i}.png";
                        CaptureScreen(width - 4, height - 4, left + 2, up + 2).Save($"{path}{fileName}", ImageFormat.Png);
                        Console.Beep();
                    }
                    // Exit.
                    if (KeyboardInterceptor.IsKeyToggled(KeyboardInterceptor.Keys.Escape))
                    {
                        gfx.BeginScene();
                        gfx.ClearScene();
                        gfx.EndScene();

                        gfx.Dispose();
                        overlayWindow.Dispose();
                        break;
                    }
                }
            });
        }

        [AsyncCommand]
        public void AddSamplesCommand(object e)
        {
            var path = e as string;
            var isP = true;
            if (path == NegativeSamplesSavePath)
            {
                isP = false;
            }

            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "All Image Files | *.*",
                Title = "Please select image."
            };

            if ((bool)fileDialog.ShowDialog())
            {
                var arrPaths = fileDialog.FileNames;

                if (arrPaths.Length > 0)
                {
                    // Save where.
                    var targetPath = isP == true ? PositiveSamplesSavePath : NegativeSamplesSavePath;
                    // Check the count of original files.
                    // Copy files to target folder.
                    foreach (var img in arrPaths)
                    {
                        File.Copy(img, $"{targetPath}{Path.GetFileName(img)}", true);
                    }
                }
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Initialize method.
        /// </summary>
        private void Initialize()
        {
            // Get process list.
            _currentProcesses = GetProcessList();
            // Clear list.
            if (ProcessInfoes.Count > 0)
                ProcessInfoes.Clear();
            foreach (var p in _currentProcesses)
            {
                ProcessInfoes.Add(new ProcessModel
                {
                    ProcessName = p.ProcessName + ".exe",
                    ProcessId = p.ProcessId,
                    ProcessWindowTitle = p.ProcessWindowTitle,
                    ProcessIcon = p.ProcessIcon,
                    MainWindowHandle = p.MainWindowHandle
                });
            }
            // Initialize var.
            PositiveSamplesSavePath = GLOBALS.SAMPLES_FOLDER_PATH + "Positive\\";
            if (!Directory.Exists(PositiveSamplesSavePath))
            {
                Directory.CreateDirectory(PositiveSamplesSavePath);
            }
            NegativeSamplesSavePath = GLOBALS.SAMPLES_FOLDER_PATH + "Negative\\";
            if (!Directory.Exists(NegativeSamplesSavePath))
            {
                Directory.CreateDirectory(NegativeSamplesSavePath);
            }
        }

        /// <summary>
        /// Get current system process list.
        /// </summary>
        private List<EnumProcessHelper.ProcessInfo> GetProcessList()
        {
            StatusText = "Loading process list...";

            // Get high priority.
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            // Get process list.
            EnumProcessHelper ph = new EnumProcessHelper();

            StatusText = "Finished loading process list.";

            return ph.EnumWindows();
        }

        private void ReceiveOneFrameCallback(Bitmap frame)
        {
            // Save current frame?
            if (_isSaveFrame == true)
            {
                // Task.Run(() => SaveCurrentFrame(frame));
                SaveCurrentFrame(frame);
            }
            else
            {
                // Task.Run(() => ImageProcessing(frame));
                ImageProcessing(frame);
            }
        }

        private void SaveCurrentFrame(Bitmap frame)
        {
            _isSaveFrame = false;

            var watch = Stopwatch.StartNew();

            Bitmap clone = frame.Clone() as Bitmap;
            ImageProcessing(frame);

            Task.Run(() => clone.Save("1.bmp", ImageFormat.Bmp));

            watch.Stop();
            Debug.WriteLine("RunTime " + watch.ElapsedMilliseconds + "ms");

        }

        private void ImageProcessing(Bitmap frame, bool isDispose = true)
        {
            // Run in background thread.
            if (GLOBALS.ENABLE_IMAGE_RECOGNITION == true)
            {
                if (GLOBALS.FRAME_PROCESS_FLAGS == true)
                {
                    Bitmap copy = (Bitmap)frame.Clone();
                    GLOBALS.FRAME_PROCESS_FLAGS = false;
                    Bitmap dst = null;

                    Task.Run(() =>
                     {
                         try
                         {
                             var watch = Stopwatch.StartNew();

                             var posList = ImageRecognition.GetSubPositionsOpenCV(copy, GLOBALS.IMAGE_LIST, out dst);

                             // Debug code.
                             lock (this)
                             {
                                 _gfx.BeginScene();
                                 _gfx.ClearScene();

                                 if (posList.Count > 0)
                                 {
                                     var j = 0;
                                     foreach (var poss in posList)
                                     {
                                         if (poss.Count > 0)
                                         {
                                             foreach (var pos in poss)
                                             {
                                                 _gfx.DrawRectangleEdges(
                                                     _gfx.CreateSolidBrush(GameOverlay.Drawing.Color.Red),
                                                     new GameOverlay.Drawing.Rectangle(pos.X, pos.Y, pos.X + GLOBALS.IMAGE_LIST[j].Width, pos.Y + GLOBALS.IMAGE_LIST[j].Height),
                                                     2);
                                             }
                                         }
                                         j++;
                                     }
                                 }

                                 _gfx.EndScene();
                             }

                             GLOBALS.FRAME_PROCESS_FLAGS = true;

                             watch.Stop();
                             GLOBALS.EXCUTE_TIME = watch.ElapsedMilliseconds;
                         }
                         finally
                         {
                             // Bitmap dispose.
                             copy?.Dispose();
                             dst?.Dispose();
                         }
                     });
                }
            }

            if (isDispose) frame.Dispose();
        }

        #region Image process writeablebitmap version.

        private void ImageProcessingEx(Bitmap frame, bool isDispose = true)
        {
            // Run in background thread.
            if (GLOBALS.ENABLE_IMAGE_RECOGNITION == true)
            {
                if (GLOBALS.FRAME_PROCESS_FLAGS == true)
                {
                    Bitmap buffer = (Bitmap)frame.Clone();
                    GLOBALS.FRAME_PROCESS_FLAGS = false;

                    Task.Run(async () =>
                    {
                        var watch = Stopwatch.StartNew();

                        var posList = ImageRecognition.GetSubPositionsOpenCV(buffer, GLOBALS.IMAGE_LIST, out Bitmap dst);

                        // Debug code.
                        lock (this)
                        {
                            using Graphics g = Graphics.FromImage(dst);
                            if (posList.Count > 0)
                            {
                                var j = 0;
                                foreach (var poss in posList)
                                {
                                    if (poss.Count > 0)
                                    {
                                        foreach (var pos in poss)
                                        {
                                            g.DrawRectangle(Pens.Red, pos.X, pos.Y, GLOBALS.IMAGE_LIST[j].Width, GLOBALS.IMAGE_LIST[j].Height);
                                        }
                                    }
                                    j++;
                                }
                            }
                        }

                        GLOBALS.FRAME_PROCESS_FLAGS = true;

                        watch.Stop();
                        GLOBALS.EXCUTE_TIME = watch.ElapsedMilliseconds;

                        await ThreadHelper.UIThread;
                        {
                            // Show the dst image in writeablebitmap control.
                            if (CurrentFrameImage == null || CurrentFrameImage.Width != dst.Width || CurrentFrameImage.Height != dst.Height)
                            {
                                CurrentFrameImage = new WriteableBitmap(dst.Width, dst.Height, 96, 96, PixelFormats.Bgra32, null);
                            }
                            var bitmapData = dst.LockBits(new Rectangle(0, 0, dst.Width, dst.Height), ImageLockMode.ReadOnly, dst.PixelFormat);
                            CurrentFrameImage.WritePixels(new Int32Rect(0, 0, dst.Width, dst.Height), bitmapData.Scan0, dst.Width * dst.Height * 4, dst.Width * 4);
                            dst.UnlockBits(bitmapData);

                            // Bitmap dispose.
                            buffer.Dispose();
                            dst.Dispose();
                        }
                    });
                }
            }
            else
            {
                // Show the frame image in writeablebitmap control.
                if (CurrentFrameImage == null || CurrentFrameImage.Width != frame.Width || CurrentFrameImage.Height != frame.Height)
                {
                    CurrentFrameImage = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32, null);
                }
                var bitmapData = frame.LockBits(new Rectangle(0, 0, frame.Width, frame.Height), ImageLockMode.ReadOnly, frame.PixelFormat);
                CurrentFrameImage.WritePixels(new Int32Rect(0, 0, frame.Width, frame.Height), bitmapData.Scan0, frame.Width * frame.Height * 4, frame.Width * 4);
                frame.UnlockBits(bitmapData);

                GLOBALS.EXCUTE_TIME = 0;
            }

            if (isDispose) frame.Dispose();
        }

        #endregion

        /// <summary>
        /// Start hwnd capture.
        /// </summary>
        /// <param name="hwnd"></param>
        private void StartHwndCapture(IntPtr hwnd)
        {
            var item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item != null)
            {
                Capturer.StartCaptureFromItem(item);
            }
        }

        /// <summary>
        /// Stop capture.
        /// </summary>
        private void StopCapture() => Capturer?.StopCapture();

        #endregion

        #region Other method

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public int X;
            public int Y;
        };
        private static System.Drawing.Point GetMousePosition()
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);

            return new System.Drawing.Point(w32Mouse.X, w32Mouse.Y);
        }

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

        #endregion
    }
}
