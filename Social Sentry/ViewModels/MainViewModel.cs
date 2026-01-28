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
        public ReelsBlockerViewModel ReelsBlockerVM { get; }
        public AdultBlockerViewModel AdultBlockerVM { get; }
        public CommunityViewModel CommunityVM { get; }
        public PrimeModeViewModel PrimeModeVM { get; }
        public RankingViewModel RankingVM { get; }

        private bool _isDeveloperModeEnabled;
        public bool IsDeveloperModeEnabled
        {
            get => _isDeveloperModeEnabled;
            set => SetProperty(ref _isDeveloperModeEnabled, value);
        }

        private bool _isRawDataVisible;
        public bool IsRawDataVisible
        {
            get => _isRawDataVisible;
            set => SetProperty(ref _isRawDataVisible, value);
        }

        private bool _isRankingVisible;
        public bool IsRankingVisible
        {
            get => _isRankingVisible;
            set => SetProperty(ref _isRankingVisible, value);
        }

        public ICommand NavigateCommand { get; }
        public ICommand ToggleTrackingCommand { get; }

        public event System.Action<bool>? OnTrackingToggled;

        public MainViewModel(Services.UsageTrackerService usageTracker, Social_Sentry.Data.DatabaseService databaseService)
        {
            _usageTracker = usageTracker;

            var classificationService = new Services.ClassificationService(databaseService);

            DashboardVM = new DashboardViewModel(usageTracker, databaseService);
            RawDataVM = new RawDataViewModel(usageTracker);
            CategoryVM = new CategoryViewModel(usageTracker, classificationService);
            LimitVM = new LimitViewModel(usageTracker, databaseService);
            UserSettingsVM = new UserSettingsViewModel();
            AllFeaturesVM = new AllFeaturesViewModel();
            AllFeaturesVM.NavigationRequest += Navigate;
            
            ReelsBlockerVM = new ReelsBlockerViewModel();
            AdultBlockerVM = new AdultBlockerViewModel();
            
            CommunityVM = new CommunityViewModel();
            PrimeModeVM = new PrimeModeViewModel();
            RankingVM = new RankingViewModel();
            
            NavigateCommand = new RelayCommand<string>(Navigate);
            ToggleTrackingCommand = new RelayCommand(ToggleTracking);

            CurrentView = DashboardVM; // Default view

            // Initialize Developer Mode state
            var settingsService = new Services.SettingsService();
            var settings = settingsService.LoadSettings();
            IsDeveloperModeEnabled = settings.IsDeveloperModeEnabled;
            IsRawDataVisible = settings.IsRawDataEnabled;
            IsRankingVisible = settings.IsRankingEnabled;
            Services.SettingsService.SettingsChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(Services.UserSettings settings)
        {
            IsDeveloperModeEnabled = settings.IsDeveloperModeEnabled;
            IsRawDataVisible = settings.IsRawDataEnabled;
            IsRankingVisible = settings.IsRankingEnabled;
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
                case "ReelsBlocker":
                    CurrentView = ReelsBlockerVM;
                    break;
                case "AdultBlocker":
                    CurrentView = AdultBlockerVM;
                    break;
                case "Prime":
                    CurrentView = PrimeModeVM;
                    break;
                case "Ranking":
                    CurrentView = RankingVM;
                    break;
            }
        }

        private void ToggleTracking()
        {
            IsTrackingEnabled = !IsTrackingEnabled;
        }
    }
}
