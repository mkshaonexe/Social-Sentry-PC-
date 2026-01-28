using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Social_Sentry.Services;

namespace Social_Sentry.ViewModels
{
    public class DesktopWidgetViewModel : INotifyPropertyChanged
    {
        private readonly UsageTrackerService _usageTrackerService;
        private readonly DispatcherTimer _timer;

        private string _screenTimeText = "0h 0m";
        public string ScreenTimeText
        {
            get => _screenTimeText;
            set { _screenTimeText = value; OnPropertyChanged(); }
        }

        private string _distractingTimeText = "0m";
        public string DistractingTimeText
        {
            get => _distractingTimeText;
            set { _distractingTimeText = value; OnPropertyChanged(); }
        }

        private Services.WidgetStyle _widgetStyle = Services.WidgetStyle.Graph; // Default
        public Services.WidgetStyle WidgetStyle
        {
            get => _widgetStyle;
            set
            {
                if (_widgetStyle != value)
                {
                    _widgetStyle = value;
                    OnPropertyChanged();
                    UpdateStats(); // Refresh data for new style
                }
            }
        }
        
        private System.Windows.Media.PointCollection _chartPoints = new System.Windows.Media.PointCollection();
        public System.Windows.Media.PointCollection ChartPoints
        {
            get => _chartPoints;
            set { _chartPoints = value; OnPropertyChanged(); }
        }

        public DesktopWidgetViewModel(UsageTrackerService usageTrackerService)
        {
            _usageTrackerService = usageTrackerService;
            
            // Load initial style
            var settings = new SettingsService().LoadSettings();
            _widgetStyle = settings.WidgetStyle;

            // Subscribe to settings changes
            SettingsService.SettingsChanged += OnSettingsChanged;

            // Subscribe to updates (either immediate or periodically)
            _usageTrackerService.OnStatsUpdated += UpdateStats;
            _usageTrackerService.OnUsageUpdated += UpdateStats;

            // Failsafe timer: update every 30 seconds to ensure fresh data
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _timer.Tick += (s, e) => UpdateStats();
            _timer.Start();

            // Initial Load
            UpdateStats();
        }

        private void OnSettingsChanged(UserSettings settings)
        {
            if (WidgetStyle != settings.WidgetStyle)
            {
                WidgetStyle = settings.WidgetStyle;
            }
        }

        private void UpdateStats()
        {
            // Use UI Thread
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                ScreenTimeText = _usageTrackerService.GetTotalDurationString();
                DistractingTimeText = FormatDuration(_usageTrackerService.TotalDistractingTime);

                if (WidgetStyle == Services.WidgetStyle.Graph)
                {
                    var data = _usageTrackerService.GetChartData();
                    var points = new System.Windows.Media.PointCollection();
                    
                    // Simple normalization for a 280x60 graph area
                    // X: 0 to 280 (distributed over 24h)
                    // Y: 0 to 60 (scaled to max value)

                    double width = 268; // padding
                    double height = 50; 
                    
                    if (data.Any())
                    {
                        double maxVal = data.Max(p => p.Value);
                        if (maxVal < 10) maxVal = 10; // Minimum scale of 10 mins

                        double stepX = width / (data.Count - 1);

                        // Add bottom-left start
                        points.Add(new System.Windows.Point(0, height));

                        for (int i = 0; i < data.Count; i++)
                        {
                            double x = i * stepX;
                            double y = height - ((data[i].Value / maxVal) * height);
                            points.Add(new System.Windows.Point(x, y));
                        }

                        // Add bottom-right end
                        points.Add(new System.Windows.Point(width, height));
                    }
                    ChartPoints = points;
                }
            });
        }

        private string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}m";
            return $"{ts.Seconds}s";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
