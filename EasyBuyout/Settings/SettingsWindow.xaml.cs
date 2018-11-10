using System;
using System.Windows;

namespace EasyBuyout.Settings {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow {
        private readonly Config _config;
        private readonly Action<string, MainWindow.Flair> _log;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        /// <param name="log"></param>
        public SettingsWindow(Config config, Action<string, MainWindow.Flair> log) {
            _config = config;
            _log = log;

            // Initialize the UI components
            InitializeComponent();

            // Add initial values to PricePrecision dropdown
            for (int i = 0; i < 4; i++) {
                ComboBox_PricePrecision.Items.Add(i);
            }

            ComboBox_PricePrecision.SelectedIndex = 0;

            // Set window options to default values
            ResetOptions();
        }

        /// <summary>
        /// Reverts all settings back to original state when cancel button is pressed
        /// </summary>
        private void ResetOptions() {
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