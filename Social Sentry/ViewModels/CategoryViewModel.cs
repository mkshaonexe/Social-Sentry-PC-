using System.Collections.ObjectModel;
using System.Linq;

namespace Social_Sentry.ViewModels
{
    public class CategoryViewModel : ViewModelBase
    {
        private readonly Services.UsageTrackerService _usageTracker;

        public ObservableCollection<CategoryGroup> Categories { get; } = new();

        public CategoryViewModel(Services.UsageTrackerService usageTracker)
        {
            _usageTracker = usageTracker;
            LoadCategories();
        }

        private void LoadCategories()
        {
            // Get all apps from usage tracker
            var apps = _usageTracker.GetTopApps();
            
            // Social Media Category
            var socialApps = apps.Where(a => 
                a.Name.Contains("chrome", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("firefox", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("edge", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("whatsapp", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("telegram", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("discord", System.StringComparison.OrdinalIgnoreCase)
            ).Select(a => a.Name).ToList();

            if (socialApps.Any())
            {
                Categories.Add(new CategoryGroup
                {
                    Name = "Social Media",
                    Icon = "💬",
                    Color = "#4CAF50",
                    Apps = new ObservableCollection<string>(socialApps)
                });
            }

            // Productivity Category
            var productivityApps = apps.Where(a => 
                a.Name.Contains("word", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("excel", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("code", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("notepad", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("visual studio", System.StringComparison.OrdinalIgnoreCase)
            ).Select(a => a.Name).ToList();

            if (productivityApps.Any())
            {
                Categories.Add(new CategoryGroup
                {
                    Name = "Productivity",
                    Icon = "💼",
                    Color = "#2196F3",
                    Apps = new ObservableCollection<string>(productivityApps)
                });
            }

            // Entertainment Category
            var entertainmentApps = apps.Where(a => 
                a.Name.Contains("spotify", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("vlc", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("steam", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("netflix", System.StringComparison.OrdinalIgnoreCase)
            ).Select(a => a.Name).ToList();

            if (entertainmentApps.Any())
            {
                Categories.Add(new CategoryGroup
                {
                    Name = "Entertainment",
                    Icon = "🎮",
                    Color = "#FF5722",
                    Apps = new ObservableCollection<string>(entertainmentApps)
                });
            }

            // Uncategorized
            var categorizedApps = socialApps.Concat(productivityApps).Concat(entertainmentApps).ToHashSet();
            var uncategorizedApps = apps.Where(a => !categorizedApps.Contains(a.Name)).Select(a => a.Name).ToList();

            if (uncategorizedApps.Any())
            {
                Categories.Add(new CategoryGroup
                {
                    Name = "Uncategorized",
                    Icon = "📁",
                    Color = "#9E9E9E",
                    Apps = new ObservableCollection<string>(uncategorizedApps)
                });
            }
        }
    }

    public class CategoryGroup
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "";
        public ObservableCollection<string> Apps { get; set; } = new();
        public string AppCount => $"{Apps.Count} apps";
    }
}
