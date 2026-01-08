using System;
using System.Windows.Data;
using System.Linq;
namespace SEToolbox.Converters
{
    public class EnumToItemsSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {   
            var enumType = value?.GetType() ?? typeof(Enum);
            value = value ??= null ?? Enum.GetValues(enumType).Cast<object>().ToArray();
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}