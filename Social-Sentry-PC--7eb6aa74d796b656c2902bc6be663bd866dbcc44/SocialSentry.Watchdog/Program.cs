using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SocialSentry.Watchdog
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                // No PID provided? Just exit or wait.
                return;
            }

            if (!int.TryParse(args[0], out int targetPid))
            {
                return;
            }

            try
            {
                Process targetProcess = Process.GetProcessById(targetPid);
                Console.WriteLine($"Monitoring Social Sentry (PID: {targetPid})...");

                // Wait for the target process to exit
                targetProcess.WaitForExit();

                Console.WriteLine("Target process exited. Restarting...");

                // Restart the application
                // We assume the main exe is named "Social Sentry.exe" and is in the same folder
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string exePath = Path.Combine(currentDir, "Social Sentry.exe");

                if (File.Exists(exePath))
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                else
                {
                    Console.WriteLine($"Could not find executable at: {exePath}");
                }
            }
            catch (ArgumentException)
            {
                // Process not found (already exited?)
                Console.WriteLine("Process not found. Restarting immediately...");
                RestartApp();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void RestartApp()
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string exePath = Path.Combine(currentDir, "Social Sentry.exe");
            if (File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true });
            }
        }
    }
}
