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
            // Always try to seed defaults to ensure new rules (e.g. updates) are applied.
            // SeedDefaultRules handles duplicates internally.
            SeedDefaultRules();
            
            _rules = _databaseService.GetClassificationRules();
        }

        private void SeedDefaultRules()
        {
                // Doom Scrolling (Browser Only guard implemented in Categorize)
                new() { Pattern = "shorts", Category = "Doom Scrolling", MatchType = "Contains", Priority = 100 },
                new() { Pattern = "reels", Category = "Doom Scrolling", MatchType = "Contains", Priority = 100 },
                new() { Pattern = "tiktok", Category = "Doom Scrolling", MatchType = "Contains", Priority = 100 },
                new() { Pattern = "facebook watch", Category = "Doom Scrolling", MatchType = "Contains", Priority = 100 },

                // Study Category (Priority: 80-90)
                // Bangla Keywords (Transliterated & Script)
                new() { Pattern = "porashona", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "gonit", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "biggan", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "itihaas", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "bhugol", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "sahitya", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "boi", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "patho", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "shikkha", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "odhyayon", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "gogeshona", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "poriksha", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "onushiloni", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "somadhan", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "proshno", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "uttor", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "bisshobidyalay", Category = "Study", MatchType = "Contains", Priority = 90 },
                // Bangla Script
                new() { Pattern = "পড়াশোনা", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "অধ্যয়ন", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "ক্লাস", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "গবেষণা", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "গণিত", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "বিজ্ঞান", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "ইতিহাস", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "ভূগোল", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "সাহিত্য", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "অ্যাসাইনমেন্ট", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "পরীক্ষা", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "ফলাফল", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "রুটিন", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "সিলেবাস", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "বই", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "পাঠ", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "শিক্ষা", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "বিশ্ববিদ্যালয়", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "কলেজ", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "স্কুল", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "অনুশীলনী", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "সমাধান", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "প্রশ্ন", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "উত্তর", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "থিসিস", Category = "Study", MatchType = "Contains", Priority = 90 },

                // English Study Keywords
                new() { Pattern = "study", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "lecture", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "tutorial", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "course", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "assignment", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "thesis", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "research", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "syllabus", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "exam", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "quiz", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "test", Category = "Study", MatchType = "Contains", Priority = 90 },
                new() { Pattern = "math", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "physics", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "chemistry", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "biology", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "literature", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "economics", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "computer science", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "programming", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "algebra", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "calculus", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "geometry", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "university", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "classroom", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "textbook", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "slides", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "presentation", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "notes", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "coursera", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "udemy", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "edx", Category = "Study", MatchType = "Contains", Priority = 80 },
                new() { Pattern = "khan academy", Category = "Study", MatchType = "Contains", Priority = 80 },

                // Productivity (Modern AI & IDEs)
                new() { Pattern = "visual studio", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "code", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "cursor", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "windsurf", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "antigravity", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "android studio", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "xcode", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "intellij", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "pycharm", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "webstorm", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "sublime text", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "vim", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "unity", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "unreal engine", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "godot", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "figma", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "blender", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "photoshop", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "illustrator", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "autocad", Category = "Productive", MatchType = "Contains", Priority = 20 },
                
                // AI Tools
                new() { Pattern = "chatgpt", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "claude", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "gemini", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "copilot", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "midjourney", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "perplexity", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "github", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "stackoverflow", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "openai", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "anthropic", Category = "Productive", MatchType = "Contains", Priority = 20 },
                new() { Pattern = "hugging face", Category = "Productive", MatchType = "Contains", Priority = 20 },

                // Standard Office
                new() { Pattern = "word", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "excel", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "powerpoint", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "notion", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "obsidian", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "trello", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "jira", Category = "Productive", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "slack", Category = "Productive", MatchType = "Contains", Priority = 10 },

                // Communication
                new() { Pattern = "discord", Category = "Communication", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "whatsapp", Category = "Communication", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "telegram", Category = "Communication", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "messenger", Category = "Communication", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "skype", Category = "Communication", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "zoom", Category = "Communication", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "teams", Category = "Communication", MatchType = "Contains", Priority = 15 },
                new() { Pattern = "outlook", Category = "Communication", MatchType = "Contains", Priority = 15 },

                // Entertainment (Social & Gaming)
                new() { Pattern = "youtube", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "netflix", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "prime video", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "disney+", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "hulu", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "twitch", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "spotify", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                // Social
                new() { Pattern = "facebook", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "instagram", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "reddit", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "twitter", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "pinterest", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                // Gaming
                new() { Pattern = "steam", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "epic games", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "battle.net", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "origin", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "uplay", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "valorant", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "league of legends", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "minecraft", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "tlauncher", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "roblox", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "fortnite", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "csgo", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "call of duty", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "overwatch", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "apex legends", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "genshin", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "honkai", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "cyberpunk", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
                new() { Pattern = "elden ring", Category = "Entertainment", MatchType = "Contains", Priority = 10 },
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

        public bool IsBrowserProcess(string processName)
        {
            var browsers = new[] 
            {
                "chrome", "msedge", "firefox", "brave", "opera", "vivaldi", "arc", "safari", "iexplore"
            };
            return browsers.Contains(processName.ToLower());
        }

        public string Categorize(string processName, string windowTitle)
        {
            string input = (processName + " " + windowTitle).ToLower();
            string pName = processName.ToLower();

            // Store high priority match
            string bestCategory = "Uncategorized";
            int highestPriority = -1;

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
                    // Browser Guard Logic:
                    // If the rule is for Doom Scrolling (Shorts/Reels), 
                    // ONLY apply if it is a Browser.
                    if (rule.Category == "Doom Scrolling")
                    {
                        // Check if it's a browser
                        if (!IsBrowserProcess(pName))
                        {
                            // It's NOT a browser (e.g. VS Code, Word)
                            // IGNORE this match. 
                            continue;
                        }
                    }

                    // Check Priority
                    // The standard logic was "return first match", but DB order might not be priority sorted.
                    // We should respecting Priority field.
                    if (rule.Priority > highestPriority)
                    {
                        highestPriority = rule.Priority;
                        bestCategory = rule.Category;
                    }
                }
            }

            return bestCategory;
        }
    }
}
