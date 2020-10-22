using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using MainView.Framework;
using MainView.Model;
using MainView.View;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Linq;
using System.Windows.Documents;
using System.Collections.Generic;

namespace MainView.ViewModel
{
    public class ImageRecognitionEditorViewModel : ViewModelBase
    {
        public ObservableCollection<ImageModel> ImageList { get; } = new ObservableCollection<ImageModel>();

        public CaptureMarkView Capturer;

        public ImageRecognitionEditorViewModel()
        {
            // Load Images.json.
            var json = File.ReadAllText("Images.json", System.Text.Encoding.Default);
            var images = JsonConvert.DeserializeObject<ObservableCollection<ImageInfo>>(json, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            // Add image to list view.
            if (images != null)
            {
                foreach (var image in images)
                {
                    if (!File.Exists(image.Path))
                    {
                        break;
                    }
                    var bitmap = new Bitmap(image.Path);
                    ImageList.Add(new ImageModel
                    {
                        Src = BitmapHelper.BitmapToImageSource(bitmap),
                        Bitmap = bitmap,
                        // Index = image.Index,
                        Width = image.Width,
                        Height = image.Height,
                        Name = image.Name,
                        PixelFormat = image.PixelFormat,
                        Path = image.Path
                    });
                }
            }

            // Update the Images.json.
            var newJson = JsonConvert.SerializeObject(ImageList, Formatting.Indented);
            File.WriteAllText("Images.json", newJson, System.Text.Encoding.Default);
        }

        [AsyncCommand]
        public void StartCaptureCommand(object sender)
        {
            // Set flag and clone image list.
            lock (this)
            {
                if (GLOBALS.ENABLE_IMAGE_RECOGNITION)
                {
                    GLOBALS.ENABLE_IMAGE_RECOGNITION = false;
                }
                else
                {
                    foreach (var image in ImageList)
                    {
                        GLOBALS.IMAGE_LIST.Add(image.Bitmap);
                    }
                    GLOBALS.ENABLE_IMAGE_RECOGNITION = true;
                }
            }
        }

        [AsyncCommand]
        public void CaptureCommand()
        {
            Capturer = new CaptureMarkView();
            Capturer.Closing += (sender, e) =>
             {
                 // Add to image list.
                 var path = $"{GLOBALS.CAPTURE_FOLDER_PATH}cap_{DateTime.Now:yyyyMMddhmmss}.bmp";
                 ImageList.Add(new ImageModel
                 {
                     // Index = ImageList.Count,
                     Bitmap = Capturer.bitmap,
                     Src = BitmapHelper.BitmapToImageSource(Capturer.bitmap),
                     Name = "unnamed",
                     Width = Capturer.bitmap.Width,
                     Height = Capturer.bitmap.Height,
                     PixelFormat = Capturer.bitmap.PixelFormat.ToString(),
                     Path = path
                 });

                 // Check.
                 if (!Directory.Exists(GLOBALS.CAPTURE_FOLDER_PATH))
                 {
                     Directory.CreateDirectory(GLOBALS.CAPTURE_FOLDER_PATH);
                 }
                 // Save the bitmap.
                 Capturer.bitmap.Save(@$"{path}", ImageFormat.Bmp);

                 // Update the Images.json.
                 var json = JsonConvert.SerializeObject(ImageList, Formatting.Indented);
                 File.WriteAllText("Images.json", json, System.Text.Encoding.Default);
             };
            Capturer.Show();
        }

        [AsyncCommand]
        public void ImportImageCommand()
        {
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
                    // Check.
                    if (!Directory.Exists(GLOBALS.CAPTURE_FOLDER_PATH))
                    {
                        Directory.CreateDirectory(GLOBALS.CAPTURE_FOLDER_PATH);
                    }
                    // Copy files to capture folder.
                    foreach (var path in arrPaths)
                    {
                        var fileName = Path.GetFileName(path);
                        if (File.Exists(@$"{GLOBALS.CAPTURE_FOLDER_PATH}{fileName}"))
                        {
                            // Is in capture folder?
                            if (path == @$"{GLOBALS.CAPTURE_FOLDER_PATH}{fileName}")
                            {
                                // Delete old, update new.
                                ImageList.Remove(x => x.Path == @$"{GLOBALS.CAPTURE_FOLDER_PATH}{fileName}");
                                var bitmap = new Bitmap(path);
                                // Add image.
                                ImageList.Add(new ImageModel
                                {
                                    // Index = ImageList.Count,
                                    Bitmap = bitmap,
                                    Src = BitmapHelper.BitmapToImageSource(bitmap),
                                    Name = "unnamed",
                                    Width = bitmap.Width,
                                    Height = bitmap.Height,
                                    PixelFormat = bitmap.PixelFormat.ToString(),
                                    Path = path
                                });
                            }
                            else
                            {
                                MessageBoxResult ret = MessageBox.Show($"The capture folder alread has a file named '{fileName}', replace it?", "Replace or skip files", MessageBoxButton.YesNo);
                                if (ret == MessageBoxResult.Yes)
                                {
                                    // Delete old, update new.
                                    for (int i = 0; i < ImageList.Count; i++)
                                    {
                                        if (ImageList[i].Path == @$"{GLOBALS.CAPTURE_FOLDER_PATH}{fileName}")
                                        {
                                            ImageList[i].Bitmap.Dispose();
                                        }
                                        ImageList.RemoveAt(i);
                                    }

                                    // Replace old.
                                    File.Delete(@$"{GLOBALS.CAPTURE_FOLDER_PATH}{fileName}");
                                    File.Copy(path, @$"{GLOBALS.CAPTURE_FOLDER_PATH}{fileName}", true);

                                    var bitmap = new Bitmap(@$"{GLOBALS.CAPTURE_FOLDER_PATH}{fileName}");
                                    // Add image.
                                    ImageList.Add(new ImageModel
                                    {
                                        // Index = ImageList.Count,
                                        Bitmap = bitmap,
                                        Src = BitmapHelper.BitmapToImageSource(bitmap),
                                        Name = "unnamed",
                                        Width = bitmap.Width,
                                        Height = bitmap.Height,
                                        PixelFormat = bitmap.PixelFormat.ToString(),
                                        Path = @$"{GLOBALS.CAPTURE_FOLDER_PATH}{fileName}"
                                    });
                                   
                                }
                            }
                        }
                        else
                        {
                            // Delete old, update new.
                            ImageList.Remove(x => x.Path == @$"{GLOBALS.CAPTURE_FOLDER_PATH}{fileName}");
                            var bitmap = new Bitmap(path);
                            // Add image.
                            ImageList.Add(new ImageModel
                            {
                                // Index = ImageList.Count,
                                Bitmap = bitmap,
                                Src = BitmapHelper.BitmapToImageSource(bitmap),
                                Name = "unnamed",
                                Width = bitmap.Width,
                                Height = bitmap.Height,
                                PixelFormat = bitmap.PixelFormat.ToString(),
                                Path = @$"{GLOBALS.CAPTURE_FOLDER_PATH}{fileName}"
                            });
                            // Copy to target folder.
                            File.Copy(path, @$"{GLOBALS.CAPTURE_FOLDER_PATH}{fileName}", true);
                        }
                    }

                    // Update the Images.json.
                    var json = JsonConvert.SerializeObject(ImageList, Formatting.Indented);
                    File.WriteAllText("Images.json", json, System.Text.Encoding.Default);
                }
            }
        }

        [AsyncCommand]
        public void RemoveSelectedImageCommand(object sender)
        {
            var item = (ImageModel)sender;
            item.Bitmap.Dispose();
            ImageList.Remove(item);

            // Update the Images.json.
            var json = JsonConvert.SerializeObject(ImageList, Formatting.Indented);
            File.WriteAllText("Images.json", json, System.Text.Encoding.Default);
        }
    }
}
