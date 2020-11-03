using CaptureCore;
using System;
using System.Windows;

namespace MainView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Windows9xClassic.Windows9xClassicWindow, IDisposable
    {
        private bool disposed;
        private BasicApplication capturer;
        // private BasicCapturer capturer;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void CompositionHost_Loaded(object sender, RoutedEventArgs e)
        {
            capturer = new BasicApplication(CHC.Compositor);
            CHC.SetChild(capturer.Visual);

            var viewModel = MainVM;
            viewModel.Capturer = capturer;
        }

        private void CompositionHost_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    capturer?.Dispose();
                    capturer = null;
                    var viewModel = MainVM;
                    viewModel.Capturer = null;
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Dispose(true);
        }

        private void Windows9xClassicWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //capturer = new BasicCapturer();

            //var viewModel = MainVM;
            //viewModel.Capturer = capturer;
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (MainVM.ProcessInfoes.Count > 0 && MainVM.CurrentProcesses != null && MainVM.CurrentProcesses.Count > 0)
            {
                MainVM.SamplesSavePath = GLOBALS.DARKNET_PATH + "projects\\" + MainVM.CurrentProcesses[MainVM.ProcessIndex].ProcessName + "\\img\\";
            }
        }
    }
}
