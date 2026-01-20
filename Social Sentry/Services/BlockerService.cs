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
        
        public void Initialize(Social_Sentry.Data.DatabaseService dbService)
        {
            _dbService = dbService;
            LoadRules();
        }

        private void LoadRules()
        {
            // TODO: Fetch from _dbService.GetRules()
            // _blockedKeywords.AddRange(...);
        }

        private Views.BlackoutWindow? _currentOverlay;

        public bool CheckAndBlock(string processName, string title, string url, int processId)
        {
            if (ShouldBlock(title, url))
            {
                Debug.WriteLine($"Blocking content: {title} | {url}");
                // For browser content, we might prefer Overlay or Close Tab.
                // For apps, we might prefer Suspend.
                // For now, let's try Overlay for Reels.
                
                Application.Current.Dispatcher.Invoke(() => 
                {
                    ShowOverlay("Restricted Content Detected");
                });
                
                // Also optionally key-inject to pause/stop?
                return true;
            }
            return false;
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

        public void ShowOverlay(string reason)
        {
            if (_currentOverlay == null || !_currentOverlay.IsLoaded)
            {
                _currentOverlay = new Views.BlackoutWindow();
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
