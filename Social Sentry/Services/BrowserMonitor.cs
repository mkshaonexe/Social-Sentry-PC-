using System;
using System.Diagnostics;
using System.Windows.Automation; // Requires 'UIAutomationClient' and 'UIAutomationTypes' references
using System.Linq;

namespace Social_Sentry.Services
{
    public class BrowserMonitor
    {
        // Monitors Chrome, Edge, and potentially Firefox
        // For WPF/Net Core, we need to add a reference to UIAutomationClient and UIAutomationTypes assemblies.
        
        public string GetCurrentUrl(IntPtr hWnd)
        {
            try
            {
                // This is a heavy operation, so we should only call it if the process is a known browser.
                if (hWnd == IntPtr.Zero) return string.Empty;

                AutomationElement element = AutomationElement.FromHandle(hWnd);
                if (element == null) return string.Empty;

                // Basic strategy for Chrome/Edge: Look for the Edit control that contains the URL.
                // This is a simplified search. A robust one traverses the tree more carefully or checks specific AutomationIDs.
                
                // Chrome/Edge Address Bar often has Name="Address and search bar"
                Condition condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit);
                AutomationElementCollection edits = element.FindAll(TreeScope.Descendants, condition);

                foreach (AutomationElement edit in edits)
                {
                    string name = edit.Current.Name;
                    if (name.Contains("Address and search bar", StringComparison.OrdinalIgnoreCase) || 
                        name.Contains("Address bar", StringComparison.OrdinalIgnoreCase)) // Firefox often uses "Search with Google or enter address" or similar
                    {
                        // Get the Value pattern
                        if (edit.TryGetCurrentPattern(ValuePattern.Pattern, out object pattern))
                        {
                            return ((ValuePattern)pattern).Current.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Browser Monitor Error: {ex.Message}");
            }

            return string.Empty;
        }

        public bool IsBrowser(string processName)
        {
            string[] browsers = { "chrome", "msedge", "firefox", "brave" };
            return browsers.Contains(processName.ToLower());
        }
    }
}
