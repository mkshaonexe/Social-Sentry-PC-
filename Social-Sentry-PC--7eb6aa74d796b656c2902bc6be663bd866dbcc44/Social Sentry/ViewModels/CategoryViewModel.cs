using System.Collections.ObjectModel;
using System.Linq;
using System;

namespace Social_Sentry.ViewModels
{
    public class CategoryViewModel : ViewModelBase
    {
        private readonly Services.UsageTrackerService _usageTracker;
        private readonly Services.ClassificationService _classificationService;

        public ObservableCollection<CategoryGroup> Categories { get; } = new();

        public CategoryViewModel(Services.UsageTrackerService usageTracker, Services.ClassificationService classificationService)
        {
            _usageTracker = usageTracker;
            _classificationService = classificationService;
            LoadCategories();
        }

        public void LoadCategories()
        {
            Categories.Clear();

            // Get all apps from usage tracker
            var apps = _usageTracker.GetTopApps();
            
            // Group apps by category using ClassificationService
            var groupedApps = apps.GroupBy(a => _classificationService.Categorize(a.Name, ""));

            // Desired Order of categories to appear
            var desiredCategories = new[] { "Entertainment", "Productive", "Study", "Doom Scrolling", "Communication" };
            
            // Create CategoryGroups for defined categories (ensures they appear even if empty if we want, or just consistent order)
            foreach (var categoryName in desiredCategories)
            {
                var appsInGroup = groupedApps.FirstOrDefault(g => g.Key == categoryName)?.ToList() ?? new System.Collections.Generic.List<Social_Sentry.Models.AppUsageItem>();
                
                Categories.Add(CreateCategoryGroup(categoryName, appsInGroup));
            }

            // Handle "Uncategorized" or any other dynamic categories not in the desired list
            foreach (var group in groupedApps)
            {
                if (!desiredCategories.Contains(group.Key))
                {
                    Categories.Add(CreateCategoryGroup(group.Key, group.ToList()));
                }
            }

            // Calculate Totals
            CalculateTotals();
        }

        private CategoryGroup CreateCategoryGroup(string name, System.Collections.Generic.List<Social_Sentry.Models.AppUsageItem> apps)
        {
            return new CategoryGroup
            {
                Name = name,
                Icon = GetIconForCategory(name),
                Color = GetColorForCategory(name),
                Apps = new ObservableCollection<string>(apps.Select(a => a.Name))
            };
        }

        private string GetIconForCategory(string category)
        {
            return category switch
            {
                "Entertainment" => "🎬",
                "Productive" => "💼",
                "Study" => "📚",
                "Doom Scrolling" => "📱",
                "Communication" => "💬",
                _ => "📁"
            };
        }

        private string GetColorForCategory(string category)
        {
             return category switch
            {
                "Entertainment" => "#FF5722",
                "Productive" => "#2196F3",
                "Study" => "#9C27B0",
                "Doom Scrolling" => "#FF9800",
                "Communication" => "#4CAF50",
                _ => "#9E9E9E"
            };
        }

        private void CalculateTotals()
        {
            var apps = _usageTracker.GetTopApps();
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
        public string PercentageFormatted => $"{(int)(Percentage * 100)}%";
    }
}
