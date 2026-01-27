using System;
using System.Diagnostics;
using System.Windows.Automation; // Requires 'UIAutomationClient' and 'UIAutomationTypes' references
using System.Linq;
using System.Collections.Generic;

namespace Social_Sentry.Services
{
    public class BrowserMonitor
    {
        // Monitors Chrome, Edge, and potentially Firefox
        // For WPF/Net Core, we need to add a reference to UIAutomationClient and UIAutomationTypes assemblies.

        // Cache for automation elements: (ProcessId, WindowHandle) -> AddressBarElement
        private Dictionary<(int ProcessId, IntPtr WindowHandle), AutomationElement> _elementCache = new Dictionary<(int, IntPtr), AutomationElement>();
        
        public string GetCurrentUrl(IntPtr hWnd)
        {
            try
            {
                if (hWnd == IntPtr.Zero) return string.Empty;

                NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
                var cacheKey = ((int)processId, hWnd);

                // Try to use cached element first
                if (_elementCache.TryGetValue(cacheKey, out AutomationElement? cachedElement))
                {
                    try 
                    {
                        // Check if valid and get value
                         return GetUrlFromElement(cachedElement);
                    }
                    catch (ElementNotAvailableException)
                    {
                        // Element is dead, remove from cache and retry full search
                        _elementCache.Remove(cacheKey);
                    }
                    catch (Exception) 
                    {
                        // Other errors, remove and retry
                         _elementCache.Remove(cacheKey);
                    }
                }

                // Cold lookup
                AutomationElement element = AutomationElement.FromHandle(hWnd);
                if (element == null) return string.Empty;

                // Basic strategy for Chrome/Edge: Look for the Edit control that contains the URL.
                Condition condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit);
                AutomationElementCollection edits = element.FindAll(TreeScope.Descendants, condition);

                foreach (AutomationElement edit in edits)
                {
                    string name = edit.Current.Name;
                    if (name.Contains("Address and search bar", StringComparison.OrdinalIgnoreCase) || 
                        name.Contains("Address bar", StringComparison.OrdinalIgnoreCase)) // Firefox often uses "Search with Google or enter address" or similar
                    {
                        // Verify it has value pattern
                        if (edit.TryGetCurrentPattern(ValuePattern.Pattern, out object pattern))
                        {
                            // Cache this specific element
                            _elementCache[cacheKey] = edit;
                            return ((ValuePattern)pattern).Current.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // UIAutomation can be noisy with exceptions if windows close mid-operation
                Debug.WriteLine($"Browser Monitor Error: {ex.Message}");
            }

            return string.Empty;
        }

        private string GetUrlFromElement(AutomationElement element)
        {
             if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object pattern))
             {
                 return ((ValuePattern)pattern).Current.Value;
             }
             return string.Empty;
        }

        public bool IsBrowser(string processName)
        {
            string[] browsers = { 
                "chrome", 
                "msedge", 
                "firefox", 
                "brave",
                "opera",
                "opera_gx", 
                "vivaldi",
                "chromium",
                "waterfox",
                "palemoon",
                "librewolf",
                "thorium",
                "yandex", 
                "browser", // Generic name often used by Yandex or others
                "arc",
                "duckduckgo",
                "maxthon",
                "qqbrowser",
                "sogouexplorer",
                "ucbrowser",
                "360se", 
                "360chrome"
            };
            return browsers.Contains(processName.ToLower());
        }
    }

}
