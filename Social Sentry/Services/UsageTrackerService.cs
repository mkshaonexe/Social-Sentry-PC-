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
        private readonly BlockerService _blockerService;
        private readonly Social_Sentry.Data.DatabaseService _databaseService;
        private readonly IconExtractionService _iconExtractionService;
        public IconExtractionService IconExtractor => _iconExtractionService;
        private readonly MediaDetector _mediaDetector;

        private DateTime _lastSwitchTime;
        private string _currentProcessName = "";
        private string _currentWindowTitle = "";
        private string _currentUrl = "";
        private string _currentCategory = "Uncategorized"; // Track current category
        
        private string _currentMetadata = ""; // Store current metadata JSON
        
        // Session Coalescing State
        private string _lastLoggedProcessName = "";
        private DateTime _lastLoggedEndTime = DateTime.MinValue;

        // Key: ProcessName

        // Key: ProcessName
        private readonly ConcurrentDictionary<string, AppUsageInfo> _dailyUsage = new();

        // Key: Hour (0-23) -> Total Seconds
        private readonly ConcurrentDictionary<int, double> _hourlyUsage = new();

        public event Action? OnUsageUpdated;

        public event Action<ActivityEvent>? OnRawActivityDetected;

        public UsageTrackerService(Social_Sentry.Data.DatabaseService databaseService, MediaDetector mediaDetector)
        {
            _databaseService = databaseService;
            _mediaDetector = mediaDetector;
            _activityTracker = new ActivityTracker();
            _blockerService = new BlockerService();
            _blockerService.Initialize(_databaseService);
            _iconExtractionService = new IconExtractionService();

            _activityTracker.OnActivityChanged += HandleActivityChanged;

            // Load today's usage from DB
            LoadTodayUsage();
        }

        private void LoadTodayUsage()
        {
            var savedUsage = _databaseService.GetTodayAppUsage();
            foreach (var kvp in savedUsage)
            {
                _dailyUsage[kvp.Key] = new AppUsageInfo 
                { 
                    ProcessName = kvp.Key, 
                    Duration = TimeSpan.FromSeconds(kvp.Value),
                    SessionCount = 1 // Approximate, doesn't matter much for total time
                };
            }

            // CRITICAL FIX: Load Hourly Stats on startup
            var hourly = _databaseService.GetHourlyUsage(DateTime.Today);
            foreach (var kvp in hourly)
            {
                _hourlyUsage[kvp.Key] = kvp.Value;
            }

            // Perform Integrity Check in background
            System.Threading.Tasks.Task.Run(() => VerifyStatsIntegrity());
        }

        private void VerifyStatsIntegrity()
        {
            try
            {
                // 1. Calculate Total from Granular Logs (Source of Truth)
                var todayActivities = _databaseService.GetTodayAppUsage();
                double totalActivitySeconds = todayActivities.Values.Sum();

                // 2. Calculate Total from Aggregated Hourly Stats
                double totalHourlySeconds = _hourlyUsage.Values.Sum();

                // 3. Compare (Tolerance: 1.0 second or 1%)
                double diff = Math.Abs(totalActivitySeconds - totalHourlySeconds);
                bool significant = totalActivitySeconds > 5.0; // Only check if there's meaningful data

                if (significant && diff > Math.Max(1.0, totalActivitySeconds * 0.01))
                {
                    System.Diagnostics.Debug.WriteLine($"[Integrity] Mismatch detected! Activity: {totalActivitySeconds}s, Hourly: {totalHourlySeconds}s. Rebuilding...");
                    
                    // Rebuild DB
                    _databaseService.RebuildHourlyStats(DateTime.Today);

                    // Reload memory
                    _hourlyUsage.Clear();
                    var reloaded = _databaseService.GetHourlyUsage(DateTime.Today);
                    foreach (var kvp in reloaded)
                    {
                        _hourlyUsage[kvp.Key] = kvp.Value;
                    }
                     System.Diagnostics.Debug.WriteLine("[Integrity] Rebuild complete.");
                }
                else
                {
                     System.Diagnostics.Debug.WriteLine($"[Integrity] Stats verified. Diff: {diff:F2}s");
                }
            }
            catch (Exception ex)
            {
                 System.Diagnostics.Debug.WriteLine($"[Integrity] Check failed: {ex.Message}");
            }
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

            string finalProcessName = newEvent.ProcessName;

            // Context Intelligence: Media Detection
            if (_mediaDetector != null && _mediaDetector.IsMediaPlaying(newEvent.ProcessName))
            {
                // Artificially differentiate this session
                finalProcessName = $"{newEvent.ProcessName} (Media)";
            }

            // Attempt to block content if restricted
            bool blocked = _blockerService.CheckAndBlock(newEvent.ProcessName, newEvent.WindowTitle, newEvent.Url, newEvent.ProcessId);
            if (blocked) 
            {
                 // Logic for blocked
            }

            // Log previous session before switching
            UpdateCurrentSession();

            _currentProcessName = finalProcessName;
            _currentWindowTitle = newEvent.WindowTitle;
            _currentUrl = newEvent.Url;
            _currentCategory = "Uncategorized"; // Reset category on native switch
            _currentMetadata = ""; // Reset metadata on native switch
            _lastSwitchTime = DateTime.Now;
        }

        public void HandleExtensionActivity(ExtensionActivityData data)
        {
            if (data == null) return;

            // Log previous session with old state
            UpdateCurrentSession();

            // Override state with rich data
            _currentUrl = data.Url ?? _currentUrl;
            _currentWindowTitle = data.Title ?? _currentWindowTitle;
            
            // Smart Process Naming (e.g., "YouTube" instead of "Chrome")
            _currentProcessName = GetSmartProcessName(data) ?? _currentProcessName;

            // Determine Category from Extension Data
            _currentCategory = DetermineCategory(data);

            // Context-Aware Process Naming Override
            if (_currentCategory == "Study" && !_currentProcessName.Contains("(Study)"))
            {
                _currentProcessName = $"{_currentProcessName} (Study)";
            }

            // Serialize Metadata
            try 
            {
                if (data.Metadata != null)
                {
                    _currentMetadata = System.Text.Json.JsonSerializer.Serialize(data.Metadata);
                }
                else
                {
                    _currentMetadata = "";
                }
            }
            catch { _currentMetadata = ""; }

            _lastSwitchTime = DateTime.Now;

            // Notify raw data subscribers (Dashboard)
            var evt = new ActivityEvent 
            {
                ProcessName = _currentProcessName, 
                WindowTitle = _currentWindowTitle,
                Url = _currentUrl,
                Timestamp = DateTime.Now
            };
            OnRawActivityDetected?.Invoke(evt);
        }

        private string? GetSmartProcessName(ExtensionActivityData data)
        {
            // Prefer explicit platform from V2 API
            if (!string.IsNullOrEmpty(data.Platform)) return data.Platform;

            if (string.IsNullOrEmpty(data.Url)) return null;

            try 
            {
                var uri = new Uri(data.Url);
                string host = uri.Host.ToLower();
                string path = uri.AbsolutePath.ToLower();

                // Doom Scrolling Specifics
                if (host.Contains("youtube.com") && (path.Contains("/shorts") || data.ContentType == "Shorts")) return "YouTube Shorts";
                if (host.Contains("facebook.com") && (path.Contains("/reel") || data.ContentType == "Reels")) return "Facebook Reels";
                if (host.Contains("instagram.com") && (path.Contains("/reels") || data.ContentType == "Reels")) return "Instagram Reels";
                if (host.Contains("tiktok.com")) return "TikTok";

                if (host.Contains("youtube.com")) return "YouTube";
                if (host.Contains("facebook.com")) return "Facebook";
                if (host.Contains("instagram.com")) return "Instagram";
                if (host.Contains("twitter.com") || host.Contains("x.com")) return "X";
                if (host.Contains("reddit.com")) return "Reddit";
                if (host.Contains("netflix.com")) return "Netflix";
                
                return null; // Fallback to existing native name (e.g. Chrome)
            }
            catch { return null; }
        }

        private string DetermineCategory(ExtensionActivityData data)
        {
            // 1. Doom Scrolling Check
            if (data.ContentType == "Shorts" || data.ContentType == "Reels") return "Doom Scrolling";
            if (!string.IsNullOrEmpty(data.Url))
            {
                if (data.Url.Contains("youtube.com/shorts")) return "Doom Scrolling";
                if (data.Url.Contains("facebook.com/reel")) return "Doom Scrolling";
                if (data.Url.Contains("instagram.com/reels")) return "Doom Scrolling";
                if (data.Url.Contains("tiktok.com")) return "Doom Scrolling";
            }

            // 2. Study Context Check (Title based)
            if (!string.IsNullOrEmpty(data.Title))
            {
                var lowerTitle = data.Title.ToLower();
                var studyKeywords = new[] { "study", "lecture", "tutorial", "course", "assignment", "thesis", "research", "math", "physics", "chemistry", "exam" };
                if (studyKeywords.Any(k => lowerTitle.Contains(k)))
                {
                    return "Study";
                }
            }

            // V2 Logic
            if (data.ContentType == "Video") return "Entertainment";

            // Fallback Logic
            if (data.ActivityType == "reels" || data.ActivityType == "shorts") return "Doom Scrolling";
            if (!string.IsNullOrEmpty(data.Url) && data.Url.Contains("youtube.com")) return "Entertainment"; // Default, could be 'Education' based on title
            
            return "Browsing";
        }

        private void UpdateCurrentSession()
        {
            if (string.IsNullOrEmpty(_currentProcessName)) return;

            var now = DateTime.Now;
            var duration = now - _lastSwitchTime;

            // 1. Session Buffering: Ignore sessions < 2s (configurable, was 0.1s)
            if (duration.TotalSeconds < 2.0) return; 

            // 2. Session Coalescing: Check if returning to same app within 5s
            bool isContinuation = false;
            if (_currentProcessName == _lastLoggedProcessName && (now - duration - _lastLoggedEndTime).TotalSeconds < 5.0)
            {
                isContinuation = true;
            }

            // update app total
            _dailyUsage.AddOrUpdate(_currentProcessName,
                new AppUsageInfo { ProcessName = _currentProcessName, Duration = duration, SessionCount = 1 },
                (key, existing) =>
                {
                    existing.Duration += duration;
                    if (!isContinuation) 
                    {
                        existing.SessionCount++;
                    }
                    return existing;
                });

            // update hourly bucket
            int hour = now.Hour;
            _hourlyUsage.AddOrUpdate(hour, duration.TotalSeconds, (key, existing) => existing + duration.TotalSeconds);

            // Log to DB (ActivityLog + HourlyStats)
            _databaseService.LogActivity(_currentProcessName, _currentWindowTitle, _currentUrl, duration.TotalSeconds, _currentCategory, _currentMetadata);
            _databaseService.UpdateHourlyStats(now, _currentProcessName, _currentCategory, duration.TotalSeconds);

            // Update state for next coalescing check
            _lastLoggedProcessName = _currentProcessName;
            _lastLoggedEndTime = now;

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
                RawDuration = u.Duration,
                Percentage = u.Duration.TotalSeconds / totalSeconds,
                Sessions = u.SessionCount,
                Icon = _iconExtractionService.GetProcessIcon(u.ProcessName) // Extract real icon
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

        public List<Social_Sentry.Data.ActivityLogItem> GetRecentLogs(int count = 200)
        {
            return _databaseService.GetRecentActivityLogs(count);
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
