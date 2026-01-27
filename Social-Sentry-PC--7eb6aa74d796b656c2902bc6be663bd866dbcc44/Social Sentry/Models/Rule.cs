
namespace Social_Sentry.Models
{
    public class Rule
    {
        public int Id { get; set; }
        // 'App', 'Url', 'Title'
        public string Type { get; set; } = string.Empty;
        // The value to match (e.g., 'chrome', '/reels/', 'YouTube')
        public string Value { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        // 'Block', 'Limit'
        public string Action { get; set; } = "Block";
        public int LimitSeconds { get; set; }
        public string ScheduleJson { get; set; } = string.Empty;
    }
}
