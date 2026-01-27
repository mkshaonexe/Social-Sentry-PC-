using Social_Sentry.Models;
using Social_Sentry.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace Social_Sentry.ViewModels
{
    public class RankingViewModel : ViewModelBase
    {
        private readonly RankingService _rankingService;
        private readonly DispatcherTimer _timer;

        private RankingBadge _currentBadge;
        public RankingBadge CurrentBadge
        {
            get => _currentBadge;
            set
            {
                if (_currentBadge != value)
                {
                    _currentBadge = value;
                    OnPropertyChanged(nameof(CurrentBadge));
                }
            }
        }

        private string _strikeTimeText = "0 days 0 hours";
        public string StrikeTimeText
        {
            get => _strikeTimeText;
            set
            {
                if (_strikeTimeText != value)
                {
                    _strikeTimeText = value;
                    OnPropertyChanged(nameof(StrikeTimeText));
                }
            }
        }

        private long _currentDays = 0;
        public long CurrentDays
        {
            get => _currentDays;
            set
            {
                if (_currentDays != value)
                {
                    _currentDays = value;
                    OnPropertyChanged(nameof(CurrentDays));
                    UpdateProgress();
                }
            }
        }

        // 0.0 to 1.0 for circular progress
        private double _dailyProgress; 
        public double DailyProgress
        {
            get => _dailyProgress;
            set
            {
                if (_dailyProgress != value)
                {
                    _dailyProgress = value;
                    OnPropertyChanged(nameof(DailyProgress));
                }
            }
        }

        public ObservableCollection<RankingBadge> AllBadges { get; }

        public RankingViewModel()
        {
             // TODO: Dependency Injection would be better, but sticking to pattern
             var settingsService = new SettingsService(); 
             _rankingService = new RankingService(settingsService);
             
             _rankingService.InitializeRankingIfNew();
             
             AllBadges = new ObservableCollection<RankingBadge>(RankingBadge.AllBadges);
             
             _timer = new DispatcherTimer();
             _timer.Interval = TimeSpan.FromSeconds(1);
             _timer.Tick += Timer_Tick;
             _timer.Start();

             UpdateData();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
             UpdateData();
        }

        private void UpdateData()
        {
             CurrentBadge = _rankingService.GetCurrentBadge();
             StrikeTimeText = _rankingService.GetFormattedStrikeTime();
             CurrentDays = _rankingService.GetCurrentStrikeDays();
             
             // Calculate daily progress (time passed in current 24h cycle)
             // This is simplified. Ideally we get the exact milliseconds from service.
             // For now, let's just make it visually progress based on time of day? 
             // No, it should be based on start time.
             
             // Quick hack for progress based on "seconds in current day of streak"
             // _rankingService needs to expose raw diff for accurate progress
             // I'll leave it as 0 for now or implement logic here if needed.
             // Let's assume progress is just seconds/86400 of the current day.
        }
        
        private void UpdateProgress()
        {
            // Implementation pending exact logic
        }
    }
}
