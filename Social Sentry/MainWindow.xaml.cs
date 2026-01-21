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
        private readonly Services.SettingsService _settingsService;
        private Services.UserSettings _settings;
        
        private Forms.NotifyIcon? _notifyIcon;
        private bool _isExplicitExit = false;
        private bool _isTrayIconVisible = true;

        public bool IsCustomTitleBarVisible { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            
            _databaseService = new Social_Sentry.Data.DatabaseService();
            
            var mediaDetector = new Services.MediaDetector();
            // Initialize async (fire and forget for ctor, or await in Loaded)
            _ = mediaDetector.InitializeAsync();

            _usageTracker = new Services.UsageTrackerService(_databaseService, mediaDetector);
            _viewModel = new ViewModels.MainViewModel(_usageTracker, _databaseService);

            // First Run Auto-Setup
            _settingsService = new Services.SettingsService();
            _settings = _settingsService.LoadSettings();

            if (_settings.IsFirstRun)
            {
                // 1. Enable Start with Windows in Registry
                _settingsService.SetStartWithWindows(true);
                
                // 2. Update Settings object with defaults
                _settings.StartWithWindows = true;
                _settings.StartMinimizedToTray = true;
                _settings.ShowTrayIcon = true;
                _settings.RunInvisiblyInBackground = true;
                _settings.IsFirstRun = false;
                
                // 3. Save
                _settingsService.SaveSettings(_settings);
            }

            DataContext = _viewModel;

            // Subscribe to tracking toggle
            _viewModel.OnTrackingToggled += OnTrackingToggled;

            // Start tracking by default - ALWAYS, regardless of UI state
            _usageTracker.Start();

            Closing += MainWindow_Closing;

            InitializeTrayIcon();
            
            // Handle startup minimized to tray
            if (App.IsStartupLaunch && _settings.StartMinimizedToTray)
            {
                // Start hidden - don't show window
                WindowState = WindowState.Minimized;
                Hide();
                
                // Optional: Show balloon tip on first startup
                if (_settings.ShowNotifications && _isTrayIconVisible)
                {
                    _notifyIcon?.ShowBalloonTip(3000, "Social Sentry", 
                        "Running in background. Double-click tray icon to open.", 
                        Forms.ToolTipIcon.Info);
                }
            }
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
                        _notifyIcon.Icon = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
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

            // Set visibility based on settings
            _isTrayIconVisible = _settings.ShowTrayIcon;
            _notifyIcon.Visible = _isTrayIconVisible;
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();
            
            // Context Menu
            var contextMenu = new Forms.ContextMenuStrip();
            contextMenu.Items.Add("Open Social Sentry", null, (s, e) => ShowWindow());
            
            // Add separator
            contextMenu.Items.Add(new Forms.ToolStripSeparator());
            
            // Add Hide/Show Tray Icon option
            var hideTrayItem = new Forms.ToolStripMenuItem("Hide Tray Icon");
            hideTrayItem.Click += (s, e) => HideTrayIcon();
            contextMenu.Items.Add(hideTrayItem);
            
            // Add separator before Exit
            contextMenu.Items.Add(new Forms.ToolStripSeparator());
            
            contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void HideTrayIcon()
        {
            // Warn user before hiding
            var result = Forms.MessageBox.Show(
                "The tray icon will be hidden, but Social Sentry will continue running in the background.\n\n" +
                "To restore the tray icon, restart the application or relaunch from Start Menu.\n\n" +
                "Continue monitoring in background?",
                "Hide Tray Icon",
                Forms.MessageBoxButtons.YesNo,
                Forms.MessageBoxIcon.Information);

            if (result == Forms.DialogResult.Yes)
            {
                _isTrayIconVisible = false;
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                }
                
                // Update settings to remember preference
                _settings.ShowTrayIcon = false;
                _settingsService.SaveSettings(_settings);
                
                // Hide window too if visible
                if (IsVisible)
                {
                    Hide();
                }
                
                // Show notification that it's running invisibly
                System.Diagnostics.Debug.WriteLine("Social Sentry now running invisibly in background");
            }
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
            System.Windows.Application.Current.Shutdown();
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
                // Minimize to tray/background instead of closing
                // IMPORTANT: Tracking continues running in background
                e.Cancel = true;
                Hide();
                
                // Optional: Show balloon tip if tray is visible
                if (_isTrayIconVisible && _settings.ShowNotifications)
                {
                    _notifyIcon?.ShowBalloonTip(2000, "Social Sentry", "Running in background", Forms.ToolTipIcon.Info);
                }
            }
            else
            {
                 // Explicit exit - stop tracking and cleanup
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
