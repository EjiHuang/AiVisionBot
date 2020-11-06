using CaptureCore;
using Composition.WindowsRuntimeHelpers;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using GameOverlay.Windows;
using ImageProcessor;
using MainView.Framework;
using MainView.Model;
using MainView.View;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
        public ObservableCollection<ProcessModel> ProcessInfoes { get; set; } = new ObservableCollection<ProcessModel>();

        /// <summary>
        /// Current process list.
        /// </summary>
        public List<EnumProcessHelper.ProcessInfo> CurrentProcesses { get; set; }

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
        /// The samples save folder's path.
        /// </summary>
        public string SamplesSavePath
        {
            get => GetProperty(() => SamplesSavePath);
            set => SetProperty(() => SamplesSavePath, value);
        }

        ///// <summary>
        ///// The negative samples save folder's path.
        ///// </summary>
        //public string NegativeSamplesSavePath
        //{
        //    get => GetProperty(() => NegativeSamplesSavePath);
        //    set => SetProperty(() => NegativeSamplesSavePath, value);
        //}

        /// <summary>
        /// Log info.
        /// </summary>
        public string Log
        {
            get => GetProperty(() => Log);
            set => SetProperty(() => Log, value);
        }

        /// <summary>
        /// Recorded mouse capturer info.
        /// </summary>
        public MouseCapturerModel MouseCapturerInfo
        {
            get => GetProperty(() => MouseCapturerInfo);
            set => SetProperty(() => MouseCapturerInfo, value);
        }

        #endregion

        #region private properties

        /// <summary>
        /// Save current frame?
        /// </summary>
        private bool _isSaveFrame = false;

        /// <summary>
        /// Game.Overlay.Net sticky window.
        /// </summary>
        private StickyWindow _stickyWindow;

        /// <summary>
        /// Game.Overlay.Net sticky window's graphics.
        /// </summary>
        private GameOverlay.Drawing.Graphics _gfx;

        /// <summary>
        /// Cmd process.
        /// </summary>
        private Process proc;

        /// <summary>
        /// The wrapper of yolo.
        /// </summary>
        private YoloWrapper.YoloWrapperNoGpu yoloWrapper;

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
            Task.Run(() =>
            {
                // Init overlay window.
                var width = MouseCapturerInfo.WinW;
                var height = MouseCapturerInfo.WinH;
                var rectWidth = MouseCapturerInfo.RectW;
                var rectHeight = MouseCapturerInfo.RectH;

                var overlayWindow = new OverlayWindow(0, 0, width, height)
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
                GameOverlay.Drawing.SolidBrush blueBrush = gfx.CreateSolidBrush(GameOverlay.Drawing.Color.Blue);

                while (true)
                {
                    var mousePos = GetMousePosition();
                    overlayWindow.X = mousePos.X - width / 2;
                    overlayWindow.Y = mousePos.Y - height / 2;

                    var left = (float)(width - rectWidth) / 2;
                    var top = (float)(height - rectHeight) / 2;
                    var right = (float)(width + rectWidth) / 2;
                    var bottom = (float)(height + rectHeight) / 2;

                    Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Resize the gfx draw.
                        if (Keyboard.IsKeyDown(Key.A))
                        {
                            if (rectWidth <= 0) rectWidth = 0;
                            else rectWidth -= 1;
                        }
                        if (Keyboard.IsKeyDown(Key.D))
                        {
                            rectWidth += 1;
                        }
                        if (Keyboard.IsKeyDown(Key.W))
                        {
                            rectHeight += 1;
                        }
                        if (Keyboard.IsKeyDown(Key.S))
                        {
                            if (rectHeight <= 0) rectHeight = 0;
                            else rectHeight -= 1;
                        }
                    }));

                    // Save the sample.
                    if (KeyboardInterceptor.IsKeyToggled(KeyboardInterceptor.Keys.Home))
                    {
                        // Clear scene.
                        gfx.BeginScene();
                        gfx.ClearScene();
                        gfx.EndScene();

                        var path = SamplesSavePath;
                        // Save the png.
                        var i = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories).Length;
                        var fileName = $"{i}.png";
                        CaptureScreen(width, height, overlayWindow.X, overlayWindow.Y).Save($"{path}{fileName}", ImageFormat.Png);
                        Console.Beep();
                        // Save to yolo txt.
                        var relativeCenterX = (float)(left + rectWidth / 2) / width;
                        var relativeCenterY = (float)(top + rectHeight / 2) / height;
                        var relativeWidth = (float)rectWidth / width;
                        var relativeHeight = (float)rectHeight / height;
                        var txt = string.Format("{0} {1} {2} {3} {4}", 0, relativeCenterX, relativeCenterY, relativeWidth, relativeHeight).Replace(",", ".");
                        File.WriteAllText($"{path}{i}.txt", txt);
                        // Save the setting.
                        MouseCapturerInfo.WinW = width;
                        MouseCapturerInfo.WinH = height;
                        MouseCapturerInfo.RectW = rectWidth;
                        MouseCapturerInfo.RectH = rectHeight;
                        // Update the MouseCapturerSetting.json.
                        var newJson = JsonConvert.SerializeObject(MouseCapturerInfo, Formatting.Indented);
                        File.WriteAllText("MouseCapturerSetting.json", newJson, System.Text.Encoding.Default);

                        continue;
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

                    gfx.BeginScene();
                    gfx.ClearScene();

                    // Draw overlay window and info text.
                    gfx.DrawText(gfx.CreateFont("Arial", 14), redBrush, new GameOverlay.Drawing.Point(1, 1),
                        $"Size: {rectWidth}x{rectHeight}  Pos:[{overlayWindow.X},{overlayWindow.Y}]");
                    gfx.DrawRectangle(greenBrush, 0, 0, width, height, 2);

                    // Draw rect box.
                    gfx.DrawRectangle(blueBrush, left, top, right, bottom, 2);

                    gfx.EndScene();
                }
            });
        }

        [AsyncCommand]
        public void AddSamplesCommand(object e)
        {
            //var path = e as string;
            //var isP = true;
            //if (path == NegativeSamplesSavePath)
            //{
            //    isP = false;
            //}

            //Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog
            //{
            //    Multiselect = true,
            //    Filter = "All Image Files | *.*",
            //    Title = "Please select image."
            //};

            //if ((bool)fileDialog.ShowDialog())
            //{
            //    var arrPaths = fileDialog.FileNames;

            //    if (arrPaths.Length > 0)
            //    {
            //        // Save where.
            //        var targetPath = isP == true ? PositiveSamplesSavePath : NegativeSamplesSavePath;
            //        // Copy files to target folder.
            //        foreach (var img in arrPaths)
            //        {
            //            File.Copy(img, $"{targetPath}{Path.GetFileName(img)}", true);
            //        }
            //    }
            //}
        }

        [AsyncCommand]
        public void BuildYoloConfigCommand()
        {
            var darknetPath = GLOBALS.DARKNET_PATH;
            var darknetDataPath = darknetPath + "data\\";
            var yoloProjectsPath = darknetPath + "projects\\";
            var yolov4DefPath = darknetPath + "yolov4def\\";
            var projectName = CurrentProcesses[ProcessIndex].ProcessName;
            var projcetPath = yoloProjectsPath + projectName + "\\";
            // Check and create project directory.
            if (!Directory.Exists(yoloProjectsPath))
            {
                Directory.CreateDirectory(yoloProjectsPath);
            }
            if (!Directory.Exists(projcetPath))
            {
                Directory.CreateDirectory(projcetPath);
            }
            if (!Directory.Exists(SamplesSavePath))
            {
                Directory.CreateDirectory(SamplesSavePath);
            }

            // Build config.
            File.Copy(yolov4DefPath + "default.cmd", $"{projcetPath + projectName}.cmd", true);
            File.Copy(yolov4DefPath + "default_trainmore.cmd", $"{projcetPath + projectName}_trainmore.cmd", true);
            File.Copy(yolov4DefPath + "yolov4-tiny.cfg", $"{projcetPath + projectName}.cfg", true);
            File.Copy(yolov4DefPath + "yolov4-tiny.conv.29", $"{projcetPath + projectName}.conv.29", true);

            File.Copy(yolov4DefPath + "default.data", $"{darknetDataPath + projectName}.data", true);
            File.Copy(yolov4DefPath + "default.names", $"{darknetDataPath + projectName}.names", true);
            File.Copy(yolov4DefPath + "default.txt", $"{darknetDataPath + projectName}.txt", true);
        }

        [AsyncCommand]
        public async void TrainingCommand()
        {
            // Build yolov4 project.
            var darknetPath = GLOBALS.DARKNET_PATH;
            var darknetDataPath = darknetPath + "data\\";
            var yoloProjectsPath = darknetPath + "projects\\";
            var projectName = CurrentProcesses[ProcessIndex].ProcessName;
            var projcetPath = yoloProjectsPath + projectName + "\\";

            var objectNum = 1;
            // Change default yolov4 config.
            File.WriteAllText($"{projcetPath + projectName}.cfg",
                File.ReadAllText($"{projcetPath + projectName}.cfg")
                .Replace("OBJ_NUMBER", objectNum.ToString())
                .Replace("FILTER_NUMBER", ((objectNum + 5) * 3).ToString()));
            File.WriteAllText($"{projcetPath + projectName}.cfg",
                File.ReadAllText($"{projcetPath + projectName}.cfg")
                .Replace("batch=1", "batch=64")
                .Replace("subdivisions=1", "subdivisions=8"));
            // Change default yolov4 .data file.
            File.WriteAllText($"{darknetDataPath + projectName}.data",
                File.ReadAllText($"{darknetDataPath + projectName}.data")
                .Replace("OBJ_NUMBER", objectNum.ToString())
                .Replace("PROJECT_NAME", projectName));
            // Change default darknet command.
            File.WriteAllText($"{projcetPath + projectName}.cmd",
                File.ReadAllText($"{projcetPath + projectName}.cmd")
                .Replace("PROJECT_NAME", projectName));
            File.WriteAllText($"{projcetPath + projectName}_trainmore.cmd",
                File.ReadAllText($"{projcetPath + projectName}_trainmore.cmd")
                .Replace("PROJECT_NAME", projectName));
            // Add object to .names file.
            File.WriteAllText($"{darknetDataPath + projectName}.names", "0");
            // Add samples path to project txt.
            FileInfo[] files = new DirectoryInfo(SamplesSavePath).GetFiles("*.png");
            string imagesPath = string.Empty;
            foreach (var file in files)
            {
                imagesPath += $"projects/{projectName}/img/{file.Name}\r\n";
            }
            File.WriteAllText($"{darknetDataPath + projectName}.txt", imagesPath);

            // Check old weight is existed?
            if (File.Exists($"projects/{projectName}/{projectName}.weights"))
            {
                Process.Start("cmd", @"/C cd " + $"{darknetPath} & ./projects/{projectName}/{projectName}_trainmore.cmd");
            }
            else
                await ExecuteCmd(@$"cd {darknetPath} & {projcetPath}{projectName}.cmd");
        }

        [AsyncCommand]
        public void StopTrainingCommand()
        {
            proc.Kill();
            foreach (var p in Process.GetProcessesByName("darknet"))
            {
                p.Kill();
            }
            Log += "Current command is stopped." + Environment.NewLine;
        }

        [AsyncCommand]
        public void TestCommand()
        {
            var darknetPath = GLOBALS.DARKNET_PATH;
            var darknetDataPath = darknetPath + "data\\";
            var yoloProjectsPath = darknetPath + "projects\\";
            var projectName = CurrentProcesses[ProcessIndex].ProcessName;
            var projcetPath = yoloProjectsPath + projectName + "\\";
            // Initialize yolo wrapper.
            yoloWrapper = new YoloWrapper.YoloWrapperNoGpu($@".\darknet\projects\{projectName}\trainfiles\yolov4-tiny.cfg",
                $@".\darknet\projects\{projectName}\trainfiles\{projectName}.weights", 0);
            var bbox = yoloWrapper.Detect(@"dog.jpg");

            Log += "Result: " + Environment.NewLine;
            var table = new ConsoleTables.ConsoleTable("Type", "Confidence", "X", "Y", "Width", "Height");
            foreach (var item in bbox.Where(o => o.h > 0 || o.w > 0))
            {
                // var type = _namesDic[(int)item.obj_id];
                var type = item.obj_id;
                table.AddRow(type, item.prob, item.x, item.y, item.w, item.h);
            }
            //table.Write(ConsoleTables.Format.MarkDown);
            Log += table.ToString() + Environment.NewLine;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Initialize method.
        /// </summary>
        private void Initialize()
        {
            // Show log.
            Log = "AI Vision Bot v0.0.1" + Environment.NewLine;
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
            // Initialize var.
            SamplesSavePath = GLOBALS.DARKNET_PATH + "projects\\" + CurrentProcesses[ProcessIndex].ProcessName + "\\img\\";
            // Load MouseCapturerSetting.json. Get mouse capturer setting info.
            var json = File.ReadAllText("MouseCapturerSetting.json", System.Text.Encoding.Default);
            MouseCapturerInfo = JsonConvert.DeserializeObject<MouseCapturerModel>(json, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
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

        /// <summary>
        /// Use cmd.exe.
        /// </summary>
        /// <param name="command"></param>
        public async Task ExecuteCmd(string command)
        {
            Log += command + Environment.NewLine;
            await Task.Run(() =>
            {
                ProcessStartInfo CmdProcessInfo = new ProcessStartInfo()
                {
                    FileName = "cmd",
                    Arguments = "/c " + command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                proc = new Process() { StartInfo = CmdProcessInfo };
                proc.StartInfo.RedirectStandardError = true;
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.OutputDataReceived += (sender, e) =>
                {
                    Log += e.Data + Environment.NewLine;
                    if (null == e.Data) Log += "Finished." + Environment.NewLine;
                };
                proc.ErrorDataReceived += (sender, e) =>
                {
                    if (null != e.Data)
                    {
                        Log += e.Data + Environment.NewLine;
                    }
                };
            });
        }

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
