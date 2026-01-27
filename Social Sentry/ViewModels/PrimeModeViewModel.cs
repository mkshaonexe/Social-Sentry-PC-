using Social_Sentry.Services;
using System.Windows.Input;

namespace Social_Sentry.ViewModels
{
    public class PrimeModeViewModel : ViewModelBase
    {
        private readonly PrimeModeService _primeModeService;

        private bool _isPrimeModeEnabled;
        public bool IsPrimeModeEnabled
        {
            get => _isPrimeModeEnabled;
            set
            {
                if (_isPrimeModeEnabled != value)
                {
                    _isPrimeModeEnabled = value;
                    OnPropertyChanged(nameof(IsPrimeModeEnabled));
                    TogglePrimeMode(value);
                }
            }
        }

        public PrimeModeViewModel()
        {
            _primeModeService = new PrimeModeService();
            _isPrimeModeEnabled = _primeModeService.IsPrimeModeActive;
        }

        private void TogglePrimeMode(bool enable)
        {
            if (enable)
            {
                _primeModeService.EnablePrimeMode();
            }
            else
            {
                _primeModeService.DisablePrimeMode();
            }
        }
    }
}
