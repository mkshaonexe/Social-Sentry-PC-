using System.Collections.ObjectModel;

namespace Social_Sentry.ViewModels
{
    public class AllFeaturesViewModel : ViewModelBase
    {
        public ObservableCollection<FeatureItem> Features { get; }

        public AllFeaturesViewModel()
        {
            Features = new ObservableCollection<FeatureItem>
            {
                new FeatureItem
                {
                    Title = "Reels Blocker",
                    Description = "Block short-form video content to regain your focus",
                    Icon = "🚫",
                    AccentColor = "#F44336"
                },
                new FeatureItem
                {
                    Title = "Usage Limits",
                    Description = "Set time limits for applications to maintain healthy digital habits",
                    Icon = "⏱️",
                    AccentColor = "#FF9800"
                },
                new FeatureItem
                {
                    Title = "Adult Blocker",
                    Description = "Protect yourself from inappropriate content",
                    Icon = "🛡️",
                    AccentColor = "#2196F3"
                }
            };
        }
    }

    public class FeatureItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string AccentColor { get; set; } = string.Empty;
    }
}
