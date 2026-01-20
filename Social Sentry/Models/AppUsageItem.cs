namespace Social_Sentry.Models
{
    public class AppUsageItem
    {
        public string Name { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public double Percentage { get; set; } // 0.0 to 1.0
        public string IconPath { get; set; } = string.Empty;
        public int Sessions { get; set; }
    }
}
