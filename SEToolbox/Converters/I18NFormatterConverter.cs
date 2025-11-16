using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WPFLocalizeExtension.Extensions;

namespace SEToolbox.Converters
{
    public class I18NFormatterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            parameter ??= values;

            object[] bindingParams = (object[])values.Clone();

            // Remove the {DependancyProperty.UnsetValue} from unbound datasources.
            for (int i = 0; i < bindingParams.Length; i++)
            {
                if (ReferenceEquals(bindingParams[i], DependencyProperty.UnsetValue))
                {
                    bindingParams[i] = null;
                }
            }

            string keyStr = (string)parameter;
            LocExtension ext = new(keyStr);
            return ext.ResolveLocalizedValue(out string localizedValue) ? string.Format(localizedValue, bindingParams) : null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return [];
        }
    }
}
