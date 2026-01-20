using System.Configuration;
using System.Data;
using System.Windows;

namespace Social_Sentry
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Services.SelfProtectionService _protectionService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            _protectionService = new Services.SelfProtectionService();
            // Start the watchdog
            _protectionService.StartWatchdog();
        }
    }

}
