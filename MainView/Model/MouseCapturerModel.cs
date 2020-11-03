using System.ComponentModel;

namespace MainView.Model
{
    public class MouseCapturerModel : INotifyPropertyChanged
    {
        public int WinW
        {
            get { return _winW; }
            set { _winW = value; NotifyPropertyChanged("WinW"); }
        }
        private int _winW;
        public int WinH
        {
            get { return _winH; }
            set { _winH = value; NotifyPropertyChanged("WinH"); }
        }
        private int _winH;
        public int RectW
        {
            get { return _rectW; }
            set { _rectW = value; NotifyPropertyChanged("RectW"); }
        }
        private int _rectW;
        public int RectH
        {
            get { return _rectH; }
            set { _rectH = value; NotifyPropertyChanged("RectH"); }
        }
        private int _rectH;

        #region Property changed

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}