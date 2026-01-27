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
                // Set the override duration in ViewModel
                ViewModels.ReelsBlockerViewModel.TempDisableMinutes = minutes;

                var settingsService = new SettingsService();
                var settings = settingsService.LoadSettings();
                settings.IsReelsBlockerEnabled = false;
                settingsService.SaveSettings(settings); 
                
                // Optional: Show feedback or just close
                // System.Windows.MessageBox.Show($"Reels Blocker paused for {minutes} minutes.", "Social Sentry", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Failed to update settings: " + ex.Message);
            }
        }
    }
}
