using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Social_Sentry.Services
{
    public class PrimeModeService
    {
        private bool _isActive;
        private readonly SelfProtectionService _selfProtectionService;

        public bool IsPrimeModeActive => _isActive;

        public event Action<bool>? PrimeModeChanged;

        public PrimeModeService()
        {
            _selfProtectionService = new SelfProtectionService();
        }

        public void EnablePrimeMode()
        {
            if (_isActive) return;

            // Strict enforcement logic
            // 1. Enable watchdog if not already
            _selfProtectionService.StartWatchdog();
            
            // 2. Prevent Task Manager (Simplified: Watchdog kills it or we set registry)
            // Implementation of TaskMgr blocking usually requires admin registry edit.
            // For now, we will rely on SelfProtectionService.
            
            _isActive = true;
            PrimeModeChanged?.Invoke(true);
        }

        public void DisablePrimeMode()
        {
            if (!_isActive) return;

            // Relax enforcement
            // Note: SelfProtection might stay on if regular blocking is active.
            
            _isActive = false;
            PrimeModeChanged?.Invoke(false);
        }

        // Additional Logic mirroring Android's "Strict blocking"
        // e.g. preventing uninstall, etc.
    }
}
