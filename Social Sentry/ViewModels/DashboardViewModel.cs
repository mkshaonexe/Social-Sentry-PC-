using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Social_Sentry.Models;

namespace Social_Sentry.ViewModels
{
    public enum DashboardScope { Day, Week, Month, Custom }

    public class DashboardViewModel : ViewModelBase
    {
        private readonly Services.UsageTrackerService _usageTracker;
        private readonly Social_Sentry.Data.DatabaseService _databaseService;

        // State
        private DashboardScope _selectedScope = DashboardScope.Day;
        private DateTime _selectedDate = DateTime.Today;
        private bool _isLoading;

        public IEnumerable<DashboardScope> Scopes => Enum.GetValues(typeof(DashboardScope)).Cast<DashboardScope>();

        // UI Properties
        public DashboardScope SelectedScope
        {
            get => _selectedScope;
            set
            {
                if (SetProperty(ref _selectedScope, value))
                {
                    RefreshData();
                }
            }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    RefreshData();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private ObservableCollection<AppUsageItem> _topApps = new();
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

        private ObservableCollection<ChartDataPoint> _chartData = new();
        public ObservableCollection<ChartDataPoint> ChartData
        {
            get => _chartData;
            set => SetProperty(ref _chartData, value);
        }

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand PreviousDateCommand { get; }
        public ICommand NextDateCommand { get; }
        public ICommand SetScopeCommand { get; }

        public DashboardViewModel(Services.UsageTrackerService usageTracker, Social_Sentry.Data.DatabaseService databaseService)
        {
            _usageTracker = usageTracker;
            _databaseService = databaseService;
            
            _usageTracker.OnUsageUpdated += OnLiveUsageUpdated;

            RefreshCommand = new RelayCommand(RefreshData);
            PreviousDateCommand = new RelayCommand(PreviousDate);
            NextDateCommand = new RelayCommand(NextDate);
            SetScopeCommand = new RelayCommand<string>(SetScope);

            // Initial Load
            RefreshData();
        }

        private void OnLiveUsageUpdated()
        {
            // Only update if viewing Today
            if (SelectedScope == DashboardScope.Day && SelectedDate.Date == DateTime.Today)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(RefreshDataInternal);
            }
        }

        private void SetScope(string scopeStr)
        {
            if (Enum.TryParse<DashboardScope>(scopeStr, out var scope))
            {
                SelectedScope = scope;
            }
        }

        private void PreviousDate() => AdjustDate(-1);
        private void NextDate() => AdjustDate(1);

        private void AdjustDate(int direction)
        {
            switch (SelectedScope)
            {
                case DashboardScope.Day:
                    SelectedDate = SelectedDate.AddDays(direction);
                    break;
                case DashboardScope.Week:
                    SelectedDate = SelectedDate.AddDays(direction * 7);
                    break;
                case DashboardScope.Month:
                    SelectedDate = SelectedDate.AddMonths(direction);
                    break;
                case DashboardScope.Custom:
                    // Maybe do nothing or day?
                    SelectedDate = SelectedDate.AddDays(direction);
                    break;
            }
        }

        public void RefreshData()
        {
            if (IsLoading) return;
            // Run on background thread if not "Today" (which is in-memory)
            // But for simplicity, we can just run everything on UI thread or Task.Run
            // Since "Today" is fast.
            
            RefreshDataInternal();
        }

        private void RefreshDataInternal()
        {
            if (SelectedScope == DashboardScope.Day && SelectedDate.Date == DateTime.Today)
            {
                LoadTodayData();
            }
            else
            {
                LoadHistoricalData();
            }
        }

        private void LoadTodayData()
        {
            // 1. Top Apps
            var apps = _usageTracker.GetTopApps();
            TopApps = new ObservableCollection<AppUsageItem>(apps);
            
            // 2. Total Time
            TotalUsageTime = _usageTracker.GetTotalDurationString();
            
            // 3. Chart (Hourly for Today)
            var hourly = _usageTracker.GetHourlyUsage();
            UpdateHourlyChart(hourly);
        }

        private void LoadHistoricalData()
        {
            // IsLoading = true; // Can cause UI flicker if fast, but good practice.
            // Simplified synchronous for now as DB is local and small.
            
            try 
            {
                Dictionary<string, double> appStats;
                double totalSeconds = 0;

                if (SelectedScope == DashboardScope.Day)
                {
                    // Single Day History
                    var hourly = _databaseService.GetHourlyUsage(SelectedDate);
                    UpdateHourlyChart(hourly);

                    // For app stats, we can use GetTopAppsForRange (Start=End=Date)
                    appStats = _databaseService.GetTopAppsForRange(SelectedDate, SelectedDate);
                }
                else
                {
                    // Week / Month Range
                    DateTime start, end;
                    GetDateRange(out start, out end);
                    
                    var dailyStats = _databaseService.GetDailyUsageRange(start, end);
                    UpdateDailyChart(dailyStats, start, end);
                    
                    appStats = _databaseService.GetTopAppsForRange(start, end);
                }

                // Process App Stats
                totalSeconds = appStats.Values.Sum();
                var appList = appStats.Select(kvp => new AppUsageItem
                {
                    Name = kvp.Key,
                    RawDuration = TimeSpan.FromSeconds(kvp.Value),
                    Duration = FormatDuration(TimeSpan.FromSeconds(kvp.Value)),
                    Percentage = totalSeconds > 0 ? kvp.Value / totalSeconds : 0,
                    Sessions = 0, // Not tracked in aggregated view currently
                    Icon = _usageTracker.IconExtractor.GetProcessIcon(kvp.Key)
                }).OrderByDescending(x => x.RawDuration).ToList();

                TopApps = new ObservableCollection<AppUsageItem>(appList);
                TotalUsageTime = FormatDuration(TimeSpan.FromSeconds(totalSeconds));

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading history: {ex.Message}");
            }
            // IsLoading = false;
        }

        private void GetDateRange(out DateTime start, out DateTime end)
        {
            if (SelectedScope == DashboardScope.Week)
            {
                // Start on Monday?
                int diff = (7 + (SelectedDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                start = SelectedDate.AddDays(-1 * diff).Date;
                end = start.AddDays(6);
            }
            else if (SelectedScope == DashboardScope.Month)
            {
                start = new DateTime(SelectedDate.Year, SelectedDate.Month, 1);
                end = start.AddMonths(1).AddDays(-1);
            }
            else
            {
                start = SelectedDate;
                end = SelectedDate;
            }
        }

        private string _chartGeometry;
        public string ChartGeometry
        {
            get => _chartGeometry;
            set => SetProperty(ref _chartGeometry, value);
        }

        private void UpdateHourlyChart(Dictionary<int, double> hourly)
        {
            var points = new ObservableCollection<ChartDataPoint>();
            double max = hourly.Values.DefaultIfEmpty(1).Max();
            if (max == 0) max = 1;

            for (int i = 0; i < 24; i++) // 0 to 23
            {
                double val = hourly.ContainsKey(i) ? hourly[i] : 0;
                
                // Show label only every 4 hours (0, 4, 8, 12, 16, 20)
                // or every 6 hours (0, 6, 12, 18)
                string label = (i % 6 == 0) ? $"{i:00}:00" : string.Empty;

                points.Add(new ChartDataPoint
                {
                    TimeLabel = label,
                    Value = val / max,
                    RawValue = val,
                    TooltipText = $"{i:00}:00 - {FormatDuration(TimeSpan.FromSeconds(val))}"
                });
            }
            ChartData = points;
            CalculateChartGeometry(points);
        }

        private void UpdateDailyChart(Dictionary<DateTime, double> daily, DateTime start, DateTime end)
        {
            var points = new ObservableCollection<ChartDataPoint>();
            double max = daily.Values.DefaultIfEmpty(1).Max();
            if (max == 0) max = 1;

            int count = 0;
            int total = (end - start).Days + 1;
            
            for (DateTime date = start; date <= end; date = date.AddDays(1))
            {
                double val = daily.ContainsKey(date) ? daily[date] : 0;
                string label = "";

                if (SelectedScope == DashboardScope.Week)
                {
                    // Week view: show all days
                    label = date.DayOfWeek.ToString().Substring(0, 3);
                }
                else
                {
                    // Month view: show roughly every 5 days
                   if (count % 5 == 0 || count == total - 1)
                   {
                        label = date.Day.ToString();
                   }
                }
                
                points.Add(new ChartDataPoint
                {
                    TimeLabel = label,
                    Value = val / max,
                    RawValue = val,
                    TooltipText = $"{date:MM/dd} - {FormatDuration(TimeSpan.FromSeconds(val))}"
                });
                count++;
            }
            ChartData = points;
            CalculateChartGeometry(points);
        }

        private void CalculateChartGeometry(ObservableCollection<ChartDataPoint> points)
        {
            if (points == null || points.Count == 0)
            {
                ChartGeometry = "";
                return;
            }

            // We define a conceptual canvas of 100x100
            // X goes from 0 to 100
            // Y goes from 100 (bottom, value 0) to 0 (top, value 1)
            
            double width = 100.0;
            double height = 100.0;
            double stepX = width / (points.Count - 1);
            if (points.Count == 1) stepX = width; 

            using (var sw = new System.IO.StringWriter())
            {
                // Start at bottom-left
                sw.Write($"M 0,{height} ");

                // First point (actual data)
                // Y = height - (Value * height)
                double firstY = height - (points[0].Value * height);
                sw.Write($"L 0,{firstY:F1} ");

                for (int i = 1; i < points.Count; i++)
                {
                    double x = i * stepX;
                    double y = height - (points[i].Value * height);
                    sw.Write($"L {x:F1},{y:F1} ");
                }

                // Close the loop to bottom-right then bottom-left to create a filled area
                sw.Write($"L {width},{height} Z");
                
                ChartGeometry = sw.ToString();
            }
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
}
