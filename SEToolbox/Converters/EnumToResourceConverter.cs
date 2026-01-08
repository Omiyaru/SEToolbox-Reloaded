using System;
using System.Windows.Data;

namespace SEToolbox.Converters
{
    public class EnumToResourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            var type = value.GetType();

            if (type.IsEnum)
            {
                string header = type.Name;
                string resource = $"{header}_{value}";
                return GetResource(resource, value);
            }
            else if (type == typeof(bool) || value is bool)
            {
                return GetResource($"{value.GetType().Name}_{value}", value);
            }

            return value ??= null ?? value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

        private static object GetResource(string resource, object value)
        {
            try
            {
                string ret = Properties.Resources.ResourceManager.GetString(resource);
                return ret ?? value;
            }
            catch
            {
                return value;
            }
        }

    }
}