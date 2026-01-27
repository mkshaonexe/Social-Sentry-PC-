using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Social_Sentry.Views
{
    public partial class BlockOverlayWindow : Window
    {
        public BlockOverlayWindow()
        {
            InitializeComponent();
            
            // Center on screen
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            // Auto close after 2 seconds
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += (s, e) => 
            {
                timer.Stop();
                this.Close();
            };
            timer.Start();
        }
    }
}
