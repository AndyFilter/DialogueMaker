using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            await Task.Delay(4500);

            var parent = this.Parent as DockPanel;

            parent.Children.Remove(this);
        }
    }
}
