using System;
using System.Windows;
using Social_Sentry.Services;

namespace Social_Sentry.Views
{
    public partial class AdultBlockerOverlay : Window
    {
        public AdultBlockerOverlay()
        {
            InitializeComponent();
            this.Loaded += (s, e) => { this.Activate(); this.Focus(); };
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TurnOff_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsService = new SettingsService();
                var settings = settingsService.LoadSettings();
                settings.IsAdultBlockerEnabled = false;
                settingsService.SaveSettings(settings); 
                
                MessageBox.Show("Adult Blocker has been turned off.", "Social Sentry", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to update settings: " + ex.Message);
            }
        }
    }
}
