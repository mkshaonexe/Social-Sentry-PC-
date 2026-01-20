namespace Social_Sentry.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private object _currentView;

        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public DashboardViewModel DashboardVM { get; }

        public MainViewModel(Services.UsageTrackerService usageTracker)
        {
            DashboardVM = new DashboardViewModel(usageTracker);
            CurrentView = DashboardVM; // Default view
        }
    }
}
