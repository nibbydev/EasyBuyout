using System;
using System.Collections.Generic;
using System.Media;
using System.Net;
using System.Web.Script.Serialization;

namespace Pricer {
    /// <summary>
    /// PriceManager handles downlading, managing and translating price data from various websites
    /// </summary>
    class PriceManager {
        private Dictionary<string, Entry> priceDataDict = new Dictionary<string, Entry>();
        public volatile bool trueIfMeanFalseIfMedian = false;
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
        public double Search(string key) {
            priceDataDict.TryGetValue(key, out Entry entry);

            if (entry != null) {
                if (trueIfMeanFalseIfMedian)
                    return entry.mean;
                else
                    return entry.median;

            } else {
                SystemSounds.Asterisk.Play();
                return 0;
            }
                
        }
    }

    public class Entry {
        public double mean { get; set; }
        public double median { get; set; }
        public int count { get; set; }
    }
}
