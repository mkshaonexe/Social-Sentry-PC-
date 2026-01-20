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

                // 1. Settings (Encryption / Config)
                string createSettings = @"
                    CREATE TABLE IF NOT EXISTS Settings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT,
                        EncryptionSalt TEXT
                    );";

                // 2. Rules (Blocking & Limits)
                string createRules = @"
                    CREATE TABLE IF NOT EXISTS Rules (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Type TEXT, -- 'App', 'Url', 'Title'
                        Value TEXT,
                        Category TEXT,
                        Action TEXT, -- 'Block', 'Limit'
                        LimitSeconds INTEGER,
                        ScheduleJson TEXT
                    );";

                // 3. Activity Log (Granular)
                string createActivity = @"
                    CREATE TABLE IF NOT EXISTS ActivityLog (
                        LogId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp TEXT NOT NULL,
                        ProcessName TEXT NOT NULL,
                        ProcessId INTEGER,
                        WindowTitle TEXT,
                        Url TEXT,
                        DurationSeconds INTEGER,
                        Category TEXT
                    );";

                // 4. Daily Stats (Aggregated)
                string createStats = @"
                    CREATE TABLE IF NOT EXISTS DailyStats (
                        Date TEXT,
                        ProcessName TEXT,
                        Category TEXT,
                        TotalSeconds INTEGER,
                        PRIMARY KEY (Date, ProcessName)
                    );";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = createSettings;
                    command.ExecuteNonQuery();

                    command.CommandText = createRules;
                    command.ExecuteNonQuery();

                    command.CommandText = createActivity;
                    command.ExecuteNonQuery();

                    command.CommandText = createStats;
                    command.ExecuteNonQuery();
                }
            }
        }

        public void LogActivity(string processName, string windowTitle, string url, double durationSeconds)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();

                // Batching/Coalescing logic is now handled by the caller (MainWindow session manager).
                // We just record the final calculated duration.
                string insertCommand = @"
                    INSERT INTO ActivityLog (Timestamp, ProcessName, WindowTitle, Url, DurationSeconds)
                    VALUES (@timestamp, @processName, @windowTitle, @url, @duration)";

                using (var command = new SqliteCommand(insertCommand, connection))
                {
                    command.Parameters.AddWithValue("@timestamp", DateTime.Now.ToString("o"));
                    command.Parameters.AddWithValue("@processName", processName);
                    command.Parameters.AddWithValue("@windowTitle", windowTitle);
                    command.Parameters.AddWithValue("@url", url ?? string.Empty);
                    command.Parameters.AddWithValue("@duration", (int)durationSeconds); // Store as integer seconds

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
