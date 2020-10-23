using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MainView.View
{
    /// <summary>
    /// ImageRecognitionEditorView.xaml 的交互逻辑
    /// </summary>
    public partial class ImageRecognitionEditorView : Window
    {
        public ImageRecognitionEditorView()
        {
            InitializeComponent();

            Loaded += ImageRecognitionEditorView_Loaded;
        }

        private void ImageRecognitionEditorView_Loaded(object sender, RoutedEventArgs e)
        {
            // Calc fps and show.
            Task.Run(() =>
            {
                int fps = 0;
                while (true)
                {
                    if (GLOBALS.EXCUTE_TIME > 0)
                    {
                        fps = (int)(1000f / GLOBALS.EXCUTE_TIME);
                    }
                    else
                    {
                        fps = 0;
                    }

                    Thread.Sleep(1000);

                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                    {
                        TB_FPS.Text = fps.ToString();
                        TB_ExcuteTime.Text = $"{GLOBALS.EXCUTE_TIME} ms";
                    }));
                }
            });
        }

        private void TextBox_ImageName_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update the Images.json.
            var json = JsonConvert.SerializeObject(SubVM.ImageList, Formatting.Indented);
            File.WriteAllText("Images.json", json, System.Text.Encoding.Default);
        }
    }
}
