using System.Windows;

namespace Social_Sentry
{
    public partial class MainWindow : Window
    {
        private readonly Services.UsageTrackerService _usageTracker;

        public MainWindow()
        {
            InitializeComponent();
            
            _usageTracker = new Services.UsageTrackerService();
            _usageTracker.Start();

            DataContext = new ViewModels.MainViewModel(_usageTracker);

            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _usageTracker.Stop();
        }
    }
}