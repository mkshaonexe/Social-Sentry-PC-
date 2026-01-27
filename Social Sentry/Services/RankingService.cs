using System;
using Social_Sentry.Models;

namespace Social_Sentry.Services
{
    public class RankingService
    {
        private readonly SettingsService _settingsService;

        public RankingService(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public long GetCurrentStrikeDays()
        {
            var settings = _settingsService.LoadSettings();
            var startTime = settings.RankingStartTimestamp;
            
            if (startTime == 0) return 0; // Or handle as null

            var startDateTime = DateTimeOffset.FromUnixTimeMilliseconds(startTime);
            var now = DateTimeOffset.Now;
            var diff = now - startDateTime;

            return (long)diff.TotalDays;
        }

        public string GetFormattedStrikeTime()
        {
            var settings = _settingsService.LoadSettings();
            var startTime = settings.RankingStartTimestamp;

            if (startTime == 0) return "0 days 0 hours";

            var startDateTime = DateTimeOffset.FromUnixTimeMilliseconds(startTime);
            var now = DateTimeOffset.Now;
            var diff = now - startDateTime;

            if (diff.TotalMilliseconds < 0) return "0 days 0 hours";

            return $"{(int)diff.TotalDays} days {diff.Hours} hours";
        }

        public string GetDurationText()
        {
            var settings = _settingsService.LoadSettings();
            var startTime = settings.RankingStartTimestamp;

            if (startTime == 0) return "00:00:00";

            var startDateTime = DateTimeOffset.FromUnixTimeMilliseconds(startTime);
            var now = DateTimeOffset.Now;
            var diff = now - startDateTime;

            if (diff.TotalMilliseconds < 0) return "00:00:00";

            // HH:mm:ss format
            return $"{diff.Hours:D2}:{diff.Minutes:D2}:{diff.Seconds:D2}";
        }

        public double GetDailyProgress()
        {
            var settings = _settingsService.LoadSettings();
            var startTime = settings.RankingStartTimestamp;

            if (startTime == 0) return 0;

            var startDateTime = DateTimeOffset.FromUnixTimeMilliseconds(startTime);
            var now = DateTimeOffset.Now;
            var diff = now - startDateTime;

            if (diff.TotalMilliseconds < 0) return 0;

            var oneDayMs = 86400000.0;
            var msIntoCurrentDay = diff.TotalMilliseconds % oneDayMs;
            
            return msIntoCurrentDay / oneDayMs;
        }

        public RankingBadge GetCurrentBadge()
        {
            long days = GetCurrentStrikeDays();
            return RankingBadge.GetBadgeForDays(days);
        }

        public void ResetStreak(string reason)
        {
             var settings = _settingsService.LoadSettings();
             // Logic to reset streak (set timestamp to now)
             // And maybe log the relapse (future)
             settings.RankingStartTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
             _settingsService.SaveSettings(settings);
        }

        public void InitializeRankingIfNew()
        {
             var settings = _settingsService.LoadSettings();
             if (settings.RankingStartTimestamp == 0)
             {
                 settings.RankingStartTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                 _settingsService.SaveSettings(settings);
             }
        }
    }
}
