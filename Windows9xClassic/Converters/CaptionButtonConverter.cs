using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Windows9xClassic.Converters
{
    public sealed class MaximizeCaptionButtonContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            // 1：maximize 2：normal
            value is WindowState && (WindowState)value == WindowState.Maximized ? "2" : "1";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }

    public sealed class MaximizeCaptionButtonTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is WindowState && (WindowState)value == WindowState.Maximized ? "Restore Down" : "Maximize";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }

    public sealed class MaximizeCaptionButtonEnableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is ResizeMode && (ResizeMode)value != ResizeMode.CanMinimize;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }

    public sealed class ResizeCaptionButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is ResizeMode && (ResizeMode)value != ResizeMode.NoResize ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
