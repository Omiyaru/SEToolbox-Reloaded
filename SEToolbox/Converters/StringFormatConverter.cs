using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace SEToolbox.Converters
{
    public class StringFormatConverter : IValueConverter, IMultiValueConverter
    {
  

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string format = parameter as string;
            if (string.IsNullOrEmpty(format))
                return value?.ToString() ?? string.Empty;

            var valueType = value.GetType().IsValueType;
            


            return valueType ? string.Format(culture, $"{value}: {format  ?? string.Empty}")

                : string.Format(culture, format ?? $"{value}");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string format || string.IsNullOrEmpty(format) || values.Length == 0)
                return string.Join(", ", values);

            var valueTypes = values.GetType().IsValueType;

            return valueTypes ? string.Format(culture, $"{values.Select(v => v?.ToString()?? string.Empty)}:{format}")
                : string.Format(culture, format ?? $"{values.Select(v => v?.ToString() ?? string.Empty)}");
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
          return [];
        }
    }
}

