using Microsoft.Data.Sqlite;
using Social_Sentry.Services; // Namespace handling
using System;
using System.IO;

namespace Social_Sentry.Data
{
    public class DatabaseService
    {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private readonly EncryptionService _encryptionService;  

        public DatabaseService()
        {
            _encryptionService = new EncryptionService();

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "SocialSentry");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            
            _dbPath = Path.Combine(folder, "sentry.db");
            
            // Set up strict encryption
            string key = _encryptionService.GetMasterKey();
            // Connection string for SQLCipher (Microsoft.Data.Sqlite with bundle_e_sqlcipher)
            _connectionString = $"Data Source={_dbPath};Password={key};Mode=ReadWriteCreate";
            
            // Initialize bundled SQLCipher provider
            SQLitePCL.Batteries_V2.Init();

            try 
            {
                InitializeDatabase();
            }
            catch (SqliteException)
            {
                // Likely invalid password (meaning old DB was unencrypted or different key).
                // Backup and reset for Zero Trust compliance.
                string backupPath = _dbPath + ".bak-" + DateTime.Now.Ticks;
                if (File.Exists(_dbPath)) 
                {
                    File.Move(_dbPath, backupPath);
                    InitializeDatabase(); // Retry with fresh DB
                }
            }
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
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

                    // 5. Activity Log Migration (Add Metadata column)
                    bool needMetadataCol = false;
                    command.CommandText = "PRAGMA table_info(ActivityLog)";
                    using (var reader = command.ExecuteReader())
                    {
                        bool hasMetadata = false;
                        while(reader.Read())
                        {
                            if (reader.GetString(1) == "Metadata") hasMetadata = true;
                        }
                        if (!hasMetadata) needMetadataCol = true;
                    }

                    if (needMetadataCol)
                    {
                        command.CommandText = "ALTER TABLE ActivityLog ADD COLUMN Metadata TEXT";
                        command.ExecuteNonQuery();
                    }

                    // 6. Classification Rules (Dynamic Categorization)
                    
                    // Simple migration/check: Ensure table has correct schema
                    // If it exists but might be old version, check for a known new column 'Pattern'
                    bool needRecreate = false;
                    command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='ClassificationRules'";
                    long tableExists = (long)(command.ExecuteScalar() ?? 0L);

                    if (tableExists > 0)
                    {
                        command.CommandText = "PRAGMA table_info(ClassificationRules)";
                        using (var reader = command.ExecuteReader())
                        {
                            bool hasPatternCol = false;
                            while (reader.Read())
                            {
                                var colName = reader.GetString(1); // name is column 1
                                if (colName == "Pattern") hasPatternCol = true;
                            }
                            if (!hasPatternCol) needRecreate = true;
                        }
                    }

                    if (needRecreate)
                    {
                        command.CommandText = "DROP TABLE ClassificationRules";
                        command.ExecuteNonQuery();
                    }

                    string createClassification = @"
                        CREATE TABLE IF NOT EXISTS ClassificationRules (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Pattern TEXT,
                            MatchType TEXT, -- 'Contains', 'Exact', 'Regex'
                            Category TEXT,
                            Priority INTEGER
                        );";

                    command.CommandText = createClassification;
                    command.ExecuteNonQuery();
                }
            }
        }

        public event Action? OnRulesChanged;

        public List<Models.Rule> GetRules()
        {
            var rules = new List<Models.Rule>();
            using (var connection = new SqliteConnection(_connectionString))
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
            using (var connection = new SqliteConnection(_connectionString))
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
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand("DELETE FROM Rules", connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            OnRulesChanged?.Invoke();
        }

        public void LogActivity(string processName, string windowTitle, string url, double durationSeconds, string category = "", string metadata = "")
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string insertCommand = @"
                    INSERT INTO ActivityLog (Timestamp, ProcessName, WindowTitle, Url, DurationSeconds, Category, Metadata)
                    VALUES (@timestamp, @processName, @windowTitle, @url, @duration, @category, @metadata)";

                using (var command = new SqliteCommand(insertCommand, connection))
                {
                    command.Parameters.AddWithValue("@timestamp", DateTime.Now.ToString("o"));
                    // Encrypt sensitive info
                    command.Parameters.AddWithValue("@processName", _encryptionService.Encrypt(processName));
                    command.Parameters.AddWithValue("@windowTitle", _encryptionService.Encrypt(windowTitle));
                    command.Parameters.AddWithValue("@url", _encryptionService.Encrypt(url ?? string.Empty));
                    command.Parameters.AddWithValue("@duration", (int)durationSeconds);
                    command.Parameters.AddWithValue("@category", _encryptionService.Encrypt(category ?? string.Empty));
                    command.Parameters.AddWithValue("@metadata", _encryptionService.Encrypt(metadata ?? string.Empty));

                    command.ExecuteNonQuery();
                }
            }
        }

        // Classification Rules Methods
        public class ClassificationRule
        {
            public int Id { get; set; }
            public string Pattern { get; set; } = "";
            public string MatchType { get; set; } = "Contains";
            public string Category { get; set; } = "Uncategorized";
            public int Priority { get; set; }
        }

        public List<ClassificationRule> GetClassificationRules()
        {
            var rules = new List<ClassificationRule>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Pattern, MatchType, Category, Priority FROM ClassificationRules ORDER BY Priority DESC";
                using (var command = new SqliteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rules.Add(new ClassificationRule
                        {
                            Id = reader.GetInt32(0),
                            Pattern = reader.GetString(1),
                            MatchType = reader.GetString(2),
                            Category = reader.GetString(3),
                            Priority = reader.GetInt32(4)
                        });
                    }
                }
            }
            return rules;
        }

        public void AddClassificationRule(ClassificationRule rule)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string commandText = @"
                    INSERT INTO ClassificationRules (Pattern, MatchType, Category, Priority)
                    VALUES (@pattern, @matchType, @category, @priority)";

                using (var command = new SqliteCommand(commandText, connection))
                {
                    command.Parameters.AddWithValue("@pattern", rule.Pattern);
                    command.Parameters.AddWithValue("@matchType", rule.MatchType);
                    command.Parameters.AddWithValue("@category", rule.Category);
                    command.Parameters.AddWithValue("@priority", rule.Priority);
                    command.ExecuteNonQuery();
                }
            }
        }
        public Dictionary<string, double> GetTodayAppUsage()
        {
            var usage = new Dictionary<string, double>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                // Aggregate duration for today
                string query = @"
                    SELECT ProcessName, SUM(DurationSeconds) 
                    FROM ActivityLog 
                    WHERE date(Timestamp) = date('now', 'localtime')
                    GROUP BY ProcessName";

                using (var command = new SqliteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string process = _encryptionService.Decrypt(reader.GetString(0));
                        double seconds = reader.GetDouble(1);
                        if (usage.ContainsKey(process))
                            usage[process] += seconds;
                        else
                            usage[process] = seconds;
                    }
                }
            }
            return usage;
        }

        public List<ActivityLogItem> GetRecentActivityLogs(int limit = 100)
        {
            var logs = new List<ActivityLogItem>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT Timestamp, ProcessName, WindowTitle, Url, DurationSeconds 
                    FROM ActivityLog 
                    ORDER BY LogId DESC 
                    LIMIT @limit";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@limit", limit);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new ActivityLogItem
                            {
                                Timestamp = DateTime.Parse(reader.GetString(0)),
                                ProcessName = _encryptionService.Decrypt(reader.GetString(1)),
                                WindowTitle = _encryptionService.Decrypt(reader.GetString(2)),
                                Url = _encryptionService.Decrypt(reader.GetString(3)),
                                DurationSeconds = reader.GetDouble(4)
                            });
                        }
                    }
                }
            }
            return logs;
        }
    }

    public class ActivityLogItem
    {
        public DateTime Timestamp { get; set; }
        public string ProcessName { get; set; } = "";
        public string WindowTitle { get; set; } = "";
        public string Url { get; set; } = "";
        public double DurationSeconds { get; set; }
    }
}
