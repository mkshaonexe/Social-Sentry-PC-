namespace Social_Sentry.Models
{
    public class ChartDataPoint
    {
        public string TimeLabel { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty; // Alias or new standard
        public double Value { get; set; } // 0.0 to 1.0 representing height relative to max
        public DateTime Timestamp { get; set; }
        
        public double RawValue { get; set; } // Actual seconds
        public string TooltipText { get; set; } = string.Empty;
    }
}
