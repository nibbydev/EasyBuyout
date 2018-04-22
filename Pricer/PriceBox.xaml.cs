using System.Windows;
using System.Windows.Input;

namespace Pricer {
    /// <summary>
    /// Interaction logic for PriceBox.xaml
    /// </summary>
    public partial class PriceBox : Window {
        public PriceBox() {
            InitializeComponent();
        }

        /// <summary>
        /// Hide the box when user moves cursor aways
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseLeave(object sender, MouseEventArgs e) {
            Hide();
        }
    }
}
