using Supabase;
using Supabase.Gotrue;
using System;
using System.Threading.Tasks;

namespace Social_Sentry.Services
{
    public class SupabaseService
    {
        private static SupabaseService _instance;
        public static SupabaseService Instance => _instance ??= new SupabaseService();

        public Client Client { get; private set; }

        private string _url;
        private string _key;

        public bool IsInitialized => Client != null;

        private SupabaseService()
        {
        }

        public async Task InitializeAsync(string url, string key)
        {
            if (IsInitialized) return;

            _url = url;
            _key = key;

            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true,
            };

            Client = new Supabase.Client(_url, _key, options);
            await Client.InitializeAsync();
        }

        public Session? CurrentSession => Client?.Auth.CurrentSession;
        public User? CurrentUser => Client?.Auth.CurrentUser;
    }
}
