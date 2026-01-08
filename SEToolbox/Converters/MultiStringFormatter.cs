using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace SEToolbox.Converters
{

    public class MultiStringFormatConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter is not string format ? string.Concat(values.Select(v => v?.ToString())) 
                                                    : string.Concat(values.Select(v => string.Format(culture, $"{v}")));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return [];
        }
    }
}