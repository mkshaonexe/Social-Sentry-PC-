using System;
using System.Diagnostics;
using System.Text;
using System.Timers;

namespace Social_Sentry.Services
{
    public class ActivityEvent
    {
        public string ProcessName { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ActivityTracker
    {
        private readonly System.Timers.Timer _timer;
        private string _lastProcessName = string.Empty;
        private string _lastWindowTitle = string.Empty;

        public event Action<ActivityEvent>? OnActivityChanged;

        public ActivityTracker()
        {
            _timer = new System.Timers.Timer(1000); // Check every 1 second
            _timer.Elapsed += CheckActivity;
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();

        private void CheckActivity(object? sender, ElapsedEventArgs e)
        {
            try
            {
                IntPtr hWnd = NativeMethods.GetForegroundWindow();
                if (hWnd == IntPtr.Zero) return;

                NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
                if (processId == 0) return;

                Process process = Process.GetProcessById((int)processId);
                string processName = process.ProcessName;

                StringBuilder sb = new StringBuilder(256);
                NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                string windowTitle = sb.ToString();

                // Only invoke event if something changed to reduce noise (or keep 1s heartbeat if needed for time tracking)
                // For now, we'll fire every second to accumulate time, or we can handle logic in UI.
                // Let's fire if it changes OR strictly every interval for accurate counting in the DB.
                
                OnActivityChanged?.Invoke(new ActivityEvent
                {
                    ProcessName = processName,
                    WindowTitle = windowTitle,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                // Process might exit found between GetWindowThreadProcessId and GetProcessById
                Debug.WriteLine($"Error tracking activity: {ex.Message}");
            }
        }
    }
}
