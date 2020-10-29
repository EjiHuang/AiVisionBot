using MainView.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Text;

namespace MainView
{
    public static class GLOBALS
    {
        public static string CAPTURE_FOLDER_PATH = AppDomain.CurrentDomain.BaseDirectory + "capture\\";
        public static string SAMPLES_FOLDER_PATH = AppDomain.CurrentDomain.BaseDirectory + "Samples\\";
        public static ObservableCollection<Bitmap> IMAGE_LIST { get; set; } = new ObservableCollection<Bitmap>();
        public static bool ENABLE_IMAGE_RECOGNITION = false;
        public static bool FRAME_PROCESS_FLAGS = true;
        public static long EXCUTE_TIME = 0;
    }
}
