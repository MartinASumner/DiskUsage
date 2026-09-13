using System;
using System.Globalization;
using System.Windows.Data;
using DiskUsage.Models;

namespace DiskUsage.Converters
{
    public class ItemTypeToGlyphConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is FileSystemItemType itemType)
            {
                return itemType == FileSystemItemType.Directory ? "📁" : "📄";
            }
            return "📄";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
