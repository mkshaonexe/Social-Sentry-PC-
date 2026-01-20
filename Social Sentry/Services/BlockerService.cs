using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace Social_Sentry.Services
{
    public class BlockerService
    {
        private Social_Sentry.Data.DatabaseService? _dbService;

        // Simple rules list for now (Phase 2). Later verify against DB.
        private readonly List<string> _blockedKeywords = new() { "porn", "xxx", "sex" }; 
        private readonly List<string> _blockedUrlSegments = new() { "/reels/", "/shorts/" };
        private readonly List<string> _blockedTitles = new() { "Reels", "Shorts" };
        
        // 1. Add a dictionary to track allowed items and their expiration time
        private readonly Dictionary<string, DateTime> _temporarilyAllowed = new();

        // 2. Helper to generate a unique key for the content
        private string GetContentKey(string processName, string title, string url)
        {
            // Use part of the title/url to identify this specific content
            return $"{processName}_{title}_{url}".GetHashCode().ToString(); 
        }
        
        public void Initialize(Social_Sentry.Data.DatabaseService dbService)
        {
            _dbService = dbService;
            
            // Subscribe to rule changes
            _dbService.OnRulesChanged += LoadRules;
            
            LoadRules();
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
                _blockedUrlSegments.Add("/reels/");
                _blockedUrlSegments.Add("/reel/");  // Facebook uses singular
                _blockedUrlSegments.Add("/shorts/");

                _blockedTitles.Clear();
                _blockedTitles.Add("Reels");
                _blockedTitles.Add("Shorts");

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

        private Views.BlackoutWindow? _currentOverlay;

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

            if (ShouldBlock(title, url))
            {
                Debug.WriteLine($"Blocking content: {title} | {url}");
                // For browser content, we might prefer Overlay or Close Tab.
                // For apps, we might prefer Suspend.
                // For now, let's try Overlay for Reels.
                
                Application.Current.Dispatcher.Invoke(() => 
                {
                    PerformBlockingAction(key);
                });
                return true;
            }
            return false;
        }

        private async void PerformBlockingAction(string key)
        {
            // 1. Navigate Back (Alt + Left Arrow)
            SimulateGoBack();

            // 2. Clear Text (Ctrl + A -> Backspace) to remove keyword if typed
            SimulateTextClear();

            // Wait a moment for the browser to process the input and navigate back
            // BEFORE we steal focus with the overlay.
            await Task.Delay(300);

            // 3. Show Overlay
            ShowOverlay("Restricted Content Detected", () => 
            {
                // ON UNLOCK: Add to allow list for 5 minutes
                _temporarilyAllowed[key] = DateTime.Now.AddMinutes(5);
            });
        }

        private void SimulateTextClear()
        {
            // Simulate CTRL + A (Select All)
            NativeMethods.INPUT[] inputsSelect = new NativeMethods.INPUT[4];

            // Ctrl Down
            inputsSelect[0].type = NativeMethods.INPUT_KEYBOARD;
            inputsSelect[0].U.ki.wVk = NativeMethods.VK_CONTROL;
            
            // A Down
            inputsSelect[1].type = NativeMethods.INPUT_KEYBOARD;
            inputsSelect[1].U.ki.wVk = NativeMethods.VK_A;

            // A Up
            inputsSelect[2].type = NativeMethods.INPUT_KEYBOARD;
            inputsSelect[2].U.ki.wVk = NativeMethods.VK_A;
            inputsSelect[2].U.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            // Ctrl Up
            inputsSelect[3].type = NativeMethods.INPUT_KEYBOARD;
            inputsSelect[3].U.ki.wVk = NativeMethods.VK_CONTROL;
            inputsSelect[3].U.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.SendInput((uint)inputsSelect.Length, inputsSelect, NativeMethods.INPUT.Size);

            // Simulate BACKSPACE (Delete Selection)
            NativeMethods.INPUT[] inputsDelete = new NativeMethods.INPUT[2];

            // Backspace Down
            inputsDelete[0].type = NativeMethods.INPUT_KEYBOARD;
            inputsDelete[0].U.ki.wVk = NativeMethods.VK_BACK;

            // Backspace Up
            inputsDelete[1].type = NativeMethods.INPUT_KEYBOARD;
            inputsDelete[1].U.ki.wVk = NativeMethods.VK_BACK;
            inputsDelete[1].U.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.SendInput((uint)inputsDelete.Length, inputsDelete, NativeMethods.INPUT.Size);
        }

        private void SimulateGoBack()
        {
            // Simulate ALT + LEFT ARROW
            NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[4];

            // Alt Down
            inputs[0].type = NativeMethods.INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = NativeMethods.VK_MENU; 

            // Left Down
            inputs[1].type = NativeMethods.INPUT_KEYBOARD;
            inputs[1].U.ki.wVk = NativeMethods.VK_LEFT; 

            // Left Up
            inputs[2].type = NativeMethods.INPUT_KEYBOARD;
            inputs[2].U.ki.wVk = NativeMethods.VK_LEFT;
            inputs[2].U.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            // Alt Up
            inputs[3].type = NativeMethods.INPUT_KEYBOARD;
            inputs[3].U.ki.wVk = NativeMethods.VK_MENU;
            inputs[3].U.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.SendInput((uint)inputs.Length, inputs, NativeMethods.INPUT.Size);
        }

        private bool ShouldBlock(string title, string url)
        {
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(url)) return false;

            string lowerTitle = title.ToLower();
            string lowerUrl = url.ToLower();

            // 1. Check Keywords (Adult)
            foreach (var keyword in _blockedKeywords)
            {
                if (lowerTitle.Contains(keyword) || lowerUrl.Contains(keyword)) return true;
            }

            // 2. Check Shorts/Reels (URL segments)
            foreach (var segment in _blockedUrlSegments)
            {
                if (lowerUrl.Contains(segment)) return true;
            }

            // 3. Check Title Specifics
            foreach (var t in _blockedTitles)
            {
                 if (lowerTitle.Contains(t.ToLower())) return true;
            }

            return false;
        }

        public void ShowOverlay(string reason, Action? onUnlockSuccess = null)
        {
            if (_currentOverlay == null || !_currentOverlay.IsLoaded)
            {
                _currentOverlay = new Views.BlackoutWindow();
                
                // Pass the callback to the window
                if (onUnlockSuccess != null)
                {
                    _currentOverlay.OnUnlock += onUnlockSuccess;
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
    }
}
