using System;
using System.Windows;
using System.Windows.Input;

namespace Social_Sentry.Views
{
    public partial class BlackoutWindow : Window
    {
        // Event to fire when correct PIN is entered
        public event Action? OnUnlock;

        public BlackoutWindow()
        {
            InitializeComponent();
            this.Loaded += BlackoutWindow_Loaded;
            this.KeyDown += BlackoutWindow_KeyDown;
        }

        private void BlackoutWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Aggressively capture focus
            this.Activate();
            this.Focus();
            PinBox.Focus();
        }

        private void BlackoutWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Trap navigation keys if possible within the app
            if (e.Key == Key.System && e.SystemKey == Key.F4)
            {
                e.Handled = true;
            }
        }

        private void Unblock_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for PIN verification logic
            if (PinBox.Password == "1234") // Temporary Hardcoded
            {
                // Notify subscribers (BlockerService) that unlock succeeded
                OnUnlock?.Invoke();
                this.Close();
            }
            else
            {
                MessageBox.Show("Incorrect PIN", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                PinBox.Password = "";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
