using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Social_Sentry.Views;
using Social_Sentry.Models;

namespace Social_Sentry.Services
{
    public class SafetyService
    {
        private readonly UsageTrackerService _usageTracker;
        private readonly SettingsService _settingsService;
        
        private DateTime _lastActivityTime;
        private DateTime _sessionStartTime;
        private bool _isBreakActive = false;
        private CancellationTokenSource? _monitorCts;
        
        // Constants (Public for testability if needed, or make configurable)
        private const double MAX_CONTINUOUS_MINUTES = 50.0;
        private const double IDLE_RESET_MINUTES = 5.0;
        
        public event Action<TimeSpan>? OnTimeRemainingChanged;

        public SafetyService(UsageTrackerService usageTracker, SettingsService settingsService)
        {
            _usageTracker = usageTracker;
            _settingsService = settingsService;
            
            _usageTracker.OnRawActivityDetected += HandleActivity;
            SettingsService.SettingsChanged += HandleSettingsChanged;
            
            if (_settingsService.LoadSettings().IsSafetyEnabled)
            {
                StartMonitoring();
            }
        }

        private void HandleSettingsChanged(UserSettings settings)
        {
            if (settings.IsSafetyEnabled)
            {
                if (_monitorCts == null) StartMonitoring();
            }
            else
            {
                StopMonitoring();
            }
        }

        private void HandleActivity(Social_Sentry.Models.ActivityEvent evt)
        {
            _lastActivityTime = DateTime.Now;
        }

        public void StartMonitoring()
        {
            if (_monitorCts != null) return;
            
            _monitorCts = new CancellationTokenSource();
            _sessionStartTime = DateTime.Now;
            _lastActivityTime = DateTime.Now;
            
            Task.Run(() => MonitorLoop(_monitorCts.Token));
            Debug.WriteLine("[SafetyService] Started monitoring.");
        }

        public void StopMonitoring()
        {
            _monitorCts?.Cancel();
            _monitorCts = null;
            Debug.WriteLine("[SafetyService] Stopped monitoring.");
        }

        private async Task MonitorLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(1000, token); // Check every second
                    
                    if (_isBreakActive) continue;

                    var now = DateTime.Now;
                    
                    // 1. Check for Idle Reset
                    // If no activity for > 5 mins, reset the session
                    var idleDuration = now - _lastActivityTime;
                    if (idleDuration.TotalMinutes > IDLE_RESET_MINUTES)
                    {
                         // Reset session
                         _sessionStartTime = now;
                         Debug.WriteLine("[SafetyService] Idle detected. Session reset.");
                    }

                    // 2. Check for Break Trigger
                    var sessionDuration = now - _sessionStartTime;
                    var remaining = TimeSpan.FromMinutes(MAX_CONTINUOUS_MINUTES) - sessionDuration;
                    OnTimeRemainingChanged?.Invoke(remaining);

                    if (sessionDuration.TotalMinutes >= MAX_CONTINUOUS_MINUTES)
                    {
                        TriggerBreak();
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Normal stop
            }
            catch (Exception ex) 
            {
                Debug.WriteLine($"[SafetyService] Error in loop: {ex.Message}");
            }
        }

        private void TriggerBreak()
        {
            _isBreakActive = true;
            
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var overlay = new SafetyOverlayWindow();
                overlay.Closed += (s, e) => 
                {
                    _isBreakActive = false;
                    _sessionStartTime = DateTime.Now; // Reset session after break
                    _lastActivityTime = DateTime.Now;
                };
                overlay.Show();
            });
        }
    }
}
