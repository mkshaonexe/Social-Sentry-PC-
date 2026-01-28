using System;

namespace Social_Sentry.Models
{
    public class ActivityEvent
    {
        public string ProcessName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string WindowTitle { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
