using System;
using System.Diagnostics;
using System.Text;
using System.Timers;

namespace Social_Sentry.Services
{
    public class ActivityEvent
    {
        public string ProcessName { get; set; } = string.Empty;
        public int ProcessId { get; set; } // Added ProcessId
        public string WindowTitle { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty; // Added URL property
        public DateTime Timestamp { get; set; }
    }

    public class ActivityTracker
    {
        private NativeMethods.WinEventDelegate? _winEventDelegate;

        private IntPtr _hookHandleForeground = IntPtr.Zero;
        private IntPtr _hookHandleNameChange = IntPtr.Zero;

        private System.Timers.Timer _debounceTimer;
        private const int DebounceInterval = 250; // ms

        public event Action<ActivityEvent>? OnActivityChanged;

        private readonly BrowserMonitor _browserMonitor;
        private readonly IdleDetector _idleDetector;
        private bool _isIdle = false;

        public ActivityTracker()
        {
            _browserMonitor = new BrowserMonitor();
            _idleDetector = new IdleDetector();
            _idleDetector.OnIdleStateChanged += HandleIdleStateChanged;

            _debounceTimer = new System.Timers.Timer(DebounceInterval);
            _debounceTimer.AutoReset = false;
            _debounceTimer.Elapsed += (s, e) => 
            {
                // Ensure we run on UI thread or valid context if needed, 
                // but since CheckActivity just does Process/Window inspection, background thread is fine 
                // as long as we don't access UI elements directly (we use P/Invoke).
                CheckActivity();
            };
        }

        public void Start()
        {
            if (_hookHandleForeground != IntPtr.Zero) return;

            _idleDetector.Start();

            _winEventDelegate = new NativeMethods.WinEventDelegate(WinEventProc);
            
            // Hook 1: Foreground Window Changes
            _hookHandleForeground = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_FOREGROUND, 
                NativeMethods.EVENT_SYSTEM_FOREGROUND, 
                IntPtr.Zero, 
                _winEventDelegate, 
                0, 
                0, 
                NativeMethods.WINEVENT_OUTOFCONTEXT);

            // Hook 2: Name Changes (detects tab switches in browsers)
            _hookHandleNameChange = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_OBJECT_NAMECHANGE,
                NativeMethods.EVENT_OBJECT_NAMECHANGE,
                IntPtr.Zero,
                _winEventDelegate,
                0,
                0,
                NativeMethods.WINEVENT_OUTOFCONTEXT);
            
            // Initial check
            CheckActivity();
        }

        private void HandleIdleStateChanged(bool isIdle)
        {
            _isIdle = isIdle;
            if (_isIdle)
            {
                // Optional: Fire an "Idle" event or just stop logging updates
                Debug.WriteLine("Tracking paused due to inactivity.");
            }
            else
            {
                Debug.WriteLine("Tracking resumed.");
                CheckActivity(); // Log the current window immediately upon return
            }
        }

        public void Stop()
        {
            if (_hookHandleForeground != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_hookHandleForeground);
                _hookHandleForeground = IntPtr.Zero;
            }

            if (_hookHandleNameChange != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_hookHandleNameChange);
                _hookHandleNameChange = IntPtr.Zero;
            }

            _idleDetector.Stop();
            _debounceTimer.Stop();
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == NativeMethods.EVENT_SYSTEM_FOREGROUND)
            {
                // Immediate update on focus change
                _debounceTimer.Stop(); 
                CheckActivity();
            }
            else if (eventType == NativeMethods.EVENT_OBJECT_NAMECHANGE)
            {
                // Only process name changes for the currently active window
                IntPtr foregroundHwnd = NativeMethods.GetForegroundWindow();
                if (hwnd == foregroundHwnd)
                {
                    // Debounce: reset timer
                    _debounceTimer.Stop();
                    _debounceTimer.Start();
                }
            }
        }


        private void CheckActivity()
        {
            if (_isIdle) return;

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
                    ProcessId = (int)processId,
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
