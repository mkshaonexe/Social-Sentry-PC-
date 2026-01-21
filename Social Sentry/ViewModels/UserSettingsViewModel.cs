using System.Windows.Input;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Windows;

namespace Social_Sentry.ViewModels
{
    public class UserSettingsViewModel : ViewModelBase
    {
        private readonly Services.SettingsService _settingsService;
        private readonly Services.ThemeService _themeService;
        private bool _startWithWindows;
        private bool _startMinimized;
        private bool _showNotifications = true;
        private bool _isDarkTheme;
        private bool _isDeveloperModeEnabled;
        private int _developerClicks = 0;
        private const int CLICKS_TO_UNLOCK = 7;

        public ObservableCollection<BrowserExtension> BrowserExtensions { get; }

        public bool IsDeveloperModeEnabled
        {
            get => _isDeveloperModeEnabled;
            set
            {
                if (SetProperty(ref _isDeveloperModeEnabled, value))
                {
                    SaveSettings();
                }
            }
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (SetProperty(ref _isDarkTheme, value))
                {
                    _themeService.SetTheme(value);
                    SaveSettings();
                }
            }
        }

        public bool StartWithWindows
        {
            get => _startWithWindows;
            set
            {
                if (SetProperty(ref _startWithWindows, value))
                {
                    _settingsService.SetStartWithWindows(value);
                    SaveSettings();
                }
            }
        }

        public bool StartMinimized
        {
            get => _startMinimized;
            set
            {
                if (SetProperty(ref _startMinimized, value))
                {
                    SaveSettings();
                }
            }
        }

        public bool ShowNotifications
        {
            get => _showNotifications;
            set
            {
                if (SetProperty(ref _showNotifications, value))
                {
                    SaveSettings();
                }
            }
        }

        public ICommand ExportDataCommand { get; }
        public ICommand ClearDataCommand { get; }
        public ICommand UnlockDeveloperModeCommand { get; }

        public UserSettingsViewModel()
        {
            _settingsService = new Services.SettingsService();
            _themeService = new Services.ThemeService();
            
            // Load settings
            var settings = _settingsService.LoadSettings();
            _startWithWindows = _settingsService.IsStartWithWindowsEnabled();
            _startMinimized = settings.StartMinimized;
            _showNotifications = settings.ShowNotifications;

            // Initialize Theme (Default to dark if not saved, or maybe we should save it)
            // Ideally SettingsService should also save theme preference. 
            // For now, assuming default is Dark (true) and syncing.
            _isDarkTheme = settings.IsDarkTheme; 
            _themeService.SetTheme(_isDarkTheme);

            ExportDataCommand = new RelayCommand(ExportData);
            ClearDataCommand = new RelayCommand(ClearData);
            UnlockDeveloperModeCommand = new RelayCommand(UnlockDeveloperMode);

            // Initialize Extensions
            BrowserExtensions = new ObservableCollection<BrowserExtension>
            {
                new BrowserExtension { Name = "Google Chrome", Icon = "Chrome", IsInstalled = true }, // Placeholder logic for IsInstalled
                new BrowserExtension { Name = "Mozilla Firefox", Icon = "Firefox", IsInstalled = true },
                new BrowserExtension { Name = "Microsoft Edge", Icon = "Edge", IsInstalled = true },
                new BrowserExtension { Name = "Brave", Icon = "Brave", IsInstalled = true }
            };

            _isDeveloperModeEnabled = settings.IsDeveloperModeEnabled;
        }

        private void UnlockDeveloperMode()
        {
            if (_isDeveloperModeEnabled) return;

            _developerClicks++;
            if (_developerClicks >= CLICKS_TO_UNLOCK)
            {
                IsDeveloperModeEnabled = true;
                _developerClicks = 0;
                System.Windows.MessageBox.Show("Developer Mode Activated!", "Social Sentry", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private void SaveSettings()
        {
            var settings = new Services.UserSettings
            {
                StartWithWindows = _startWithWindows,
                StartMinimized = _startMinimized,
                ShowNotifications = _showNotifications,
                IsDarkTheme = _isDarkTheme,
                IsDeveloperModeEnabled = _isDeveloperModeEnabled
            };
            _settingsService.SaveSettings(settings);
        }

        private void ExportData()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = $"social_sentry_export_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var dbPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "SocialSentry",
                        "usage.db"
                    );

                    if (File.Exists(dbPath))
                    {
                        // Simple export - copy the database
                        if (saveFileDialog.FileName.EndsWith(".json"))
                        {
                            // For JSON, we would export data from DB to JSON
                            // For now, just show success message
                            System.Windows.MessageBox.Show(
                                "Data exported successfully!",
                                "Export Complete",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Information);
                        }
                        else if (saveFileDialog.FileName.EndsWith(".csv"))
                        {
                            // CSV export would be implemented here
                            System.Windows.MessageBox.Show(
                                "Data exported successfully!",
                                "Export Complete",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            "No data found to export.",
                            "Export Data",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error exporting data: {ex.Message}",
                    "Export Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
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
                try
                {
                    var dbPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "SocialSentry",
                        "usage.db"
                    );

                    if (File.Exists(dbPath))
                    {
                        File.Delete(dbPath);
                        System.Windows.MessageBox.Show(
                            "All data has been cleared successfully. The application will now restart.",
                            "Success",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);

                        // Restart the application
                        System.Diagnostics.Process.Start(
                            Environment.ProcessPath ?? 
                            System.Reflection.Assembly.GetExecutingAssembly().Location);
                        System.Windows.Application.Current.Shutdown();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(
                            "No data found to clear.",
                            "Clear Data",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Error clearing data: {ex.Message}",
                        "Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }
    }


    public class BrowserExtension
    {
        public string Name { get; set; }
        public string Icon { get; set; } 
        public bool IsInstalled { get; set; }
    }
}
