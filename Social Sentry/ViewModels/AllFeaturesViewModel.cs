using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Social_Sentry.Services;

namespace Social_Sentry.ViewModels
{
    public class AllFeaturesViewModel : ViewModelBase
    {
        public ObservableCollection<FeatureItem> Features { get; }

        public ICommand NavigateCommand { get; }
        public event System.Action<string>? NavigationRequest;
        
        private readonly SettingsService? _settingsService;

        public AllFeaturesViewModel()
        {
            // Initialize with default items
            Features = new ObservableCollection<FeatureItem>
            {
                new FeatureItem
                {
                    Title = "Reels Blocker",
                    Description = "Block short-form video content to regain your focus",
                    Icon = "🚫",
                    AccentColor = "#F44336",
                    Route = "ReelsBlocker"
                },
                new FeatureItem
                {
                    Title = "Usage Limits",
                    Description = "Set time limits for applications to maintain healthy digital habits",
                    Icon = "⏱️",
                    AccentColor = "#FF9800",
                    Route = "Limit"
                },
                new FeatureItem
                {
                    Title = "Adult Blocker",
                    Description = "Protect yourself from inappropriate content",
                    Icon = "🛡️",
                    AccentColor = "#2196F3",
                    Route = "AdultBlocker"
                }
            };
            
            NavigateCommand = new RelayCommand<string>(Navigate);

            try
            {
                _settingsService = new SettingsService();
                
                // Load initial state
                UpdateFeatureStates(_settingsService.LoadSettings());
                
                // Subscribe to changes
                SettingsService.SettingsChanged += UpdateFeatureStates;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AllFeaturesViewModel Error: {ex.Message}");
            }
        }

        private void UpdateFeatureStates(UserSettings settings)
        {
            foreach (var feature in Features)
            {
                if (feature.Route == "ReelsBlocker")
                {
                    feature.IsActive = settings.IsReelsBlockerEnabled;
                }
                else if (feature.Route == "AdultBlocker")
                {
                    feature.IsActive = settings.IsAdultBlockerEnabled;
                }
                 else if (feature.Route == "Limit")
                {
                    // Assuming Usage Limits is always "available" but maybe not "Active" in the same sense?
                    // Or maybe we verify if any limits are actually set?
                    // For now, let's assume it's "Active" if any limits exist OR just leave it as false/true?
                    // The user said "and users limit its node". I assume they mean "Is Note"? or "Is Mode"?
                    // "and users limit its node And let's make a individual use blocker"
                    // "users limited but if any features are turned on so that should be highlighted"
                    // Let's assume Usage Limits can be "On" if user has set it up. 
                    // Since we don't have a simple bool for Limits, let's assume it matches the others in visual style.
                    // IMPORTANT: The prompt said "users limit its node" -> maybe "it's not"? "it's mode"?
                    // Actually: "and default when user install ... the real blocker it's by default turn off and users limit its node"
                    // "its node" -> "is not"? "is on"?
                    // Context clue: "users limited but if any features are turned on..."
                    // Use a safe default (false) for Limits logic unless we check if limits exist.
                    // I will leave it false or implement a check if I can. 
                    // Without DB access here easily, I'll default to False (Inactive/Red Dot) until user configures it.
                    // But actually, for now, let's bind it to nothing special or false.
                    feature.IsActive = false; // Placeholder until we have a proper "IsLimitsEnabled" setting.
                }
            }
        }

        private void Navigate(string route)
        {
            if (!string.IsNullOrEmpty(route))
            {
                NavigationRequest?.Invoke(route);
            }
        }
    }

    public class FeatureItem : INotifyPropertyChanged
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string AccentColor { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
