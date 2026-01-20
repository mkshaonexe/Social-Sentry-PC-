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

        public void LoadCategories()
        {
            Categories.Clear();

            // Get all apps from usage tracker
            var apps = _usageTracker.GetTopApps();
            
            // Entertainment Category (Videos, Streaming, Gaming)
            var entertainmentApps = apps.Where(a => 
                a.Name.Contains("youtube", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("netflix", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("spotify", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("vlc", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("steam", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("game", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("twitch", System.StringComparison.OrdinalIgnoreCase)
            ).Select(a => a.Name).ToList();

            Categories.Add(new CategoryGroup
            {
                Name = "Entertainment",
                Icon = "🎬",
                Color = "#FF5722",
                Apps = new ObservableCollection<string>(entertainmentApps)
            });

            // Productive Category (Work, Development, Office)
            var productiveApps = apps.Where(a => 
                a.Name.Contains("visual studio", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("code", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("word", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("excel", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("powerpoint", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("notepad", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("jetbrains", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("eclipse", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("brave", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("chrome", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("edge", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("firefox", System.StringComparison.OrdinalIgnoreCase)
            ).Select(a => a.Name).ToList();

            Categories.Add(new CategoryGroup
            {
                Name = "Productive",
                Icon = "💼",
                Color = "#2196F3",
                Apps = new ObservableCollection<string>(productiveApps)
            });

            // Study Category (Learning, Reading, Research)
            var studyApps = apps.Where(a => 
                a.Name.Contains("notion", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("onenote", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("evernote", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("coursera", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("udemy", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("khan", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("duolingo", System.StringComparison.OrdinalIgnoreCase)
            ).Select(a => a.Name).ToList();

            Categories.Add(new CategoryGroup
            {
                Name = "Study",
                Icon = "📚",
                Color = "#9C27B0",
                Apps = new ObservableCollection<string>(studyApps)
            });

            // Doom Scrolling Category (Mindless browsing, Social feeds)
            var doomScrollingApps = apps.Where(a => 
                a.Name.Contains("facebook", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("instagram", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("twitter", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("tiktok", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("reddit", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("9gag", System.StringComparison.OrdinalIgnoreCase)
            ).Select(a => a.Name).ToList();

            Categories.Add(new CategoryGroup
            {
                Name = "Doom Scrolling",
                Icon = "📱",
                Color = "#FF9800",
                Apps = new ObservableCollection<string>(doomScrollingApps)
            });

            // Communication Category (Messaging, Email)
            var communicationApps = apps.Where(a => 
                a.Name.Contains("whatsapp", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("telegram", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("discord", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("slack", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("teams", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("outlook", System.StringComparison.OrdinalIgnoreCase) ||
                a.Name.Contains("gmail", System.StringComparison.OrdinalIgnoreCase)
            ).Select(a => a.Name).ToList();

            Categories.Add(new CategoryGroup
            {
                Name = "Communication",
                Icon = "💬",
                Color = "#4CAF50",
                Apps = new ObservableCollection<string>(communicationApps)
            });

            // Uncategorized
            var categorizedApps = entertainmentApps
                .Concat(productiveApps)
                .Concat(studyApps)
                .Concat(doomScrollingApps)
                .Concat(communicationApps)
                .ToHashSet();
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
        public string PercentageFormatted => $"{(int)(Percentage * 100)}%";
    }
}
