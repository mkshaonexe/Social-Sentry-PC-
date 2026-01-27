using Social_Sentry.Models;
using Social_Sentry.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Linq;

namespace Social_Sentry.ViewModels
{
    public class CommunityViewModel : ViewModelBase
    {
        private readonly SupabaseService _supabaseService;
        private readonly RankingService _rankingService;
        
        public ObservableCollection<Message> Messages { get; } = new ObservableCollection<Message>();

        private string _newMessageContent;
        public string NewMessageContent
        {
            get => _newMessageContent;
            set
            {
                if (_newMessageContent != value)
                {
                    _newMessageContent = value;
                    OnPropertyChanged(nameof(NewMessageContent));
                }
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged(nameof(IsBusy));
                }
            }
        }

        public ICommand SendMessageCommand { get; }
        public ICommand RefreshCommand { get; }

        public CommunityViewModel()
        {
            _supabaseService = SupabaseService.Instance;
            var settingsService = new SettingsService();
            _rankingService = new RankingService(settingsService);

            SendMessageCommand = new RelayCommand(ExecuteSendMessage);
            RefreshCommand = new RelayCommand(ExecuteRefresh);

            LoadMessages();
            SubscribeToRealtime();
        }

        private async void ExecuteRefresh()
        {
            await LoadMessages();
        }

        private async Task LoadMessages()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                if (!_supabaseService.IsInitialized) return; // Should be initialized in App.xaml.cs

                var response = await _supabaseService.Client.From<Message>()
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(50)
                    .Get();

                Messages.Clear();
                foreach (var msg in response.Models)
                {
                    Messages.Add(msg);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading messages: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void ExecuteSendMessage()
        {
            if (string.IsNullOrWhiteSpace(NewMessageContent)) return;
            if (!_supabaseService.IsInitialized) return;

            IsBusy = true;
            try
            {
                // Anonymous auth if not logged in?
                // For now, assuming user is anonymous or we just send as "User"
                // Android app sends much more metadata. We should try to match it.
                
                var currentRank = _rankingService.GetCurrentBadge();
                var strikeTime = _rankingService.GetFormattedStrikeTime();
                
                // Determine user ID and Username
                var userId = _supabaseService.CurrentUser?.Id ?? "anon_user_" + Guid.NewGuid().ToString().Substring(0, 8);
                // Try to persist user ID in settings if possible, but for anon it changes strictly speaking.
                // ideally we do auth. Assume Anon for now.

                var message = new Message
                {
                    Content = NewMessageContent,
                    UserId = userId,
                    Username = "Desktop User", // TODO: Get from settings or auth
                    Rank = currentRank.Title,
                    Role = "member",
                    StrikeTime = strikeTime,
                    CreatedAt = DateTime.UtcNow,
                    IsPinned = false,
                    IsVerified = false
                };

                await _supabaseService.Client.From<Message>().Insert(message);
                
                NewMessageContent = "";
                // Realtime should handle adding it to the list, or we add manually
                // Messages.Insert(0, message); // Optimistic add?
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send message: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void SubscribeToRealtime()
        {
             if (!_supabaseService.IsInitialized) return;

             await _supabaseService.Client.Realtime.ConnectAsync();
             
             var channel = _supabaseService.Client.Realtime.Channel("realtime", "public", "messages");
             
             // Different library versions handle this differently. 
             // Using generic 'OnInsert' pattern typical for Supabase C#
             channel.OnInsert += (sender, args) =>
             {
                 // Need to deserialize args.Payload to Message
                 // Or re-fetch. Re-fetching is safer for now if deserialization is complex.
                 // Ideally deserializing locally.
                 Application.Current.Dispatcher.Invoke(() => 
                 {
                     // Placeholder: simplest is to just re-fetch or insert if we can parse
                     // For now, let's trigger a refresh or parse if possible.
                     // C# Client might return a model directly?
                     // Verify library capabilities.
                     LoadMessages(); 
                 });
             };
             
             await channel.Subscribe();
        }
    }
}
