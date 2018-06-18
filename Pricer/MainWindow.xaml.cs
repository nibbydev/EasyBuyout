using Pricer.hooks;
using Pricer.Utility;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Pricer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private readonly WebClient webClient;
        private readonly PriceBox priceBox;
        private readonly SettingsWindow settingsWindow;
        private readonly PriceManager priceManager;
        private readonly Button runButton;
        private readonly UpdateWindow updateWindow;

        private static TextBox console;

        /// <summary>
        /// Initializes the form and sets event listeners
        /// </summary>
        public MainWindow() {
            // Initialize objects
            webClient = new WebClient() { Encoding = System.Text.Encoding.UTF8 };
            priceManager = new PriceManager(webClient);

            // Define eventhandlers
            ClipboardNotification.ClipboardUpdate += new EventHandler(Event_clipboard);
            MouseHook.MouseAction += new EventHandler(Event_mouse);

            // Initialize the UI components
            InitializeComponent();

            // Set objects that need to be accessed from outside
            console = console_window;
            runButton = Button_Run;
            priceBox = new PriceBox();
            settingsWindow = new SettingsWindow(this);
            updateWindow = new UpdateWindow(webClient);

            priceManager.SetProgressBar(settingsWindow.ProgressBar_Progress);

            // Set window title
            Title = Settings.programTitle + " (" + Settings.programVersion + ")";
            Log(Settings.programTitle + " (" + Settings.programVersion + ")" + " by Siegrest", 0);

            Task.Run(() => {
                // Get list of active leagues from official API
                settingsWindow.AddLeagues();
                // Check for updates now that we finished using the webclient
                updateWindow.Run();
            });
        }

        //-----------------------------------------------------------------------------------------------------------
        // Major event handlers
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Mouse event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_mouse (object sender, EventArgs e) {
            // Do not run if user has not pressed run button
            if (!Settings.flag_run || !Settings.flag_runOnRightClick) return;
            // Only run if "Path of Exile" is the main focused window
            if (WindowDiscovery.GetActiveWindowTitle() != Settings.activeWindowTitle) return;

            // Send Ctrl+C on mouse click
            KeyEmulator.SendCtrlC();
        }

        /// <summary>
        /// Clipboard event handler
        /// </summary>
        private void Event_clipboard (object sender, EventArgs e) {
            // Do not run if user has not pressed run button
            if (!Settings.flag_run && !Settings.flag_runOnRightClick) return;
            // Only run if "Path of Exile" is the main focused window
            if (WindowDiscovery.GetActiveWindowTitle() != Settings.activeWindowTitle) return;
            // At this point there should be text in the clipboard
            if (!Clipboard.ContainsText()) return;

            // TODO: check limits
            // Sleep to allow clipboard write action to finish
            Thread.Sleep(4);

            // Get clipboard contents
            string clipboardString = Clipboard.GetText();

            // Since this event handles *all* clipboard events AND we put the buyout note in the clipboard
            // then this event will also fire when we do that. So, to prevent an infinte loop, this is needed
            if (clipboardString.Contains("~b/o ") || clipboardString.Contains("~price "))
                Task.Run(() => ClipBoard_NotePasteTask());
            else
                Task.Run(() => ClipBoard_ItemParseTask(clipboardString));
        }

        /// <summary>
        /// Takes the clipboard data, parses it and depending whether the item was desirable, outputs data
        /// </summary>
        /// <param name="clipboardString">Item data from the clipboard</param>
        private void ClipBoard_ItemParseTask(string clipboardString) {
            Item item = new Item(clipboardString);
            item.ParseData();

            Console.WriteLine("key: " + item.key);

            // If the item was shite, discard it
            if (item.discard) {
                System.Media.SystemSounds.Asterisk.Play();

                switch (item.errorCode) {
                    case 1:
                        Log("Did not find any item data", 2);
                        break;
                    case 2:
                        Log("Unable to price unidentified items", 2);
                        break;
                    case 3:
                        Log("Unable to price items with notes", 2);
                        break;
                    case 4:
                        Log("Poe.ninja does not have any data for that item. Try poe-stats.com instead", 2);
                        break;
                }
                return;
            }

            // Attempt to get entry from database
            Entry entry = priceManager.Search(item.key);

            // If there were no matches
            if (entry == null) {
                // If user had enabled poeprices fallback
                if (Settings.flag_fallback) {
                    Log("No database entry found. Feeding item to PoePrices...", 0);

                    // If pricebox was enabled, display "Searching..." in it until a price is found
                    if (Settings.flag_showOverlay) {
                        Dispatcher.Invoke(() => {
                            priceBox.Content = "Searching...";
                            SetPriceBoxPosition();
                            priceBox.Show();
                        });
                    }

                    entry = priceManager.SearchPoePrices(item.GetRaw());
                }
            }

            // Display error
            if (entry == null && Settings.flag_showOverlay) {
                
                Dispatcher.Invoke(() => {
                    priceBox.Content = "No match...";
                    SetPriceBoxPosition();
                    priceBox.Show();
                });

                return;
            }

            // If the price is 0c
            if (entry.value == 0) {
                Log("Worth: 0c: " + item.key, 1);

                // If pricebox was enabled, display the value in it
                if (Settings.flag_showOverlay) {
                    Dispatcher.Invoke(() => {
                        priceBox.Content = "Value: 0c";
                        SetPriceBoxPosition();
                        priceBox.Show();
                    });
                }

                return;
            }

            // Display some info about some error codes, if any
            if (entry.value < 0) {
                // Play a warning sound
                System.Media.SystemSounds.Asterisk.Play();

                string errorMessage;
                int entryValue = (int)entry.value;
                switch (entryValue) {
                    case -1:
                        errorMessage = "Empty HTTP reply from PoePrices";
                        break;
                    case -2:
                        errorMessage = "Invalid item for PoePrices";
                        break;
                    case -3:
                        errorMessage = "No exalted conversion rate found for PoePrices";
                        break;
                    case -4:
                        errorMessage = "Unable to send request to PoePrices";
                        break;
                    default:
                        errorMessage = "Something went wrong with PoePrices";
                        break;
                }

                Log(errorMessage, 2);

                // If pricebox was enabled, display error in it
                if (Settings.flag_showOverlay) {
                    Dispatcher.Invoke(() => {
                        priceBox.Content = errorMessage;
                        SetPriceBoxPosition();
                        priceBox.Show();
                    });
                }

                return;
            }

            // Send a warning message when count is less than 10 as these items probably have inaccurate prices
            if (entry.quantity < 5 && !Settings.source.Equals("poe.ninja") && item.GetFrame() != 5) {
                System.Media.SystemSounds.Asterisk.Play();
                Log("Likely incorrect price (quantity: " + entry.quantity + ")", 1);
            }

            // Calculate prices
            double oldPrice = Math.Ceiling(entry.value * 2) / 2.0;
            double newPrice = Math.Ceiling(entry.value * (100 - Settings.lowerPricePercentage) / 100.0 * 2) / 2.0;

            string note = priceManager.MakeNote(newPrice);

            // If the LowerPriceByPercentage slider is more than 0, change output message
            if (Settings.lowerPricePercentage == 0)
                Log("[" + Settings.source + "] " + item.key + ": " + oldPrice + "c", 0);
            else
                Log("[" + Settings.source + "] " + item.key + ": " + oldPrice + "c -> " + newPrice + "c", 0);

            if (Settings.flag_showOverlay) {
                Dispatcher.Invoke(() => {
                    priceBox.Content = "Value: " + newPrice + "c";
                    SetPriceBoxPosition();
                    if (!priceBox.IsVisible) priceBox.Show();
                });

                return;
            }

            // Error code 2 means items already has a note. Can't overwrite it
            if (item.errorCode == 3) {
                Log("Item already has a note", 2);
                return;
            }

            // Raise the flag that indicates we will permit 1 buyout note to pass the clipboard event
            Settings.flag_clipBoardPaste = true;
            if (Settings.flag_sendNote) Dispatcher.Invoke(() => Clipboard.SetText(note));
        }

        /// <summary>
        /// Called via task, this method pastes the current clipboard contents and presses enter
        /// </summary>
        private void ClipBoard_NotePasteTask() {
            if (Settings.flag_clipBoardPaste)
                Settings.flag_clipBoardPaste = false;
            else
                return;

            Thread.Sleep(Settings.pasteDelay);

            // Paste clipboard contents
            System.Windows.Forms.SendKeys.SendWait("^v");

            // Send enter key if checkbox is checked
            if (Settings.flag_sendEnter) System.Windows.Forms.SendKeys.SendWait("{ENTER}");
        }

        //-----------------------------------------------------------------------------------------------------------
        // WPF event handlers
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Unhooks hooks on program exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            MouseHook.Stop();

            // Close app
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Run button event handler
        /// </summary>
        private void Button_Run_Click(object sender, RoutedEventArgs e) {
            if (Button_Run.Content.ToString() == "Run") {
                Button_Run.Content = "Pause";
                Settings.flag_run = true;
                Log("Service started", 0);
                MouseHook.Start();
            } else {
                Button_Run.Content = "Run";
                Settings.flag_run = false;
                Log("Service paused", 0);
            }
        }

        /// <summary>
        /// Calculates position and opens settings window
        /// </summary>
        private void Button_Settings_Click(object sender, RoutedEventArgs e) {
            settingsWindow.Left = Left + Width / 2 - settingsWindow.Width / 2;
            settingsWindow.Top = Top + Height / 2 - settingsWindow.Height / 2;
            settingsWindow.ShowDialog();
        }

        /// <summary>
        /// Prevents main window from resizing whenever anything is written to main textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            // Reset the SizeToContent property
            SizeToContent = SizeToContent.Manual;

            // Position window to screen center manually
            try {
                Rect rect = SystemParameters.WorkArea;
                this.Left = (rect.Width - Width) / 2 + rect.Left;
                this.Top = (rect.Height - Height) / 2 + rect.Top;
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        //-----------------------------------------------------------------------------------------------------------
        // Generic methods
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Prints text to window in console-like fashion, prefixes a timestamp
        /// </summary>
        /// <param name="str">String to print</param>
        /// <param name="status">Status code to indicate INFO/WARN/ERROR/CRITICAL</param>
        public static void Log(string str, int status) {
            string prefix;

            switch (status) {
                default:
                case 0:
                    prefix = "[INFO] ";
                    break;
                case 1:
                    prefix = "[WARN] ";
                    break;
                case 2:
                    prefix = "[ERROR] ";
                    break;
                case 3:
                    prefix = "[CRITICAL] ";
                    break;
            }

            string time = string.Format("{0:HH:mm:ss}", DateTime.Now);

            Application.Current.Dispatcher.Invoke(() => {
                console.AppendText("[" + time + "]" + prefix + str + "\n");
                console.ScrollToEnd();
            });
        }

        /// <summary>
        /// Set the position of the price overlay under the user's cursor
        /// </summary>
        private void SetPriceBoxPosition() {
            priceBox.Left = System.Windows.Forms.Cursor.Position.X - priceBox.Width / 2;
            priceBox.Top = System.Windows.Forms.Cursor.Position.Y - priceBox.Height / 2;
        }

        //-----------------------------------------------------------------------------------------------------------
        // Getters and setters
        //-----------------------------------------------------------------------------------------------------------

        public PriceManager GetPriceManager() {
            return priceManager;
        }

        public WebClient GetWebClient() {
            return webClient;
        }
    }
}
