using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Social_Sentry.ViewModels
{
    public class LimitViewModel : ViewModelBase
    {
        private readonly Services.UsageTrackerService _usageTracker;
        private readonly Social_Sentry.Data.DatabaseService _dbService;
        private int _dailyLimitHours = 8;

        public int DailyLimitHours
        {
            get => _dailyLimitHours;
            set => SetProperty(ref _dailyLimitHours, value);
        }

        public ObservableCollection<AppLimitItem> AppLimits { get; } = new();

        public ICommand SaveLimitsCommand { get; }

        public LimitViewModel(Services.UsageTrackerService usageTracker, Social_Sentry.Data.DatabaseService dbService)
        {
            _usageTracker = usageTracker;
            _dbService = dbService;
            SaveLimitsCommand = new RelayCommand(SaveLimits);
            LoadAppLimits();
        }

        private void LoadAppLimits()
        {
            var apps = _usageTracker.GetTopApps().Take(10);
            
            foreach (var app in apps)
            {
                AppLimits.Add(new AppLimitItem
                {
                    AppName = app.Name,
                    CurrentUsage = $"Used: {app.Duration}",
                    LimitMinutes = 30,
                    BlockWhenExceeded = false
                });
            }
        }

        private void SaveLimits()
        {
            try
            {
                // Simple implementation: Create a rule for each "BlockWhenExceeded" item
                foreach (var item in AppLimits)
                {
                    if (item.BlockWhenExceeded)
                    {
                        var rule = new Models.Rule
                        {
                            Type = "App", // Assuming Process Name match
                            Value = item.AppName, // Should be process name
                            Action = "Block",
                            LimitSeconds = item.LimitMinutes * 60,
                            Category = "UserLimit"
                        };
                        _dbService.AddRule(rule);
                    }
                }

                System.Windows.MessageBox.Show("Limits saved successfully!", "Success", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving limits: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    public class AppLimitItem : ViewModelBase
    {
        private string _appName = "";
        private string _currentUsage = "";
        private int _limitMinutes;
        private bool _blockWhenExceeded;

        public string AppName
        {
            get => _appName;
            set => SetProperty(ref _appName, value);
        }

        public string CurrentUsage
        {
            get => _currentUsage;
            set => SetProperty(ref _currentUsage, value);
        }

        public int LimitMinutes
        {
            get => _limitMinutes;
            set => SetProperty(ref _limitMinutes, value);
        }

        public bool BlockWhenExceeded
        {
            get => _blockWhenExceeded;
            set => SetProperty(ref _blockWhenExceeded, value);
        }
    }
}
