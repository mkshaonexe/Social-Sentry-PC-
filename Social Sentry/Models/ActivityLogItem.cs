using System;

namespace Social_Sentry.Models
{
    public class ActivityLogItem
    {
        public string Timestamp { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
