using System;
using System.Windows.Data;

namespace SEToolbox.Converters
{
    public class EnumToResourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                if (value is Enum @enum)
                {
                    string header = @enum.GetType().Name;
                     string resource = $"{header}_{value}";
                    return GetResource(resource, value);
                }
                else if (value is bool)
                {
                    return GetResource(string.Format($"{value.GetType().Name}_{value}"), value);
                }

                return value;
            }
            return null;
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