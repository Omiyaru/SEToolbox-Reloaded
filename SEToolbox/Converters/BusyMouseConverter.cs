using System;
using System.Windows.Data;
using System.Windows.Input;

namespace SEToolbox.Converters
{
    /// <summary>
    /// Sets the cursor state of the mouse.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Cursors))]
    public class BusyMouseConverter : IValueConverter
    {
        public static readonly Cursor WaitCursor = Cursors.Wait;
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool? boolValue = value as bool?;
            if (boolValue.HasValue && boolValue.Value)
            {
                return WaitCursor;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Cursor cursor && cursor == WaitCursor)
            {
                return true;
            }

            return false;
        }
}
}
