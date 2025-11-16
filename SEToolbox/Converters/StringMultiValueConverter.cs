using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SEToolbox.Converters
{
    public class StringMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var format = (string)parameter;
            var count = values.Length;

            var args = new object[count];
            int i = 0;
            foreach (var value in values)
                args[i++] = value ?? string.Empty;

            return string.Format(format, args);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return [];
        }
    }
}
