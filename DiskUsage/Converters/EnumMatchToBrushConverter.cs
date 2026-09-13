using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DiskUsage.Converters
{
    public class EnumMatchToBrushConverter : IValueConverter
    {
        public Brush ActiveBrush { get; set; } = new SolidColorBrush(Color.FromRgb(13, 110, 253));
        public Brush InactiveBrush { get; set; } = new SolidColorBrush(Color.FromRgb(248, 249, 250));

        public Brush ActiveTextBrush { get; set; } = Brushes.White;
        public Brush InactiveTextBrush { get; set; } = new SolidColorBrush(Color.FromRgb(73, 80, 87));

        public bool TargetTextForeground { get; set; }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return TargetTextForeground ? InactiveTextBrush : InactiveBrush;
            }

            string valStr = value.ToString() ?? string.Empty;
            string paramStr = parameter.ToString() ?? string.Empty;

            bool isMatch = string.Equals(valStr, paramStr, StringComparison.OrdinalIgnoreCase);

            if (TargetTextForeground)
            {
                return isMatch ? ActiveTextBrush : InactiveTextBrush;
            }

            return isMatch ? ActiveBrush : InactiveBrush;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
