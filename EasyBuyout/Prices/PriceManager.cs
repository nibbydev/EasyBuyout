using EasyBuyout.League;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows;

namespace EasyBuyout.Prices {
    /// <summary>
    /// PriceManager handles downlading, managing and translating price data from various websites
    /// </summary>
    public class PriceManager {
        private System.Windows.Controls.ProgressBar progressBar;
        private readonly JavaScriptSerializer javaScriptSerializer;
        private readonly WebClient webClient;
        private readonly LeagueManager leagueManager;
        private readonly PriceBox priceBox;
        private readonly Dictionary<String, Entry> entryMap;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="webClient"></param>
        /// <param name="leagueManager"></param>
        public PriceManager (WebClient webClient, LeagueManager leagueManager) {
            this.webClient = webClient;
            this.leagueManager = leagueManager;

            javaScriptSerializer = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue };

            priceBox = new PriceBox();
            entryMap = new Dictionary<string, Entry>();
        }

        //-----------------------------------------------------------------------------------------------------------
        // Data download
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Picks download source depending on source selection
        /// </summary>
        public void Download() {
            switch (Settings.source.ToLower()) {
                case "poe.ninja":
                    DownloadPoeNinjaData();
                    break;
                case "poe-stats.com":
                    DownloadPoeStatsData();
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        /// Download data from http://poe-stats.com and populate price dict
        /// </summary>
        private void DownloadPoeStatsData() {
            ConfigureProgressBar(Settings.poeStatsKeys.Length);
            entryMap.Clear();

            foreach (string category in Settings.poeStatsKeys) {
                MainWindow.Log("[PS] Downloading: " + category + " for " + leagueManager.GetSelectedLeague(), 0);

                try {
                    string url = "http://api.poe-stats.com/get?league=" + leagueManager.GetSelectedLeague() + "&category=" + category;
                    string jsonString = webClient.DownloadString(url);

                    // Deserialize
                    List<PoeStatsEntry> poeStatsEntryList = javaScriptSerializer.Deserialize<List<PoeStatsEntry>>(jsonString);

                    if (poeStatsEntryList == null) {
                        MainWindow.Log("[PS][" + leagueManager.GetSelectedLeague() + "] Reply was null: " + category, 0);
                        return;
                    }

                    // Add all entries from temp list to prices dict
                    foreach (PoeStatsEntry statsEntry in poeStatsEntryList) {
                        Entry entry = new Entry() {
                            value = statsEntry.median,
                            quantity = statsEntry.quantity
                        };

                        if (entryMap.ContainsKey(statsEntry.key)) {
                            MainWindow.Log("[PS][" + leagueManager.GetSelectedLeague() + "] Duplicate key: " + statsEntry.key, 1);
                        } else {
                            entryMap.Add(statsEntry.key, entry);
                        }
                    }
                } catch (Exception ex) {
                    MainWindow.Log(ex.ToString(), 2);
                } finally {
                    IncProgressBar();
                }
            }
        }

        /// <summary>
        /// Download data from http://poe.ninja and populate price dict
        /// </summary>
        private void DownloadPoeNinjaData() {
            ConfigureProgressBar(Settings.poeNinjaKeys.Length);
            entryMap.Clear();

            foreach (string category in Settings.poeNinjaKeys) {
                MainWindow.Log("[PN] Downloading: " + category + " for " + leagueManager.GetSelectedLeague(), 0);

                try {
                    string url = "http://poe.ninja/api/Data/Get" + category + "Overview?league=" + leagueManager.GetSelectedLeague();
                    string jsonString = webClient.DownloadString(url);

                    // Deserialize
                    PoeNinjasEntryDict poeNinjaEntryDict = javaScriptSerializer.Deserialize<PoeNinjasEntryDict> (jsonString);

                    if (poeNinjaEntryDict == null) {
                        MainWindow.Log("[PN][" + leagueManager.GetSelectedLeague() + "] Reply was null: " + category, 0);
                        return;
                    } else if (poeNinjaEntryDict.lines == null) {
                        MainWindow.Log("[PN][" + leagueManager.GetSelectedLeague() + "] Got invalid JSON format for:" + category, 0);
                        return;
                    }

                    // Add all entries from temp list to prices dict
                    foreach (PoeNinjaEntry ninjaEntry in poeNinjaEntryDict.lines) {
                        Entry entry = new Entry {
                            quantity = ninjaEntry.count
                        };

                        string key = FormatPoeNinjaItemKey(category, ninjaEntry, entry);

                        if (key == null) {
                            MainWindow.Log("[PN][" + leagueManager.GetSelectedLeague() + "] Couldn't generate key for:" + ninjaEntry.name, 1);
                            return;
                        }

                        if (entryMap.ContainsKey(key)) {
                            MainWindow.Log("[PN][" + leagueManager.GetSelectedLeague() + "] Duplicate key: " + key, 1);
                        } else {
                            entryMap.Add(key, entry);
                        }
                    }
                } catch (Exception ex) {
                    MainWindow.Log(ex.ToString(), 2);
                } finally {
                    IncProgressBar();
                }
            }
        }

        /// <summary>
        /// Forms a unique dictionary key for each item based on the data from http://poe.ninja's API
        /// </summary>
        /// <param name="category">PoeNinja's category key</param>
        /// <param name="ninjaEntry">Serialized PoeNinja item object</param>
        /// <param name="entry">Output entry to be added to price dict</param>
        /// <returns>Unique database key or null on error</returns>
        private string FormatPoeNinjaItemKey(string category, PoeNinjaEntry ninjaEntry, Entry entry) {
            string key, variant;

            switch (category) {
                case "Currency":
                    entry.value = ninjaEntry.chaosEquivalent;
                    return ninjaEntry.currencyTypeName + "|5";

                case "Fragment":
                    entry.value = ninjaEntry.chaosEquivalent;
                    return ninjaEntry.currencyTypeName + "|0";

                case "UniqueArmour":
                case "UniqueWeapon":
                    entry.value = ninjaEntry.chaosValue;

                    key = ninjaEntry.name + ":" + ninjaEntry.baseType + "|" + ninjaEntry.itemClass;
                    if (ninjaEntry.links >= 5) key += "|links:" + ninjaEntry.links;

                    variant = FormatPoeNinjaItemVariant(ninjaEntry);
                    if (variant != null) key += variant;

                    return key;

                case "UniqueMap":
                case "UniqueJewel":
                case "UniqueFlask":
                case "UniqueAccessory":
                    entry.value = ninjaEntry.chaosValue;
                    key = ninjaEntry.name + ":" + ninjaEntry.baseType + "|" + ninjaEntry.itemClass;

                    variant = FormatPoeNinjaItemVariant(ninjaEntry);
                    if (variant != null) key += variant;

                    return key;

                case "Essence":
                case "DivinationCards":
                case "Prophecy":
                    entry.value = ninjaEntry.chaosValue;
                    return ninjaEntry.name + "|" + ninjaEntry.itemClass;

                case "Map":
                    entry.value = ninjaEntry.chaosValue;
                    return ninjaEntry.name + "|0";

                case "SkillGem":
                    entry.value = ninjaEntry.chaosValue;
                    return ninjaEntry.name + 
                        "|" + ninjaEntry.itemClass + 
                        "|l:" + ninjaEntry.gemLevel + 
                        "|q:" + ninjaEntry.gemQuality + 
                        (ninjaEntry.corrupted ? "|c:1" : "|c:0");
                case "HelmetEnchant":
                    entry.value = ninjaEntry.chaosValue;

                    string name = Regex.Replace(ninjaEntry.name, "[-]?\\d*\\.?\\d+", "#");
                    string num = String.Join("-", Regex.Replace(ninjaEntry.name, "[^-.0-9]+", " ").Trim().Split(' '));

                    return name + "|-1" + (num != null ? "|var:" + num : "");
            }

            // Wasn't able to find a key, return null
            return null;
        }

        /// <summary>
        /// Converts PoeNinja's odd variants to a standard variant format
        /// </summary>
        /// <param name="ninjaEntry">Serialized PoeNinja item object</param>
        /// <returns>Standardized variant key or null on error</returns>
        private string FormatPoeNinjaItemVariant(PoeNinjaEntry ninjaEntry) {
            switch (ninjaEntry.name) {
                case "Atziri's Splendour":

                    switch (ninjaEntry.variant) {
                        case "Armour/ES":           return "|var:ar/es";
                        case "ES":                  return "|var:es";
                        case "Armour/Evasion":      return "|var:ar/ev";
                        case "Armour/ES/Life":      return "|var:ar/es/li";
                        case "Evasion/ES":          return "|var:ev/es";
                        case "Armour/Evasion/ES":   return "|var:ar/ev/es";
                        case "Evasion":             return "|var:ev";
                        case "Evasion/ES/Life":     return "|var:ev/es/li";
                        case "Armour":              return "|var:ar";
                    }
                    break;

                case "Yriel's Fostering":

                    switch (ninjaEntry.variant) {
                        case "Bleeding":            return "|var:ursa";
                        case "Poison":              return "|var:snake";
                        case "Maim":                return "|var:rhoa";
                    }
                    break;

                case "Volkuur's Guidance":

                    switch (ninjaEntry.variant) {
                        case "Lightning":           return "|var:lightning";
                        case "Fire":                return "|var:fire";
                        case "Cold":                return "|var:cold";
                    }
                    break;

                case "Lightpoacher":
                case "Shroud of the Lightless":
                case "Bubonic Trail":
                case "Tombfist":

                    switch (ninjaEntry.variant) {
                        case "2 Jewels":            return "|var:2 sockets";
                        case "1 Jewel":             return "|var:1 socket";
                    }
                    break;

                case "Vessel of Vinktar":

                    switch (ninjaEntry.variant) {
                        case "Added Attacks":       return "|var:attacks";
                        case "Added Spells":        return "|var:spells";
                        case "Penetration":         return "|var:penetration";
                        case "Conversion":          return "|var:conversion";
                    }
                    break;

                case "Doryani's Invitation":

                    switch (ninjaEntry.variant) {
                        case null: // Bug on poe.ninja's end
                        case "Physical":            return "|var:physical";
                        case "Fire":                return "|var:fire";
                        case "Cold":                return "|var:cold";
                        case "Lightning":           return "|var:lightning";
                    }
                    break;

                case "Impresence":

                    switch (ninjaEntry.variant) {
                        case "Chaos":               return "|var:chaos";
                        case "Physical":            return "|var:physical";
                        case "Fire":                return "|var:fire";
                        case "Cold":                return "|var:cold";
                        case "Lightning":           return "|var:lightning";
                    }
                    break;

                case "The Beachhead":
                    // "T15" -> "|var:15"
                    // Could use mapTier field but haven't set up the deserializer for that
                    return "|var:" + ninjaEntry.variant.Substring(1);
            }

            // Wasn't able to find a variant match, return null
            return null;
        }

        /// <summary>
        /// Uses machiene learning website http://poeprices.info to price magic/rare items
        /// </summary>
        /// <param name="rawItemData">Ctrl+C'd raw item data</param>
        /// <returns>Suggested price as double, 0 if unsuccessful</returns>
        public Entry SearchPoePrices(string rawItemData) {
            Entry returnEntry = new Entry() {
                // Hacky solution as there is no quantity for PoePrices so that quantity checks wouldn't warn about it
                quantity = 20
            };

            try {
                // Make request to http://poeprices.info
                string jsonString = webClient.DownloadString("https://www.poeprices.info/api?l=" + leagueManager.GetSelectedLeague() + 
                    "&i=" + Base64Encode(rawItemData));

                // Deserialize JSON-encoded reply string
                PoePricesReply reply = new JavaScriptSerializer().Deserialize<PoePricesReply>(jsonString);

                // Some protection
                if (reply == null || reply.error != "0") return null;

                // If the price was in exalts, convert it to chaos
                if (reply.currency == "exalt") {
                    Entry exaltedEntry;
                    entryMap.TryGetValue("Exalted Orb|5", out exaltedEntry);
                    if (exaltedEntry == null) return null;
                    returnEntry.value = (reply.max * exaltedEntry.value + reply.min * exaltedEntry.value) / 2.0;
                } else {
                    returnEntry.value = (reply.max + reply.min) / 2.0;
                }

                // Return the constructed entry
                return returnEntry;
            } catch (Exception ex) {
                MainWindow.Log(ex.ToString(), 2);
                return null;
            }
        }

        //-----------------------------------------------------------------------------------------------------------
        // Generic methods
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Get Entry instances associated with provided keys.
        /// Objects are ordered as the provided keys
        /// </summary>
        /// <param name="keys">Keys to search</param>
        /// <returns>List of Entry objects</returns>
        public Entry[] Search(string[] keys) {
            Entry[] returnEntryList = new Entry[keys.Length];

            for (int i = 0; i < keys.Length; i++) {
                string key = keys[i];

                if (key == null) {
                    returnEntryList[i] = null;
                    continue;
                }

                // Get the item entry
                Entry tempEntry;
                entryMap.TryGetValue(key, out tempEntry);

                returnEntryList[i] = tempEntry == null ? null : new Entry(tempEntry);
            }

            return returnEntryList;
        }

        /// <summary>
        /// Formats the buyout note that will be pasted on the item
        /// </summary>
        /// <param name="price">Price that will be present in the note</param>
        /// <returns>Formatted buyout note (e.g. "~b/o 53.2 chaos")</returns>
        public string MakeNote(double price) {
            // Replace "," with "." due to game limitations
            return Settings.prefix + " " + price.ToString().Replace(',', '.') + " chaos";
        }

        /// <summary>
        /// Encodes text in base64, used for https://poeprices.info API calls
        /// </summary>
        /// <param name="text">Raw item data</param>
        /// <returns>Base64 encoded raw item data</returns>
        public static string Base64Encode(string text) {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(text);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        //-----------------------------------------------------------------------------------------------------------
        // Progressbar-related shenanigans
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Just a setter
        /// </summary>
        /// <param name="progressBar"></param>
        public void SetProgressBar(System.Windows.Controls.ProgressBar progressBar) {
            this.progressBar = progressBar;
        }

        /// <summary>
        /// Set initial progressbar values
        /// </summary>
        /// <param name="size">Progress step count</param>
        private void ConfigureProgressBar(int size) {
            if (progressBar == null) return;

            Application.Current.Dispatcher.Invoke(() => {
                progressBar.Maximum = size;
                progressBar.Value = 0;
            });
        }

        /// <summary>
        /// Increment the progressbar by one
        /// </summary>
        private void IncProgressBar() {
            if (progressBar == null) return;

            Application.Current.Dispatcher.Invoke(() => ++progressBar.Value);
        }

        //-----------------------------------------------------------------------------------------------------------
        // Pricebox control
        //-----------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Set the content, position and visibility of the pricebox with one method
        /// </summary>
        /// <param name="content">String to be displayed in the overlay</param>
        public void DisplayPriceBox(string content) {
            if (priceBox == null) return;

            Application.Current.Dispatcher.Invoke(() => {
                priceBox.Content = content;
                priceBox.SetPosition();
                priceBox.Show();
            });
        }
    }
}
