using System.ComponentModel;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace MainView.Model
{
    public class ImageModel : INotifyPropertyChanged
    {
        // public int Index { set; get; }
        public BitmapSource Src { set; get; }
        public Bitmap Bitmap { set; get; }
        public int Width { set; get; }
        public int Height { set; get; }
        public string Name
        {
            get { return _name; }
            set { _name = value; NotifyPropertyChanged("Name"); }
        }
        private string _name;

        public string PixelFormat { set; get; }
        public string Path { set; get; }

        #region Property changed

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public class ImageInfo
    {
        // public int Index { set; get; }
        public string Src { set; get; }
        public string Bitmap { set; get; }
        public int Width { set; get; }
        public int Height { set; get; }
        public string Name { set; get; }
        public string PixelFormat { set; get; }
        public string Path { set; get; }
    }
}
