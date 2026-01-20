using System.Windows.Input;

namespace Social_Sentry.ViewModels
{
    public class UserSettingsViewModel : ViewModelBase
    {
        private bool _startWithWindows;
        private bool _startMinimized;
        private bool _showNotifications = true;

        public bool StartWithWindows
        {
            get => _startWithWindows;
            set => SetProperty(ref _startWithWindows, value);
        }

        public bool StartMinimized
        {
            get => _startMinimized;
            set => SetProperty(ref _startMinimized, value);
        }

        public bool ShowNotifications
        {
            get => _showNotifications;
            set => SetProperty(ref _showNotifications, value);
        }

        public ICommand ExportDataCommand { get; }
        public ICommand ClearDataCommand { get; }

        public UserSettingsViewModel()
        {
            ExportDataCommand = new RelayCommand(ExportData);
            ClearDataCommand = new RelayCommand(ClearData);
        }

        private void ExportData()
        {
            // TODO: Implement data export functionality
            System.Windows.MessageBox.Show("Data export feature coming soon!", "Export Data",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void ClearData()
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to clear all usage data? This action cannot be undone.",
                "Clear All Data",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // TODO: Implement data clearing functionality
                System.Windows.MessageBox.Show("All data has been cleared.", "Success",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
    }
}
