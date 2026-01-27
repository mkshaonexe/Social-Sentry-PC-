using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.IO;
using System.Reflection;

namespace Social_Sentry.Services
{
    public class NotificationService
    {
        public void ShowHakariCheckIn(string message, string stats)
        {
            new ToastContentBuilder()
                .AddText("Hakari Check-in:")
                .AddText(message)
                .AddText(stats) // Appears as third line/description
                .AddAttributionText("Social Sentry AI")
                //.AddAppLogoOverride(new Uri(Path.GetFullPath("Images/app.ico")), ToastGenericAppLogoCrop.Circle)
                .Show(); 
        }

        public void ShowTimeUp()
        {
             new ToastContentBuilder()
                .AddText("Time's up! Let's get back to work")
                .AddText("Your unblock allowance has ended. Apps are now blocked again.")
                .AddAttributionText("Social Sentry Blocking")
                .Show();
        }

        public void ShowBlockerActive()
        {
             new ToastContentBuilder()
                .AddText("🛡️ Blocking Active")
                .AddText("Social media apps are now blocked!")
                .Show();
        }

        public void ShowAdultContentBlocked(string keyword)
        {
            new ToastContentBuilder()
                .AddText("🛡️ Adult Content Blocked")
                .AddText("Adult content keyword found so it blocked for safety. If this was wrong you can turn it off from the settings.")
                .AddAttributionText("Social Sentry Safety")
                .Show();
        }

        public void ShowDailyReport(string todayTime, string averageTime)
        {
            new ToastContentBuilder()
                .AddText("📊 Daily Screen Time Report")
                .AddText($"Today: {todayTime} | Average: {averageTime}")
                .Show();
        }
    }
}
