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
        }
    }
}