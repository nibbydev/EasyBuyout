using EasyBuyout.League;
using EasyBuyout.Prices;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EasyBuyout.Settings {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        private readonly MainWindow main;
        private readonly LeagueManager leagueManager;
        private readonly PriceManager priceManager;

        private string notePrefix = "~b/o";
        private string selectedSource;
        private string selectedLeague;
        private int lowerPricePercentage = 0;
        private int pasteDelay = 120;
        private bool flag_sendNote = true;
        private bool flag_sendEnter = true;
        private bool flag_fallback = true;
        private bool flag_showOverlay = false;
        private bool flag_runOnRightClick = true;
        private bool flag_includeEnchantment = false;
        private bool flag_liveUpdate = true;

        

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="main"></param>
        /// <param name="leagueManager"></param>
        public SettingsWindow(MainWindow main, LeagueManager leagueManager, PriceManager priceManager) {
            this.leagueManager = leagueManager;
            this.priceManager = priceManager;
            this.main = main;

            InitializeComponent();

            foreach (string source in Config.sourceList) ComboBox_Source.Items.Add(source);
            ComboBox_Source.SelectedIndex = 0;
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

                ComboBox_League.Items.Add(Config.manualLeagueDisplay);

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
            ComboBox_Source.SelectedValue = selectedSource;

            // Reset text fields
            TextBox_Delay.Text = pasteDelay.ToString();
            TextBox_LowerPrice.Text = lowerPricePercentage.ToString();

            // Reset checkbox states
            CheckBox_Fallback.IsChecked = flag_fallback;
            CheckBox_SendEnter.IsChecked = flag_sendEnter;
            Radio_SendNote.IsChecked = flag_sendNote;
            Radio_ShowOverlay.IsChecked = flag_showOverlay;
            CheckBox_RunOnRightClick.IsChecked = flag_runOnRightClick;
            CheckBox_IncludeEnchant.IsChecked = flag_includeEnchantment;
            CheckBox_LiveUpdate.IsChecked = flag_liveUpdate;

            // Reset ~b/o radio states
            bool tempCheck1 = notePrefix == (string)Radio_Buyout.Content;
            Radio_Buyout.IsChecked = tempCheck1;
            Radio_Price.IsChecked = !tempCheck1;

            // Reset enabled states
            CheckBox_SendEnter.IsEnabled = flag_sendNote;
            Radio_Buyout.IsEnabled = flag_sendNote;
            Radio_Price.IsEnabled = flag_sendNote;
            TextBox_Delay.IsEnabled = flag_sendNote;
        }

        /// <summary>
        /// Opens dialog allowing user to manually input league
        /// </summary>
        public string DisplayManualLeagueInputDialog() {
            ManualLeagueWindow manualLeagueWindow = new ManualLeagueWindow();
            manualLeagueWindow.ShowDialog();

            return manualLeagueWindow.input;
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
            // Delay box
            int delay;
            Int32.TryParse(TextBox_Delay.Text, out delay);
            if (delay != pasteDelay) {
                if (delay < 1 || delay > 500) {
                    MainWindow.Log("Invalid input - delay (allowed: 1 - 500)", 2);
                    TextBox_Delay.Text = pasteDelay.ToString();
                } else {
                    MainWindow.Log("Changed delay " + pasteDelay + " -> " + delay, 0);
                    pasteDelay = delay;
                }
            }

            // Lower price % box
            int percentage;
            Int32.TryParse(TextBox_LowerPrice.Text, out percentage);
            if (percentage != lowerPricePercentage) {
                if (percentage < 0 || percentage > 100) {
                    MainWindow.Log("Invalid input - percentage (allowed: 0 - 100)", 2);
                    TextBox_LowerPrice.Text = lowerPricePercentage.ToString();
                } else {
                    MainWindow.Log("Changed percentage " + lowerPricePercentage + " -> " + percentage, 0);
                    lowerPricePercentage = percentage;
                }
            }

            // Checkboxes
            flag_showOverlay = (bool)Radio_ShowOverlay.IsChecked;
            flag_fallback = (bool)CheckBox_Fallback.IsChecked;
            flag_sendEnter = (bool)CheckBox_SendEnter.IsChecked;
            flag_sendNote = (bool)Radio_SendNote.IsChecked;
            flag_runOnRightClick = (bool)CheckBox_RunOnRightClick.IsChecked;
            flag_includeEnchantment = (bool)CheckBox_IncludeEnchant.IsChecked;
            flag_liveUpdate = (bool)CheckBox_LiveUpdate.IsChecked;

            // Radio buttons
            if ((bool)Radio_Buyout.IsChecked) {
                notePrefix = Radio_Buyout.Content.ToString();
            } else {
                notePrefix = Radio_Price.Content.ToString();
            }

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
            selectedSource = (string)ComboBox_Source.SelectedValue;
            selectedLeague = (string)ComboBox_League.SelectedValue;

            if (selectedLeague == Config.manualLeagueDisplay) {
                selectedLeague = DisplayManualLeagueInputDialog();
                if (selectedLeague == null) return;
            }

            Button_Download.IsEnabled = false;
            leagueManager.SetSelectedLeague(selectedLeague);

            Task.Run(() => {
                MainWindow.Log("Downloading data for " + selectedLeague + " from " + selectedSource, 0);

                // Download and format price data
                priceManager.Download(selectedSource, selectedLeague);

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

        //-----------------------------------------------------------------------------------------------------------
        // Getters and Setters
        //-----------------------------------------------------------------------------------------------------------

        public string GetNotePrefix() {
            return notePrefix;
        }

        public string GetSelectedSource() {
            return selectedSource;
        }

        public string GetSelectedLeague() {
            return selectedLeague;
        }

        public int GetLowerPricePercentage() {
            return lowerPricePercentage;
        }

        public int GetPasteDelay() {
            return pasteDelay;
        }

        public bool IsSendNote() {
            return flag_sendNote;
        }

        public bool IsSendEnter() {
            return flag_sendEnter;
        }

        public bool IsFallBack() {
            return flag_fallback;
        }

        public bool IsShowOverlay() {
            return flag_showOverlay;
        }

        public bool IsRunOnRightClick() {
            return flag_runOnRightClick;
        }

        public bool IsIncludeEnchant() {
            return flag_includeEnchantment;
        }

        public bool IsLiveUpdate() {
            return flag_liveUpdate;
        }
    }
}
