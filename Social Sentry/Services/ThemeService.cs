using System;
using System.Windows;

namespace Social_Sentry.Services
{
    public class ThemeService
    {
        private const string DarkThemeSource = "Themes/DarkTheme.xaml";
        private const string LightThemeSource = "Themes/LightTheme.xaml";

        public bool IsDarkTheme { get; private set; }

        public ThemeService()
        {
            // Default to dark theme on startup or check settings
            IsDarkTheme = true; 
        }

        public void SetTheme(bool isDark)
        {
            IsDarkTheme = isDark;
            string themeSource = isDark ? DarkThemeSource : LightThemeSource;
            Uri themeUri = new Uri(themeSource, UriKind.Relative);

            ResourceDictionary newTheme = new ResourceDictionary() { Source = themeUri };

            System.Windows.Application.Current.Resources.MergedDictionaries.Clear();
            System.Windows.Application.Current.Resources.MergedDictionaries.Add(newTheme);
        }

        public void ToggleTheme()
        {
            SetTheme(!IsDarkTheme);
        }
    }
}
