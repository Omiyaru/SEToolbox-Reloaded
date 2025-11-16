using System;
using System.Windows.Data;

namespace SEToolbox.Converters
{
    public class EnumToItemsSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {   
            var enumType = value.GetType();
            value ??= Enum.GetValues(enumType);
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}