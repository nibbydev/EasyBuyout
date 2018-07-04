using EasyBuyout.hooks;
using EasyBuyout.League;
using EasyBuyout.Prices;
using EasyBuyout.Settings;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace EasyBuyout {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private readonly WebClient webClient;
        private readonly SettingsWindow settingsWindow;
        private readonly PriceManager priceManager;
        private readonly UpdateWindow updateWindow;
        private readonly LeagueManager leagueManager;

        private static TextBox console;
        private volatile bool flag_clipBoardPaste = false;
        private volatile bool flag_run = false;

        /// <summary>
        /// Initializes the form and sets event listeners
        /// </summary>
        public MainWindow() {
            // Initialize objects
            webClient = new WebClient() { Encoding = System.Text.Encoding.UTF8 };
            leagueManager = new LeagueManager(webClient);
            updateWindow = new UpdateWindow(webClient);
            priceManager = new PriceManager(webClient, leagueManager);
            settingsWindow = new SettingsWindow(this, leagueManager, priceManager);

            priceManager.SetProgressBar(settingsWindow.ProgressBar_Progress);
            priceManager.SetSettingsWindow(settingsWindow);

            // Define eventhandlers
            ClipboardNotification.ClipboardUpdate += new EventHandler(Event_clipboard);
            MouseHook.MouseAction += new EventHandler(Event_mouse);

            // Initialize the UI components
            InitializeComponent();

            // Set objects that need to be accessed from outside
            console = console_window;
            
            // Set window title
            Title = Config.programTitle + " " + Config.programVersion;
            Log(Config.programTitle + " " + Config.programVersion + " by Siegrest", 0);

            Task.Run(() => {
                // Get list of active leagues from official API
                leagueManager.Run();
                // Add those leagues to settings window
                settingsWindow.AddLeagues();
                // Check for updates now that we finished using the webclient
                if (Config.flag_updaterEnabled) updateWindow.Run();
            });
        }

        //-----------------------------------------------------------------------------------------------------------
        // External event handlers
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Mouse event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_mouse(object sender, EventArgs e) {
            // Do not run if user has not pressed run button
            if (!flag_run || !settingsWindow.IsRunOnRightClick()) return;
            // Only run if "Path of Exile" is the main focused window
            if (WindowDiscovery.GetActiveWindowTitle() != Config.activeWindowTitle) return;

            // Send Ctrl+C on mouse click
            KeyEmulator.SendCtrlC();
        }

        /// <summary>
        /// Clipboard event handler
        /// </summary>
        private void Event_clipboard(object sender, EventArgs e) {
            // Do not run if user has not pressed run button
            if (!flag_run && !settingsWindow.IsRunOnRightClick()) return;
            // Only run if "Path of Exile" is the main focused window
            if (WindowDiscovery.GetActiveWindowTitle() != Config.activeWindowTitle) return;
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
            priceManager.RefreshLastUseTime();

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

            // Get entries associated with item keys
            Entry[] entries = priceManager.Search(new string[] { item.key, item.enchantKey });
            Entry itemEntry = entries[0];
            Entry enchantEntry = entries[1];

            // Form enchantEntry's displaystring
            string enchantDisplay = "";
            if (settingsWindow.IsIncludeEnchant()) {
                if (enchantEntry != null && enchantEntry.value > 0) {
                    enchantDisplay += "\nEnchant: " + enchantEntry.value + "c";
                }
            }

            // If there were no matches
            if (itemEntry == null) {
                // If user had enabled poeprices fallback
                if (settingsWindow.IsFallBack()) {
                    Log("No database entry found. Feeding item to PoePrices...", 0);

                    // If pricebox was enabled, display "Searching..." in it until a price is found
                    if (settingsWindow.IsShowOverlay()) {
                        priceManager.DisplayPriceBox("Searching...");
                    }

                    itemEntry = priceManager.SearchPoePrices(item.GetRaw());
                }
            }

            // Display error
            if (itemEntry == null && settingsWindow.IsShowOverlay()) {
                priceManager.DisplayPriceBox("Item: No match..." + enchantDisplay);
                return;
            }

            // Display some info about some error codes, if any
            if (itemEntry.value < 0) {
                // Play a warning sound
                System.Media.SystemSounds.Asterisk.Play();

                string errorMessage;
                int entryValue = (int)itemEntry.value;
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
                if (settingsWindow.IsShowOverlay()) {
                    priceManager.DisplayPriceBox(errorMessage);
                }

                return;
            }

            // Send a warning message when count is less than 10 as these items probably have inaccurate prices
            if (itemEntry.quantity < 5 && !settingsWindow.GetSelectedSource().ToLower().Equals("poe.ninja") && item.GetFrame() != 5) {
                System.Media.SystemSounds.Asterisk.Play();
                Log("Likely incorrect price (quantity: " + itemEntry.quantity + ")", 1);
            }

            // Calculate prices
            double oldPrice = Math.Ceiling(itemEntry.value * 2) / 2.0;
            double newPrice = Math.Ceiling(itemEntry.value * (100 - settingsWindow.GetLowerPricePercentage()) / 100.0 * 2) / 2.0;

            if (settingsWindow.IsIncludeEnchant()) {
                if (enchantEntry != null && enchantEntry.value > 0) {
                    oldPrice += Math.Ceiling(enchantEntry.value * 2) / 2.0;
                    newPrice += Math.Ceiling(enchantEntry.value * (100 - settingsWindow.GetLowerPricePercentage()) / 100.0 * 2) / 2.0;
                }
            }

            string note = priceManager.MakeNote(newPrice);

            // If the LowerPriceByPercentage slider is more than 0, change output message
            if (settingsWindow.GetLowerPricePercentage() > 0) {
                Log(item.key + ": " + oldPrice + "c -> " + newPrice + "c", 0);
            } else {
                Log(item.key + ": " + oldPrice + "c", 0);
            }

            if (settingsWindow.IsShowOverlay()) {
                priceManager.DisplayPriceBox("Item: " + newPrice + "c" + enchantDisplay);
                return;
            }

            // Item already has a note. Can't overwrite it
            if (item.errorCode == 3) {
                Log("Item already has a note", 2);
                return;
            }

            // Raise flag allowing next cb event to be processed
            flag_clipBoardPaste = true;
            if (settingsWindow.IsSendNote()) Dispatcher.Invoke(() => Clipboard.SetText(note));
        }

        /// <summary>
        /// Called via task, this method pastes the current clipboard contents and presses enter
        /// </summary>
        private void ClipBoard_NotePasteTask() {
            if (flag_clipBoardPaste) {
                flag_clipBoardPaste = false;
            } else return;

            Thread.Sleep(settingsWindow.GetPasteDelay());

            // Paste clipboard contents
            System.Windows.Forms.SendKeys.SendWait("^v");

            // Send enter key if checkbox is checked
            if (settingsWindow.IsSendEnter()) System.Windows.Forms.SendKeys.SendWait("{ENTER}");
        }

        //-----------------------------------------------------------------------------------------------------------
        // WPF Event handlers
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Unhooks hooks on program exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            MouseHook.Stop();
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Run button event handler
        /// </summary>
        private void Button_Run_Click(object sender, RoutedEventArgs e) {
            if (Button_Run.Content.ToString() == "Run") {
                Button_Run.Content = "Pause";
                flag_run = true;
                Log("Service started", 0);
                MouseHook.Start();
            } else {
                Button_Run.Content = "Run";
                flag_run = false;
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
                Left = (rect.Width - Width) / 2 + rect.Left;
                Top = (rect.Height - Height) / 2 + rect.Top;
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        //-----------------------------------------------------------------------------------------------------------
        // Static methods
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
    }
}
