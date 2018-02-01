using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;

namespace Pricer {
    /// <summary>
    /// PriceManager handles downlading, managing and translating price data from various websites
    /// </summary>
    class PriceManager {
        private Dictionary<string, Entry> priceDataDict = new Dictionary<string, Entry>();
        private WebClient client;
        public string league { get; set; }
        public string prefix { get; set; }
        public string source { get; set; }
        public int lowerPercentage { get; set; }
        public int ovhPriceMode = 0;
        private static string[] poeNinjaURLs = {
            "Currency", "UniqueArmour", "Fragment",
            "Essence", "DivinationCards", "Prophecy",
            "UniqueMap", "Map", "UniqueJewel",
            "UniqueFlask", "UniqueWeapon", "UniqueAccessory",
            "SkillGem"
        };

        /// <summary>
        /// Initializes the instance. Must be given a WebClient instance
        /// </summary>
        /// <param name="client">WebClient to use for data connections</param>
        public PriceManager (WebClient client) { this.client = client; }

        /// <summary>
        /// Picks download source depending on source selection
        /// </summary>
        public void UpdateDatabase() {
            if (source.ToLower() == "poe.ninja") DownloadPoeNinjaData();
            else if (source.ToLower() == "poe.ovh") DownloadPoeOvhData();
        }

        /// <summary>
        /// Downloads and populates price data from http://poe.ovh
        /// </summary>
        private void DownloadPoeOvhData() {
            // Clear previous data
            priceDataDict.Clear();

            try {
                // Download JSON-encoded string
                string jsonString = client.DownloadString("http://api.poe.ovh/Stats?league=" + league);

                // Deserialize JSON string
                Dictionary<string, Dictionary<string, PoeOvhEntry>> temp_deSerDict = 
                    new JavaScriptSerializer().Deserialize<Dictionary<string, Dictionary<string, PoeOvhEntry>>>(jsonString);

                if (temp_deSerDict == null) return;

                // Add all values from temp dict to new dict (for ease of use)
                foreach (string name_category in temp_deSerDict.Keys) {
                    temp_deSerDict.TryGetValue(name_category, out Dictionary<string, PoeOvhEntry> category);

                    foreach (string name_item in category.Keys) {
                        // Get OvhEntry from list
                        category.TryGetValue(name_item, out PoeOvhEntry ovhEntry);

                        // Create Entry instance
                        Entry entry = new Entry();

                        // Set Entry value
                        switch (ovhPriceMode) {
                            case 0:
                                entry.value = ovhEntry.mean;
                                break;
                            case 1:
                                entry.value = ovhEntry.median;
                                break;
                            case 2:
                                entry.value = ovhEntry.mode;
                                break;
                            default:
                                break;
                        }

                        // Set misc data
                        entry.count = ovhEntry.count;
                        entry.source = source;

                        // Add to database
                        priceDataDict.Add(name_item, entry);
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// Downloads and populates price data from http://poe.ninja
        /// </summary>
        private void DownloadPoeNinjaData() {
            // Clear previous data
            priceDataDict.Clear();

            foreach(string key in poeNinjaURLs) {
                try {
                    // Download JSON-encoded string
                    string jsonString = client.DownloadString("http://poe.ninja/api/Data/Get" + key + "Overview?league=" + league);

                    // Deserialize JSON string
                    Dictionary<string, List<PoeNinjaEntry>> temp_deSerDict = 
                        new JavaScriptSerializer().Deserialize<Dictionary<string, List<PoeNinjaEntry>>>(jsonString);

                    if (temp_deSerDict == null) continue;

                    temp_deSerDict.TryGetValue("lines", out List<PoeNinjaEntry> entryList);

                    if (entryList == null) continue;

                    foreach(PoeNinjaEntry ninjaEntry in entryList) {
                        // Quick and dirty workarounds
                        Entry entry = new Entry {
                            count = 300,
                            source = source
                        };
                        string itemKey;

                        switch(key) {
                            case "Currency":
                                entry.value = ninjaEntry.chaosEquivalent;
                                itemKey = ninjaEntry.currencyTypeName + "|5";
                                if (!priceDataDict.ContainsKey(itemKey)) priceDataDict.Add(itemKey, entry);
                                break;

                            case "Fragment":
                                entry.value = ninjaEntry.chaosEquivalent;
                                itemKey = ninjaEntry.currencyTypeName + "|0";
                                if (!priceDataDict.ContainsKey(itemKey)) priceDataDict.Add(itemKey, entry);
                                break;

                            case "UniqueArmour":
                            case "UniqueWeapon":
                                entry.value = ninjaEntry.chaosValue;

                                switch (ninjaEntry.links) {
                                    case 6:
                                        itemKey = ninjaEntry.name + "|" + ninjaEntry.baseType + "|" + ninjaEntry.itemClass + "|6L";
                                        break;
                                    case 5:
                                        itemKey = ninjaEntry.name + "|" + ninjaEntry.baseType + "|" + ninjaEntry.itemClass + "|5L";
                                        break;
                                    default:
                                        itemKey = ninjaEntry.name + "|" + ninjaEntry.baseType + "|" + ninjaEntry.itemClass;
                                        break;
                                }

                                switch (ninjaEntry.name) {
                                    case "Atziri's Splendour":
                                        switch(ninjaEntry.variant) {
                                            case "Armour/ES":
                                                itemKey += "|var:ar/es";
                                                break;
                                            case "ES":
                                                itemKey += "|var:es";
                                                break;
                                            case "Armour/Evasion":
                                                itemKey += "|var:ar/ev";
                                                break;
                                            case "Armour/ES/Life":
                                                itemKey += "|var:ar/es/li";
                                                break;
                                            case "Evasion/ES":
                                                itemKey += "|var:ev/es";
                                                break;
                                            case "Armour/Evasion/ES":
                                                itemKey += "|var:ar/ev/es";
                                                break;
                                            case "Evasion":
                                                itemKey += "|var:ev";
                                                break;
                                            case "Evasion/ES/Life":
                                                itemKey += "|var:ev/es/li";
                                                break;
                                            case "Armour":
                                                itemKey += "|var:ar";
                                                break;
                                            default:
                                                break;
                                        }
                                        break;

                                    case "Yriel's Fostering":
                                        switch (ninjaEntry.variant) {
                                            case "Bleeding":
                                                itemKey += "|var:physical";
                                                break;
                                            case "Poison":
                                                itemKey += "|var:chaos";
                                                break;
                                            case "Maim":
                                                itemKey += "|var:speed";
                                                break;
                                            default:
                                                break;
                                        }
                                        break;

                                    case "Volkuur's Guidance":
                                        switch (ninjaEntry.variant) {
                                            case "Lightning":
                                                itemKey += "|var:lightning";
                                                break;
                                            case "Fire":
                                                itemKey += "|var:fire";
                                                break;
                                            case "Cold":
                                                itemKey += "|var:cold";
                                                break;
                                            default:
                                                break;
                                        }
                                        break;

                                    case "Lightpoacher":
                                    case "Shroud of the Lightless":
                                    case "Bubonic Trail":
                                    case "Tombfist":
                                        switch (ninjaEntry.variant) {
                                            case "2 Jewels":
                                                itemKey += "|var:2";
                                                break;
                                            case "1 Jewel":
                                                itemKey += "|var:1";
                                                break;
                                            default:
                                                break;
                                        }
                                        break;

                                    default:
                                        break;
                                }
                                
                                if (!priceDataDict.ContainsKey(itemKey)) priceDataDict.Add(itemKey, entry);
                                break;

                            case "UniqueMap":
                            case "UniqueJewel":
                            case "UniqueFlask":
                            case "UniqueAccessory":
                                entry.value = ninjaEntry.chaosValue;
                                itemKey = ninjaEntry.name + "|" + ninjaEntry.baseType + "|" + ninjaEntry.itemClass;

                                switch (ninjaEntry.name) {
                                    case "Vessel of Vinktar":
                                        switch (ninjaEntry.variant) {
                                            case "Added Attacks":
                                                itemKey += "|var:attacks";
                                                break;
                                            case "Added Spells":
                                                itemKey += "|var:spells";
                                                break;
                                            case "Penetration":
                                                itemKey += "|var:penetration";
                                                break;
                                            case "Conversion":
                                                itemKey += "|var:conversion";
                                                break;
                                            default:
                                                break;
                                        }
                                        break;

                                    case "Doryani's Invitation":
                                        switch (ninjaEntry.variant) {
                                            case null: // Bug on poe.ninja's end
                                            case "Physical":
                                                itemKey += "|var:physical";
                                                break;
                                            case "Fire":
                                                itemKey += "|var:fire";
                                                break;
                                            case "Cold":
                                                itemKey += "|var:cold";
                                                break;
                                            case "Lightning":
                                                itemKey += "|var:lightning";
                                                break;
                                            default:
                                                break;
                                        }
                                        break;

                                    case "Impresence":
                                        switch (ninjaEntry.variant) {
                                            case "Chaos":
                                                itemKey += "|var:chaos";
                                                break;
                                            case "Physical":
                                                itemKey += "|var:physical";
                                                break;
                                            case "Fire":
                                                itemKey += "|var:fire";
                                                break;
                                            case "Cold":
                                                itemKey += "|var:cold";
                                                break;
                                            case "Lightning":
                                                itemKey += "|var:lightning";
                                                break;
                                            default:
                                                break;
                                        }
                                        break;

                                    default:
                                        break;
                                }

                                if (!priceDataDict.ContainsKey(itemKey)) priceDataDict.Add(itemKey, entry);
                                break;

                            case "Essence":
                            case "DivinationCards":
                            case "Prophecy":
                                entry.value = ninjaEntry.chaosValue;

                                itemKey = ninjaEntry.name + "|" + ninjaEntry.itemClass;
                                if (!priceDataDict.ContainsKey(itemKey)) priceDataDict.Add(itemKey, entry);
                                break;

                            case "Map":
                                entry.value = ninjaEntry.chaosValue;

                                itemKey = ninjaEntry.name + "|0";
                                if (!priceDataDict.ContainsKey(itemKey)) priceDataDict.Add(itemKey, entry);
                                break;

                            case "SkillGem":
                                entry.value = ninjaEntry.chaosValue;

                                itemKey = ninjaEntry.name + "|" + ninjaEntry.itemClass + "|" + ninjaEntry.gemLevel + "|" + ninjaEntry.gemQuality;
                                if (ninjaEntry.corrupted) itemKey += "|1";
                                else itemKey += "|0";

                                if (!priceDataDict.ContainsKey(itemKey)) priceDataDict.Add(itemKey, entry);
                                break;
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// Uses machiene learning website http://poeprices.info to price magic/rare items
        /// </summary>
        /// <param name="rawItemData">Ctrl+C'd raw item data</param>
        /// <returns>Suggested price as double, 0 if unsuccessful</returns>
        public double SearchPoePrices(string rawItemData) {
            try {
                // Make request to http://poeprices.info
                string jsonString = client.DownloadString("https://www.poeprices.info/api?l=" + league + 
                    "&i=" + UtilityMethods.Base64Encode(rawItemData));

                // Deserialize JSON-encoded reply string
                PoePricesReply reply = new JavaScriptSerializer().Deserialize<PoePricesReply>(jsonString);

                // Some protection, idk
                if (reply == null) return -1;
                if (reply.error != "0") return -2;

                // If the price was in exalteds, convert it to chaos and return the price
                if (reply.currency == "exalt") {
                    priceDataDict.TryGetValue("Exalted Orb|5", out Entry exaltedEntry);
                    if (exaltedEntry == null) return -3;
                    return (reply.max * exaltedEntry.value + reply.min * exaltedEntry.value) / 2.0;
                }

                // Otherwise, if it was in chaos, return the price
                return (reply.max + reply.min) / 2.0;
            } catch (Exception ex) {
                Console.WriteLine(ex);
                return -4;
            }
        }

        /// <summary>
        /// Primitive method for looking up gem prices
        /// </summary>
        /// <param name="key">Database key to search for</param>
        /// <returns>Median value in chaos</returns>
        public Entry Search(Item item) {
            // Get the database entry
            priceDataDict.TryGetValue(item.key, out Entry tempEntry);

            // Precaution
            if (tempEntry == null) return null;

            // Manual copy so the original database entry is not affected
            Entry entry = new Entry() {
                value = tempEntry.value,
                source = tempEntry.source,
                count = tempEntry.count
            };

            // Return match
           return entry;
        }

        /// <summary>
        /// Formats the buyout note that will be pasted on the item
        /// </summary>
        /// <param name="price">Price that will be present in the note</param>
        /// <returns>Formatted buyout note (e.g. "~b/o 53.2 chaos")</returns>
        public string MakeNote(double price) {
            // Replace "," with "." due to game limitations
            return prefix + " " + price.ToString().Replace(',', '.') + " chaos";
        }
    }

    /// <summary>
    /// Used to populate local database
    /// </summary>
    public class Entry {
        public double value { get; set; }
        public string source { get; set; }
        public int count { get; set; }
        public int error { get; set; }
    }

    /// <summary>
    /// Used to desierialize http://poe.ovh API calls
    /// </summary>
    public class PoeOvhEntry {
        public double mean { get; set; }
        public double median { get; set; }
        public double mode { get; set; }
        public int count { get; set; }
    }

    /// <summary>
    /// Used to desierialize http://poe.ninja API calls
    /// </summary>
    public class PoeNinjaEntry {
        public string name { get; set; }
        public string baseType { get; set; }
        public int itemClass { get; set; }
        public string currencyTypeName { get; set; }
        public string variant { get; set; }
        public int links { get; set; }

        public double chaosValue { get; set; }
        public double chaosEquivalent { get; set; }

        public bool corrupted { get; set; }
        public int gemLevel { get; set; }
        public int gemQuality { get; set; }
    }

    /// <summary>
    /// Used to desierialize http://poeprices.com API calls
    /// </summary>
    public class PoePricesReply {
        public string currency { get; set; }
        public string error { get; set; }
        public double min { get; set; }
        public double max { get; set; }
    }
}
