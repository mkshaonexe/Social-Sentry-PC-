using System;
using System.Drawing; // For Icon
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms; // Alias to avoid ambiguity

namespace Social_Sentry
{
    public partial class MainWindow : Window
    {
        private readonly Services.UsageTrackerService _usageTracker;
        private readonly ViewModels.MainViewModel _viewModel;
        private readonly Social_Sentry.Data.DatabaseService _databaseService;
        
        private Forms.NotifyIcon? _notifyIcon;
        private bool _isExplicitExit = false;

        public MainWindow()
        {
            InitializeComponent();
            
            _databaseService = new Social_Sentry.Data.DatabaseService();
            _usageTracker = new Services.UsageTrackerService(_databaseService);
            _viewModel = new ViewModels.MainViewModel(_usageTracker, _databaseService);

            // First Run Auto-Setup
            var settingsService = new Services.SettingsService();
            var settings = settingsService.LoadSettings();

            if (settings.IsFirstRun)
            {
                // 1. Enable Start with Windows in Registry
                settingsService.SetStartWithWindows(true);
                
                // 2. Update Settings object
                settings.StartWithWindows = true;
                settings.IsFirstRun = false;
                
                // 3. Save
                settingsService.SaveSettings(settings); // This is now encrypted
            }

            DataContext = _viewModel;

            // Subscribe to tracking toggle
            _viewModel.OnTrackingToggled += OnTrackingToggled;

            // Start tracking by default
            _usageTracker.Start();

            Closing += MainWindow_Closing;

            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();
            _notifyIcon.Text = "Social Sentry";
            
            // Try to load icon from Images/AppLogo.png and convert to Icon
            try 
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "AppLogo.png");
                if (File.Exists(iconPath))
                {
                    using (var bitmap = new Bitmap(iconPath))
                    {
                        // Get Hicon creates a handle to an icon. We must be careful with GDI+ handles but for single instance it's okay.
                        // Ideally we clone it or manage handle destroy, but .NET Icon.FromHandle wraps it.
                        _notifyIcon.Icon = Icon.FromHandle(bitmap.GetHicon());
                    }
                }
                else
                {
                    // Fallback to system icon if file missing
                    _notifyIcon.Icon = SystemIcons.Shield; 
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Shield; // Safe fallback
            }

            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();
            
            // Context Menu
            var contextMenu = new Forms.ContextMenuStrip();
            contextMenu.Items.Add("Open Social Sentry", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ExitApplication()
        {
            _isExplicitExit = true;
            _notifyIcon?.Dispose();
            _notifyIcon = null;
            Application.Current.Shutdown();
        }

        private void OnTrackingToggled(bool isEnabled)
        {
            if (isEnabled)
            {
                _usageTracker.Start();
            }
            else
            {
                _usageTracker.Stop();
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitExit)
            {
                // Minimize to tray instead of closing
                e.Cancel = true;
                Hide();
                
                // Optional: Show balloon tip
                // _notifyIcon?.ShowBalloonTip(2000, "Social Sentry", "Running in background", Forms.ToolTipIcon.Info);
            }
            else
            {
                 _usageTracker.Stop();
            
                 // Kill the watchdog to prevent restart
                 try 
                 {
                     var watchdogs = System.Diagnostics.Process.GetProcessesByName("SocialSentry.Watchdog");
                     foreach (var wd in watchdogs)
                     {
                         wd.Kill();
                     }
                 }
                 catch (Exception ex)
                 {
                     System.Diagnostics.Debug.WriteLine($"Error killing watchdog: {ex.Message}");
                 }
            }
        }
    }
}
