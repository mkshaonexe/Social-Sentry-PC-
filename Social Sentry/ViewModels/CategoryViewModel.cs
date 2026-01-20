using System.Collections.ObjectModel;
using System.Linq;
using System;

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

            // Calculate Totals
            var appDurations = apps.ToDictionary(a => a.Name, a => a.RawDuration);
            TimeSpan totalUsage = TimeSpan.Zero;

            foreach (var category in Categories)
            {
                TimeSpan catDuration = TimeSpan.Zero;
                foreach (var appName in category.Apps)
                {
                    if (appDurations.TryGetValue(appName, out var dur))
                    {
                        catDuration += dur;
                    }
                }
                category.TotalDuration = catDuration;
                category.FormattedDuration = FormatDuration(catDuration);
                totalUsage += catDuration;
            }

            // Calculate Percentages
            double totalSeconds = totalUsage.TotalSeconds;
            if (totalSeconds < 1) totalSeconds = 1;

            foreach (var category in Categories)
            {
                category.Percentage = category.TotalDuration.TotalSeconds / totalSeconds;
            }

            TotalUsageFormatted = FormatDuration(totalUsage);
        }

        private string _totalUsageFormatted = "0s";
        public string TotalUsageFormatted
        {
            get => _totalUsageFormatted;
            set => SetProperty(ref _totalUsageFormatted, value);
        }

        private string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}m";
            return $"{ts.Seconds}s";
        }
    }

    public class CategoryGroup : ViewModelBase // Inherit ViewModelBase if needed, or just plain class if static
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "";
        public ObservableCollection<string> Apps { get; set; } = new();
        public string AppCount => $"{Apps.Count} apps";
        
        public TimeSpan TotalDuration { get; set; }
        public string FormattedDuration { get; set; } = "";
        public double Percentage { get; set; }
    }
}
