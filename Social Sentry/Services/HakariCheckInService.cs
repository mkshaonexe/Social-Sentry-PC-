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

            Task.Run(() => MonitorLoop(_cancellationTokenSource.Token));
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
            // NOTE: UsageTrackerService returns total seconds for today
            string totalDurationStr = _usageTrackerService.GetTotalDurationString();
            TimeSpan totalDuration = ParseDurationString(totalDurationStr);
            
            int totalHours = (int)totalDuration.TotalHours;

            // Debug
            System.Diagnostics.Debug.WriteLine($"[HakariCheckIn] TotalHours: {totalHours}, LastNotified: {lastNotifiedHour}");

            if (totalHours < 1) return;

            if (totalHours > lastNotifiedHour)
            {
                // Trigger Notification
                string message = MessageGenerator.GetHourlyCheckInMessage(totalHours);
                
                // Get distracted stats
                TimeSpan distracted = _usageTrackerService.TotalDistractingTime;
                
                // Port note: Android used average time too, skipped here for simplicity or add later
                long totalMinutes = (long)totalDuration.TotalMinutes;
                long distractedMinutes = (long)distracted.TotalMinutes;

                // Personality passed or random/default? 
                // MessageGenerator.GetHourlyCheckInMessage only takes hour currently in C# port.
                // But GetTotalScreenTimeMessage uses it. Let's use GetTotalScreenTimeMessage?
                // The Android code called showHakariCheckInNotification with message from `getHourlyCheckInMessage`.
                // Let's stick to that for now.
                
                string stats = $"\n📊 Total: {FormatTimeSpan(totalDuration)}\n🎯 Distracted: {FormatTimeSpan(distracted)}";

                _notificationService.ShowHakariCheckIn(message, stats);

                // Update Settings
                settings.HakariLastNotifiedDate = todayMillis;
                settings.HakariLastNotifiedHour = totalHours;
                _settingsService.SaveSettings(settings); // This saves and persists
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
            // Parses "1h 30m", "45m", "30s" roughly
            // UsageTracker returns exact string, but for logic we might want raw access.
            // UsageTrackerService exposes internal but better to add a public property for raw TotalTimeSpan.
            // But since I can't modify UsageTracker heavily right now without reading its full state,
            // I'll parse the string or rely on calculation.
            // Actually, querying DB or calculating from GetTotalDurationString is flaky.
            // Let's rely on GetHourlyUsage().Values.Sum() which is public.
            
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
