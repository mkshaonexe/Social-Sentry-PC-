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

        private RankingBadge _currentBadge = RankingBadge.AllBadges[0];
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

        public ObservableCollection<RankingBadge> AllBadges { get; } = new ObservableCollection<RankingBadge>(RankingBadge.AllBadges);

        private string _durationText = "00:00:00";
        public string DurationText
        {
            get => _durationText;
            set
            {
                if (_durationText != value)
                {
                    _durationText = value;
                    OnPropertyChanged(nameof(DurationText));
                }
            }
        }

        private bool _showInfo;
        public bool ShowInfo
        {
            get => _showInfo;
            set
            {
                if (_showInfo != value)
                {
                    _showInfo = value;
                    OnPropertyChanged(nameof(ShowInfo));
                }
            }
        }

        private bool _adultBlockingEnabled;
        public bool AdultBlockingEnabled
        {
            get => _adultBlockingEnabled;
            set
            {
                if (_adultBlockingEnabled != value)
                {
                    _adultBlockingEnabled = value;
                    OnPropertyChanged(nameof(AdultBlockingEnabled));
                }
            }
        }

        // Command to toggle info
        public RelayCommand ToggleInfoCommand { get; } = null!;
        
        // Command to turn off protection
        public RelayCommand TurnOffProtectionCommand { get; } = null!;
        public RelayCommand BackCommand { get; } = null!;
        public RelayCommand OpenSettingsCommand { get; } = null!;

        public RankingViewModel()
        {
             // Check for Design Mode to prevent VS Designer crashes
             if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
             {
                 _currentBadge = RankingBadge.AllBadges[5]; // Example badge
                 _strikeTimeText = "5 days 12 hours";
                 _currentDays = 5;
                 _dailyProgress = 0.5;
                 return;
             }

             // TODO: Dependency Injection would be better, but sticking to pattern
             var settingsService = new SettingsService(); 
             _rankingService = new RankingService(settingsService);
             
             _rankingService.InitializeRankingIfNew();
             
             AllBadges = new ObservableCollection<RankingBadge>(RankingBadge.AllBadges);
             
             // Constructor initialization of properties
             ToggleInfoCommand = new RelayCommand(ExecuteToggleInfo);
             TurnOffProtectionCommand = new RelayCommand(ExecuteTurnOffProtection);
             BackCommand = new RelayCommand(ExecuteBack);
             OpenSettingsCommand = new RelayCommand(ExecuteOpenSettings);

             // Start timer
             _timer = new DispatcherTimer();
             _timer.Interval = TimeSpan.FromSeconds(1);
             _timer.Tick += Timer_Tick;
             _timer.Start();

             UpdateData();
        }

        private void ExecuteToggleInfo()
        {
            ShowInfo = !ShowInfo;
        }

        private void ExecuteTurnOffProtection()
        {
            // TODO: Show disabling dialog/logic similar to Android
            // For now, toggle settings or show message.
            // Requires interacting with SettingsService to save state.
            
            // This is a placeholder action. In a real scenario, this would trigger
            // the penalty dialog or navigation to settings.
            System.Windows.MessageBox.Show("Disabling protection will reset your streak. Are you sure?", "Warning", System.Windows.MessageBoxButton.YesNo);
        }

        private void ExecuteBack()
        {
            // Placeholder: Check if navigation service exists or just log
        }

        private void ExecuteOpenSettings()
        {
            // Placeholder: Navigate to settings
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
             DurationText = _rankingService.GetDurationText();
             DailyProgress = _rankingService.GetDailyProgress();
             
             // Sync with settings
             var settings = new SettingsService().LoadSettings();
             AdultBlockingEnabled = settings.IsAdultBlockerEnabled;
        }
        
        private void UpdateProgress()
        {
            // Deprecated, handled in UpdateData
        }
    }
}
