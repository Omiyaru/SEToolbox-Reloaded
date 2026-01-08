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
                string s => bool.TryParse(s, out bool result) && result,
                _ => true,
            };
            finalValue = !finalValue;

            return targetType == typeof(Visibility) ? finalValue ? Visibility.Visible 
                                                    : Visibility.Collapsed 
                                                    : finalValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool finalValue = value switch
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
