using System;

namespace Social_Sentry.Models
{
    public class ActivityLogItem
    {
        public string Timestamp { get; set; }
        public string ProcessName { get; set; }
        public string WindowTitle { get; set; }
        public string Url { get; set; }
    }
}
