using System.Windows;

namespace EasyBuyout.Settings {
    /// <summary>
    /// Interaction logic for ManualLeagueWindow.xaml
    /// </summary>
    public partial class ManualLeagueWindow : Window {
        public string input;

        /// <summary>
        /// Constructor
        /// </summary>
        public ManualLeagueWindow() {
            InitializeComponent();
        }

        /// <summary>
        /// Apply button event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Apply_Click(object sender, RoutedEventArgs e) {
            input = TextBox_League.Text;
            Hide();
        }
    }
}
