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
        private Services.SelfProtectionService _protectionService;
        private Services.LocalApiServer _localApiServer;
        public static bool IsStartupLaunch { get; private set; } = false;
        private static Services.SettingsService _settingsService;

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

            if (themeName == "Mica")
            {
                window.WindowStyle = WindowStyle.None;
                window.AllowsTransparency = true; // Required for true transparency, but might be tricky with WindowChrome sometimes. 
                // Actually, for WinUI 3 feel, standard Window with WindowChrome is better than AllowsTransparency=True which has performance penalties.
                // Let's try just WindowStyle=None + WindowChrome first.
                window.AllowsTransparency = false; 
                
                var chrome = new System.Windows.Shell.WindowChrome
                {
                    CaptionHeight = 32,
                    CornerRadius = new CornerRadius(8),
                    GlassFrameThickness = new Thickness(0),
                    ResizeBorderThickness = new Thickness(4)
                };
                System.Windows.Shell.WindowChrome.SetWindowChrome(window, chrome);
                
                // Show Custom Title Bar (handled in MainWindow code behind via binding or event, 
                // but we can imply it by the theme name if we notify the window)
                if (window is MainWindow mainWin)
                {
                    mainWin.SetTitleBarVisibility(true);
                }
            }
            else
            {
                window.WindowStyle = WindowStyle.SingleBorderWindow;
                window.AllowsTransparency = false;
                System.Windows.Shell.WindowChrome.SetWindowChrome(window, null);
                
                if (window is MainWindow mainWin)
                {
                    mainWin.SetTitleBarVisibility(false);
                }
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Check if launched with --startup argument (from Windows boot)
            if (e.Args.Length > 0 && e.Args[0] == "--startup")
            {
                IsStartupLaunch = true;
            }
            
            _protectionService = new Services.SelfProtectionService();
            // Start the watchdog
            _protectionService.StartWatchdog();
            _protectionService.ApplySelfProtection();

            // Load Settings and Apply Theme
            _settingsService = new Services.SettingsService();
            var settings = _settingsService.LoadSettings();
            
            // Migrate legacy boolean if needed (SettingsService might have done it, but let's be safe)
            if (string.IsNullOrEmpty(settings.SelectedTheme))
            {
                settings.SelectedTheme = settings.IsDarkTheme ? "Dark" : "Light";
            }
            
            ApplyTheme(settings.SelectedTheme);

            // Start Local API Server for browser extension communication
            try
            {
                var activityTracker = new Services.ActivityTracker();
                _localApiServer = new Services.LocalApiServer(activityTracker);
                _localApiServer.Start();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start API server: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _localApiServer?.Dispose();
            base.OnExit(e);
        }
    }

}
