using System.Threading.Tasks;
using System.Windows.Controls;

namespace DialogueMaker.Controls
{
    /// <summary>
    /// Interaction logic for UserNotification.xaml
    /// </summary>

    public partial class UserNotification : UserControl
    {
        public UserNotification()
        {
            InitializeComponent();

            TimeControl();
        }

        private async void TimeControl()
        {
            await Task.Delay(4600);

            var parent = this.Parent as DockPanel;

            parent.Children.Remove(this);
        }
    }
}
