using System.Windows.Input;

namespace Social_Sentry.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private object _currentView = null!;
        private bool _isTrackingEnabled = true;
        private readonly Services.UsageTrackerService _usageTracker;

        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        private string _currentPageName = "Dashboard";
        public string CurrentPageName
        {
            get => _currentPageName;
            set => SetProperty(ref _currentPageName, value);
        }

        public bool IsTrackingEnabled
        {
            get => _isTrackingEnabled;
            set
            {
                if (SetProperty(ref _isTrackingEnabled, value))
                {
                    OnTrackingToggled?.Invoke(value);
                }
            }
        }

        public DashboardViewModel DashboardVM { get; }
        public RawDataViewModel RawDataVM { get; }
        public CategoryViewModel CategoryVM { get; }
        public LimitViewModel LimitVM { get; }
        public UserSettingsViewModel UserSettingsVM { get; }
        public AllFeaturesViewModel AllFeaturesVM { get; }
        public CommunityViewModel CommunityVM { get; }

        public ICommand NavigateCommand { get; }
        public ICommand ToggleTrackingCommand { get; }

        public event System.Action<bool>? OnTrackingToggled;

        public MainViewModel(Services.UsageTrackerService usageTracker, Social_Sentry.Data.DatabaseService databaseService)
        {
            _usageTracker = usageTracker;

            var classificationService = new Services.ClassificationService(databaseService);

            DashboardVM = new DashboardViewModel(usageTracker);
            RawDataVM = new RawDataViewModel(usageTracker);
            CategoryVM = new CategoryViewModel(usageTracker, classificationService);
            LimitVM = new LimitViewModel(usageTracker, databaseService);
            UserSettingsVM = new UserSettingsViewModel();
            AllFeaturesVM = new AllFeaturesViewModel();
            CommunityVM = new CommunityViewModel();
            
            NavigateCommand = new RelayCommand<string>(Navigate);
            ToggleTrackingCommand = new RelayCommand(ToggleTracking);

            CurrentView = DashboardVM; // Default view
        }


        private void Navigate(string viewName)
        {
            CurrentPageName = viewName;
            switch (viewName)
            {
                case "Dashboard":
                    CurrentView = DashboardVM;
                    break;
                case "Raw":
                    CurrentView = RawDataVM;
                    break;
                case "Categories":
                    CurrentView = CategoryVM;
                    break;
                case "Limit":
                    CurrentView = LimitVM;
                    break;
                case "Settings":
                    CurrentView = UserSettingsVM;
                    break;
                case "AllFeatures":
                    CurrentView = AllFeaturesVM;
                    break;
                case "Community":
                    CurrentView = CommunityVM;
                    break;
            }
        }

        private void ToggleTracking()
        {
            IsTrackingEnabled = !IsTrackingEnabled;
        }
    }
}
