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
            BlockerService.SimulateGoBack();
            this.Close();
        }

        private void CloseBrowser_Click(object sender, RoutedEventArgs e)
        {
            BlockerService.SimulateCloseBrowser();
            this.Close();
        }
    }
}
