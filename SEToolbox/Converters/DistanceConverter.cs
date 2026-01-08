using System;
using System.Globalization;
using System.Windows.Data;
using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.Converters
{
    public class DistanceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double distance = (double)value;

            return distance > 1000 ? $"{distance / 1000:#,###0.0.0} {Res.GlobalSIDistanceKilometre}"
                                   : $"{distance:#,###0.0} {Res.GlobalSIDistanceMetre}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
