using System.Windows.Input;
using Social_Sentry.Services;

namespace Social_Sentry.ViewModels
{
    public class ReelsBlockerViewModel : ViewModelBase
    {
        private readonly SettingsService _settingsService;
        private bool _isEnabled;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (SetProperty(ref _isEnabled, value))
                {
                    SaveSettings();
                }
            }
        }

        public ICommand ToggleCommand { get; }

        public ReelsBlockerViewModel()
        {
            _settingsService = new SettingsService();
            var settings = _settingsService.LoadSettings();
            _isEnabled = settings.IsReelsBlockerEnabled;

            ToggleCommand = new RelayCommand(Toggle);
            
            SettingsService.SettingsChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(UserSettings settings)
        {
            // Update local state without triggering save loop
            if (_isEnabled != settings.IsReelsBlockerEnabled)
            {
                _isEnabled = settings.IsReelsBlockerEnabled;
                OnPropertyChanged(nameof(IsEnabled));
                
                // Handle timer based on new state
                HandleAutoReEnable(_isEnabled);
            }
        }
        
        public static int? TempDisableMinutes { get; set; }
        private System.Windows.Threading.DispatcherTimer? _reEnableTimer;

        private void Toggle()
        {
            IsEnabled = !IsEnabled;
        }

        private void HandleAutoReEnable(bool isEnabled)
        {
            if (!isEnabled)
            {
                // Disabled: Start Timer
                int duration = TempDisableMinutes ?? 10; // Default 10 mins if not specified
                TempDisableMinutes = null; // Reset

                if (_reEnableTimer == null)
                {
                    _reEnableTimer = new System.Windows.Threading.DispatcherTimer();
                    _reEnableTimer.Tick += (s, e) => 
                    {
                        IsEnabled = true;
                        StopTimer();
                    };
                }
                
                _reEnableTimer.Interval = TimeSpan.FromMinutes(duration);
                _reEnableTimer.Start();
            }
            else
            {
                // Enabled: Stop Timer
                StopTimer();
            }
        }

        private void StopTimer()
        {
            if (_reEnableTimer != null)
            {
                _reEnableTimer.Stop();
            }
        }

        private void SaveSettings()
        {
            var settings = _settingsService.LoadSettings();
            settings.IsReelsBlockerEnabled = _isEnabled;
            _settingsService.SaveSettings(settings); // Events will propagate
            
            HandleAutoReEnable(_isEnabled);
        }
    }
}
