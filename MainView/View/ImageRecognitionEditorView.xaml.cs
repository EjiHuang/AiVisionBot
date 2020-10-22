using Newtonsoft.Json;
using System.IO;
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
        }

        private void TextBox_ImageName_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update the Images.json.
            var json = JsonConvert.SerializeObject(SubVM.ImageList, Formatting.Indented);
            File.WriteAllText("Images.json", json, System.Text.Encoding.Default);
        }
    }
}
