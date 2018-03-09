using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Pricer {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        public SettingsWindow() {
            InitializeComponent();

            foreach (string method in Settings.priceMethods) ComboBox_Method.Items.Add(method);
            foreach (string source in Settings.sourceList) ComboBox_Source.Items.Add(source);
            ComboBox_Method.SelectedIndex = 0;
            ComboBox_Source.SelectedIndex = 0;
            Settings.source = Settings.sourceList[0];
            Settings.method = Settings.priceMethods[0];
        }

        /// <summary>
        /// Gets active leagues and adds them to controls
        /// </summary>
        public void AddLeagues() {
            string[] leagues = Utility.MiscMethods.GetLeagueList();
            MainWindow.Log("Downloading league list...", 0);
            Dispatcher.Invoke(() => {
                foreach (string league in leagues) ComboBox_League.Items.Add(league);
                ComboBox_League.SelectedIndex = 0;
                Settings.league = ComboBox_League.SelectedValue.ToString();
                Button_Download.IsEnabled = true;
            });
            MainWindow.Log("League list updated", 0);
        }

        /// <summary>
        /// Intercepts window close event and hides it instead
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            ResetOptions();
            e.Cancel = true;
            Hide();
        }

        /// <summary>
        /// Verifies current settings and saves them
        /// </summary>
        private void Button_Apply_Click(object sender, RoutedEventArgs e) {
            // Radio buttons
            if ((bool)Radio_Buyout.IsChecked) Settings.prefix = "~b/o";
            else Settings.prefix = "price";

            // Delay box
            Int32.TryParse(TextBox_Delay.Text, out int delay);
            if (delay != Settings.pasteDelay) {
                if (delay < 1 || delay > 500) {
                    MainWindow.Log("Invalid input - delay (allowed: 1 - 500)", 2);
                    TextBox_Delay.Text = Settings.pasteDelay.ToString();
                } else {
                    MainWindow.Log("Changed delay " + Settings.pasteDelay + " -> " + delay, 0);
                    Settings.pasteDelay = delay;
                }
            }

            // Lower price % box
            Int32.TryParse(TextBox_LowerPrice.Text, out int percentage);
            if (percentage != Settings.lowerPricePercentage) {
                if (percentage < 0 || percentage > 100) {
                    MainWindow.Log("Invalid input - percentage (allowed: 0 - 100)", 2);
                    TextBox_LowerPrice.Text = Settings.lowerPricePercentage.ToString();
                } else {
                    MainWindow.Log("Changed percentage " + Settings.lowerPricePercentage + " -> " + percentage, 0);
                    Settings.lowerPricePercentage = percentage;
                }
            }

            // Checkboxes
            Settings.flag_priceBox = (bool)CheckBox_ShowOverlay.IsChecked;
            Settings.flag_fallback = (bool)CheckBox_Fallback.IsChecked;
            Settings.flag_sendEnter = (bool)CheckBox_SendEnter.IsChecked;
            Settings.flag_sendNote = (bool)CheckBox_SendNote.IsChecked;

            // Radio buttons
            if ((bool)Radio_Buyout.IsChecked) Settings.prefix = Radio_Buyout.Content.ToString();
            else Settings.prefix = Radio_Price.Content.ToString();

            Hide();
        }

        /// <summary>
        /// Enables/disables other controls based on combobox
        /// </summary>
        private void ComboBox_Source_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (ComboBox_League.SelectedItem != null) Button_Download.IsEnabled = true;
            if (ComboBox_Source.SelectedValue.ToString().ToLower() == "poe.ninja") {
                ComboBox_Method.IsEnabled = false;
                ComboBox_Method.SelectedIndex = -1;
            } else {
                ComboBox_Method.IsEnabled = true;
                ComboBox_Method.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Runs database download task
        /// </summary>
        private void Button_Download_Click(object sender, RoutedEventArgs e) {
            Settings.source = (string)ComboBox_Source.SelectedValue;
            Settings.method = (string)ComboBox_Method.SelectedValue;
            Settings.league = (string)ComboBox_League.SelectedValue;

            Button_Download.IsEnabled = false;

            MainWindow.Log("Downloading " + ComboBox_Method.SelectedValue + 
                " price data for " + ComboBox_League.SelectedValue + 
                " from " + ComboBox_Source.SelectedValue, 0);

            // Download, parse, update as task
            Task.Run(() => {
                MainWindow.priceManager.Download();
                Application.Current.Dispatcher.Invoke(() => MainWindow.runButton.IsEnabled = true);
                MainWindow.Log("Download finished", 0);
            });
        }

        /// <summary>
        /// Enables/disables other controls based on checkbox
        /// </summary>
        private void CheckBox_ShowOverlay_Click(object sender, RoutedEventArgs e) {
            CheckBox_SendNote.IsChecked = false;

            CheckBox_SendEnter.IsEnabled = !(bool)CheckBox_ShowOverlay.IsChecked;
            Radio_Buyout.IsEnabled = !(bool)CheckBox_ShowOverlay.IsChecked;
            Radio_Price.IsEnabled = !(bool)CheckBox_ShowOverlay.IsChecked;
            TextBox_Delay.IsEnabled = !(bool)CheckBox_ShowOverlay.IsChecked;
        }

        /// <summary>
        /// Enables/disables other controls based on checkbox
        /// </summary>
        private void CheckBox_SendNote_Click(object sender, RoutedEventArgs e) {
            CheckBox_ShowOverlay.IsChecked = false;

            CheckBox_SendEnter.IsEnabled = (bool)CheckBox_SendNote.IsChecked;
            Radio_Buyout.IsEnabled = (bool)CheckBox_SendNote.IsChecked;
            Radio_Price.IsEnabled = (bool)CheckBox_SendNote.IsChecked;
            TextBox_Delay.IsEnabled = (bool)CheckBox_SendNote.IsChecked;
        }

        /// <summary>
        /// Cancel button handler
        /// </summary>
        private void Button_Cancel_Click(object sender, RoutedEventArgs e) {
            ResetOptions();
            Hide();
        }

        /// <summary>
        /// Reverts all settings back to original state when cancel button is pressed
        /// </summary>
        private void ResetOptions() {
            ComboBox_League.SelectedValue = Settings.league;
            ComboBox_Method.SelectedValue = Settings.method;
            ComboBox_Source.SelectedValue = Settings.source;

            TextBox_Delay.Text = Settings.pasteDelay.ToString();
            TextBox_LowerPrice.Text = Settings.lowerPricePercentage.ToString();

            CheckBox_Fallback.IsChecked = Settings.flag_fallback;
            CheckBox_SendEnter.IsChecked = Settings.flag_sendEnter;
            CheckBox_SendNote.IsChecked = Settings.flag_sendNote;
            CheckBox_ShowOverlay.IsChecked = Settings.flag_priceBox;
            
            if (Settings.prefix == (string)Radio_Buyout.Content) {
                Radio_Buyout.IsChecked = true;
                Radio_Price.IsChecked = false;
            } else {
                Radio_Buyout.IsChecked = false;
                Radio_Price.IsChecked = true;
            }

            CheckBox_SendEnter.IsEnabled = Settings.flag_sendNote;
            Radio_Buyout.IsEnabled = Settings.flag_sendNote;
            Radio_Price.IsEnabled = Settings.flag_sendNote;
            TextBox_Delay.IsEnabled = Settings.flag_sendNote;

            if (ComboBox_Source.SelectedValue.ToString().ToLower() == "poe.ninja") {
                ComboBox_Method.IsEnabled = false;
                ComboBox_Method.SelectedIndex = -1;
            } else {
                ComboBox_Method.IsEnabled = true;
                ComboBox_Method.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Re-enables download button when user switches price methods
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox_Method_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (ComboBox_Method.SelectedItem != null) Button_Download.IsEnabled = true;
        }
    }
}
