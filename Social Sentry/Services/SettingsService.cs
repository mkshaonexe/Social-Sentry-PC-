using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace Social_Sentry.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;
        private readonly EncryptionService _encryptionService;
        private const string APP_NAME = "SocialSentry";
        private const string REGISTRY_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public SettingsService()
        {
            _encryptionService = new EncryptionService();
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "SocialSentry");
            Directory.CreateDirectory(appFolder);
            _settingsPath = Path.Combine(appFolder, "settings.json");
        }

        public UserSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    // For backward compatibility: try reading as plain text first
                    // or check if it looks encrypted (Base64). 
                    // Simpler approach: Try decrypt. If fail, try deserialize raw.
                    var content = File.ReadAllText(_settingsPath);
                    
                    string json;
                    try 
                    {
                        // Try to decrypt assuming it's encrypted
                        json = _encryptionService.Decrypt(content);
                    }
                    catch 
                    {
                        // Fallback implies it was plain text (old version)
                        json = content; 
                    }

                    // If decryption returned "[Encrypted Data]" or failure, it might be just bad data, 
                    // but allow trying to parse what we got.
                    return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }

            return new UserSettings();
        }

        public void SaveSettings(UserSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                // Encrypt the entire JSON blob
                var encrypted = _encryptionService.Encrypt(json);
                File.WriteAllText(_settingsPath, encrypted);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public bool SetStartWithWindows(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true);
                if (key != null)
                {
                    if (enable)
                    {
                        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        // For .NET 6+, we need to get the executable path differently
                        exePath = exePath.Replace(".dll", ".exe");
                        key.SetValue(APP_NAME, $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue(APP_NAME, false);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting startup: {ex.Message}");
            }

            return false;
        }

        public bool IsStartWithWindowsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, false);
                return key?.GetValue(APP_NAME) != null;
            }
            catch
            {
                return false;
            }
        }
    }

    public class UserSettings
    {
        public bool IsFirstRun { get; set; } = true;
        public bool StartWithWindows { get; set; }
        public bool StartMinimized { get; set; }
        public bool ShowNotifications { get; set; } = true;
    }
}
