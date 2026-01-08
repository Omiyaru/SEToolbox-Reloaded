using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace SEToolbox.Converters
{
    public class StringMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var format = (string)parameter;
            var args = values.Select(v => v ?? string.Empty).ToArray();


            var cultureArg = values.FirstOrDefault() as CultureInfo;
            var formatArgs = args.Skip(1).ToArray();
            var isCulture = cultureArg != null;
            var argString = string.Concat(formatArgs.Select(arg => arg?.ToString() ?? string.Empty));
            return isCulture ? string.Format(cultureArg, format, argString)
                             : string.Format(format, argString);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return [];
        }
    }
}
