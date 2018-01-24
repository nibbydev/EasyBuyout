using Pricer.hooks;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

/*
 * TODO:
 *     1) Add special item detection
 *     2) Add 6L base detection
 *     3) Outoput price should use "." not ","
 *     -) Demo version should have [demo] in places (buyout note) title
 *     5) if price has ",0" the game can't auto-assign note and ^a can be used
 */

namespace Pricer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private EventHandler eventHandler_mouse;
        private EventHandler eventHandler_clip;
        private PriceManager priceManager;
        private WebClient client;

        private const string program_MOTD = "Item pricer v0.6";
        private const string activeWindowTitle = "Path of Exile";
        private volatile bool flag_userControl_run = false;
        private volatile bool flag_hasLeagueSelected = false;
        private volatile bool flag_sendBuyNote = true;
        private volatile bool flag_sendEnterKey = true;
        private volatile bool flag_clipBoardPaste = true;
        private volatile bool flag_useMedianWhenTrue = true;
        private volatile int userInput_delay = 120;

        /// <summary>
        /// Initializes the form and sets event listeners
        /// </summary>
        public MainWindow() {
            // Initialize web client
            client = new WebClient();

            // Initialize PriceManager instance and share the WebClient
            priceManager = new PriceManager(client);

            // Define eventhandlers
            eventHandler_mouse = new EventHandler(Event_mouse);
            eventHandler_clip = new EventHandler(Event_clipboard);
            
            // Hook eventhandlers 
            ClipboardNotification.ClipboardUpdate += eventHandler_clip;
            MouseHook.MouseAction += eventHandler_mouse;

            // Start mouse hook
            MouseHook.Start();

            // Initialize the UI components
            InitializeComponent();

            // Set window title
            Title = program_MOTD;

            // Credit
            Log(program_MOTD + " by Siegrest", 0);

            // Get list of active leagues from http://pathofexile.com and adds them to the
            // leagueSelector
            Log("Downloading league list...", 0);
            Task.Run(() => {
                // Get league list
                string[] leagues = UtilityMethods.GetLeagueList(client);

                // Invoke dispatcher, allowing UI element updates
                Dispatcher.Invoke(new Action(() => {
                    foreach (string league in leagues) leagueSelector.Items.Add(league);
                    Log("League list updated", 0);
                }));
            });
        }

        //
        // Event handlers
        //

        /// <summary>
        /// Mouse event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_mouse (object sender, EventArgs e) {
            // Only run if "Path of Exile" is the main focused window
            if (WindowDiscovery.GetActiveWindowTitle() != activeWindowTitle) return;

            // Do not run if user has not pressed the run button
            if (flag_userControl_run) KeyEmulator.SendCtrlC();
        }

        /// <summary>
        /// Clipboard event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_clipboard (object sender, EventArgs e) {
            if (!flag_userControl_run)
                return;
            else if (WindowDiscovery.GetActiveWindowTitle() != activeWindowTitle)
                // Only run if "Path of Exile" is the main focused window
                return;
            else if (!Clipboard.ContainsText()) {
                Log("Clipboard action without text", 2);
                return;
            }

            // Sleep to allow clipboard write action to finish
            Thread.Sleep(4);

            // Get clipboard contents
            string clipboardString = Clipboard.GetText();

            // Since this event handles *all* clipboard events AND we put the buyout note in the clipboard
            // then this event will also fire when we do that. So, to prevent an infinte loop, this is needed
            if (clipboardString.Contains("~b/o ") || clipboardString.Contains("~price ")) {
                Task.Run(() => ClipBoard_NotePasteTask());
            } else {
                Task.Run(() => ClipBoard_ItemParseTask(clipboardString));
            }
        }

        /// <summary>
        /// Takes the clipboard data, parses it and depending whether the item was desirable, outputs data
        /// </summary>
        /// <param name="clipboardString">Item data from the clipboard</param>
        private void ClipBoard_ItemParseTask(string clipboardString) {
            // Raise the flag that indicates we will permit 1 buyout note to pass the clipboard event
            flag_clipBoardPaste = true;

            // Create Item instance
            Item item = new Item(clipboardString);

            // If the item was shit, discard it
            if (item.discard) {
                System.Media.SystemSounds.Asterisk.Play();
                return;
            }
            
            // Get object from database
            Entry entry = priceManager.Search(item);

            // Make sure return value was not null (i.e. there was no entry with that key)
            if (entry == null) {
                // Play a warning sound
                System.Media.SystemSounds.Asterisk.Play();

                // Invoke dispatcher, allowing UI element updates (and access to elements outside)
                Dispatcher.Invoke(new Action(() => { Log("No item with key \"" + item.key + "\"", 2); }));

                return;
            // Send a warning message when count is less than 200 as those items probably have an inaccurate price
            } else if (entry.count < 200) {
                // Play a warning sound
                System.Media.SystemSounds.Asterisk.Play();

                // Invoke dispatcher, allowing UI element updates (and access to elements outside)
                Dispatcher.Invoke(new Action(() => { Log("Likely incorrect price (count: " + entry.count + ")", 1); }));
            }

            // Pick either mean or median price
            double price = 0;
            if (flag_useMedianWhenTrue)
                price = Math.Round(entry.median, 2);
            else
                price = Math.Round(entry.mean, 2);

            // Invoke dispatcher, allowing UI element updates (and access to elements outside)
            // Needed for: slider_lowerPrice.Value, Log(), Clipboard.SetText()
            Dispatcher.Invoke(new Action(() => {
                double newPrice = price * (100 - slider_lowerPrice.Value) / 100.0;
                string note = priceManager.MakeNote(newPrice);

                // If the LowerPriceByPercentage slider is more than 0, change output message
                if (slider_lowerPrice.Value == 0)
                    Log(item.name + ": " + price + "c", 0);
                else
                    Log(item.name + ": " + price + "c -> " + newPrice + "c", 0);

                // Copy the buyout note to the clipboard if checkbox is checked (The clipboard event
                // handler will handle that aswell)
                if (flag_sendBuyNote) Clipboard.SetText(note); ;
            }));
        }

        /// <summary>
        /// Called via task, this method pastes the current clipboard contents and presses enter
        /// </summary>
        private void ClipBoard_NotePasteTask() {
            if (flag_clipBoardPaste)
                flag_clipBoardPaste = false;
            else
                return;

            // TODO: make this modifiable by the user
            Thread.Sleep(userInput_delay);

            // Paste clipboard contents
            System.Windows.Forms.SendKeys.SendWait("^v");

            // Send enter key if checkbox is checked
            if (flag_sendEnterKey) System.Windows.Forms.SendKeys.SendWait("{ENTER}");
        }

        /// <summary>
        /// Run button event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_run_Click(object sender, RoutedEventArgs e) {
            if (button_run.Content.ToString() == "Run") {
                button_run.Content = "Stop";
                flag_userControl_run = true;
                Log("Service started", 0);
            } else {
                button_run.Content = "Run";
                flag_userControl_run = false;
                Log("Service stopped", 0);
            }
        }

        /// <summary>
        /// Prints text to window in console-like fashion, prefixes a timestamp
        /// </summary>
        /// <param name="str">String to print</param>
        /// <param name="status">Status code to indicate INFO/WARN/ERROR/CRITICAL</param>
        public void Log(string str, int status) {
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
            console_window.AppendText("[" + time + "]" + prefix + str + "\n");
            console_window.ScrollToEnd();
        }

        /// <summary>
        /// Update button handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_download_Click(object sender, RoutedEventArgs e) {
            // Disable update button
            button_download.IsEnabled = false;
            Log("Downloading price data for " + leagueSelector.SelectedValue, 0);

            // Run as task so it does not freeze the program
            Task.Run(() => {
                // Download, parse, update the data
                priceManager.DownloadPriceData();

                // Invoke dispatcher, allowing UI element updates
                Dispatcher.Invoke(new Action(() => {
                    button_run.IsEnabled = true;
                    Log("Download finished", 0);
                }));
            });
        }

        /// <summary>
        /// Unhooks hooks on program exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            ClipboardNotification.ClipboardUpdate -= eventHandler_clip;
            MouseHook.MouseAction -= eventHandler_mouse;
            MouseHook.Stop();
        }

        private void radio_bo_Checked(object sender, RoutedEventArgs e) {
            priceManager.prefix = "~b/o";
        }

        private void radio_price_Checked(object sender, RoutedEventArgs e) {
            priceManager.prefix = "~price";
        }

        private void button_meanMedSel_Click(object sender, RoutedEventArgs e) {
            if (button_meanMedSel.Content.ToString() == "Mean") {
                button_meanMedSel.Content = "Median";
                flag_useMedianWhenTrue = false;
                Log("Using mean prices", 0);
            } else {
                button_meanMedSel.Content = "Mean";
                flag_useMedianWhenTrue = true;
                Log("Using median prices", 0);
            }
        }

        private void slider_lowerPrice_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            label_lowerPrice.Content = "Lower price by " + slider_lowerPrice.Value + "%";
        }

        private void checkBox_outputNote_Click(object sender, RoutedEventArgs e) {
            flag_sendBuyNote = (bool)checkBox_outputNote.IsChecked;
            checkBox_enter.IsEnabled = flag_sendBuyNote;
        }

        private void checkBox_enter_Click(object sender, RoutedEventArgs e) {
            flag_sendEnterKey = (bool)checkBox_enter.IsChecked;
        }

        /// <summary>
        /// Fires when league selection has changed (enables/disables buttons accordingly)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void leagueSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // On first launch, enable Update button only after user has picked a league
            if (!flag_hasLeagueSelected) {
                flag_hasLeagueSelected = true;
                button_download.IsEnabled = true;
            }

            // Set league value in priceManager according to the leagueSelector
            priceManager.league = (string)leagueSelector.SelectedItem;

            // Enable update button
            if (!button_download.IsEnabled) button_download.IsEnabled = true;

            // Stop service and disable run button when user changes league
            if (button_run.IsEnabled) {
                button_run.Content = "Run";
                flag_userControl_run = false;
                button_run.IsEnabled = false;
            }
        }

        private void textBox_delay_LostFocus(object sender, RoutedEventArgs e) {
            Int32.TryParse(textBox_delay.Text, out int result);

            // Don't announce when there's no change
            if (result == userInput_delay) return;

            if (result < 1 || result > 500) {
                Log("Invalid input (allowed: 1 - 500)", 2);
                textBox_delay.Text = userInput_delay.ToString();
            } else {
                Log("Changed delay " + userInput_delay + " -> " + result, 0);
                userInput_delay = result;
            }
        }
    }

    /// <summary>
    /// Contains some various methods that don't quite fit under other classes
    /// </summary>
    public class UtilityMethods {
        private class LeagueObject { public string id { get; set; } }

        /// <summary>
        /// Search predicate returns true if a string contains "SSF"
        /// </summary>
        /// <param name="obj">LeagueObject to be passed</param>
        /// <returns>True if string contains "SSF"</returns>
        private static bool ContainsSSF(LeagueObject obj) { return obj.id.Contains("SSF"); }

        public static string[] GetLeagueList(WebClient client) {
            try {
                // Download JSON-encoded string
                string jsonString = client.DownloadString("http://api.pathofexile.com/leagues?type=main&compact=1");

                // Deserialize JSON string
                List<LeagueObject> temp_deSerList = new JavaScriptSerializer().Deserialize<List<LeagueObject>>(jsonString);

                temp_deSerList.RemoveAll(ContainsSSF);

                // Init returnString
                string[] returnString = new string[temp_deSerList.Count];

                // Add all values from temp list to returnString list
                int counter = 0;
                foreach(LeagueObject league in temp_deSerList) {
                    returnString[counter] = league.id;
                    counter++;
                }

                // Return list
                return returnString;
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }

            // If an error occured return null
            return null;
        }
    }
}
