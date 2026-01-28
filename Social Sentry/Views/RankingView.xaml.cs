using System.Windows.Controls;

namespace Social_Sentry.Views
{
    public partial class RankingView : System.Windows.Controls.UserControl
    {
        public RankingView()
        {
            Services.TraceLogger.Log("RankingView Constructor Start");
            InitializeComponent();
            Services.TraceLogger.Log("RankingView Constructor End");
        }
    }
}
