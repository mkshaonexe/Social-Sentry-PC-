using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Social_Sentry.Converters
{
    /// <summary>
    /// Converts a boolean value to a color brush (green when true, gray when false).
    /// </summary>
    public class BooleanToColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush EnabledBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xE6, 0x76)); // #00E676 - Green
        private static readonly SolidColorBrush DisabledBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xB0, 0xB3, 0xB8)); // #B0B3B8 - Gray

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? EnabledBrush : DisabledBrush;
            }
            return DisabledBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
