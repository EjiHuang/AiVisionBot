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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        #endregion

        #region private properties

        /// <summary>
        /// Save current frame?
        /// </summary>
        private bool IsSaveFrame = false;

        /// <summary>
        /// Current process list.
        /// </summary>
        private static List<EnumProcessHelper.ProcessInfo> CurrentProcesses;

        /// <summary>
        /// Game.Overlay.Net sticky window.
        /// </summary>
        private StickyWindow overlayWindow;

        /// <summary>
        /// Game.Overlay.Net sticky window's graphics.
        /// </summary>
        private GameOverlay.Drawing.Graphics gfx;

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
                CurrentProcesses = GetProcessList();
                foreach (var p in CurrentProcesses)
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
                    foreach (var p in CurrentProcesses)
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

                        // Init overlay window.
                        gfx = new GameOverlay.Drawing.Graphics
                        {
                            MeasureFPS = true,
                            PerPrimitiveAntiAliasing = true,
                            TextAntiAliasing = true,
                            UseMultiThreadedFactories = false,
                            VSync = true,
                            WindowHandle = IntPtr.Zero
                        };
                        overlayWindow = new StickyWindow(hwnd, gfx)
                        {
                            IsTopmost = true,
                            IsVisible = true,
                            FPS = 30
                        };
                        overlayWindow.Create();
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
            IsSaveFrame = true;
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

        #endregion

        #region private methods

        /// <summary>
        /// Initialize method.
        /// </summary>
        private void Initialize()
        {
            // Get process list.
            CurrentProcesses = GetProcessList();
            // Clear list.
            if (ProcessInfoes.Count > 0)
                ProcessInfoes.Clear();
            foreach (var p in CurrentProcesses)
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
            if (IsSaveFrame == true)
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
            IsSaveFrame = false;

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
                                 gfx.BeginScene();
                                 gfx.ClearScene();

                                 if (posList.Count > 0)
                                 {
                                     var j = 0;
                                     foreach (var poss in posList)
                                     {
                                         if (poss.Count > 0)
                                         {
                                             foreach (var pos in poss)
                                             {
                                                 gfx.DrawRectangleEdges(
                                                     gfx.CreateSolidBrush(GameOverlay.Drawing.Color.Red),
                                                     new GameOverlay.Drawing.Rectangle(pos.X, pos.Y, pos.X + GLOBALS.IMAGE_LIST[j].Width, pos.Y + GLOBALS.IMAGE_LIST[j].Height),
                                                     2);
                                             }
                                         }
                                         j++;
                                     }
                                 }

                                 gfx.EndScene();
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

    }
}
