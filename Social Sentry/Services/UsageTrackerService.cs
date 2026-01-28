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
        public event Action? OnStatsUpdated; // New event for aggregated stats updates

        // Exposed metrics for Widget
        public TimeSpan TotalDistractingTime { get; private set; }
        public TimeSpan TotalProductiveTime { get; private set; }
        
        // Expose current context for AI/Logic
        public string CurrentProcessName => _currentProcessName;
        public string CurrentCategory => _currentCategory;

        private readonly ClassificationService _classificationService;

        public UsageTrackerService(Social_Sentry.Data.DatabaseService databaseService, MediaDetector mediaDetector)
        {
            _databaseService = databaseService;
            _mediaDetector = mediaDetector;
            _activityTracker = new ActivityTracker();
            _blockerService = new BlockerService();
            _blockerService.Initialize(_databaseService);
            _classificationService = new ClassificationService(_databaseService); // Initialize internal classification service
            _iconExtractionService = new IconExtractionService();
            _notificationService = new NotificationService();

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

            // Calculate initial category breakdown
            RecalculateCategoryStats();

            // Perform Integrity Check in background
            System.Threading.Tasks.Task.Run(() => VerifyStatsIntegrity());
        }

        public void RecalculateCategoryStats()
        {
            var catUsage = _databaseService.GetTodayCategoryUsage();
            double distractedSeconds = 0;
            double productiveSeconds = 0;

            foreach (var kvp in catUsage)
            {
                if (IsDistractingCategory(kvp.Key))
                    distractedSeconds += kvp.Value;
                else if (IsProductiveCategory(kvp.Key))
                    productiveSeconds += kvp.Value;
            }

            TotalDistractingTime = TimeSpan.FromSeconds(distractedSeconds);
            TotalProductiveTime = TimeSpan.FromSeconds(productiveSeconds);
            OnStatsUpdated?.Invoke();
        }

        private bool IsDistractingCategory(string category)
        {
             return category == "Doom Scrolling" || 
                    category == "Entertainment" || 
                    category == "Social Media" || 
                    category == "Games";
        }

        private bool IsProductiveCategory(string category) // Minimal definition for now
        {
            return category == "Productive" || category == "Study" || category == "Education";
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
            
            // Native Categorization (Uses ClassificationService)
            _currentCategory = _classificationService.Categorize(_currentProcessName, _currentWindowTitle);

            // Context-Aware Process Naming Override (Native)
            // If it's a browser and categorized as special (Study/Productive), separate it.
            if (_classificationService.IsBrowserProcess(_currentProcessName))
            {
                if ((_currentCategory == "Study" || _currentCategory == "Productive") && 
                    !_currentProcessName.Contains($"({_currentCategory})"))
                {
                    _currentProcessName = $"{_currentProcessName} ({_currentCategory})";
                }
            }
            
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

            // Context-Aware Process Naming Override (Extension)
            // Rename if Study or Productive to ensure Dashboard separates it (e.g. "Chrome (Study)")
            // Note: GetSmartProcessName might return "YouTube", which is not strictly a browser process name,
            // but we treat it as a "Source".
            bool isBrowserOrSmartSource = _classificationService.IsBrowserProcess(_currentProcessName) || _currentProcessName == "YouTube"; /* Simplified check */
            
            // Just check if the current name doesn't already have the tag
            if ((_currentCategory == "Study" || _currentCategory == "Productive") && 
                !_currentProcessName.Contains($"({_currentCategory})"))
            {
                 // Only append if it looks like a generic tool or browser
                 // If it's "Visual Studio", we don't need "Visual Studio (Productive)".
                 // But "Chrome" needs "Chrome (Productive)".
                 // We can check if the base name is a browser.
                 // Ideally we check if `GetSmartProcessName` result or original process was a browser.
                 // For now, let's just apply it to everything that isn't obviously a Dev Tool?
                 // Safer: Check if it's in the Browser list OR if it's "YouTube"/"Google" etc.
                 // Let's use IsBrowserProcess check on the ORIGINAL process name if we had it, but here we only have _currentProcessName
                 // which might be "YouTube Shorts".
                 // "YouTube Shorts (Productive)"? Unlikely.
                 // "Chrome (Productive)"? Yes.
                 // Let's stick to the heuristic: if it contains "Chrome", "Edge", "Firefox" etc.
                 
                 // Simpler: Just do it. "YouTube (Study)" is fine. "Code (Productive)" is redundant but harmless.
                 // Better: Check IsBrowserProcess on the RAW process name? We don't have it easily passed here except via implicit state.
                 // Let's assume _currentProcessName is what we want to display.
                 // IF we want to split "Antigravity" usage in Chrome from "Doom Scrolling" in Chrome, we MUST rename.
                 // So we append.
                 _currentProcessName = $"{_currentProcessName} ({_currentCategory})";
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

            // V2 Logic
            if (data.ContentType == "Video") return "Entertainment";

            // Fallback Logic
            if (data.ActivityType == "reels" || data.ActivityType == "shorts") return "Doom Scrolling";
            if (!string.IsNullOrEmpty(data.Url) && data.Url.Contains("youtube.com")) return "Entertainment"; // Generic YouTube fallback

            // 2. Delegate to Rule Engine (Study, Productive, etc.)
            // We pass the Title and the Smart Process Name (or Browser Name)
            // Ideally use the Title for keyword matching.
            string pName = GetSmartProcessName(data) ?? "Browser"; // Use smart name or generic
            return _classificationService.Categorize(pName, data.Title ?? "");
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

            // Update category stats live (lightweight enough since it queries DB for today's summary? 
            // Querying DB every 5s might be heavy. Let's do it in-memory or optimizing.
            // For MVP, since SQLite is fast and local:
            RecalculateCategoryStats();

            CheckNotifications();

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
            // 24h format: 00:00 to 23:00 for the current day
            // We want to show a graph of usage distribution today.
            
            // Get raw hourly data
            var hourlyData = _hourlyUsage.ToDictionary(k => k.Key, v => v.Value);

             // Create points for every few hours to make the graph smooth but not too dense?
             // Or just every hour. The mock shows a smooth area chart.
             // We'll return 24 points (one for each hour).
             
             for (int hour = 0; hour < 24; hour++)
             {
                 double seconds = hourlyData.ContainsKey(hour) ? hourlyData[hour] : 0;
                 points.Add(new ChartDataPoint 
                 { 
                     Label = $"{hour:00}:00", 
                     Value = seconds / 60.0, // Minutes
                     Timestamp = DateTime.Today.AddHours(hour)
                 });
             }

            return points;
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

        private readonly NotificationService _notificationService;
        private int _lastNotifiedHour = 0;
        private DateTime _lastDailyReportDate = DateTime.MinValue;

        private class AppUsageInfo
        {
            public string ProcessName { get; set; } = "";
            public TimeSpan Duration { get; set; }
            public int SessionCount { get; set; } = 1;
        }

        private void CheckNotifications()
        {
            var now = DateTime.Now;
            var totalDuration = TimeSpan.FromSeconds(_dailyUsage.Values.Sum(u => u.Duration.TotalSeconds));
            int currentHourTotal = (int)totalDuration.TotalHours;

            // 1. Hourly Notification (e.g., at 1h, 2h, 3h...)
            //    Check if we crossed a new hour threshold
            if (currentHourTotal > _lastNotifiedHour && currentHourTotal > 0)
            {
                _lastNotifiedHour = currentHourTotal;
                var avg = _databaseService.GetLast7DaysAverageUsage();
                _notificationService.ShowHourlyScreenTimeNotification(FormatDuration(totalDuration), FormatDuration(avg));
            }

            // 2. Daily Summary at 9:00 PM (21:00)
            if (now.Hour == 21 && now.Minute == 0 && _lastDailyReportDate.Date != now.Date)
            {
                _lastDailyReportDate = now.Date;
                var avg = _databaseService.GetLast7DaysAverageUsage();
                _notificationService.ShowDailyReport(FormatDuration(totalDuration), FormatDuration(avg));
            }
        }
    }
}
