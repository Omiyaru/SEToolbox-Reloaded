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
                string stringValue => !string.IsNullOrEmpty(stringValue),
                _ => true,
            };
            finalValue ^= _isInverse;

            if (targetType == typeof(Visibility))
                return finalValue ? Visibility.Visible : Visibility.Collapsed;

            return finalValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool finalValue = value switch
            {
                null => false,
                Visibility vis => vis == Visibility.Visible,
                bool b => b,
                _ => true,
            };
            return finalValue ^ _isInverse;
        }
    }
}
