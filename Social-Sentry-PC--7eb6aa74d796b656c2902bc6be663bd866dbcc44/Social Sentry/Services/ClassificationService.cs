using Social_Sentry.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Social_Sentry.Services
{
    public class ClassificationService
    {
        private readonly DatabaseService _databaseService;
        private List<DatabaseService.ClassificationRule> _rules;

        public ClassificationService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            LoadRules();
        }

        public void LoadRules()
        {
            _rules = _databaseService.GetClassificationRules();
            // Fallback defaults if empty (Seeding)
            if (_rules.Count == 0)
            {
                SeedDefaultRules();
                _rules = _databaseService.GetClassificationRules();
            }
        }

        private void SeedDefaultRules()
        {
            var defaults = new List<DatabaseService.ClassificationRule>
            {
                new() { Pattern = "youtube", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "netflix", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "steam", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "game", Category = "Entertainment", MatchType = "Contains", Priority = 5 },
                new() { Pattern = "visual studio", Category = "Productive", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "code", Category = "Productive", MatchType = "Contains", Priority = 8 },
                new() { Pattern = "word", Category = "Productive", MatchType = "Contains", Priority = 8 },
                new() { Pattern = "excel", Category = "Productive", MatchType = "Contains", Priority = 8 },
                new() { Pattern = "discord", Category = "Communication", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "slack", Category = "Communication", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "whatsapp", Category = "Communication", MatchType = "Contains", Priority = 10 },
                // Generic Media Context Rule
                new() { Pattern = "(Media)", Category = "Entertainment", MatchType = "Contains", Priority = 100 }
            };

            foreach (var rule in defaults)
            {
                _databaseService.AddClassificationRule(rule);
            }
        }

        public string Categorize(string processName, string windowTitle)
        {
            string input = (processName + " " + windowTitle).ToLower();

            foreach (var rule in _rules)
            {
                bool match = false;
                switch (rule.MatchType)
                {
                    case "Contains":
                        match = input.Contains(rule.Pattern.ToLower());
                        break;
                    case "Exact":
                        match = input == rule.Pattern.ToLower();
                        break;
                    case "Regex":
                        try { match = Regex.IsMatch(input, rule.Pattern, RegexOptions.IgnoreCase); } catch { }
                        break;
                }

                if (match)
                {
                    return rule.Category;
                }
            }

            return "Uncategorized";
        }
    }
}
