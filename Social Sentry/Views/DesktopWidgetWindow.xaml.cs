using System.Windows;
using System.Windows.Input;
using Social_Sentry.ViewModels;

namespace Social_Sentry.Views
{
    public partial class DesktopWidgetWindow : Window
    {
        public DesktopWidgetWindow(DesktopWidgetViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            // Initial Position (Top Right)
            // Ideally this should load from saved settings, but for now defaults to Top Right
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 20;
            this.Top = desktopWorkingArea.Top + 20;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
