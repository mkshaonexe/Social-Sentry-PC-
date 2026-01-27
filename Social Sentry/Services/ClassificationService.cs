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
                // Doom Scrolling
                new() { Pattern = "shorts", Category = "Doom Scrolling", MatchType = "Contains", Priority = 100 },
                new() { Pattern = "reels", Category = "Doom Scrolling", MatchType = "Contains", Priority = 100 },
                new() { Pattern = "tiktok", Category = "Doom Scrolling", MatchType = "Contains", Priority = 100 },
                
                // Study Context (High Priority)
                new() { Pattern = "study", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "lecture", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "tutorial", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "course", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "assignment", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "thesis", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "research", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "math", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "physics", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "chemistry", Category = "Study", MatchType = "Contains", Priority = 80 },

                // Productivity
                new() { Pattern = "visual studio", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "code", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "word", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "excel", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "powerpoint", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "notion", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "obsidian", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "trello", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "jira", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "slack", Category = "Productive", MatchType = "Contains", Priority = 10 }, // Can be communication too, context dependent potentially

                // Communication
                new() { Pattern = "discord", Category = "Communication", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "whatsapp", Category = "Communication", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "telegram", Category = "Communication", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "messenger", Category = "Communication", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "skype", Category = "Communication", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "zoom", Category = "Communication", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "teams", Category = "Communication", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "outlook", Category = "Communication", MatchType = "Contains", Priority = 15 },

                // Entertainment
                new() { Pattern = "youtube", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "netflix", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "steam", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "game", Category = "Entertainment", MatchType = "Contains", Priority = 5 },

                // Generic Media Context Rule
                new() { Pattern = "(Media)", Category = "Entertainment", MatchType = "Contains", Priority = 100 }
            };

            foreach (var rule in defaults)
            {
                // Only add if not exists (simple check, or re-seed logic)
                // For simplicity here, we assume DB handles duplicates or we just add
                // In a real app we might want to check for existing rules first
                try { _databaseService.AddClassificationRule(rule); } catch {}
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
