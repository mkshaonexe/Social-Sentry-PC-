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
                
                // CRITICAL FIX: Clear connection pools to release file lock
                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                string backupPath = _dbPath + ".bak-" + DateTime.Now.Ticks;
                if (File.Exists(_dbPath)) 
                {
                    try 
                    {
                        File.Move(_dbPath, backupPath);
                        InitializeDatabase(); // Retry with fresh DB
                    }
                    catch (Exception ex)
                    {
                        // If we still can't move it, we might be stuck, but at least don't crash the whole app just yet.
                        // Ideally rethrow or handle specifically. 
                        // For now, let's treat it as fatal but caught by global handler if rethrown.
                        throw new Exception($"Failed to reset database. Please delete 'sentry.db' manually. Error: {ex.Message}", ex);
                    }
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

                    // 6. Hourly Stats (Aggregated for Graph Performance)
                    string createHourlyStats = @"
                        CREATE TABLE IF NOT EXISTS HourlyStats (
                            Date TEXT,
                            Hour INTEGER,
                            ProcessName TEXT,
                            Category TEXT,
                            TotalSeconds REAL,
                            PRIMARY KEY (Date, Hour, ProcessName)
                        );";
                    command.CommandText = createHourlyStats;
                    command.ExecuteNonQuery();

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


        public string GetSetting(string key, string defaultValue = "")
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqliteCommand("SELECT Value FROM Settings WHERE Key = @key", connection))
                {
                    command.Parameters.AddWithValue("@key", key);
                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return result.ToString();
                    }
                }
            }
            return defaultValue;
        }

        public void SaveSetting(string key, string value)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                    INSERT INTO Settings (Key, Value) VALUES (@key, @value)
                    ON CONFLICT(Key) DO UPDATE SET Value = @value";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@key", key);
                    command.Parameters.AddWithValue("@value", value);
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

        public void UpdateHourlyStats(DateTime timestamp, string processName, string category, double durationSeconds)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string dateStr = timestamp.ToString("yyyy-MM-dd");
                int hour = timestamp.Hour;

                string upsert = @"
                    INSERT INTO HourlyStats (Date, Hour, ProcessName, Category, TotalSeconds)
                    VALUES (@date, @hour, @process, @category, @duration)
                    ON CONFLICT(Date, Hour, ProcessName) 
                    DO UPDATE SET TotalSeconds = TotalSeconds + @duration;";

                using (var command = new SqliteCommand(upsert, connection))
                {
                    command.Parameters.AddWithValue("@date", dateStr);
                    command.Parameters.AddWithValue("@hour", hour);
                    command.Parameters.AddWithValue("@process", _encryptionService.Encrypt(processName));
                    command.Parameters.AddWithValue("@category", _encryptionService.Encrypt(category));
                    command.Parameters.AddWithValue("@duration", durationSeconds);
                    command.ExecuteNonQuery();
                }
            }
        }

        public Dictionary<int, double> GetHourlyUsage(DateTime date)
        {
            var result = new Dictionary<int, double>();
            // Initialize 0-23
            for (int i = 0; i < 24; i++) result[i] = 0;

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT Hour, SUM(TotalSeconds) 
                    FROM HourlyStats 
                    WHERE Date = @date
                    GROUP BY Hour";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int hour = reader.GetInt32(0);
                            double seconds = reader.GetDouble(1);
                            if (result.ContainsKey(hour))
                                result[hour] = seconds; 
                        }
                    }
                }
            }
            return result;
        }

        public Dictionary<DateTime, double> GetDailyUsageRange(DateTime start, DateTime end)
        {
            var result = new Dictionary<DateTime, double>();
            // Pre-fill range
            for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
            {
                result[day] = 0;
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT Date, SUM(TotalSeconds)
                    FROM HourlyStats
                    WHERE Date >= @start AND Date <= @end
                    GROUP BY Date";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd"));
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (DateTime.TryParse(reader.GetString(0), out DateTime date))
                            {
                                result[date] = reader.GetDouble(1);
                            }
                        }
                    }
                }
            }
            return result;
        }

         public Dictionary<string, double> GetTopAppsForRange(DateTime start, DateTime end)
        {
            var usage = new Dictionary<string, double>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT ProcessName, SUM(TotalSeconds)
                    FROM HourlyStats
                    WHERE Date >= @start AND Date <= @end
                    GROUP BY ProcessName";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd"));
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
            }
            return usage;
        }

        public void RebuildHourlyStats(DateTime date)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string dateStr = date.ToString("yyyy-MM-dd");

                // 1. Fetch Activity Log for today
                var activities = new List<(int Hour, string Process, string Category, double Duration)>();
                
                string query = "SELECT Timestamp, ProcessName, Category, DurationSeconds FROM ActivityLog WHERE date(Timestamp) = @date";
                using (var cmd = new SqliteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@date", dateStr);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (DateTime.TryParse(reader.GetString(0), out DateTime ts))
                            {
                                string proc = _encryptionService.Decrypt(reader.GetString(1));
                                string cat = _encryptionService.Decrypt(reader.GetString(2));
                                double dur = reader.GetDouble(3);
                                activities.Add((ts.Hour, proc, cat, dur));
                            }
                        }
                    }
                }

                // 2. Aggregate In-Memory
                var aggregated = new Dictionary<(int Hour, string Process), (string Category, double TotalSeconds)>();
                foreach (var act in activities)
                {
                    var key = (act.Hour, act.Process);
                    if (aggregated.ContainsKey(key))
                    {
                        var existing = aggregated[key];
                        aggregated[key] = (existing.Category, existing.TotalSeconds + act.Duration);
                    }
                    else
                    {
                        aggregated[key] = (act.Category, act.Duration);
                    }
                }

                // 3. Update DB in Transaction
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                         // clear old stats for the day
                        using (var delCmd = new SqliteCommand("DELETE FROM HourlyStats WHERE Date = @date", connection, transaction))
                        {
                            delCmd.Parameters.AddWithValue("@date", dateStr);
                            delCmd.ExecuteNonQuery();
                        }

                        string insert = "INSERT INTO HourlyStats (Date, Hour, ProcessName, Category, TotalSeconds) VALUES (@date, @hour, @proc, @cat, @dur)";
                        foreach (var kvp in aggregated)
                        {
                             using (var insCmd = new SqliteCommand(insert, connection, transaction))
                             {
                                 insCmd.Parameters.AddWithValue("@date", dateStr);
                                 insCmd.Parameters.AddWithValue("@hour", kvp.Key.Hour);
                                 insCmd.Parameters.AddWithValue("@proc", _encryptionService.Encrypt(kvp.Key.Process));
                                 insCmd.Parameters.AddWithValue("@cat", _encryptionService.Encrypt(kvp.Value.Category));
                                 insCmd.Parameters.AddWithValue("@dur", kvp.Value.TotalSeconds);
                                 insCmd.ExecuteNonQuery();
                             }
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw; // Re-throw to handle upstream
                    }
                }
            }
        }

        public List<string> GetAllKnownApps()
        {
            var apps = new List<string>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT DISTINCT ProcessName FROM HourlyStats ORDER BY ProcessName";
                using (var command = new SqliteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try 
                        {
                             string decryptedObj = _encryptionService.Decrypt(reader.GetString(0));
                             if (!string.IsNullOrEmpty(decryptedObj))
                             {
                                 apps.Add(decryptedObj);
                             }
                        }
                        catch { }
                    }
                }
            }
            return apps.Distinct().OrderBy(x => x).ToList();
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
