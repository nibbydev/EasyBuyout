using EasyBuyout.hooks;
using EasyBuyout.Prices;
using EasyBuyout.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using EasyBuyout;
using EasyBuyout.League;
using EasyBuyout.Updater;

namespace EasyBuyout
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public enum Flair
        {
            Info,
            Warn,
            Error,
            Critical
        }

        private readonly SettingsWindow _settingsWindow;
        private readonly PriceboxWindow _priceBox;
        private readonly PriceManager   _priceManager;
        private readonly Config         _config;
        private static   TextBox        _console;
        private volatile bool           _flagClipBoardPaste;

        private readonly LeagueManager      _leagueManager;
        private readonly ManualLeagueWindow _manualLeagueWindow;

        /// <summary>
        /// Initializes the form and sets event listeners
        /// </summary>
        public MainWindow()
        {
            _config      = new Config();

            // Web client setup
            var webClient = new WebClient {Encoding = System.Text.Encoding.UTF8};
            webClient.Headers.Add("user-agent", $"{_config.ProgramTitle} {_config.ProgramVersion}");
            ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            // Define event handlers
            ClipboardNotification.ClipboardUpdate += Event_clipboard;
            MouseHook.MouseAction                 += Event_mouse;

            // Initialize the UI components
            InitializeComponent();

            // Set objects that need to be accessed from outside
            _console = console_window;

            // Object setup
            _settingsWindow = new SettingsWindow(_config, Log);
            _priceManager   = new PriceManager(_config, webClient, Log);
            _priceBox       = new PriceboxWindow();
            var updateWindow = new UpdateWindow(_config, webClient, Log);
            _leagueManager = new LeagueManager(_config, webClient, Log);

            // Set window title
            Title = $"{_config.ProgramTitle} {_config.ProgramVersion}";
            Log($"{_config.ProgramTitle} {_config.ProgramVersion} by Siegrest");

            Task.Run(() =>
            {
                // Check for updates
                updateWindow.Run();
                // Query PoE API for active league list and add them to settings selectors
                UpdateLeagues();
            });
        }

        private bool _checkActive = false;
        //-----------------------------------------------------------------------------------------------------------
        // External event handlers
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Mouse event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_mouse()
        {
            // Do not run if user has not pressed run button
            if (!_config.FlagRun)
            {
                return;
            }

            _checkActive = true;
            Task.Delay(100).ContinueWith(t => _checkActive = false);

            // Send Ctrl+C on mouse click
            KeyEmulator.SendCtrlC();
        }

        /// <summary>
        /// Clipboard event handler
        /// </summary>
        private void Event_clipboard(object sender, EventArgs e)
        {
            // Do not run if user has not pressed run button
            if (!_config.FlagRun || !_checkActive)
            {
                return;
            }

            // At this point there should be text in the clipboard
            if (!Clipboard.ContainsText())
            {
                return;
            }

            // TODO: check limits
            // Sleep to allow clipboard write action to finish
            Thread.Sleep(_config.ClipboardWriteDelay);

            // Get clipboard contents
            var clipboardString = string.Empty;
            try
            {
                clipboardString = Clipboard.GetText();
            }
            catch (Exception exception)
            {
                //awakened poe trade came first. probably should check if right mouse has been pressed shortly before to prevent this kind of fetching
                //Log(exception.ToString());
            }

            // This event handles *all* clipboard events
            if (clipboardString.StartsWith("~"))
            {
                Task.Run(() => ClipBoard_NotePasteTask());
            }
            else
            {
                Task.Run(() => ClipBoard_ItemParseTask(clipboardString));
            }
        }

        /// <summary>
        /// Takes the clipboard data, parses it and depending whether the item was desirable, outputs data
        /// </summary>
        /// <param name="clipboardString">Item data from the clipboard</param>
        private void ClipBoard_ItemParseTask(string clipboardString)
        {
            var item = new Item.Item(clipboardString);

            // If the item was shite, discard it
            if (item.Discard)
            {
                //System.Media.SystemSounds.Asterisk.Play();

                foreach (var error in item.Errors)
                {
                    Log(error, Flair.Error);
                }

                return;
            }

            // Get entries associated with item keys
            var entry = _priceManager.GetEntry(item.Key);

            // Display error
            if (entry == null)
            {
                Log($"No match for: {item.Key}", Flair.Warn);

                if (!_config.FlagSendNote)
                {
                    DisplayPriceBox("No match");
                }

                return;
            }

            double price;
            if (_config.LowerPricePercentage > -100)
            {
                price = (entry.Value * (100 - _config.LowerPricePercentage)) / 100.0;
            }
            else
            {
                price = entry.Value;
            }

            // Round price
            var pow = Math.Pow(10, _config.PricePrecision);
            price = Math.Round(price * pow) / pow;

            // Replace "," with "." due to game limitations
            var strPrice = price.ToString().Replace(',', '.');
            var note     = $"{_config.NotePrefix} {strPrice} chaos";

            // Log item price to main console
            Log($"{item.Key}: {price} chaos");

            if (!_config.FlagSendNote)
            {
                DisplayPriceBox($"{price} chaos");
                return;
            }

            // Raise flag allowing next cb event to be processed
            if (_config.FlagSendNote)
            {
                _flagClipBoardPaste = true;
                Dispatcher.Invoke(() => Clipboard.SetText(note));
            }
        }

        /// <summary>
        /// Called via task, this method pastes the current clipboard contents and presses enter
        /// </summary>
        private void ClipBoard_NotePasteTask()
        {
            if (_flagClipBoardPaste)
            {
                _flagClipBoardPaste = false;
            }
            else
            {
                return;
            }

            Thread.Sleep(_config.PasteDelay);

            // Paste clipboard contents
            System.Windows.Forms.SendKeys.SendWait("^v");

            // Send enter key if checkbox is checked
            if (_config.FlagSendEnter)
            {
                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
            }

            _flagClipBoardPaste = false;
        }

        //-----------------------------------------------------------------------------------------------------------
        // WPF Event handlers
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Unhooks hooks on program exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MouseHook.Stop();
            Application.Current.Shutdown();
        }

        /// <summary>
        /// GetLeagueList button event handler
        /// </summary>
        private void Button_Run_Click(object sender, RoutedEventArgs e)
        {
            if (Button_Run.Content.ToString() == "Run")
            {
                Button_Run.Content = "Pause";
                _config.FlagRun    = true;
                Log("Service started");
                MouseHook.Start();
            }
            else
            {
                Button_Run.Content = "Run";
                _config.FlagRun    = false;
                Log("Service paused");
            }
        }

        /// <summary>
        /// Calculates position and opens settings window
        /// </summary>
        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            _settingsWindow.Left = Left + Width  / 2 - _settingsWindow.Width  / 2;
            _settingsWindow.Top  = Top  + Height / 2 - _settingsWindow.Height / 2;
            _settingsWindow.ShowDialog();
        }

        /// <summary>
        /// Prevents main window from resizing whenever anything is written to main textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Reset the SizeToContent property
            SizeToContent = SizeToContent.Manual;

            // Position window to screen center manually
            try
            {
                var rect = SystemParameters.WorkArea;
                Left = (rect.Width  - Width)  / 2 + rect.Left;
                Top  = (rect.Height - Height) / 2 + rect.Top;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        //-----------------------------------------------------------------------------------------------------------
        // Other
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Set the content, position and visibility of the pricebox with one method
        /// </summary>
        /// <param name="content">String to be displayed in the overlay</param>
        public void DisplayPriceBox(string content)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _priceBox.Content = content;
                _priceBox.SetPosition();
                _priceBox.Show();
            });
        }

        /// <summary>
        /// Prints text to window in console-like fashion, prefixes a timestamp
        /// </summary>
        /// <param name="msg">String to print</param>
        /// <param name="flair">Status code of message</param>
        public void Log(string msg, Flair flair = Flair.Info)
        {
            var prefix = "";

            switch (flair)
            {
                case Flair.Info:
                    prefix = "INFO";
                    break;
                case Flair.Warn:
                    prefix = "WARN";
                    break;
                case Flair.Error:
                    prefix = "ERROR";
                    break;
                case Flair.Critical:
                    prefix = "CRITICAL";
                    break;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                _console.AppendText($"[{DateTime.Now:HH:mm:ss}][{prefix}] {msg}\n");
                _console.ScrollToEnd();
            });
        }

        /// <summary>
        /// Adds provided league names to league selector
        /// </summary>
        public void UpdateLeagues()
        {
            Log("Updating league list...");

            var leagues = _leagueManager.GetLeagueList();
            if (leagues == null)
            {
                Log("Unable to update leagues");

                ComboBox_League.Items.Add(_config.ManualLeagueDisplay);
                ComboBox_League.SelectedIndex = 0;

                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var league in leagues)
                {
                    ComboBox_League.Items.Add(league);
                }

                ComboBox_League.Items.Add(_config.ManualLeagueDisplay);
                var index = ComboBox_League.Items.IndexOf(_config.SelectedLeague);
                ComboBox_League.SelectedIndex = index != -1 ? index : 0;
                Log("League list updated");
            });
        }

        private void Download()
        {
            Task.Run(() =>
            {
                Log($"Downloading data for {_config.SelectedLeague}");

                _priceManager.Download();


                Log("Download finished");
            });
        }


        private void ComboBox_League_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _config.SelectedLeague = (string) ComboBox_League.SelectedValue;
            // User has chosen to set league manually
            if (_config.SelectedLeague == _config.ManualLeagueDisplay)
            {
                var manualLeagueWindow = new ManualLeagueWindow();
                manualLeagueWindow.ShowDialog();

                if (string.IsNullOrEmpty(manualLeagueWindow.input))
                {
                    Log("Invalid league", Flair.Error);
                    return;
                }

                _config.SelectedLeague = manualLeagueWindow.input;
            }
            Download();
        }
    }
}