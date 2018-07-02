using System.Windows;
using System.Windows.Input;

namespace EasyBuyout.Prices {
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

        /// <summary>
        /// Set the position of the price overlay under the user's cursor
        /// </summary>
        public void SetPosition() {
            Left = System.Windows.Forms.Cursor.Position.X - Width / 2;
            Top = System.Windows.Forms.Cursor.Position.Y - Height / 2;
        }
    }
}
