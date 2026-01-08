using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace SEToolbox.Converters
{
    public class StringFormatMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {   
            var varEmpty = parameter is null || string.IsNullOrEmpty((string)parameter) || values.Length == 0;

            if (varEmpty)
            {
                return string.Empty;
            }

                var format = parameter as string;
                var valuesList = values.Select(clone => values.Clone()).ToList();
                valuesList.RemoveAll(i => i != null && i == DependencyProperty.UnsetValue);
               
                                      
               var valArray = values.Select(v => v.ToString() ?? string.Empty)
                                    .ToArray();

            return valArray[0].GetType() != typeof(CultureInfo) 
                                         ? string.Format(format, valArray)
                                         : string.Format(culture,format, valArray );
        }
        
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return [];
        }
    }
}