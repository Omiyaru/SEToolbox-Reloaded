using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace SEToolbox.Converters
{
    public class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string format = parameter as string;
            if (string.IsNullOrEmpty(format))
            {
                return value?.ToString() ?? string.Empty;
            }

            var valueType = value.GetType().IsValueType;

            var valueString = value?.ToString() ?? string.Empty;
            var formatString = !string.IsNullOrEmpty(format) ? $"{format}" : string.Empty;
            
            return valueType ? $"{valueString}{formatString}"
            
                             : string.Format(culture, format ?? $"{valueString}");
              
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
   
    }
}

