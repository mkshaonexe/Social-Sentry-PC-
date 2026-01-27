using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Social_Sentry.Services;

namespace Social_Sentry.ViewModels
{
    public class DesktopWidgetViewModel : INotifyPropertyChanged
    {
        private readonly UsageTrackerService _usageTrackerService;
        private readonly DispatcherTimer _timer;

        private string _screenTimeText = "0h 0m";
        public string ScreenTimeText
        {
            get => _screenTimeText;
            set { _screenTimeText = value; OnPropertyChanged(); }
        }

        private string _distractingTimeText = "0m";
        public string DistractingTimeText
        {
            get => _distractingTimeText;
            set { _distractingTimeText = value; OnPropertyChanged(); }
        }

        public DesktopWidgetViewModel(UsageTrackerService usageTrackerService)
        {
            _usageTrackerService = usageTrackerService;
            
            // Subscribe to updates (either immediate or periodically)
            _usageTrackerService.OnStatsUpdated += UpdateStats;
            _usageTrackerService.OnUsageUpdated += UpdateStats;

            // Failsafe timer: update every 30 seconds to ensure fresh data
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _timer.Tick += (s, e) => UpdateStats();
            _timer.Start();

            // Initial Load
            UpdateStats();
        }

        private void UpdateStats()
        {
            // Use UI Thread
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                ScreenTimeText = _usageTrackerService.GetTotalDurationString();
                DistractingTimeText = FormatDuration(_usageTrackerService.TotalDistractingTime);
            });
        }

        private string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}m";
            return $"{ts.Seconds}s";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
