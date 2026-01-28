using System;
using System.Globalization;
using System.Windows.Data;

namespace Social_Sentry.Converters
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            
            string checkValue = value.ToString()!;
            string targetValue = parameter.ToString()!;
            
            return checkValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked)
            {
                return Enum.Parse(targetType, parameter.ToString()!);
            }
            return System.Windows.Data.Binding.DoNothing;
        }
    }
}
