using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pricer.Utility {
    /// <summary>
    /// Used to populate local database
    /// </summary>
    public class Entry {
        public double value { get; set; }
        public int count { get; set; }

        public Entry() { }

        public Entry(Entry input) {
            value = input.value;
            count = input.count;
        }
    }

    /// <summary>
    /// Used to desierialize http://poe.ovh API calls
    /// </summary>
    public class PoeOvhEntry {
        public double mean { get; set; }
        public double median { get; set; }
        public double mode { get; set; }
        public int count { get; set; }
        public int frame { get; set; }
        public string links { get; set; }
        public string lvl { get; set; }
        public string quality { get; set; }
        public string corrupted { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string var { get; set; }
    }

    /// <summary>
    /// Used to desierialize http://poe.ninja API calls
    /// </summary>
    public class PoeNinjaEntry {
        public string name { get; set; }
        public int count { get; set; }
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
