using System;
using System.Windows;
using Social_Sentry.Services;

namespace Social_Sentry.Views
{
    public partial class ReelsBlockerOverlay : Window
    {
        public ReelsBlockerOverlay()
        {
            InitializeComponent();
            this.Loaded += (s, e) => { this.Activate(); this.Focus(); };
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            // Just close the overlay, assuming user will navigate away or has been navigated back
            this.Close();
        }

        private void ShowTimeSelection_Click(object sender, RoutedEventArgs e)
        {
            MainPanel.Visibility = Visibility.Collapsed;
            TimeSelectionPanel.Visibility = Visibility.Visible;
        }

        private void CancelSelection_Click(object sender, RoutedEventArgs e)
        {
            TimeSelectionPanel.Visibility = Visibility.Collapsed;
            MainPanel.Visibility = Visibility.Visible;
        }

        private void TimeOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string minutesStr && int.TryParse(minutesStr, out int minutes))
            {
                DisableBlocker(minutes);
            }
        }

        private void DisableBlocker(int minutes)
        {
            try
            {
                // Use BlockerService Snooze logic
                // Assuming access to the service via App.xaml.cs or similar global access
                // Since we don't have DI container visible here easily, we need to find the active instance.
                // NOTE: Phase 2 refactor should introduce IoC.
                // For now, let's try to access it via App.Current if exposed, OR 
                // modify DisableBlocker to use a static event or shared state/singleton access.
                
                // Hack/Fix: Access UsageTracker via MainWindow
                if (System.Windows.Application.Current.MainWindow is MainWindow mainWin && mainWin.UsageTracker != null)
                {
                    mainWin.UsageTracker.SnoozeReelsBlocker(minutes);
                }
                
                // Remove legacy settings modification
                /*
                var settingsService = new SettingsService();
                var settings = settingsService.LoadSettings();
                settings.IsReelsBlockerEnabled = false;
                settingsService.SaveSettings(settings); 
                */

                this.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Failed to update settings: " + ex.Message);
            }
        }
    }
}
