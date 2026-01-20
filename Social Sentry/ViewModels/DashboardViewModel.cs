using System.Collections.ObjectModel;
using Social_Sentry.Models;

namespace Social_Sentry.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly Services.UsageTrackerService _usageTracker;

        private ObservableCollection<AppUsageItem> _topApps;
        public ObservableCollection<AppUsageItem> TopApps
        {
            get => _topApps;
            set => SetProperty(ref _topApps, value);
        }

        private string _totalUsageTime = "0s";
        public string TotalUsageTime
        {
            get => _totalUsageTime;
            set => SetProperty(ref _totalUsageTime, value);
        }

        private ObservableCollection<ChartDataPoint> _chartData;
        public ObservableCollection<ChartDataPoint> ChartData
        {
            get => _chartData;
            set => SetProperty(ref _chartData, value);
        }

        public DashboardViewModel(Services.UsageTrackerService usageTracker)
        {
            _usageTracker = usageTracker;
            _usageTracker.OnUsageUpdated += UpdateDashboard;

            TopApps = new ObservableCollection<AppUsageItem>();
            ChartData = new ObservableCollection<ChartDataPoint>();
            
            // Initialize empty chart
            for(int i = 0; i < 24; i++) // 24h
            {
                // We'll just show empty for now, or update in UpdateDashboard
            }

            UpdateDashboard();
        }

        private void UpdateDashboard()
        {
            // Update Top Apps
            var apps = _usageTracker.GetTopApps();
            
            // Update on UI Thread if needed
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                TopApps = new ObservableCollection<AppUsageItem>(apps);
                TotalUsageTime = _usageTracker.GetTotalDurationString();
                UpdateChart();
            });
        }

        private void UpdateChart()
        {
            var hourly = _usageTracker.GetHourlyUsage();
            var points = new ObservableCollection<ChartDataPoint>();

            // Simple Logic: Show last 8 hours or a fixed "Day View"
            // Let's do a fixed "Day View" with major markers
            // Or just existing implementation: specific points.
            
            // Let's map 0-23 hours to the chart
            // For concise UI, let's just show active hours or a condensed view. 
            // The mockup showed "8 PM", "9 PM".
            
            int currentHour = System.DateTime.Now.Hour;
            int startHour = Math.Max(0, currentHour - 5); // Show last 6 hours
            
            for (int i = startHour; i <= currentHour; i++)
            {
                double seconds = hourly.ContainsKey(i) ? hourly[i] : 0;
                // Normalize value for bar height (0.0 - 1.0)
                // We need a global max to normalize against
                double max = hourly.Values.DefaultIfEmpty(1).Max();
                if (max == 0) max = 1;
                
                points.Add(new ChartDataPoint 
                { 
                    TimeLabel = $"{i}:00", 
                    Value = seconds / max 
                });
            }

            ChartData = points;
        }
    }
}
