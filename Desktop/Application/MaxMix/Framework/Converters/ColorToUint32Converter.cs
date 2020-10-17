using System;
using System.Windows.Data;
using System.Windows.Media;

namespace MaxMix.Framework.Converters
{
    public class ColorToUint32Converter : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                var input = (Color)value;
                var result = 0xFF << 24 | input.B << 16 | input.G << 8 | input.R;
                return (uint)result;
            }

            return 0;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(value != null)
            {
                var input = (uint)value;
                byte b = (byte)(input >> 16);
                byte g = (byte)(input >> 8);
                byte r = (byte)(input);
                return Color.FromRgb(r, g, b);
            }

            return Color.FromRgb(0, 0, 0);
        }
    }
}

