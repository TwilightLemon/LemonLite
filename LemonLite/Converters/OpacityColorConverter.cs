using System;
using System.Globalization;
using System.Windows.Data;

namespace LemonLite.Converters
{
    public class OpacityColorConverter : IValueConverter
    {
        public static readonly OpacityColorConverter Instance = new();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Media.Color color && parameter is double opacity)
            {
                return System.Windows.Media.Color.FromArgb((byte)(opacity * 255), color.R, color.G, color.B);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
