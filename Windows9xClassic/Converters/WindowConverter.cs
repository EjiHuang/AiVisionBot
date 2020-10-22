using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Windows9xClassic.Converters
{
    public sealed class BackgroundBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool && (bool)value) ? parameter as SolidColorBrush : Brushes.LightGray;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }

    public sealed class CaptionBarBackgroundConverter : IValueConverter
    {

        // Windows 95
        //static SolidColorBrush Active = new SolidColorBrush( Color.FromRgb( 0, 0x02, 0x81 ) );
        //static SolidColorBrush InActive = new SolidColorBrush( Color.FromRgb( 0x80, 0x80, 0x80 ) );
        // Windows 98
        //static LinearGradientBrush Active = new LinearGradientBrush(Color.FromRgb(0, 0x02, 0x81), Color.FromRgb(0x10, 0x84, 0xD0), new Point(0, 0.5), new Point(0.5, 1));
        //static LinearGradientBrush InActive = new LinearGradientBrush(Color.FromRgb(0x80, 0x80, 0x80), Color.FromRgb(0xB3, 0xB3, 0xB3), new Point(0, 0.5), new Point(0.5, 1));
        // Windows Me
        static readonly LinearGradientBrush Active = new LinearGradientBrush(Color.FromRgb(0x0B, 0x25, 0x6B), Color.FromRgb(0xA6, 0xCA, 0xF0), new Point(0, 0.5), new Point(0.5, 1));
        static readonly LinearGradientBrush InActive = new LinearGradientBrush(Color.FromRgb(0x80, 0x80, 0x80), Color.FromRgb(0xB6, 0xB6, 0xB6), new Point(0, 0.5), new Point(0.5, 1));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool && (bool)value ? Active : InActive;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }

    public sealed class BorderThicknessByWindowStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is WindowState && (WindowState)value == WindowState.Maximized ? 8.0 : 2.0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
