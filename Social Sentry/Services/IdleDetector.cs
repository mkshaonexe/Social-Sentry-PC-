using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;

namespace Social_Sentry.Services
{
    public class IdleDetector
    {
        private readonly System.Timers.Timer _timer;
        private bool _isIdle;
        public uint IdleThresholdMs { get; set; }

        public event Action<bool>? OnIdleStateChanged;

        public IdleDetector(uint idleThresholdMs = 60000)
        {
            IdleThresholdMs = idleThresholdMs;
            _timer = new System.Timers.Timer(1000); // Check every second
            _timer.Elapsed += CheckIdleState;
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void CheckIdleState(object? sender, ElapsedEventArgs e)
        {
            try
            {
                var lastInputInfo = new NativeMethods.LASTINPUTINFO();
                lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

                if (NativeMethods.GetLastInputInfo(ref lastInputInfo))
                {
                    // Environment.TickCount is in int (can be negative due to overflow), 
                    // casting to uint gives consistent uptime result.
                    uint tickCount = (uint)Environment.TickCount;
                    uint lastInputTick = lastInputInfo.dwTime;

                    // Calculate elapsed (handle potential overflow if system runs for > 49.7 days)
                    uint elapsedTicks = tickCount - lastInputTick;

                    bool isNowIdle = elapsedTicks > IdleThresholdMs;

                    if (isNowIdle != _isIdle)
                    {
                        _isIdle = isNowIdle;
                        OnIdleStateChanged?.Invoke(_isIdle);
                        Debug.WriteLine($"User is now {(_isIdle ? "Idle" : "Active")} (Inactive for {elapsedTicks}ms)");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Idle check failed: {ex.Message}");
            }
        }
    }
}
