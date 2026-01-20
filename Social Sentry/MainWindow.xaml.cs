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
        public ObservableCollection<ActivityLogItem> Logs { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            _tracker = new ActivityTracker();
            _browserMonitor = new BrowserMonitor();
            _database = new DatabaseService();
            _blocker = new BlockerService(); // Phase 2
            Logs = new ObservableCollection<ActivityLogItem>();
            LogGrid.ItemsSource = Logs;

            _tracker.OnActivityChanged += OnActivityDetected;
        }

        private void OnActivityDetected(ActivityEvent e)
        {
            // Run on UI Thread
            Dispatcher.Invoke(() =>
            {
                string url = string.Empty;
                if (_browserMonitor.IsBrowser(e.ProcessName))
                {
                    // This can be slow, might want to run async in real app
                    url = _browserMonitor.GetCurrentUrl(NativeMethods.GetForegroundWindow());
                }

                // Phase 2: Check Block Rules
                bool blocked = _blocker.CheckAndBlock(e.ProcessName, e.WindowTitle, url);

                if (blocked)
                {
                    StatusText.Text = $"BLOCKED: {e.WindowTitle}";
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                    
                    // Show Overlay
                    var overlay = new Social_Sentry.Views.BlockOverlayWindow();
                    overlay.Show();
                } 
                else 
                {
                     StatusText.Text = $"Monitoring: {e.ProcessName}";
                     StatusText.Foreground = System.Windows.Media.Brushes.White;
                }

                var logItem = new ActivityLogItem
                {
                    Timestamp = e.Timestamp.ToLongTimeString(),
                    ProcessName = e.ProcessName,
                    WindowTitle = blocked ? "[BLOCKED] " + e.WindowTitle : e.WindowTitle,
                    Url = url
                };

                Logs.Insert(0, logItem);
                
                // Keep list small for UI performance
                if (Logs.Count > 100) Logs.RemoveAt(Logs.Count - 1);

                StatusText.Text = $"Monitoring: {e.ProcessName}";

                // Save to DB
                // Run in background to avoid blocking UI
                System.Threading.Tasks.Task.Run(() => _database.LogActivity(e.ProcessName, e.WindowTitle, url));
            });
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            _tracker.Start();
            StatusText.Text = "Sentry Started...";
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _tracker.Stop();
            StatusText.Text = "Sentry Stopped.";
        }
    }
}