using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SEToolbox.Converters
{
    public class MainColorToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string mainColor = value as string;

            byte ParseByte(int i, int j) => byte.Parse(mainColor.Substring(i, j), NumberStyles.HexNumber);
            return mainColor.TrimStart('#') switch 
            {
               null => new SolidColorBrush(Colors.Transparent),
                { Length: 8 }  => new SolidColorBrush(Color.FromArgb(ParseByte(0, 2), ParseByte(2, 2), ParseByte(4, 2), ParseByte(6, 2))),
                { Length: 6 } => new SolidColorBrush(Color.FromRgb(ParseByte(0, 2), ParseByte(2, 2), ParseByte(4, 2))),
                _ => new SolidColorBrush(Colors.Black),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
