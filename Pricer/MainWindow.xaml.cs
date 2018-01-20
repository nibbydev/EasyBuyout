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

namespace Pricer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private EventHandler eventHandler_mouse;
        private EventHandler eventHandler_clip;
        private PriceManager priceManager;
        private WebClient client;

        private const string program_MOTD = "Item pricer v0.4";
        private const string activeWindowTitle = "Path of Exile";
        private volatile bool flag_userControl_run = false;
        private volatile bool flag_hasLeagueSelected = false;

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

            Item item = new Item(Clipboard.GetText());
            //if (item.match) SystemSounds.Asterisk.Play();

            double price = priceManager.Search(item.key);
            Log(item.key + ": " + price + " chaos", 0);

            Task task = new Task(() => {
                Thread.Sleep(100);
                System.Windows.Forms.SendKeys.SendWait("{~}b{/}o " + price + " chaos");
            });
            task.Start();
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
        private void button_update_Click(object sender, RoutedEventArgs e) {
            // Disable update button
            button_update.IsEnabled = false;
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

        /// <summary>
        /// Fires when league selection has changed (enables/disables buttons accordingly)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void leagueSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // On first launch, enable Update button only after user has picked a league
            if (!flag_hasLeagueSelected) {
                flag_hasLeagueSelected = true;
                button_update.IsEnabled = true;
            }

            // Set league value in priceManager according to the leagueSelector
            priceManager.league = (string) leagueSelector.SelectedItem;

            // Enable update button
            if (!button_update.IsEnabled) button_update.IsEnabled = true;

            // Stop service and disable run button when user changes league
            if (button_run.IsEnabled) {
                button_run.Content = "Run";
                flag_userControl_run = false;
                button_run.IsEnabled = false;
            }
        }

        private void radio_bo_Checked(object sender, RoutedEventArgs e) {
            priceManager.prefix = "~b/o";
        }

        private void radio_price_Checked(object sender, RoutedEventArgs e) {
            priceManager.prefix = "~price";
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
