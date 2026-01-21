using System;
using System.Globalization;
using System.Windows.Data;

namespace Social_Sentry.Converters
{
    /// <summary>
    /// Converts a boolean value to a status string ("ON" or "OFF").
    /// </summary>
    public class BooleanToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? "ON" : "OFF";
            }
            return "OFF";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.Equals("ON", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}
