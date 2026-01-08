using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SEToolbox.Converters
{
    public class BooleanConverter : IValueConverter
    {
        private bool _isInverse;

        public bool IsInverse
        {
            get => _isInverse;
            set => _isInverse = value;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool finalValue = value switch
            {
                null => false,
                bool boolValue => boolValue,
                string stringValue => bool.TryParse(stringValue, out bool result) && result,
                _ => true,
            };
            finalValue ^= _isInverse;

            return targetType == typeof(Visibility) ? finalValue ? Visibility.Visible 
                                                    : Visibility.Collapsed 
                                                    : finalValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool finalValue = value switch
            {
                null => false,
                Visibility vis => vis == Visibility.Visible,
                bool boolValue => boolValue,
                _ => true,
            };
            return finalValue ^ _isInverse;
        }
    }
}
