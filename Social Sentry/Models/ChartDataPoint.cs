namespace Social_Sentry.Models
{
    public class ChartDataPoint
    {
        public string TimeLabel { get; set; } = string.Empty;
        public double Value { get; set; } // 0.0 to 1.0 representing height relative to max
        
        public double RawValue { get; set; } // Actual seconds
        public string TooltipText { get; set; } = string.Empty;
    }
}
