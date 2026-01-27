using System.Configuration;
using System.Data;
using System.Windows;
using System.Linq; // Added for OfType

namespace Social_Sentry
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private Services.SelfProtectionService? _protectionService;

        public static Services.LocalApiServer? Server { get; private set; }
        public static bool IsStartupLaunch { get; private set; } = false;
        private static Services.SettingsService? _settingsService;

        public static void ApplyTheme(string themeName)
        {
            var dict = new ResourceDictionary();
            string themePath;

            switch (themeName)
            {
                case "Light":
                    themePath = "Themes/LightTheme.xaml";
                    break;
                case "Mica":
                    themePath = "Themes/MicaTheme.xaml";
                    break;
                case "Dark":
                default:
                    themePath = "Themes/DarkTheme.xaml";
                    break;
            }

            dict.Source = new System.Uri(themePath, System.UriKind.Relative);

            // Remove existing theme dictionaries
            var mergedDicts = Current.Resources.MergedDictionaries;
            var oldTheme = mergedDicts.OfType<ResourceDictionary>()
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Themes/"));
            
            if (oldTheme != null)
            {
                mergedDicts.Remove(oldTheme);
            }

            mergedDicts.Add(dict);

            // Handle Window Styling for Mica
            if (System.Windows.Application.Current.MainWindow != null)
            {
               UpdateWindowStyle(System.Windows.Application.Current.MainWindow, themeName);
            }
        }

        public static void UpdateWindowStyle(Window window, string themeName)
        {
            if (window == null) return;

            // Get Window Handle
            var helper = new System.Windows.Interop.WindowInteropHelper(window);
            var handle = helper.Handle;

            // 1. Apply Immersive Dark Mode for Title Bar / Borders
            if (themeName == "Dark" || themeName == "Mica")
            {
                 int useImmersiveDarkMode = 1;
                 // Try enabling for newer Windows 10/11 (Attribute 20)
                 int result = Services.NativeMethods.DwmSetWindowAttribute(
                     handle, 
                     Services.NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, 
                     ref useImmersiveDarkMode, 
                     sizeof(int));
                 
                 if (result != 0)
                 {
                     // Fallback for older Windows 10 versions (Attribute 19)
                     Services.NativeMethods.DwmSetWindowAttribute(
                         handle, 
                         Services.NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, 
                         ref useImmersiveDarkMode, 
                         sizeof(int));
                 }
            }
            else
            {
                int useImmersiveDarkMode = 0;
                Services.NativeMethods.DwmSetWindowAttribute(
                     handle, 
                     Services.NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, 
                     ref useImmersiveDarkMode, 
                     sizeof(int));
            }

            // 2. Handle Custom Window Styles
            if (themeName == "Mica")
            {
                // Mica theme: Keep custom title bar with improved WindowChrome
                window.WindowStyle = WindowStyle.None;
                
                var chrome = new System.Windows.Shell.WindowChrome
                {
                    CaptionHeight = 32,
                    CornerRadius = new CornerRadius(0), // Use 0 for proper resize/maximize behavior
                    GlassFrameThickness = new Thickness(-1), // Enable glass frame
                    ResizeBorderThickness = new Thickness(4),
                    NonClientFrameEdges = System.Windows.Shell.NonClientFrameEdges.None,
                    UseAeroCaptionButtons = false
                };
                System.Windows.Shell.WindowChrome.SetWindowChrome(window, chrome);
                
                if (window is MainWindow mainWin)
                {
                    mainWin.SetTitleBarVisibility(true);
                }
            }
            else
            {
                // Revert to standard window for "Dark" and "Light" to get standard OS chrome 
                // but with the DWM dark mode applied above.
                window.WindowStyle = WindowStyle.SingleBorderWindow;
                System.Windows.Shell.WindowChrome.SetWindowChrome(window, null);
                
                if (window is MainWindow mainWin)
                {
                    mainWin.SetTitleBarVisibility(false);
                }
            }
        }

        public App()
        {
            // Global Exception Handling
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ShowErrorAndExit(e.Exception);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is System.Exception ex)
            {
                ShowErrorAndExit(ex);
            }
        }

        private void ShowErrorAndExit(System.Exception ex)
        {
            string errorMessage = $"A detailed error occurred:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nInner Exception:\n{ex.InnerException.Message}";
            }

            System.Windows.MessageBox.Show(errorMessage, "Social Sentry Crash Report", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Check if launched with --startup argument (from Windows boot)
            if (e.Args.Length > 0 && e.Args[0] == "--startup")
            {
                IsStartupLaunch = true;
            }
            
            try 
            {
                _protectionService = new Services.SelfProtectionService();
                // Start the watchdog
                _protectionService.StartWatchdog();
                _protectionService.ApplySelfProtection();

                // Load Settings and Apply Theme
                _settingsService = new Services.SettingsService();
                var settings = _settingsService.LoadSettings();
                
                // Migrate legacy boolean if needed
                if (string.IsNullOrEmpty(settings.SelectedTheme))
                {
                    settings.SelectedTheme = settings.IsDarkTheme ? "Dark" : "Light";
                }
                
                ApplyTheme(settings.SelectedTheme);

                // Start Local API Server for browser extension communication
                Server = new Services.LocalApiServer();
                Server.Start();

                // Initialize Supabase
                // Keys from .env
                string supabaseUrl = "https://eckumaylnjynriffyzos.supabase.co";
                string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVja3VtYXlsbmp5bnJpZmZ5em9zIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjcwMTc5MzAsImV4cCI6MjA4MjU5MzkzMH0.BMnlodyQLrYz6BBmRn9T9nmz6VRuZqHuxED2CQBUQLg";
                
                await Services.SupabaseService.Instance.InitializeAsync(supabaseUrl, supabaseKey);

                // Initialize Notification and Hakari Services
                // Need access to UsageTrackerService usually created by MainWindow or global?
                // Wait, UsageTrackerService is created in MainWindow or needs to be accessible here.
                // Currently MainWindow creates it. We need it *before* or grab it from MainWindow.
                
                // Hack: We can initialize them after MainWindow loads or change architecture to be DI container based.
                // For now, let's defer it slightly or check MainWindow availability.
                // Actually, MainWindow is instantiated by StartupUri in App.xaml usually?
                // No, App.xaml usually has StartupUri="MainWindow.xaml".
                // Let's check App.xaml content if we can view it, or assume standard WPF.
                // If StartupUri is set, MainWindow is created automatically.
                // We can hook into MainWindow.Loaded in MainWindow.xaml.cs or here.
                
                // Let's rely on hooking into MainWindow's creation logic if possible, 
                // OR we'll do it right after we know MainWindow exists.
                // But wait, HakariCheckInService logic is background.
                
                // Alternative: Initialize here if we can create UsageTrackerService here.
                // But UsageTrackerService is typically singleton per app session.
                // Let's make UsageTrackerService available globally or wait for MainWindow.
            }
            catch (System.Exception ex)
            {
                // Catch startup errors specifically to show them before UI loads
                ShowErrorAndExit(ex);
            }
        }
        
        public static Services.HakariCheckInService? HakariService { get; private set; }
        
        public static void InitializeHakariService(Services.UsageTrackerService usageTracker)
        {
             var notificationService = new Services.NotificationService();
             _settingsService = _settingsService ?? new Services.SettingsService(); // Ensure loaded
             
             HakariService = new Services.HakariCheckInService(_settingsService, usageTracker, notificationService);
             HakariService.Start();
        }

        public static void ToggleDesktopWidget(bool show)
        {
            if (Current is App app)
            {
                if (show)
                    app.ShowDesktopWidget();
                else
                    app.CloseDesktopWidget();
            }
        }

        private Views.DesktopWidgetWindow? _desktopWidget;

        public void ShowDesktopWidget()
        {
            if (_desktopWidget == null)
            {
                if (System.Windows.Application.Current.MainWindow is MainWindow mainWin && mainWin.UsageTracker != null)
                {
                    // Create VM with real service
                    var vm = new ViewModels.DesktopWidgetViewModel(mainWin.UsageTracker);
                    _desktopWidget = new Views.DesktopWidgetWindow(vm);
                    _desktopWidget.Show();
                }
                else
                {
                    // Retry slightly later if MainWindow isn't ready (e.g. startup race condition)
                    // Or ignore if MainWindow is closed.
                }
            }
            else
            {
                _desktopWidget.Show();
                _desktopWidget.Activate();
            }
        }

        public void CloseDesktopWidget()
        {
            if (_desktopWidget != null)
            {
                _desktopWidget.Close();
                _desktopWidget = null;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            CloseDesktopWidget();
            Server?.Dispose();
            base.OnExit(e);
        }
    }
}
