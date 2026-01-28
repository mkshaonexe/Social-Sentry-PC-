using System;
using System.IO;

namespace Social_Sentry.Services
{
    public static class TraceLogger
    {
        private static readonly string LogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "social_sentry_trace.txt");

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            }
            catch { }
        }
    }
}
