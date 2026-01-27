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
                // ... Existing Mica Logic ...
                window.WindowStyle = WindowStyle.None;
                window.AllowsTransparency = false; 
                
                var chrome = new System.Windows.Shell.WindowChrome
                {
                    CaptionHeight = 32,
                    CornerRadius = new CornerRadius(8),
                    GlassFrameThickness = new Thickness(0),
                    ResizeBorderThickness = new Thickness(4)
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
                window.AllowsTransparency = false;
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

        protected override void OnStartup(StartupEventArgs e)
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
            }
            catch (System.Exception ex)
            {
                // Catch startup errors specifically to show them before UI loads
                ShowErrorAndExit(ex);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Server?.Dispose();
            base.OnExit(e);
        }
    }
}
