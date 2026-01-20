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
        }
    }

}
