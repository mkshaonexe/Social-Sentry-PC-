using System.Windows;

namespace Social_Sentry
{
    public partial class MainWindow : Window
    {
        private readonly Services.UsageTrackerService _usageTracker;
        private readonly ViewModels.MainViewModel _viewModel;
        private readonly Social_Sentry.Data.DatabaseService _databaseService;

        public MainWindow()
        {
            InitializeComponent();
            
            _databaseService = new Social_Sentry.Data.DatabaseService();
            _usageTracker = new Services.UsageTrackerService(_databaseService);
            _viewModel = new ViewModels.MainViewModel(_usageTracker, _databaseService);

            // First Run Auto-Setup
            var settingsService = new Services.SettingsService();
            var settings = settingsService.LoadSettings();

            if (settings.IsFirstRun)
            {
                // 1. Enable Start with Windows in Registry
                settingsService.SetStartWithWindows(true);
                
                // 2. Update Settings object
                settings.StartWithWindows = true;
                settings.IsFirstRun = false;
                
                // 3. Save
                settingsService.SaveSettings(settings); // This is now encrypted
            }

            DataContext = _viewModel;

            // Subscribe to tracking toggle
            _viewModel.OnTrackingToggled += OnTrackingToggled;

            // Start tracking by default
            _usageTracker.Start();

            Closing += MainWindow_Closing;
        }

        private void OnTrackingToggled(bool isEnabled)
        {
            if (isEnabled)
            {
                _usageTracker.Start();
            }
            else
            {
                _usageTracker.Stop();
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _usageTracker.Stop();
            
            // Kill the watchdog to prevent restart
            try 
            {
                var watchdogs = System.Diagnostics.Process.GetProcessesByName("SocialSentry.Watchdog");
                foreach (var wd in watchdogs)
                {
                    wd.Kill();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error killing watchdog: {ex.Message}");
            }
        }
    }
}