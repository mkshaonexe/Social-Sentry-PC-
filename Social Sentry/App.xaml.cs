using System.Configuration;
using System.Data;
using System.Windows;

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
