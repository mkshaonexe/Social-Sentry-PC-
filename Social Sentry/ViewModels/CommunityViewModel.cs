namespace Social_Sentry.ViewModels
{
    public class CommunityViewModel : ViewModelBase
    {
        public System.Windows.Input.ICommand OpenGlobalChatCommand { get; }
        public System.Windows.Input.ICommand OpenGlobalChallengeCommand { get; }
        public System.Windows.Input.ICommand OpenYourStoryCommand { get; }

        public CommunityViewModel()
        {
            OpenGlobalChatCommand = new RelayCommand(ExecuteOpenGlobalChat);
            OpenGlobalChallengeCommand = new RelayCommand(ExecuteOpenGlobalChallenge);
            OpenYourStoryCommand = new RelayCommand(ExecuteOpenYourStory);
        }

        private void ExecuteOpenGlobalChat()
        {
            // Placeholder: Implement navigation or logic
            System.Windows.MessageBox.Show("Global Chat Feature Coming Soon!", "Social Sentry");
        }

        private void ExecuteOpenGlobalChallenge()
        {
            // Placeholder: Implement navigation or logic
            System.Windows.MessageBox.Show("Global Challenge Feature Coming Soon!", "Social Sentry");
        }

        private void ExecuteOpenYourStory()
        {
            // Placeholder: Implement navigation or logic
            System.Windows.MessageBox.Show("Your Story Feature Coming Soon!", "Social Sentry");
        }
    }
}
