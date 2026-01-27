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

        private void UpdateHourlyChart(Dictionary<int, double> hourly)
        {
            var points = new ObservableCollection<ChartDataPoint>();
            double max = hourly.Values.DefaultIfEmpty(1).Max();
            if (max == 0) max = 1;

            for (int i = 0; i < 24; i++)
            {
                double val = hourly.ContainsKey(i) ? hourly[i] : 0;
                points.Add(new ChartDataPoint
                {
                    TimeLabel = $"{i:00}:00",
                    Value = val / max,
                    RawValue = val,
                    TooltipText = $"{i:00}:00 - {FormatDuration(TimeSpan.FromSeconds(val))}"
                });
            }
            ChartData = points;
        }

        private void UpdateDailyChart(Dictionary<DateTime, double> daily, DateTime start, DateTime end)
        {
            var points = new ObservableCollection<ChartDataPoint>();
            double max = daily.Values.DefaultIfEmpty(1).Max();
            if (max == 0) max = 1;

            for (DateTime date = start; date <= end; date = date.AddDays(1))
            {
                double val = daily.ContainsKey(date) ? daily[date] : 0;
                string label = SelectedScope == DashboardScope.Week ? date.DayOfWeek.ToString().Substring(0, 3) : date.Day.ToString();
                
                points.Add(new ChartDataPoint
                {
                    TimeLabel = label,
                    Value = val / max,
                    RawValue = val,
                    TooltipText = $"{date:MM/dd} - {FormatDuration(TimeSpan.FromSeconds(val))}"
                });
            }
            ChartData = points;
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
