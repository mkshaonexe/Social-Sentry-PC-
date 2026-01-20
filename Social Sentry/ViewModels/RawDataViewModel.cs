using System;
using System.Collections.ObjectModel;
using Social_Sentry.Services;

namespace Social_Sentry.ViewModels
{
    public class RawDataViewModel : ViewModelBase
    {
        private readonly UsageTrackerService _usageTracker;

        public ObservableCollection<RawLogItem> Logs { get; } = new();

        public RawDataViewModel(UsageTrackerService usageTracker)
        {
            _usageTracker = usageTracker;
            _usageTracker.OnRawActivityDetected += OnActivityDetected;
        }

        private void OnActivityDetected(ActivityEvent evt)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Logs.Insert(0, new RawLogItem
                {
                    Timestamp = evt.Timestamp.ToString("HH:mm:ss"),
                    ProcessName = evt.ProcessName,
                    WindowTitle = evt.WindowTitle
                });

                // Keep list size manageable
                if (Logs.Count > 200)
                {
                    Logs.RemoveAt(Logs.Count - 1);
                }
            });
        }
    }

    public class RawLogItem
    {
        public string Timestamp { get; set; } = "";
        public string ProcessName { get; set; } = "";
        public string WindowTitle { get; set; } = "";
    }
}
