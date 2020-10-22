using System;
using System.Windows.Media.Imaging;

namespace MainView.Model
{
    public class ProcessModel
    {
        public BitmapSource ProcessIcon { set; get; }
        public int ProcessId { set; get; }
        public string ProcessName { set; get; }
        public string ProcessWindowTitle { set; get; }
        public string ProcessPath { set; get; }
        public IntPtr MainWindowHandle { set; get; }
    }
}
