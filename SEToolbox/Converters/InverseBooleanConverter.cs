using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SEToolbox.Converters
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var finalValue = value switch
            {
                null => false,
                bool b => b,
                string s => !string.IsNullOrEmpty(s),
                _ => true,
            };
            finalValue = !finalValue;

            if (targetType == typeof(Visibility))
                return finalValue ? Visibility.Visible : Visibility.Collapsed;

            return finalValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var finalValue = value switch
            {
                null => false,
                Visibility visibility when visibility == Visibility.Visible => true,
                Visibility _ => false,
                bool boolValue => boolValue,
                _ => true,
            };
            return !finalValue;
        }
    }
}
