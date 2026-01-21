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

        private void Toggle()
        {
            IsEnabled = !IsEnabled;
        }

        private void SaveSettings()
        {
            var settings = _settingsService.LoadSettings();
            settings.IsAdultBlockerEnabled = _isEnabled;
            _settingsService.SaveSettings(settings);
        }
    }
}
