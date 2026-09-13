using System;
using System.Globalization;
using System.Windows.Data;
using DiskUsage.Helpers;

namespace DiskUsage.Converters
{
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                return ByteSizeFormatter.Format(bytes);
            }
            if (value is int intBytes)
            {
                return ByteSizeFormatter.Format(intBytes);
            }
            return "0 B";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
