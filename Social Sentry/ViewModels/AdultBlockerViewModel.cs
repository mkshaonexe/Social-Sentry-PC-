using System.Windows.Input;
using Social_Sentry.Services;

namespace Social_Sentry.ViewModels
{
    public class AdultBlockerViewModel : ViewModelBase
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

        public AdultBlockerViewModel()
        {
            _settingsService = new SettingsService();
            var settings = _settingsService.LoadSettings();
            _isEnabled = settings.IsAdultBlockerEnabled;

            ToggleCommand = new RelayCommand(Toggle);
        }

        private System.Windows.Threading.DispatcherTimer? _reEnableTimer;

        private void Toggle()
        {
            IsEnabled = !IsEnabled;

            if (!IsEnabled)
            {
                // User turned it OFF. Start timer to re-enable automatically.
                if (_reEnableTimer == null)
                {
                    _reEnableTimer = new System.Windows.Threading.DispatcherTimer();
                    _reEnableTimer.Interval = TimeSpan.FromMinutes(5); // 5 minutes delay
                    _reEnableTimer.Tick += (s, e) => 
                    {
                        // Time's up! Re-enable.
                        IsEnabled = true;
                        StopTimer(); 
                    };
                }
                _reEnableTimer.Start();
            }
            else
            {
                // User turned it ON. Stop timer if running.
                StopTimer();
            }
        }

        private void StopTimer()
        {
            if (_reEnableTimer != null)
            {
                _reEnableTimer.Stop();
                _reEnableTimer = null;
            }
        }

        private void SaveSettings()
        {
            var settings = _settingsService.LoadSettings();
            settings.IsAdultBlockerEnabled = _isEnabled;
            _settingsService.SaveSettings(settings);
        }
    }
}
