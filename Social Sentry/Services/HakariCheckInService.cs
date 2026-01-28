using System;
using System.Threading;
using System.Threading.Tasks;

namespace Social_Sentry.Services
{
    public class HakariCheckInService
    {
        private readonly SettingsService _settingsService;
        private readonly UsageTrackerService _usageTrackerService;
        private readonly NotificationService _notificationService;
        private CancellationTokenSource? _cancellationTokenSource;

        private const int CHECK_INTERVAL_MS = 5 * 60 * 1000; // 5 minutes
        
        // State
        private bool _hasWelcomed = false;
        private DateTime _lastContextNotificationTime = DateTime.MinValue;
        private DateTime _lastLateNightNotificationTime = DateTime.MinValue;

        public HakariCheckInService(
            SettingsService settingsService,
            UsageTrackerService usageTrackerService,
            NotificationService notificationService)
        {
            _settingsService = settingsService;
            _usageTrackerService = usageTrackerService;
            _notificationService = notificationService;
        }

        public void Start()
        {
            if (_cancellationTokenSource != null) return;
            _cancellationTokenSource = new CancellationTokenSource();

            // Startup Greeting (Fire and Forget)
            Task.Run(async () => 
            {
                await Task.Delay(2000); // Small delay to ensuring UI loaded
                CheckStartupGreeting();
                MonitorLoop(_cancellationTokenSource.Token);
            });
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    CheckAndNotify();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[HakariCheckIn] Error: {ex.Message}");
                }

                await Task.Delay(CHECK_INTERVAL_MS, token);
            }
        }

        private void CheckStartupGreeting()
        {
            var settings = _settingsService.LoadSettings();
            if (!settings.IsHakariNotificationEnabled || !settings.ShowNotifications) return;

            if (!_hasWelcomed)
            {
                _hasWelcomed = true;
                // Only show if it's the first run of the day or session?
                // For now, simpler: Show on app start if enabled.
                string msg = MessageGenerator.GetStartupMessage();
                _notificationService.ShowHakariCheckIn(msg, "System Online");
            }
        }

        private void CheckAndNotify()
        {
            var settings = _settingsService.LoadSettings();

            if (!settings.IsHakariNotificationEnabled || !settings.ShowNotifications)
                return;

            DateTime now = DateTime.Now;
            long todayMillis = new DateTimeOffset(now.Date).ToUnixTimeMilliseconds();

            // Check day reset
            bool isNewDay = !IsSameDay(settings.HakariLastNotifiedDate, todayMillis);
            int lastNotifiedHour = isNewDay ? 0 : settings.HakariLastNotifiedHour;

            // Get usage stats
            string totalDurationStr = _usageTrackerService.GetTotalDurationString();
            TimeSpan totalDuration = ParseDurationString(totalDurationStr);
            // TimeSpan totalDuration = TimeSpan.Zero; // Dummy
            int totalHours = (int)totalDuration.TotalHours;

            // 1. Hourly Usage Check
            if (totalHours > lastNotifiedHour && totalHours >= 1)
            {
                 // Trigger Notification
                string message = MessageGenerator.GetHourlyCheckInMessage(totalHours);
                
                // Get distracted stats
                TimeSpan distracted = _usageTrackerService.TotalDistractingTime;
                
                string stats = $"\n📊 Total: {FormatTimeSpan(totalDuration)}\n🎯 Distracted: {FormatTimeSpan(distracted)}";

                _notificationService.ShowHakariCheckIn(message, stats);

                // Update Settings
                settings.HakariLastNotifiedDate = todayMillis;
                settings.HakariLastNotifiedHour = totalHours;
                _settingsService.SaveSettings(settings); // This saves and persists
            }

            // 2. Context Intelligence Check
            CheckContextIntelligence(totalDuration);

            // 3. Late Night Check
            CheckLateNight(now);
        }

        private void CheckContextIntelligence(TimeSpan totalDuration)
        {
            // Rate Limit: Only once every 90 minutes
            if ((DateTime.Now - _lastContextNotificationTime).TotalMinutes < 90) return;

            // Don't trigger if total usage is very low (< 30 mins)
            if (totalDuration.TotalMinutes < 30) return;

            string currentApp = _usageTrackerService.CurrentProcessName;
            string category = _usageTrackerService.CurrentCategory;

            if (string.IsNullOrEmpty(currentApp)) return;

            string? message = null;

            // Gaming / Entertainment Logic
            if (category == "Games" || category == "Entertainment")
            {
                // 30% chance to trigger if browsing, higher if gaming
                if (new Random().NextDouble() > 0.7)
                {
                    message = MessageGenerator.GetCampingMessage(currentApp);
                }
            }
            // Productive logic
            else if (category == "Productive" || category == "Study" || currentApp.Contains("Visual Studio") || currentApp.Contains("Code"))
            {
                 // 20% chance to Encourage
                 if (new Random().NextDouble() > 0.8)
                 {
                     message = MessageGenerator.GetCodingMessage();
                 }
            }

            if (message != null)
            {
                _notificationService.ShowHakariCheckIn(message, $"Active in: {currentApp}");
                _lastContextNotificationTime = DateTime.Now;
            }
        }

        private void CheckLateNight(DateTime now)
        {
             // Logic: If between 1 AM and 5 AM
             if (now.Hour >= 1 && now.Hour < 5)
             {
                 // Rate Limit: Once every hour
                 if ((DateTime.Now - _lastLateNightNotificationTime).TotalMinutes < 60) return;

                 string msg = MessageGenerator.GetLateNightMessage();
                 _notificationService.ShowHakariCheckIn(msg, "Sleep Deprivation Risk");
                 _lastLateNightNotificationTime = DateTime.Now;
             }
        }

        private bool IsSameDay(long time1, long time2)
        {
            var dt1 = DateTimeOffset.FromUnixTimeMilliseconds(time1).DateTime.ToLocalTime();
            var dt2 = DateTimeOffset.FromUnixTimeMilliseconds(time2).DateTime.ToLocalTime();
            return dt1.Date == dt2.Date;
        }

        private TimeSpan ParseDurationString(string durationStr)
        {
            double totalSeconds = 0;
            var hourly = _usageTrackerService.GetHourlyUsage();
            foreach (var val in hourly.Values) totalSeconds += val;
            
            return TimeSpan.FromSeconds(totalSeconds);
        }

        private string FormatTimeSpan(TimeSpan ts)
        {
             if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            return $"{ts.Minutes}m";
        }
    }
}
