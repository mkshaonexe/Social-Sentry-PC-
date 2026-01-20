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
        public RawDataViewModel RawDataVM { get; }

        public System.Windows.Input.ICommand NavigateCommand { get; }

        public MainViewModel(Services.UsageTrackerService usageTracker)
        {
            DashboardVM = new DashboardViewModel(usageTracker);
            RawDataVM = new RawDataViewModel(usageTracker);
            
            NavigateCommand = new RelayCommand<string>(Navigate);

            CurrentView = DashboardVM; // Default view
        }

        private void Navigate(string viewName)
        {
            switch (viewName)
            {
                case "Dashboard":
                    CurrentView = DashboardVM;
                    break;
                case "Raw":
                    CurrentView = RawDataVM;
                    break;
            }
        }
    }
}
