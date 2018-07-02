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
using System.Windows.Shapes;

namespace EasyBuyout.League {
    /// <summary>
    /// Interaction logic for ManualLeagueWindow.xaml
    /// </summary>
    public partial class ManualLeagueWindow : Window {
        private readonly LeagueManager leagueManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="leagueManager"></param>
        public ManualLeagueWindow(LeagueManager leagueManager) {
            this.leagueManager = leagueManager;
            InitializeComponent();
        }

        /// <summary>
        /// Apply button event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Apply_Click(object sender, RoutedEventArgs e) {
            leagueManager.SetSelectedLeague(TextBox_League.Text);
            Hide();
        }
    }
}
