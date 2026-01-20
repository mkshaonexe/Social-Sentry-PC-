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
                    Title = "Dashboard",
                    Description = "Real-time screen time visualization with beautiful charts and graphs",
                    Icon = "📊",
                    AccentColor = "#00E676"
                },
                new FeatureItem
                {
                    Title = "Categories",
                    Description = "Group applications by category and track usage patterns",
                    Icon = "📁",
                    AccentColor = "#00BCD4"
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
                    Title = "Raw Data",
                    Description = "View detailed usage logs and data collected from your apps",
                    Icon = "📈",
                    AccentColor = "#9C27B0"
                },
                new FeatureItem
                {
                    Title = "Real-time Tracking",
                    Description = "Monitor your screen time as it happens with live updates",
                    Icon = "🔴",
                    AccentColor = "#F44336"
                },
                new FeatureItem
                {
                    Title = "Settings & Preferences",
                    Description = "Customize your experience with flexible settings and options",
                    Icon = "⚙️",
                    AccentColor = "#607D8B"
                },
                new FeatureItem
                {
                    Title = "Data Export",
                    Description = "Export your usage data for analysis or backup purposes",
                    Icon = "💾",
                    AccentColor = "#4CAF50"
                },
                new FeatureItem
                {
                    Title = "Notifications",
                    Description = "Get notified when you exceed your usage limits",
                    Icon = "🔔",
                    AccentColor = "#FFC107"
                },
                new FeatureItem
                {
                    Title = "Community",
                    Description = "Connect, chat, and share stories with the global community",
                    Icon = "🌍",
                    AccentColor = "#00E676"
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
