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
        public string Url { get; set; } = string.Empty; // Added URL property
        public DateTime Timestamp { get; set; }
    }

    public class ActivityTracker
    {
        private NativeMethods.WinEventDelegate? _winEventDelegate;
        private IntPtr _hookHandle = IntPtr.Zero;

        public event Action<ActivityEvent>? OnActivityChanged;

        private readonly BrowserMonitor _browserMonitor;

        public ActivityTracker()
        {
            _browserMonitor = new BrowserMonitor();
        }

        public void Start()
        {
            if (_hookHandle != IntPtr.Zero) return;

            _winEventDelegate = new NativeMethods.WinEventDelegate(WinEventProc);
            _hookHandle = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_FOREGROUND, 
                NativeMethods.EVENT_SYSTEM_FOREGROUND, 
                IntPtr.Zero, 
                _winEventDelegate, 
                0, 
                0, 
                NativeMethods.WINEVENT_OUTOFCONTEXT);
            
            // Initial check
            CheckActivity();
        }

        public void Stop()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            CheckActivity();
        }

        private void CheckActivity()
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

                string url = string.Empty;

                if (_browserMonitor.IsBrowser(processName))
                {
                    url = _browserMonitor.GetCurrentUrl(hWnd);
                    // For debugging/logging verify we got something
                    if (!string.IsNullOrEmpty(url))
                    {
                        Debug.WriteLine($"Captured URL: {url}");
                    }
                }

                OnActivityChanged?.Invoke(new ActivityEvent
                {
                    ProcessName = processName,
                    WindowTitle = windowTitle,
                    Url = url,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error tracking activity: {ex.Message}");
            }
        }
    }
}
