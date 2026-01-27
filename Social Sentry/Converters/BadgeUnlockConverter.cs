using System;
using System.Globalization;
using System.Windows.Data;
using Social_Sentry.Models;

namespace Social_Sentry.Converters
{
    public class BadgeUnlockConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && 
                values[0] is RankingBadge itemBadge && 
                values[1] is RankingBadge currentBadge)
            {
                // Simple logic: if item threshold <= current threshold, it's unlocked
                // Or better, compare indices if we had them, but threshold works if monotonic
                return itemBadge.DayThreshold <= currentBadge.DayThreshold;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
