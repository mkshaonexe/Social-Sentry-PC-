using System;
using System.Collections.ObjectModel;
using System.Windows;
using Social_Sentry.Services;
using Social_Sentry.Data;
using Social_Sentry.Models;

namespace Social_Sentry
{
    public partial class MainWindow : Window
    {

        private readonly ActivityTracker _tracker;
        private readonly BrowserMonitor _browserMonitor;
        private readonly DatabaseService _database;
        private readonly BlockerService _blocker; // Phase 2
        private readonly System.Windows.Threading.DispatcherTimer _urlTimer; // Phase 3

        // Session State
        private ActivityLogItem? _currentSession;
        private DateTime _sessionStartTime;
        private bool _isBrowserActive = false;
        private string _lastUrl = string.Empty;

        public ObservableCollection<ActivityLogItem> Logs { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            _tracker = new ActivityTracker();
            _browserMonitor = new BrowserMonitor();
            _database = new DatabaseService();
            _blocker = new BlockerService(); 
            Logs = new ObservableCollection<ActivityLogItem>();
            LogGrid.ItemsSource = Logs;

            // Phase 3: URL Polling Timer (Only runs when browser is active)
            _urlTimer = new System.Windows.Threading.DispatcherTimer();
            _urlTimer.Interval = TimeSpan.FromSeconds(1);
            _urlTimer.Tick += UrlTimer_Tick;

            _tracker.OnActivityChanged += OnActivityDetected;
            _tracker.Start();
        }

        private void OnActivityDetected(ActivityEvent e)
        {
            Dispatcher.Invoke(() =>
            {
                // 1. End Previous Session
                EndCurrentSession();

                // 2. Start New Session
                _sessionStartTime = e.Timestamp;
                _isBrowserActive = _browserMonitor.IsBrowser(e.ProcessName);
                _lastUrl = string.Empty;

                _currentSession = new ActivityLogItem
                {
                    Timestamp = _sessionStartTime.ToLongTimeString(),
                    ProcessName = e.ProcessName,
                    WindowTitle = e.WindowTitle,
                    Url = ""
                };

                // 3. Handle Browser Logic
                if (_isBrowserActive)
                {
                    if (!_urlTimer.IsEnabled) _urlTimer.Start();
                    // Check URL immediately
                    CheckUrlAndUpdate();
                }
                else
                {
                    if (_urlTimer.IsEnabled) _urlTimer.Stop();
                }

                // 4. Update UI (Show we are monitoring this, but maybe don't add to log list until it's done or updated?)
                // For better UX, we add it to the list immediately, and maybe update the "Duration" column later (if we had one).
                // Or just insert it.
                Logs.Insert(0, _currentSession);
                StatusText.Text = $"Monitoring: {e.ProcessName}";
            });
        }

        private void UrlTimer_Tick(object? sender, EventArgs e)
        {
            CheckUrlAndUpdate();
        }

        private void CheckUrlAndUpdate()
        {
            if (_currentSession == null) return;

            string url = _browserMonitor.GetCurrentUrl(NativeMethods.GetForegroundWindow());
            
            // Check blocking (Phase 2) - Continually check
            bool blocked = _blocker.CheckAndBlock(_currentSession.ProcessName, _currentSession.WindowTitle, url);
            if (blocked)
            {
                StatusText.Text = $"BLOCKED: {_currentSession.WindowTitle}";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                var overlay = new Social_Sentry.Views.BlockOverlayWindow();
                overlay.Show();
                return; // Tab closed, so wait for next activity event
            }

            // Detect URL Change
            if (url != _lastUrl && !string.IsNullOrEmpty(url))
            {
                // If URL changed in the SAME app, we treat it as a new "Session" row for clarity? 
                // Or update the current one?
                // Better approach: End current session (log it) and start new one with new URL.
                
                if (!string.IsNullOrEmpty(_lastUrl)) // If we already had a URL, this is a change
                {
                    EndCurrentSession();
                    // Start new session
                    _sessionStartTime = DateTime.Now;
                    _currentSession = new ActivityLogItem
                    {
                        Timestamp = _sessionStartTime.ToLongTimeString(),
                        ProcessName = _currentSession.ProcessName, // Keep same
                        WindowTitle = _currentSession.WindowTitle, // Title might have changed, but we rely on Hook for title change usually.
                        Url = url
                    };
                    Logs.Insert(0, _currentSession);
                }
                else
                {
                    // First time seeing URL in this session, just update the current object
                    _currentSession.Url = url;
                    // Trigger UI update (Hack: Remove and Re-insert or implement INotifyPropertyChanged)
                    // Logs.RemoveAT(0); Logs.Insert(0, _currentSession); relies on ObservableCollection
                    // Simple refresh
                    int index = Logs.IndexOf(_currentSession);
                    if (index >= 0)
                    {
                         Logs[index] = new ActivityLogItem 
                         { 
                             Timestamp = _currentSession.Timestamp,
                             ProcessName = _currentSession.ProcessName,
                             WindowTitle = _currentSession.WindowTitle,
                             Url = url 
                         };
                         _currentSession = Logs[index];
                    }
                }
                _lastUrl = url;
            }
        }

        private void EndCurrentSession()
        {
            if (_currentSession != null)
            {
                // Calculate duration
                TimeSpan duration = DateTime.Now - _sessionStartTime;
                
                // Log to Database (Batching/Coalescing achieved: we wrote 1 row for X seconds)
                // Need to update DatabaseService to accept Duration.
                // For now, we mock passing it.
                string finalUrl = _currentSession.Url;
                string finalProc = _currentSession.ProcessName;
                string finalTitle = _currentSession.WindowTitle;
                
                System.Threading.Tasks.Task.Run(() => _database.LogActivity(finalProc, finalTitle, finalUrl, duration.TotalSeconds));
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            _tracker.Start();
            StatusText.Text = "Sentry Started...";
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _tracker.Stop();
            _urlTimer.Stop();
            EndCurrentSession();
            StatusText.Text = "Sentry Stopped.";
        }
    }
}