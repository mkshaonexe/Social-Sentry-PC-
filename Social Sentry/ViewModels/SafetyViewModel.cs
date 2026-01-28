using System.Windows.Input;
using Social_Sentry.Services;

namespace Social_Sentry.ViewModels
{
    public class SafetyViewModel : ViewModelBase
    {
        private readonly SettingsService _settingsService;
        
        // This command delegates back to MainViewModel logic (which we'll wire up or pass via Action)
        public ICommand BackCommand { get; }
        public event System.Action? BackRequested;

        private bool _isSafetyEnabled;
        public bool IsSafetyEnabled
        {
            get => _isSafetyEnabled;
            set
            {
                if (SetProperty(ref _isSafetyEnabled, value))
                {
                    SaveSettings();
                }
            }
        }

        public SafetyViewModel()
        {
            _settingsService = new SettingsService();
            var settings = _settingsService.LoadSettings();
            _isSafetyEnabled = settings.IsSafetyEnabled;
            
            BackCommand = new RelayCommand(OnBack);
        }

        private void OnBack()
        {
            BackRequested?.Invoke();
        }

        private void SaveSettings()
        {
            var settings = _settingsService.LoadSettings();
            settings.IsSafetyEnabled = IsSafetyEnabled;
            _settingsService.SaveSettings(settings);
        }
    }
}
