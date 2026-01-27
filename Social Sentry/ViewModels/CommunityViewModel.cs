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
        
        public ObservableCollection<Models.Message> Messages { get; } = new ObservableCollection<Models.Message>();

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

        public enum CommunitySection
        {
            GlobalChat,
            GlobalMission,
            YourStudies
        }

        private CommunitySection _currentSection;
        public CommunitySection CurrentSection
        {
            get => _currentSection;
            set
            {
                if (_currentSection != value)
                {
                    _currentSection = value;
                    OnPropertyChanged(nameof(CurrentSection));
                    OnPropertyChanged(nameof(IsGlobalChatVisible));
                    OnPropertyChanged(nameof(IsGlobalMissionVisible));
                    OnPropertyChanged(nameof(IsYourStudiesVisible));
                }
            }
        }

        public bool IsGlobalChatVisible => CurrentSection == CommunitySection.GlobalChat;
        public bool IsGlobalMissionVisible => CurrentSection == CommunitySection.GlobalMission;
        public bool IsYourStudiesVisible => CurrentSection == CommunitySection.YourStudies;

        public ICommand SwitchSectionCommand { get; }
        public ICommand SendMessageCommand { get; }
        public ICommand RefreshCommand { get; }

        public CommunityViewModel()
        {
            _supabaseService = SupabaseService.Instance;
            var settingsService = new SettingsService();
            _rankingService = new RankingService(settingsService);

            SwitchSectionCommand = new RelayCommand(ExecuteSwitchSection);
            SendMessageCommand = new RelayCommand(ExecuteSendMessage);
            RefreshCommand = new RelayCommand(ExecuteRefresh);

            CurrentSection = CommunitySection.GlobalChat; // Default

            LoadMessages();
            SubscribeToRealtime();
        }

        private void ExecuteSwitchSection(object parameter)
        {
            if (parameter is CommunitySection section)
            {
                CurrentSection = section;
            }
            else if (parameter is string sectionName)
            {
                 if (Enum.TryParse(sectionName, out CommunitySection parsedSection))
                 {
                     CurrentSection = parsedSection;
                 }
            }
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
                if (!_supabaseService.IsInitialized) return; 

                var response = await _supabaseService.Client.From<Models.Message>()
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
                var currentRank = _rankingService.GetCurrentBadge();
                var strikeTime = _rankingService.GetFormattedStrikeTime();
                
                var currentUser = _supabaseService.CurrentUser;
                var userId = currentUser?.Id ?? "anon_user_" + Guid.NewGuid().ToString().Substring(0, 8);
                
                // Try to get username and avatar from metadata
                string username = "Desktop User";
                string avatarUrl = null;

                if (currentUser?.UserMetadata != null)
                {
                    if (currentUser.UserMetadata.ContainsKey("username"))
                        username = currentUser.UserMetadata["username"]?.ToString();
                    else if (currentUser.UserMetadata.ContainsKey("name"))
                        username = currentUser.UserMetadata["name"]?.ToString();
                        
                    if (currentUser.UserMetadata.ContainsKey("avatar_url"))
                        avatarUrl = currentUser.UserMetadata["avatar_url"]?.ToString();
                }

                if (string.IsNullOrEmpty(username) || username.Length < 3) 
                {
                     username = "User " + userId.Substring(0, 4);
                }

                var message = new Models.Message
                {
                    Content = NewMessageContent,
                    UserId = userId,
                    Username = username, 
                    AvatarUrl = avatarUrl,
                    Rank = currentRank.Title,
                    Role = "member", // Default to member for now
                    StrikeTime = strikeTime,
                    CreatedAt = DateTime.UtcNow,
                    IsPinned = false,
                    IsVerified = false
                };

                await _supabaseService.Client.From<Models.Message>().Insert(message);
                
                NewMessageContent = "";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to send message: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void SubscribeToRealtime()
        {
             if (!_supabaseService.IsInitialized) return;
             // Realtime implementation pending library verification
        }
    }
}
