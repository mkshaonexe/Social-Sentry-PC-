using Microsoft.Data.Sqlite;
using Social_Sentry.Services; // Namespace handling
using System;
using System.IO;

namespace Social_Sentry.Data
{
    public class DatabaseService
    {
        private readonly string _dbPath;
        private readonly EncryptionService _encryptionService; 

        public DatabaseService()
        {
            _encryptionService = new EncryptionService();

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

        public event Action? OnRulesChanged;

        public List<Models.Rule> GetRules()
        {
            var rules = new List<Models.Rule>();
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                string query = "SELECT * FROM Rules";
                using (var command = new SqliteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var rule = new Models.Rule
                        {
                            Id = reader.GetInt32(0),
                            Type = reader.GetString(1),
                            // Decrypt potential sensitive user input
                            Value = _encryptionService.Decrypt(reader.GetString(2)),
                            Category = reader.IsDBNull(3) ? "" : _encryptionService.Decrypt(reader.GetString(3)),
                            Action = reader.GetString(4),
                            LimitSeconds = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                            ScheduleJson = reader.IsDBNull(6) ? "" : reader.GetString(6)
                        };

                        // Safety check: if decryption failed heavily, might return empty string
                        if (!string.IsNullOrEmpty(rule.Value))
                        {
                            rules.Add(rule);
                        }
                    }
                }
            }
            return rules;
        }

        public void AddRule(Models.Rule rule)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                string commandText = @"
                    INSERT INTO Rules (Type, Value, Category, Action, LimitSeconds, ScheduleJson)
                    VALUES (@type, @value, @category, @action, @limit, @schedule)";

                using (var command = new SqliteCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@type", rule.Type);
                    // Encrypt Value and Category
                    command.Parameters.AddWithValue("@value", _encryptionService.Encrypt(rule.Value));
                    command.Parameters.AddWithValue("@category", _encryptionService.Encrypt(rule.Category));
                    command.Parameters.AddWithValue("@action", rule.Action);
                    command.Parameters.AddWithValue("@limit", rule.LimitSeconds);
                    command.Parameters.AddWithValue("@schedule", rule.ScheduleJson);
                    command.ExecuteNonQuery();
                }
            }
            OnRulesChanged?.Invoke();
        }

        public void ClearRules()
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                using (var command = new SqliteCommand("DELETE FROM Rules", connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            OnRulesChanged?.Invoke();
        }

        public void LogActivity(string processName, string windowTitle, string url, double durationSeconds)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();

                string insertCommand = @"
                    INSERT INTO ActivityLog (Timestamp, ProcessName, WindowTitle, Url, DurationSeconds)
                    VALUES (@timestamp, @processName, @windowTitle, @url, @duration)";

                using (var command = new SqliteCommand(insertCommand, connection))
                {
                    command.Parameters.AddWithValue("@timestamp", DateTime.Now.ToString("o"));
                    // Encrypt sensitive info
                    command.Parameters.AddWithValue("@processName", _encryptionService.Encrypt(processName));
                    command.Parameters.AddWithValue("@windowTitle", _encryptionService.Encrypt(windowTitle));
                    command.Parameters.AddWithValue("@url", _encryptionService.Encrypt(url ?? string.Empty));
                    command.Parameters.AddWithValue("@duration", (int)durationSeconds); 

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
