using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DiskUsage.Converters
{
    public class PercentageToBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double percentage)
            {
                if (percentage >= 50.0)
                {
                    // Coral/Red for high disk space consumers
                    return new SolidColorBrush(Color.FromRgb(220, 53, 69));
                }
                if (percentage >= 20.0)
                {
                    // Amber/Orange for medium consumers
                    return new SolidColorBrush(Color.FromRgb(253, 126, 20));
                }
                if (percentage >= 5.0)
                {
                    // Blue/Cyan for moderate consumers
                    return new SolidColorBrush(Color.FromRgb(13, 110, 253));
                }
                // Slate/Gray for minor files
                return new SolidColorBrush(Color.FromRgb(108, 117, 125));
            }

            return new SolidColorBrush(Color.FromRgb(13, 110, 253));
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
