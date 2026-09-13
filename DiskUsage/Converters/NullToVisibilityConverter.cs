using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiskUsage.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isNullOrEmpty = value == null || (value is string s && string.IsNullOrWhiteSpace(s));
            if (Invert)
            {
                return isNullOrEmpty ? Visibility.Visible : Visibility.Collapsed;
            }
            return isNullOrEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
