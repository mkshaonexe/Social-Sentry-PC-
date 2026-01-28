using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Social_Sentry.Services
{
    public class LocalApiServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts;
        private Task _serverTask;
        private bool _isRunning;

        public event EventHandler<ExtensionActivityData> OnActivityReceived;

        public LocalApiServer()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:5123/");
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _listener.Start();
                _isRunning = true;
                _serverTask = Task.Run(ListenAsync);
                System.Diagnostics.Debug.WriteLine("LocalApiServer started on port 5123");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start LocalApiServer: {ex.Message}");
            }
        }

        private async Task ListenAsync()
        {
            while (!_cts.Token.IsCancellationRequested && _isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(context));
                }
                catch (HttpListenerException) when (_cts.Token.IsCancellationRequested)
                {
                    // Expected when stopping
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LocalApiServer error: {ex.Message}");
                }
            }
        }

        private async Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            // Add CORS headers for extension
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            // Handle preflight
            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            string responseBody = "";
            int statusCode = 200;

            try
            {
                switch (request.Url?.AbsolutePath)
                {
                    case "/api/heartbeat":
                        responseBody = JsonSerializer.Serialize(new { status = "ok", timestamp = DateTime.UtcNow });
                        break;

                    case "/api/activity": // Legacy Endpoint
                    case "/api/v2/activity": // New V2 Endpoint
                        if (request.HttpMethod == "POST")
                        {
                            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                            var body = await reader.ReadToEndAsync();
                            
                            // Deserialize to V2 model
                            var activityData = JsonSerializer.Deserialize<ExtensionActivityData>(body, new JsonSerializerOptions 
                            { 
                                PropertyNameCaseInsensitive = true,
                                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } 
                            });

                            if (activityData != null)
                            {
                                OnActivityReceived?.Invoke(this, activityData);
                            }

                            responseBody = JsonSerializer.Serialize(new { success = true });
                        }
                        break;

                    case "/api/config":
                        responseBody = JsonSerializer.Serialize(new
                        {
                            trackingEnabled = true,
                            reportInterval = 5000,
                            idleThreshold = 30000
                        });
                        break;

                    default:
                        statusCode = 404;
                        responseBody = JsonSerializer.Serialize(new { error = "Not found" });
                        break;
                }
            }
            catch (Exception ex)
            {
                statusCode = 500;
                responseBody = JsonSerializer.Serialize(new { error = ex.Message });
            }

            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            var buffer = Encoding.UTF8.GetBytes(responseBody);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
            response.Close();
        }

        public void Stop()
        {
            if (_isRunning)
            {
                _isRunning = false;
                try
                {
                    _cts?.Cancel();
                    if (_listener.IsListening)
                    {
                        _listener.Stop();
                    }
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex) 
                {
                    System.Diagnostics.Debug.WriteLine($"Error stopping LocalApiServer: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            Stop();
            try
            {
                _listener.Close();
                _cts?.Dispose();
            }
            catch (Exception) { }
        }
    }

    // Updated Model to support V2
    public class ExtensionActivityData
    {
        public string? Platform { get; set; } // e.g. "YouTube", "Facebook"
        public string? ContentType { get; set; } // e.g. "Shorts", "Video", "Feed"
        
        // Legacy/Generic fields
        public string? ActivityType { get; set; } 
        public int ScrollDepth { get; set; }
        public double VideoWatchTime { get; set; }
        public int Duration { get; set; }
        public string? Url { get; set; }
        public string? Title { get; set; } // Can be enriched by Metadata
        public string? Timestamp { get; set; }
        
        public ExtensionSession? Session { get; set; }
        public Dictionary<string, object>? Metadata { get; set; } // Flexible metadata bag
    }

    public class ExtensionSession
    {
        public int? TabId { get; set; }
        public int? WindowId { get; set; }
        public bool IsFocused { get; set; }
    }

    public class ExtensionMetadata // Legacy support if needed, but Metadata Dict is preferred for flexibility
    {
        public string? Hostname { get; set; }
        public string? Pathname { get; set; }
        public double ScrollVelocity { get; set; }
    }
}
