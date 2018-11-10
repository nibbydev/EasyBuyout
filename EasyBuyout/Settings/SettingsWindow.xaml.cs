using EasyBuyout.League;
using EasyBuyout.Prices;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace EasyBuyout.Settings {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow {
        private readonly ManualLeagueWindow _manualLeagueWindow;
        private readonly Config _config;

        private readonly Action<bool> _setStartButtonState;
        private readonly Action<string, MainWindow.Flair> _log;
        public Action<string> Download;

        private readonly LeagueManager _leagueManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        /// <param name="setSetStartButtonState"></param>
        /// <param name="log"></param>
        /// <param name="webClient"></param>
        public SettingsWindow(Config config, Action<bool> setSetStartButtonState, Action<string, MainWindow.Flair> log,
            WebClient webClient) {
            _config = config;
            _setStartButtonState = setSetStartButtonState;
            _log = log;

            // Initialize the UI components
            InitializeComponent();

            // Instantiate objects
            _manualLeagueWindow = new ManualLeagueWindow();
            _leagueManager = new LeagueManager(config, webClient, _log);

            // Add initial values to PricePrecision dropdown
            for (int i = 0; i < 4; i++) {
                ComboBox_PricePrecision.Items.Add(i);
            }

            ComboBox_League.SelectedIndex = 0;

            // Set window options to default values
            ResetOptions();
        }

        //-----------------------------------------------------------------------------------------------------------
        // Generic methods
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Adds provided league names to league selector
        /// </summary>
        public void UpdateLeagues() {
            _log("Updating league list...", MainWindow.Flair.Info);

            var leagues = _leagueManager.GetLeagueList();
            if (leagues == null) {
                _log("Unable to update leagues", MainWindow.Flair.Error);
                return;
            }

            Application.Current.Dispatcher.Invoke(() => {
                foreach (var league in leagues) {
                    ComboBox_League.Items.Add(league);
                }

                ComboBox_League.Items.Add(_config.ManualLeagueDisplay);
                ComboBox_League.SelectedIndex = 0;
                Button_Download.IsEnabled = true;

                _log("League list updated", MainWindow.Flair.Info);
            });
        }

        /// <summary>
        /// Reverts all settings back to original state when cancel button is pressed
        /// </summary>
        private void ResetOptions() {
            // Reset dropdown boxes
            if (_config.SelectedLeague != null) {
                ComboBox_League.SelectedValue = _config.SelectedLeague;
            }

            ComboBox_PricePrecision.SelectedValue = _config.PricePrecision;

            // Reset text fields
            TextBox_Delay.Text = _config.PasteDelay.ToString();
            TextBox_LowerPrice.Text = _config.LowerPricePercentage.ToString();

            // Reset checkbox states
            CheckBox_SendEnter.IsChecked = _config.FlagSendEnter;
            Radio_SendNote.IsChecked = _config.FlagSendNote;
            Radio_ShowOverlay.IsChecked = _config.FlagShowOverlay;
            CheckBox_LiveUpdate.IsChecked = _config.FlagLiveUpdate;

            // Reset ~b/o radio states
            var tmp = _config.NotePrefix == (string) Radio_Buyout.Content;
            Radio_Buyout.IsChecked = tmp;
            Radio_Price.IsChecked = !tmp;

            // Reset enabled states
            CheckBox_SendEnter.IsEnabled = _config.FlagSendNote;
            Radio_Buyout.IsEnabled = _config.FlagSendNote;
            Radio_Price.IsEnabled = _config.FlagSendNote;
            TextBox_Delay.IsEnabled = _config.FlagSendNote;
        }

        /// <summary>
        /// Opens dialog allowing user to manually input league
        /// </summary>
        public string DisplayManualLeagueInputDialog() {
            _manualLeagueWindow.ShowDialog();
            return _manualLeagueWindow.input;
        }

        /// <summary>
        /// Increment the progressbar by one step
        /// </summary>
        public void IncProgressBar() {
            if (ProgressBar_Progress == null) {
                return;
            }

            Application.Current.Dispatcher.Invoke(() => ++ProgressBar_Progress.Value);
        }

        /// <summary>
        /// Set initial progressbar values
        /// </summary>
        /// <param name="size">Progress step count</param>
        public void ConfigureProgressBar(int size) {
            if (ProgressBar_Progress == null) {
                return;
            }

            Application.Current.Dispatcher.Invoke(() => {
                ProgressBar_Progress.Maximum = size;
                ProgressBar_Progress.Value = 0;
            });
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
            int.TryParse(TextBox_Delay.Text, out var newPasteDelay);
            if (newPasteDelay != _config.PasteDelay) {
                if (newPasteDelay < 1 || newPasteDelay > 1000) {
                    _log("Invalid input - delay (allowed: 1 - 1000)", MainWindow.Flair.Warn);
                    TextBox_Delay.Text = _config.PasteDelay.ToString();
                } else {
                    _log($"Changed delay {_config.PasteDelay} -> {newPasteDelay}", MainWindow.Flair.Info);
                    _config.PasteDelay = newPasteDelay;
                }
            }

            // Lower price % box
            int.TryParse(TextBox_LowerPrice.Text, out var newLowerPercentage);
            if (newLowerPercentage != _config.LowerPricePercentage) {
                if (newLowerPercentage < 0 || newLowerPercentage > 100) {
                    _log("Invalid input - percentage (allowed: 0 - 100)", MainWindow.Flair.Warn);
                    TextBox_LowerPrice.Text = _config.LowerPricePercentage.ToString();
                } else {
                    _log($"Changed percentage {_config.LowerPricePercentage} -> {newLowerPercentage}",
                        MainWindow.Flair.Info);
                    _config.LowerPricePercentage = newLowerPercentage;
                }
            }

            // Dropdowns
            if (_config.PricePrecision != (int) ComboBox_PricePrecision.SelectedValue) {
                _log($"Changed price precision {_config.PricePrecision} -> {ComboBox_PricePrecision.SelectedValue}",
                    MainWindow.Flair.Info);
                _config.PricePrecision = (int) ComboBox_PricePrecision.SelectedValue;
            }

            // Checkboxes
            _config.FlagShowOverlay = Radio_ShowOverlay.IsChecked ?? false;
            _config.FlagSendEnter = CheckBox_SendEnter.IsChecked ?? false;
            _config.FlagSendNote = Radio_SendNote.IsChecked ?? false;
            _config.FlagLiveUpdate = CheckBox_LiveUpdate.IsChecked ?? false;

            // Radio buttons
            _config.NotePrefix = Radio_Buyout.IsChecked != null && (bool) Radio_Buyout.IsChecked
                ? Radio_Buyout.Content.ToString()
                : Radio_Price.Content.ToString();

            Hide();
        }

        /// <summary>
        /// Download price data on button press
        /// </summary>
        private void Button_Download_Click(object sender, RoutedEventArgs e) {
            _config.SelectedLeague = (string) ComboBox_League.SelectedValue;

            if (_config.SelectedLeague == _config.ManualLeagueDisplay) {
                _config.SelectedLeague = DisplayManualLeagueInputDialog();

                if (_config.SelectedLeague == null) {
                    return;
                }
            }

            Button_Download.IsEnabled = false;

            Task.Run(() => {
                _log($"Downloading data for {_config.SelectedLeague}", MainWindow.Flair.Info);

                // Download price data
                Download(_config.SelectedLeague);

                // Enable run button on MainWindow
                _setStartButtonState(true);
                Application.Current.Dispatcher.Invoke(() => { Button_Download.IsEnabled = true; });

                _log("Download finished", MainWindow.Flair.Info);
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
            CheckBox_SendEnter.IsEnabled = true;
            Radio_Buyout.IsEnabled = true;
            Radio_Price.IsEnabled = true;
            TextBox_Delay.IsEnabled = true;
        }
    }
}