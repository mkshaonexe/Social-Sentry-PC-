using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace Social_Sentry.Data
{
    public class DatabaseService
    {
        private readonly string _dbPath;

        public DatabaseService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "SocialSentry");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            
            _dbPath = Path.Combine(folder, "sentry.db");
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();

                string tableCommand = @"
                    CREATE TABLE IF NOT EXISTS ActivityLog (
                        LogId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp TEXT NOT NULL,
                        ProcessName TEXT NOT NULL,
                        WindowTitle TEXT,
                        Url TEXT,
                        DurationSeconds INTEGER
                    );";

                using (var command = new SqliteCommand(tableCommand, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public void LogActivity(string processName, string windowTitle, string url)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();

                // Simple insert. optimized logic would update the last row if same activity.
                string insertCommand = @"
                    INSERT INTO ActivityLog (Timestamp, ProcessName, WindowTitle, Url, DurationSeconds)
                    VALUES (@timestamp, @processName, @windowTitle, @url, 1)";

                using (var command = new SqliteCommand(insertCommand, connection))
                {
                    command.Parameters.AddWithValue("@timestamp", DateTime.Now.ToString("o"));
                    command.Parameters.AddWithValue("@processName", processName);
                    command.Parameters.AddWithValue("@windowTitle", windowTitle);
                    command.Parameters.AddWithValue("@url", url ?? string.Empty);

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
