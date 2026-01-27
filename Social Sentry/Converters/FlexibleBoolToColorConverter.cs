using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Social_Sentry.Converters
{
    public class FlexibleBoolToColorConverter : IValueConverter
    {
        public SolidColorBrush TrueColor { get; set; } = Brushes.Green;
        public SolidColorBrush FalseColor { get; set; } = Brushes.Gray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? TrueColor : FalseColor;
            }
            return FalseColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
