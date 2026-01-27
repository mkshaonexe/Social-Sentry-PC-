using System;
using System.Globalization;
using System.Windows.Data;

namespace Social_Sentry.Converters
{
    public class BooleanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isInstalled && isInstalled)
            {
                return "Installed";
            }
            return "Not Installed";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
