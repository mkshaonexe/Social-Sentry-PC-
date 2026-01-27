using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace Social_Sentry.Services
{
    public class BlockerService
    {
        private Social_Sentry.Data.DatabaseService? _dbService;
        private bool _isReelsBlockerEnabled;
        private bool _isAdultBlockerEnabled;

        // Simple rules list for now (Phase 2). Later verify against DB.
        private readonly List<string> _blockedKeywords = new() { "porn", "xxx", "sex" }; 
        private readonly List<string> _blockedUrlSegments = new() { "youtube.com/shorts", "facebook.com/reel" };
        private readonly List<string> _blockedTitles = new(); 
        
        // 1. Add a dictionary to track allowed items and their expiration time
        private readonly Dictionary<string, DateTime> _temporarilyAllowed = new();

        // 2. Helper to generate a unique key for the content
        private string GetContentKey(string processName, string title, string url)
        {
            // Use part of the title/url to identify this specific content
            return $"{processName}_{title}_{url}".GetHashCode().ToString(); 
        }
        
        private readonly HashSet<string> _permanentBlockedDomains = new(); // 3. Permanent blocklist
        
        public void Initialize(Social_Sentry.Data.DatabaseService dbService)
        {
            _dbService = dbService;
            
            // Subscribe to rule changes
            _dbService.OnRulesChanged += LoadRules;
            
            // Subscribe to settings changes
            SettingsService.SettingsChanged += UpdateSettings;

            // Load initial settings
            var settingsService = new SettingsService();
            UpdateSettings(settingsService.LoadSettings());

            LoadRules();
            LoadBlocklistsAsync(); // Load large lists asynchronously
        }

        private async void LoadBlocklistsAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    var appDir = AppDomain.CurrentDomain.BaseDirectory;
                    // Adjust path if needed, assuming files are in root or copied to output
                    // Based on user context, files are at root "e:\Cursor Play ground\Social-Sentry-PC-\"
                    // In debug/release they might need to be copied or read from project root relative
                    
                    // Trying to find the files by walking up if not in current
                    string rootPath = FindProjectRoot(appDir);
                    
                    LoadFileIfExists(Path.Combine(rootPath, "adult_blocklist.txt"), false);
                    LoadFileIfExists(Path.Combine(rootPath, "adult_hosts.txt"), true);
                    
                    Debug.WriteLine($"BlockerService: Loaded {_permanentBlockedDomains.Count} domains into permanent blocklist.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"BlockerService: Error loading blocklists: {ex.Message}");
                }
            });
        }

        private string FindProjectRoot(string startPath)
        {
             // Simple heuristic: walk up until we find the .sln or specific txt files
             // Or just hardcode for this specific user environment if robust relative path fails?
             // Let's try standard walk up.
             DirectoryInfo? dir = new DirectoryInfo(startPath);
             while (dir != null)
             {
                 if (File.Exists(Path.Combine(dir.FullName, "Social Sentry.sln"))) return dir.FullName;
                 dir = dir.Parent;
             }
             return startPath; // Fallback
        }

        private void LoadFileIfExists(string path, bool isHostsFormat)
        {
            if (!File.Exists(path)) return;

            foreach (var line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                var trimmed = line.Trim();
                string? domain = null;

                if (isHostsFormat)
                {
                    // Format: 0.0.0.0 domain.com
                    var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        domain = parts[1].ToLower();
                    }
                    else if (parts.Length == 1 && !char.IsDigit(parts[0][0]))
                    {
                        // Fallback if just domain
                        domain = parts[0].ToLower();
                    }
                }
                else
                {
                    // Format: domain.com or 0.0.0.0 domain.com? 
                    // User showed: 0.0.0.0 0.0.0.0 in blocklist too?
                    // Let's handle both just in case, similar parsing
                    var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    // Usually raw blocklists are just domains, but user showed "0.0.0.0 0.0.0.0" type garbage or "0.0.0.0 domain"
                    // If it starts with 0.0.0.0 or 127.0.0.1, take the second part
                    if (parts.Length >= 2 && (parts[0] == "0.0.0.0" || parts[0] == "127.0.0.1"))
                    {
                         domain = parts[1].ToLower();
                    }
                    else 
                    {
                        domain = parts[0].ToLower();
                    }
                }

                if (!string.IsNullOrEmpty(domain) && domain != "0.0.0.0" && domain != "localhost")
                {
                    _permanentBlockedDomains.Add(domain);
                }
            }
        }

        private void UpdateSettings(UserSettings settings)
        {
            _isReelsBlockerEnabled = settings.IsReelsBlockerEnabled;
            _isAdultBlockerEnabled = settings.IsAdultBlockerEnabled;
            Debug.WriteLine($"BlockerService: Settings Updated - Reels: {_isReelsBlockerEnabled}, Adult: {_isAdultBlockerEnabled}");
        }

        private void LoadRules()
        {
            if (_dbService == null) return;
            
            try
            {
                var rules = _dbService.GetRules();
                
                // Clear existing (except hardcoded fallbacks if we want to keep them, 
                // but Phase 3 goal is dynamic. For safety, let's KEEP hardcoded as "Base Rules" 
                // and ADD DB rules).
                
                _blockedKeywords.Clear();
                _blockedKeywords.Add("porn");
                _blockedKeywords.Add("xxx");
                _blockedKeywords.Add("sex");
                
                _blockedUrlSegments.Clear();
                _blockedUrlSegments.Add("youtube.com/shorts");
                _blockedUrlSegments.Add("facebook.com/reel"); 
                _blockedUrlSegments.Add("instagram.com/reels");

                _blockedTitles.Clear();

                foreach (var rule in rules)
                {
                    if (rule.Action == "Block")
                    {
                        // Map rule types
                        // Types: 'App', 'Url', 'Title' ... 
                        // But _blockedKeywords iterates on Title AND Url checks.
                        
                        // If Type == 'Url', add to _blockedUrlSegments or Keyword?
                        // Our ShouldBlock logic is:
                        // 1. Keywords (in Title OR Url)
                        // 2. Url Segments (in Url)
                        // 3. Titles (in Title)
                        
                        if (rule.Type == "Url")
                        {
                             // If it's a segment e.g. "/reels/", add to segments.
                             // If it's a domain e.g. "facebook.com", add to segments.
                             _blockedUrlSegments.Add(rule.Value.ToLower());
                        }
                        else if (rule.Type == "Title")
                        {
                            _blockedTitles.Add(rule.Value);
                        }
                        else if (rule.Type == "Keyword") // If we support this
                        {
                            _blockedKeywords.Add(rule.Value.ToLower());
                        }
                    }
                }
                
                Debug.WriteLine($"BlockerService: Loaded {rules.Count} rules from DB.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BlockerService: Error loading rules: {ex.Message}");
            }
        }

        private Window? _currentOverlay;

        public enum BlockReason { None, Reels, Adult }

        public bool CheckAndBlock(string processName, string title, string url, int processId)
        {
            string key = GetContentKey(processName, title, url);

            // CLEANUP: Remove expired entries first
            var expired = _temporarilyAllowed.Where(x => x.Value < DateTime.Now).Select(x => x.Key).ToList();
            foreach (var k in expired) _temporarilyAllowed.Remove(k);

            // CHECK: Is this specific content allowed right now?
            if (_temporarilyAllowed.ContainsKey(key))
            {
                return false; // Skip blocking
            }

            var reason = ShouldBlock(title, url);
            if (reason != BlockReason.None)
            {
                Debug.WriteLine($"Blocking content: {title} | {url} | Reason: {reason}");
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    PerformBlockingAction(key, reason);
                });
                return true;
            }
            return false;
        }

        private async void PerformBlockingAction(string key, BlockReason reason)
        {
            // 1. Navigate Back (Alt + Left Arrow)
            SimulateGoBack();

            // 2. Clear Text (Ctrl + A -> Backspace) to remove keyword if typed
            SimulateTextClear();

            // Wait a moment for the browser to process the input and navigate back
            // BEFORE we steal focus with the overlay.
            await Task.Delay(300);

            // 3. Show Overlay
            ShowOverlay(reason);
        }

        private BlockReason ShouldBlock(string title, string url)
        {
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(url)) return BlockReason.None;

            string lowerTitle = title.ToLower();
            string lowerUrl = url.ToLower();

            // 0. Check Permanent Blocklist (File-based) - ACTIVE ALWAYS
            // Extract domain from URL roughly
            if (!string.IsNullOrEmpty(lowerUrl))
            {
                // Simple domain extraction
                try 
                {
                    // Handle bare domains or full urls
                    string domainToCheck = lowerUrl;
                    if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                    {
                        domainToCheck = uri.Host;
                    }
                    else if (lowerUrl.Contains("/"))
                    {
                         domainToCheck = lowerUrl.Split('/')[0];
                    }

                    if (_permanentBlockedDomains.Contains(domainToCheck))
                    {
                        return BlockReason.Adult;
                    }
                    
                    // Also check partial string matches against domain list? 
                    // No, that might be too slow for 150k items. HashSet lookup is O(1).
                    // But we might want to check if the domain ENDS with a blocked domain? e.g. sub.porn.com
                    // For performance, let's stick to exact or host match for now.
                }
                catch {}
            }

            // 1. Check Keywords (Adult) - TOGGLE DEPENDENT
            if (_isAdultBlockerEnabled)
            {
                foreach (var keyword in _blockedKeywords)
                {
                    if (lowerTitle.Contains(keyword) || lowerUrl.Contains(keyword)) return BlockReason.Adult;
                }
            }

            // 2. Check Shorts/Reels (URL segments)
            if (_isReelsBlockerEnabled)
            {
                foreach (var segment in _blockedUrlSegments)
                {
                    if (lowerUrl.Contains(segment)) return BlockReason.Reels;
                }
            }

            // 3. Check Title Specifics - TOGGLE DEPENDENT
            if (_isAdultBlockerEnabled)
            {
                foreach (var t in _blockedTitles)
                {
                     if (lowerTitle.Contains(t.ToLower())) return BlockReason.Adult;
                }
            }

            return BlockReason.None;
        }

        public void ShowOverlay(BlockReason reason)
        {
            if (_currentOverlay == null || !_currentOverlay.IsLoaded)
            {
                if (reason == BlockReason.Reels)
                {
                    _currentOverlay = new Views.ReelsBlockerOverlay();
                }
                else
                {
                    _currentOverlay = new Views.AdultBlockerOverlay();
                }

                _currentOverlay.Closed += (s, e) => _currentOverlay = null;
                _currentOverlay.Show();
            }
            
            // Force reset to top if already open
            _currentOverlay.Activate();
            _currentOverlay.Topmost = true;
        }

        public void SuspendProcess(int processId)
        {
            try
            {
                IntPtr hProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_SUSPEND_RESUME, false, processId);
                if (hProcess != IntPtr.Zero)
                {
                    NativeMethods.NtSuspendProcess(hProcess);
                    NativeMethods.CloseHandle(hProcess); // Need CloseHandle in NativeMethods? (It's standard kernel32, but check if defined)
                    // If CloseHandle is missing, we might leak handles. 
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to suspend process: {ex.Message}");
            }
        }

        // Keep legacy close method for now
        private void CloseCurrentTab()
        {
            // Simulate CTRL + W
            NativeMethods.INPUT[] inputsDown = new NativeMethods.INPUT[2];
            
            inputsDown[0].type = NativeMethods.INPUT_KEYBOARD;
            inputsDown[0].U.ki.wVk = NativeMethods.VK_CONTROL;
            inputsDown[0].U.ki.dwFlags = 0; 

            inputsDown[1].type = NativeMethods.INPUT_KEYBOARD;
            inputsDown[1].U.ki.wVk = NativeMethods.VK_W;
            inputsDown[1].U.ki.dwFlags = 0; 

            NativeMethods.SendInput((uint)inputsDown.Length, inputsDown, NativeMethods.INPUT.Size);

            // Release
            NativeMethods.INPUT[] inputsUp = new NativeMethods.INPUT[2];

            inputsUp[0].type = NativeMethods.INPUT_KEYBOARD;
            inputsUp[0].U.ki.wVk = NativeMethods.VK_W;
            inputsUp[0].U.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            inputsUp[1].type = NativeMethods.INPUT_KEYBOARD;
            inputsUp[1].U.ki.wVk = NativeMethods.VK_CONTROL;
            inputsUp[1].U.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.SendInput((uint)inputsUp.Length, inputsUp, NativeMethods.INPUT.Size);
        }

        private void SimulateGoBack()
        {
            // Simulate Alt + Left Arrow
            NativeMethods.INPUT[] inputsDown = new NativeMethods.INPUT[2];

            inputsDown[0].type = NativeMethods.INPUT_KEYBOARD;
            inputsDown[0].U.ki.wVk = NativeMethods.VK_MENU; // VK_MENU is Alt
            inputsDown[0].U.ki.dwFlags = 0;

            inputsDown[1].type = NativeMethods.INPUT_KEYBOARD;
            inputsDown[1].U.ki.wVk = NativeMethods.VK_LEFT;
            inputsDown[1].U.ki.dwFlags = 0;

            NativeMethods.SendInput((uint)inputsDown.Length, inputsDown, NativeMethods.INPUT.Size);

            // Release
            NativeMethods.INPUT[] inputsUp = new NativeMethods.INPUT[2];

            inputsUp[0].type = NativeMethods.INPUT_KEYBOARD;
            inputsUp[0].U.ki.wVk = NativeMethods.VK_LEFT;
            inputsUp[0].U.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            inputsUp[1].type = NativeMethods.INPUT_KEYBOARD;
            inputsUp[1].U.ki.wVk = NativeMethods.VK_MENU;
            inputsUp[1].U.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.SendInput((uint)inputsUp.Length, inputsUp, NativeMethods.INPUT.Size);
        }

        private void SimulateTextClear()
        {
            // 1. Select All (Ctrl + A)
            NativeMethods.INPUT[] selectAllDown = new NativeMethods.INPUT[2];
            selectAllDown[0].type = NativeMethods.INPUT_KEYBOARD;
            selectAllDown[0].U.ki.wVk = NativeMethods.VK_CONTROL;
            selectAllDown[0].U.ki.dwFlags = 0;

            selectAllDown[1].type = NativeMethods.INPUT_KEYBOARD;
            selectAllDown[1].U.ki.wVk = NativeMethods.VK_A;
            selectAllDown[1].U.ki.dwFlags = 0;

            NativeMethods.SendInput((uint)selectAllDown.Length, selectAllDown, NativeMethods.INPUT.Size);

            NativeMethods.INPUT[] selectAllUp = new NativeMethods.INPUT[2];
            selectAllUp[0].type = NativeMethods.INPUT_KEYBOARD;
            selectAllUp[0].U.ki.wVk = NativeMethods.VK_A;
            selectAllUp[0].U.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            selectAllUp[1].type = NativeMethods.INPUT_KEYBOARD;
            selectAllUp[1].U.ki.wVk = NativeMethods.VK_CONTROL;
            selectAllUp[1].U.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.SendInput((uint)selectAllUp.Length, selectAllUp, NativeMethods.INPUT.Size);

            // 2. Press Backspace
            NativeMethods.INPUT[] backspace = new NativeMethods.INPUT[2];
            backspace[0].type = NativeMethods.INPUT_KEYBOARD;
            backspace[0].U.ki.wVk = NativeMethods.VK_BACK;
            backspace[0].U.ki.dwFlags = 0;

            backspace[1].type = NativeMethods.INPUT_KEYBOARD;
            backspace[1].U.ki.wVk = NativeMethods.VK_BACK;
            backspace[1].U.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.SendInput((uint)backspace.Length, backspace, NativeMethods.INPUT.Size);
        }
    }
}
