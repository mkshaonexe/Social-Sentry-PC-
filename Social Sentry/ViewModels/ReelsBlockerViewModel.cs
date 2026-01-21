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
        }

        private void Toggle()
        {
            IsEnabled = !IsEnabled;
        }

        private void SaveSettings()
        {
            var settings = _settingsService.LoadSettings();
            settings.IsReelsBlockerEnabled = _isEnabled;
            _settingsService.SaveSettings(settings);
        }
    }
}
