using System.Windows.Media.Imaging;

namespace Social_Sentry.Models
{
    public class AppUsageItem
    {
        public string Name { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public double Percentage { get; set; } // 0.0 to 1.0
        public BitmapSource? Icon { get; set; } // Real-time extracted icon
        public int Sessions { get; set; }
    }
}
