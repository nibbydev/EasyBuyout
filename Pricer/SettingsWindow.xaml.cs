using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Pricer {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        private readonly MainWindow main;

        public SettingsWindow(MainWindow main) {
            this.main = main;

            InitializeComponent();

            foreach (string method in Settings.priceMethods) ComboBox_Method.Items.Add(method);
            foreach (string source in Settings.sourceList) ComboBox_Source.Items.Add(source);
            ComboBox_Method.SelectedIndex = 0;
            ComboBox_Source.SelectedIndex = 0;
            Settings.source = Settings.sourceList[0];
            Settings.method = Settings.priceMethods[0];
        }

        //-----------------------------------------------------------------------------------------------------------
        // Generic methods
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets active leagues and adds them to controls
        /// </summary>
        public void AddLeagues() {
            MainWindow.Log("Updating league list...", 0);
            string[] leagues = Utility.MiscMethods.GetLeagueList(main.GetWebClient());

            if (leagues == null) {
                MainWindow.Log("Unable to update leagues", 3);
                return;
            }

            Dispatcher.Invoke(() => {
                foreach (string league in leagues) {
                    ComboBox_League.Items.Add(league);
                }

                ComboBox_League.SelectedIndex = 0;
                Settings.league = ComboBox_League.SelectedValue.ToString();

                Button_Download.IsEnabled = true;
            });

            MainWindow.Log("League list updated", 0);
        }

        /// <summary>
        /// Reverts all settings back to original state when cancel button is pressed
        /// </summary>
        private void ResetOptions() {
            // Reset dropdown boxes
            ComboBox_League.SelectedValue = Settings.league;
            ComboBox_Method.SelectedValue = Settings.method;
            ComboBox_Source.SelectedValue = Settings.source;

            // Reset text fields
            TextBox_Delay.Text = Settings.pasteDelay.ToString();
            TextBox_LowerPrice.Text = Settings.lowerPricePercentage.ToString();

            // Reset checkboxe states
            CheckBox_Fallback.IsChecked = Settings.flag_fallback;
            CheckBox_SendEnter.IsChecked = Settings.flag_sendEnter;
            CheckBox_SendNote.IsChecked = Settings.flag_sendNote;
            CheckBox_ShowOverlay.IsChecked = Settings.flag_showOverlay;
            CheckBox_RunOnRightClick.IsChecked = Settings.flag_runOnRightClick;

            // Reset ~b/o radio states
            bool tempCheck1 = Settings.prefix == (string)Radio_Buyout.Content;
            Radio_Buyout.IsChecked = tempCheck1;
            Radio_Price.IsChecked = !tempCheck1;

            // Reset enabled states
            CheckBox_SendEnter.IsEnabled = Settings.flag_sendNote;
            Radio_Buyout.IsEnabled = Settings.flag_sendNote;
            Radio_Price.IsEnabled = Settings.flag_sendNote;
            TextBox_Delay.IsEnabled = Settings.flag_sendNote;

            // Reset method selection
            bool tempCheck2 = ComboBox_Source.SelectedValue.ToString().ToLower() == "poe.ninja";
            ComboBox_Method.IsEnabled = !tempCheck2;
            ComboBox_Method.SelectedIndex = tempCheck2 ? -1 : 0;
        }

        //-----------------------------------------------------------------------------------------------------------
        // WPF events
        //-----------------------------------------------------------------------------------------------------------

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
            int delay;
            Int32.TryParse(TextBox_Delay.Text, out delay);
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
            int percentage;
            Int32.TryParse(TextBox_LowerPrice.Text, out percentage);
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
            Settings.flag_showOverlay = (bool)CheckBox_ShowOverlay.IsChecked;
            Settings.flag_fallback = (bool)CheckBox_Fallback.IsChecked;
            Settings.flag_sendEnter = (bool)CheckBox_SendEnter.IsChecked;
            Settings.flag_sendNote = (bool)CheckBox_SendNote.IsChecked;
            Settings.flag_runOnRightClick = (bool)CheckBox_RunOnRightClick.IsChecked;

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

            MainWindow.Log("Downloading " + Settings.method + " price data for " + 
                Settings.league +  " from " + Settings.source, 0);

            Task.Run(() => {
                // Download and format price data
                main.GetPriceManager().Download();
                // Enable run button on MainWindow
                Application.Current.Dispatcher.Invoke(() => {
                    main.Button_Run.IsEnabled = true;
                    // Re-enable the button so user knows dl finished
                    Button_Download.IsEnabled = true;
                });

                MainWindow.Log("Download finished", 0);
            });
        }

        /// <summary>
        /// Enables/disables other controls based on checkbox
        /// </summary>
        private void CheckBox_ShowOverlay_Click(object sender, RoutedEventArgs e) {
            CheckBox_SendNote.IsChecked = false;

            CheckBox_SendEnter.IsEnabled = false;
            Radio_Buyout.IsEnabled = false;
            Radio_Price.IsEnabled = false;
            TextBox_Delay.IsEnabled = false;
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
        /// Re-enables download button when user switches price methods
        /// </summary>
        private void ComboBox_Method_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (ComboBox_Method.SelectedItem != null) Button_Download.IsEnabled = true;
        }
    }
}
