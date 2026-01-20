using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Social_Sentry.Services
{
    public class BlockerService
    {
        // Simple rules list for now (Phase 2). Later verify against DB.
        private readonly string[] _blockedKeywords = { "porn", "xxx", "sex" }; 
        private readonly string[] _blockedUrlSegments = { "/reels/", "/shorts/" };
        private readonly string[] _blockedTitles = { "Reels", "Shorts" };

        public bool CheckAndBlock(string processName, string title, string url)
        {
            if (ShouldBlock(title, url))
            {
                Debug.WriteLine($"Blocking content: {title} | {url}");
                BlockContent();
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
            // Only if "Instagram" or "Facebook" or "YouTube" is also in title/url to avoid false positives?
            // For now, strict block on "Reels".
            foreach (var t in _blockedTitles)
            {
                 if (lowerTitle.Contains(t.ToLower())) return true;
            }

            return false;
        }

        private void BlockContent()
        {
            // Simulate CTRL + W (Close Tab)
            // This assumes the browser window is still active, which it should be.
            
            // Press Ctrl
            NativeMethods.INPUT[] inputsDown = new NativeMethods.INPUT[2];
            
            inputsDown[0].type = NativeMethods.INPUT_KEYBOARD;
            inputsDown[0].U.ki.wVk = NativeMethods.VK_CONTROL;
            inputsDown[0].U.ki.dwFlags = 0; // KeyDown

            // Press W
            inputsDown[1].type = NativeMethods.INPUT_KEYBOARD;
            inputsDown[1].U.ki.wVk = NativeMethods.VK_W;
            inputsDown[1].U.ki.dwFlags = 0; // KeyDown

            NativeMethods.SendInput((uint)inputsDown.Length, inputsDown, NativeMethods.INPUT.Size);

            // Release Keys (W then Ctrl)
            NativeMethods.INPUT[] inputsUp = new NativeMethods.INPUT[2];

            inputsUp[0].type = NativeMethods.INPUT_KEYBOARD;
            inputsUp[0].U.ki.wVk = NativeMethods.VK_W;
            inputsUp[0].U.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            inputsUp[1].type = NativeMethods.INPUT_KEYBOARD;
            inputsUp[1].U.ki.wVk = NativeMethods.VK_CONTROL;
            inputsUp[1].U.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.SendInput((uint)inputsUp.Length, inputsUp, NativeMethods.INPUT.Size);
            
            // Optional: Send a notification or sound?
            // System.Media.SystemSounds.Hand.Play();
        }
    }
}
