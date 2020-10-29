using System.Windows;
using Windows.UI.Composition;

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                // Create the compositor.
                var compositor = new Compositor();
            };
        }
    }
}
