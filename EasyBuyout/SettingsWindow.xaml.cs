using EasyBuyout.League;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EasyBuyout {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        private readonly MainWindow main;
        private readonly LeagueManager leagueManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="main"></param>
        /// <param name="leagueManager"></param>
        public SettingsWindow(MainWindow main, LeagueManager leagueManager) {
            this.main = main;
            this.leagueManager = leagueManager;

            InitializeComponent();

            foreach (string source in Settings.sourceList) ComboBox_Source.Items.Add(source);
            ComboBox_Source.SelectedIndex = 0;
            Settings.source = Settings.sourceList[0];
        }

        //-----------------------------------------------------------------------------------------------------------
        // Generic methods
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets active leagues and adds them to controls
        /// </summary>
        public void AddLeagues() {
            Dispatcher.Invoke(() => {
                if (leagueManager.GetLeagues() != null) {
                    foreach (string league in leagueManager.GetLeagues()) {
                        ComboBox_League.Items.Add(league);
                    }
                }

                ComboBox_League.Items.Add(Settings.manualLeagueDisplay);

                ComboBox_League.SelectedIndex = 0;
                leagueManager.SetSelectedLeague(ComboBox_League.SelectedValue.ToString());

                Button_Download.IsEnabled = true;
            });
        }

        /// <summary>
        /// Reverts all settings back to original state when cancel button is pressed
        /// </summary>
        private void ResetOptions() {
            // Reset dropdown boxes
            ComboBox_League.SelectedValue = leagueManager.GetSelectedLeague();
            ComboBox_Source.SelectedValue = Settings.source;

            // Reset text fields
            TextBox_Delay.Text = Settings.pasteDelay.ToString();
            TextBox_LowerPrice.Text = Settings.lowerPricePercentage.ToString();

            // Reset checkbox states
            CheckBox_Fallback.IsChecked = Settings.flag_fallback;
            CheckBox_SendEnter.IsChecked = Settings.flag_sendEnter;
            Radio_SendNote.IsChecked = Settings.flag_sendNote;
            Radio_ShowOverlay.IsChecked = Settings.flag_showOverlay;
            CheckBox_RunOnRightClick.IsChecked = Settings.flag_runOnRightClick;
            CheckBox_IncludeEnchant.IsChecked = Settings.flag_includeEnchantment;

            // Reset ~b/o radio states
            bool tempCheck1 = Settings.prefix == (string)Radio_Buyout.Content;
            Radio_Buyout.IsChecked = tempCheck1;
            Radio_Price.IsChecked = !tempCheck1;

            // Reset enabled states
            CheckBox_SendEnter.IsEnabled = Settings.flag_sendNote;
            Radio_Buyout.IsEnabled = Settings.flag_sendNote;
            Radio_Price.IsEnabled = Settings.flag_sendNote;
            TextBox_Delay.IsEnabled = Settings.flag_sendNote;
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
            Settings.flag_showOverlay = (bool)Radio_ShowOverlay.IsChecked;
            Settings.flag_fallback = (bool)CheckBox_Fallback.IsChecked;
            Settings.flag_sendEnter = (bool)CheckBox_SendEnter.IsChecked;
            Settings.flag_sendNote = (bool)Radio_SendNote.IsChecked;
            Settings.flag_runOnRightClick = (bool)CheckBox_RunOnRightClick.IsChecked;
            Settings.flag_includeEnchantment = (bool)CheckBox_IncludeEnchant.IsChecked;

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
        }

        /// <summary>
        /// Download price data on button press
        /// </summary>
        private void Button_Download_Click(object sender, RoutedEventArgs e) {
            Button_Download.IsEnabled = false;

            string source = (string)ComboBox_Source.SelectedValue;
            string league = (string)ComboBox_League.SelectedValue;

            if (league == Settings.manualLeagueDisplay) {
                leagueManager.DisplayManualInputWindow();
                league = leagueManager.GetSelectedLeague();
            }

            leagueManager.SetSelectedLeague(league);

            Task.Run(() => {
                MainWindow.Log("Downloading data for " + league + " from " + source, 0);

                // Download and format price data
                main.GetPriceManager().Download(source, league);

                // Enable run button on MainWindow
                Application.Current.Dispatcher.Invoke(() => {
                    main.Button_Run.IsEnabled = true;
                    Button_Download.IsEnabled = true;
                });

                MainWindow.Log("Download finished", 0);
            });
        }

        /// <summary>
        /// Cancel button handler
        /// </summary>
        private void Button_Cancel_Click(object sender, RoutedEventArgs e) {
            ResetOptions();
            Hide();
        }

        /// <summary>
        /// Enables/disables other controls based on checkbox
        /// </summary>
        private void Radio_ShowOverlay_Click(object sender, RoutedEventArgs e) {
            CheckBox_SendEnter.IsEnabled = false;
            Radio_Buyout.IsEnabled = false;
            Radio_Price.IsEnabled = false;
            TextBox_Delay.IsEnabled = false;
        }

        /// <summary>
        /// Enables/disables other controls based on checkbox
        /// </summary>
        private void Radio_SendNote_Click(object sender, RoutedEventArgs e) {
            CheckBox_SendEnter.IsEnabled = (bool)Radio_SendNote.IsChecked;
            Radio_Buyout.IsEnabled = (bool)Radio_SendNote.IsChecked;
            Radio_Price.IsEnabled = (bool)Radio_SendNote.IsChecked;
            TextBox_Delay.IsEnabled = (bool)Radio_SendNote.IsChecked;
        }
    }
}
