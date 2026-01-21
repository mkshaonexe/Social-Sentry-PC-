using System;
using System.Diagnostics;
using System.IO;

namespace Social_Sentry.Services
{
    public class ExtensionService
    {
        private readonly string _extensionSourcePath;
        private readonly string _extensionInstallPath;

        public ExtensionService()
        {
            // Source: extension folder in app directory
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            _extensionSourcePath = Path.Combine(appDir, "extension");

            // Install location: AppData folder
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _extensionInstallPath = Path.Combine(appDataPath, "SocialSentry", "Extension");
        }

        public string ExtensionPath => _extensionInstallPath;

        public bool IsExtensionInstalled()
        {
            return Directory.Exists(_extensionInstallPath) && 
                   File.Exists(Path.Combine(_extensionInstallPath, "manifest.json"));
        }

        public bool CopyExtensionFiles()
        {
            try
            {
                // Create install directory
                Directory.CreateDirectory(_extensionInstallPath);

                // Copy all extension files
                if (Directory.Exists(_extensionSourcePath))
                {
                    CopyDirectory(_extensionSourcePath, _extensionInstallPath);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to copy extension: {ex.Message}");
                return false;
            }
        }

        private void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (var file in Directory.GetFiles(source))
            {
                var destFile = Path.Combine(destination, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(source))
            {
                var destDir = Path.Combine(destination, Path.GetFileName(dir));
                CopyDirectory(dir, destDir);
            }
        }

        public void OpenBrowserWithInstructions(string browser = "chrome")
        {
            // Copy extension first
            CopyExtensionFiles();

            string extensionsUrl = browser.ToLower() switch
            {
                "chrome" => "chrome://extensions",
                "edge" => "edge://extensions",
                "brave" => "brave://extensions",
                _ => "chrome://extensions"
            };

            string browserExe = browser.ToLower() switch
            {
                "chrome" => "chrome.exe",
                "edge" => "msedge.exe",
                "brave" => "brave.exe",
                _ => "chrome.exe"
            };

            try
            {
                // Open browser extensions page
                Process.Start(new ProcessStartInfo
                {
                    FileName = browserExe,
                    Arguments = extensionsUrl,
                    UseShellExecute = true
                });

                // Show instructions dialog
                System.Windows.MessageBox.Show(
                    $"To install the Social Sentry extension:\n\n" +
                    $"1. Enable 'Developer mode' (toggle in top right)\n" +
                    $"2. Click 'Load unpacked'\n" +
                    $"3. Navigate to:\n   {_extensionInstallPath}\n" +
                    $"4. Click 'Select Folder'\n\n" +
                    $"The extension folder path has been copied to your clipboard.",
                    "Install Social Sentry Extension",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information
                );

                // Copy path to clipboard
                System.Windows.Clipboard.SetText(_extensionInstallPath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Could not open browser. Please manually navigate to {extensionsUrl}\n\nError: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning
                );
            }
        }
    }
}
