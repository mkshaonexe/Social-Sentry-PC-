using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace Social_Sentry.Services
{
    public class MediaDetector
    {
        private GlobalSystemMediaTransportControlsSessionManager? _manager;

        public async Task InitializeAsync()
        {
            try
            {
                _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MediaDetector Init Failed: {ex.Message}");
            }
        }

        public bool IsMediaPlaying(string processName)
        {
            if (_manager == null) return false;

            try
            {
                var session = _manager.GetCurrentSession();
                if (session == null) return false;

                // Check playback status
                var info = session.GetPlaybackInfo();
                if (info.PlaybackStatus != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                    return false;

                // Compare Source App
                string appId = session.SourceAppUserModelId ?? "";
                
                // Heuristic: If appId contains target process name (case insensitive)
                if (appId.Contains(processName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
