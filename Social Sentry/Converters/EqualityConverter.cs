using System;
using System.Globalization;
using System.Windows.Data;

namespace Social_Sentry.Converters
{
    public class EqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2)
            {
                // Handle strings specifically for case-sensitive or insensitive?
                // Default C# object.Equals is usually sufficient for simple types/strings.
                // But let's handle nulls safely.
                if (values[0] == null && values[1] == null) return true;
                if (values[0] == null || values[1] == null) return false;

                return values[0].Equals(values[1]);
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
