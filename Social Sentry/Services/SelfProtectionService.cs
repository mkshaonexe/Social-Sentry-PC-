using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Social_Sentry.Services
{
    public class SelfProtectionService
    {
        public void ApplySelfProtection()
        {
            try
            {
                Process p = Process.GetCurrentProcess();
                // Note: This requires the app to be running as Administrator to succeed fully
                // and even then, SetSecurityInfo on one's own process is tricky.
                // For this implementation, we will focus on the Watchdog as the primary defense.
                // The ACL part is complex to get right without locking ONESELF out of needed rights often.
                // We'll leave the method stubbed for now or implement a basic "Deny Everyone Terminate" if needed.
                
                Debug.WriteLine("SelfProtection: ACL modification bypassed for safety in this iteration.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying self protection: {ex.Message}");
            }
        }

        public void StartWatchdog()
        {
            try
            {
                // Path to Watchdog Exe 
                // Assuming it's in the same directory (after build copy)
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string watchdogPath = System.IO.Path.Combine(currentDir, "SocialSentry.Watchdog.exe");

                if (System.IO.File.Exists(watchdogPath))
                {
                    int myPid = Process.GetCurrentProcess().Id;
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = watchdogPath,
                        Arguments = $"{myPid}", // Pass my PID to watchdog
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden // Hide the watchdog console
                    };
                    Process.Start(psi);
                    Debug.WriteLine($"Watchdog started with PID target: {myPid}");
                }
                else
                {
                    Debug.WriteLine("Watchdog executable not found.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting watchdog: {ex.Message}");
            }
        }
    }
}
