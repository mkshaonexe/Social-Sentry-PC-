using System;
using System.Windows;

namespace Social_Sentry.Services
{
    public class ThemeService
    {
        public void SetTheme(string themeName)
        {
            App.ApplyTheme(themeName);
        }
    }
}
