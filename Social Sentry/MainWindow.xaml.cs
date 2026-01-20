using System.Windows;

namespace Social_Sentry
{
    public partial class MainWindow : Window
    {
        private readonly Services.UsageTrackerService _usageTracker;
        private readonly ViewModels.MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            _usageTracker = new Services.UsageTrackerService();
            _viewModel = new ViewModels.MainViewModel(_usageTracker);

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