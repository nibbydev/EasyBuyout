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
        public int lowerPercentage { get; set; }

        /// <summary>
        /// Initializes the instance. Must be given a WebClient instance
        /// </summary>
        /// <param name="client">WebClient to use for data connections</param>
        public PriceManager (WebClient client) { this.client = client; }

        /// <summary>
        /// Downloads price data from http://poe.ovh
        /// </summary>
        public void DownloadPriceData() {
            // Clear previous data
            priceDataDict.Clear();

            try {
                // Download JSON-encoded string
                string jsonString = client.DownloadString("http://api.poe.ovh/Stats?league=" + league);

                // Deserialize JSON string
                Dictionary<string, Dictionary<string, Entry>> temp_deSerDict = 
                    new JavaScriptSerializer().Deserialize<Dictionary<string, Dictionary<string, Entry>>>(jsonString);

                if (temp_deSerDict == null) return;

                // Add all values from temp dict to new dict (for ease of use)
                foreach (string name_category in temp_deSerDict.Keys) {
                    temp_deSerDict.TryGetValue(name_category, out Dictionary<string, Entry> category);

                    foreach (string name_item in category.Keys) {
                        category.TryGetValue(name_item, out Entry entry);
                        priceDataDict.Add(name_item, entry);
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// Primitive method for looking up gem prices
        /// </summary>
        /// <param name="key">Database key to search for</param>
        /// <returns>Median value in chaos</returns>
        public Entry Search(Item item) {
            // Get the database entry
            priceDataDict.TryGetValue(item.key, out Entry entry);

            // If item is a gem and has quality more than 10, get price by missing GCP
            if(item.rarity == "Gem" && item.gem_q > 10 && item.gem_q < 22) {
                // Get GCP's chaos value
                priceDataDict.TryGetValue("Gemcutter's Prism|5", out Entry gcpCost);

                // Subtract the missing GCPChaosValue from the gem's price
                entry.mean = entry.mean - (20 - item.gem_q) * gcpCost.median;
                entry.median = entry.median - (20 - item.gem_q) * gcpCost.median;

                // If we went into the negatives, set to 0
                if (entry.mean < 0) entry.mean = 0;
                if (entry.median < 0) entry.median = 0;
            }

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

    public class Entry {
        public double mean { get; set; }
        public double median { get; set; }
        public int count { get; set; }
    }
}
