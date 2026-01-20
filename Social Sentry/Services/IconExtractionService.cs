using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Social_Sentry.Services
{
    public class IconExtractionService
    {
        private readonly ConcurrentDictionary<string, BitmapSource> _iconCache = new();
        private BitmapSource? _defaultIcon;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
            public uint dwAttributes;
        }

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000;
        private const uint SHGFI_SMALLICON = 0x000000001;

        /// <summary>
        /// Gets the icon for a process by its process ID
        /// </summary>
        public BitmapSource? GetProcessIcon(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                return GetProcessIcon(process);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get icon for process ID {processId}: {ex.Message}");
                return GetDefaultIcon();
            }
        }

        /// <summary>
        /// Gets the icon for a process by its name
        /// </summary>
        public BitmapSource? GetProcessIcon(string processName)
        {
            // Check cache first
            if (_iconCache.TryGetValue(processName, out var cachedIcon))
            {
                return cachedIcon;
            }

            try
            {
                // Try to find a running process with this name
                if (processName.Equals("Social Sentry", StringComparison.OrdinalIgnoreCase) || 
                    processName.Equals("Social Sentry.exe", StringComparison.OrdinalIgnoreCase))
                {
                     try 
                     {
                         // Use the specific AppLogo.png for our own app
                         var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "AppLogo.png");
                         if (File.Exists(logoPath))
                         {
                             var bitmap = new BitmapImage();
                             bitmap.BeginInit();
                             bitmap.CacheOption = BitmapCacheOption.OnLoad;
                             bitmap.UriSource = new Uri(logoPath, UriKind.Absolute);
                             bitmap.EndInit();
                             bitmap.Freeze();
                             
                             _iconCache[processName] = bitmap;
                             return bitmap;
                         }
                     }
                     catch (Exception ex)
                     {
                         Debug.WriteLine($"Failed to load custom Social Sentry logo: {ex.Message}");
                     }
                }

                var processes = Process.GetProcessesByName(processName.Replace(".exe", ""));
                if (processes.Length > 0)
                {
                    var icon = GetProcessIcon(processes[0]);
                    if (icon != null)
                    {
                        _iconCache[processName] = icon;
                    }
                    return icon;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get icon for process name {processName}: {ex.Message}");
            }

            return GetDefaultIcon();
        }

        /// <summary>
        /// Gets the icon for a running process
        /// </summary>
        private BitmapSource? GetProcessIcon(Process process)
        {
            try
            {
                var processName = process.ProcessName;

                // Check cache first
                if (_iconCache.TryGetValue(processName, out var cachedIcon))
                {
                    return cachedIcon;
                }

                string? exePath = null;

                try
                {
                    exePath = process.MainModule?.FileName;
                }
                catch (Exception ex)
                {
                    // Access denied for some system processes
                    Debug.WriteLine($"Cannot access main module for {processName}: {ex.Message}");
                }

                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    return GetDefaultIcon();
                }

                var icon = ExtractIcon(exePath);
                if (icon != null)
                {
                    _iconCache[processName] = icon;
                }

                return icon ?? GetDefaultIcon();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to extract icon: {ex.Message}");
                return GetDefaultIcon();
            }
        }

        /// <summary>
        /// Extracts icon from an executable file path
        /// </summary>
        private BitmapSource? ExtractIcon(string filePath)
        {
            try
            {
                // Method 1: Try using Icon.ExtractAssociatedIcon (simpler)
                using (var icon = Icon.ExtractAssociatedIcon(filePath))
                {
                    if (icon != null)
                    {
                        return ConvertIconToBitmapSource(icon);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExtractAssociatedIcon failed for {filePath}: {ex.Message}");
            }

            try
            {
                // Method 2: Try using SHGetFileInfo (more robust)
                SHFILEINFO shinfo = new SHFILEINFO();
                IntPtr hSuccess = SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_LARGEICON);

                if (hSuccess != IntPtr.Zero && shinfo.hIcon != IntPtr.Zero)
                {
                    try
                    {
                        using (var icon = Icon.FromHandle(shinfo.hIcon))
                        {
                            var bitmapSource = ConvertIconToBitmapSource(icon);
                            return bitmapSource;
                        }
                    }
                    finally
                    {
                        // Clean up the icon handle
                        DestroyIcon(shinfo.hIcon);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SHGetFileInfo failed for {filePath}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Converts a System.Drawing.Icon to WPF BitmapSource
        /// </summary>
        private BitmapSource ConvertIconToBitmapSource(Icon icon)
        {
            var bitmap = icon.ToBitmap();
            var hBitmap = bitmap.GetHbitmap();

            try
            {
                var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                // Freeze for performance (makes it thread-safe and improves rendering)
                bitmapSource.Freeze();
                return bitmapSource;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        /// <summary>
        /// Gets a default fallback icon
        /// </summary>
        public BitmapSource GetDefaultIcon()
        {
            if (_defaultIcon != null)
            {
                return _defaultIcon;
            }

            try
            {
                // Create a simple default icon (a colored square with "App" text)
                // For now, return a system application icon
                var systemIcon = SystemIcons.Application;
                _defaultIcon = ConvertIconToBitmapSource(systemIcon);
                return _defaultIcon;
            }
            catch
            {
                // If all else fails, create a minimal bitmap
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("pack://application:,,,/Resources/default_app_icon.png", UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                _defaultIcon = bitmap;
                return _defaultIcon;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
