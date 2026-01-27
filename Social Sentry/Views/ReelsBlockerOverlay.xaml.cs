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

        private void TurnOff_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsService = new SettingsService();
                var settings = settingsService.LoadSettings();
                settings.IsReelsBlockerEnabled = false;
                settingsService.SaveSettings(settings); // triggers event in existing services
                
                MessageBox.Show("Reels Blocker has been turned off.", "Social Sentry", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update settings: " + ex.Message);
            }
        }
    }
}
