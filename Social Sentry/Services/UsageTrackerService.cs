using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Social_Sentry.Models;

namespace Social_Sentry.Services
{
    public class UsageTrackerService
    {
        private readonly ActivityTracker _activityTracker;
        private DateTime _lastSwitchTime;
        private string _currentProcessName = "";
        
        // Key: ProcessName
        private readonly ConcurrentDictionary<string, AppUsageInfo> _dailyUsage = new();

        // Key: Hour (0-23) -> Total Seconds
        private readonly ConcurrentDictionary<int, double> _hourlyUsage = new();

        public event Action? OnUsageUpdated;

        public event Action<ActivityEvent>? OnRawActivityDetected;

        public UsageTrackerService()
        {
            _activityTracker = new ActivityTracker();
            _activityTracker.OnActivityChanged += HandleActivityChanged;
        }

        public void Start()
        {
            _lastSwitchTime = DateTime.Now;
            _activityTracker.Start();
        }

        public void Stop()
        {
            // Capture final session
            UpdateCurrentSession();
            _activityTracker.Stop();
        }

        private void HandleActivityChanged(ActivityEvent newEvent)
        {
            // Update raw log subscribers
            OnRawActivityDetected?.Invoke(newEvent);

            UpdateCurrentSession();

            _currentProcessName = newEvent.ProcessName;
            _lastSwitchTime = DateTime.Now;
        }

        private void UpdateCurrentSession()
        {
            if (string.IsNullOrEmpty(_currentProcessName)) return;

            var now = DateTime.Now;
            var duration = now - _lastSwitchTime;

            if (duration.TotalSeconds < 0.1) return; // Ignore micro-switches

            // update app total
            _dailyUsage.AddOrUpdate(_currentProcessName,
                new AppUsageInfo { ProcessName = _currentProcessName, Duration = duration },
                (key, existing) =>
                {
                    existing.Duration += duration;
                    existing.SessionCount++;
                    return existing;
                });

            // update hourly bucket (simple approx: assign entire duration to the *end* hour for simplicity, or split?)
            // Splitting is better but standard "Digital Wellbeing" often buckets by start or end strictly.
            // Let's just add to the current hour bucket for simplicity of "when it happened".
            int hour = now.Hour;
            _hourlyUsage.AddOrUpdate(hour, duration.TotalSeconds, (key, existing) => existing + duration.TotalSeconds);

            OnUsageUpdated?.Invoke();
        }

        public List<AppUsageItem> GetTopApps()
        {
            // Convert internal model to UI model
            var sorted = _dailyUsage.Values
                .OrderByDescending(u => u.Duration)
                .ToList();

            double totalSeconds = sorted.Sum(u => u.Duration.TotalSeconds);
            if (totalSeconds < 1) totalSeconds = 1;

            return sorted.Select(u => new AppUsageItem
            {
                Name = u.ProcessName,
                // Format: 1h 30m or 45m or 30s
                Duration = FormatDuration(u.Duration),
                Percentage = u.Duration.TotalSeconds / totalSeconds,
                Sessions = u.SessionCount,
                IconPath = "" // TODO: Extract Icon
            }).ToList();
        }

        public string GetTotalDurationString()
        {
            var total = TimeSpan.FromSeconds(_dailyUsage.Values.Sum(u => u.Duration.TotalSeconds));
            return FormatDuration(total);
        }

        public List<ChartDataPoint> GetChartData()
        {
            var points = new List<ChartDataPoint>();
            // Assume 24h day or just "active hours"? 
            // Dashboard shows ~5 bars. Let's return last 12 hours or strict 24h.
            // The mockup had 8 PM, 9 PM... let's do a fixed range or dynamic.
            
            // Return full 24h for today, or valid range.
            // Let's do 00:00 to 23:00
            double max = _hourlyUsage.Values.DefaultIfEmpty(0).Max();
            if (max == 0) max = 1;

            for (int i = 0; i < 24; i++)
            {
                 // Filter for relevant hours if needed (e.g. only 6am to 12am)
                 // But simply, we can just return all or a specific set.
                 // let's just return a list. Logic in VM can filter for "view".
            }

            // To match mockup "Recent Hours" (or user preference):
            // We'll return the whole list, let VM slice it.
            return new List<ChartDataPoint>(); 
        }

        // Helper for raw hourly data
        public Dictionary<int, double> GetHourlyUsage()
        {
            return _hourlyUsage.ToDictionary(k => k.Key, v => v.Value);
        }

        private string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}m";
            return $"{ts.Seconds}s";
        }

        private class AppUsageInfo
        {
            public string ProcessName { get; set; } = "";
            public TimeSpan Duration { get; set; }
            public int SessionCount { get; set; } = 1;
        }
    }
}
