using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

namespace Social_Sentry.Services
{
    public class PrimeModeService
    {
        private bool _isActive;
        private readonly SelfProtectionService _selfProtectionService;
        private readonly NotificationService _notificationService;
        private System.Windows.Media.MediaPlayer _mediaPlayer;

        public bool IsPrimeModeActive => _isActive;

        public event Action<bool>? PrimeModeChanged;

        public PrimeModeService()
        {
            _selfProtectionService = new SelfProtectionService();
            _notificationService = new NotificationService();
            _mediaPlayer = new System.Windows.Media.MediaPlayer();
        }

        public void EnablePrimeMode()
        {
            if (_isActive) return;

            // Strict enforcement logic
            // 1. Enable watchdog if not already
            _selfProtectionService.StartWatchdog();

            // Play Sound
            PlayActivationSound();

            // Show Notification
            _notificationService.ShowPrimeModeActive(true);
            
            _isActive = true;
            PrimeModeChanged?.Invoke(true);
        }

        public void DisablePrimeMode()
        {
            if (!_isActive) return;

            // Relax enforcement
            // Note: SelfProtection might stay on if regular blocking is active.

            // Show Notification
            _notificationService.ShowPrimeModeActive(false);
            
            _isActive = false;
            PrimeModeChanged?.Invoke(false);
        }

        private void PlayActivationSound()
        {
            try
            {
                string soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds", "prime_mode.mp3");
                if (File.Exists(soundPath))
                {
                    _mediaPlayer.Open(new Uri(soundPath));
                    _mediaPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error playing sound: {ex.Message}");
            }
        }
    }
}
