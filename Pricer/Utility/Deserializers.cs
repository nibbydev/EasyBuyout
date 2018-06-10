using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pricer.Utility {
    /// <summary>
    /// Used to populate local database
    /// </summary>
    public sealed class Entry {
        public double value { get; set; }
        public int quantity { get; set; }

        public Entry() { }

        public Entry(Entry input) {
            value = input.value;
            quantity = input.quantity;
        }
    }

    /// <summary>
    /// Used to desierialize http://poe-stats.com API calls
    /// </summary>
    public sealed class PoeStatsEntry {
        public double median { get; set; }
        public int quantity { get; set; }
        public string key { get; set; }
    }

    /// <summary>
    /// Used to desierialize http://poe.ninja API calls
    /// </summary>
    public sealed class PoeNinjaEntry {
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
    public sealed class PoePricesReply {
        public string currency { get; set; }
        public string error { get; set; }
        public double min { get; set; }
        public double max { get; set; }
    }

    /// <summary>
    /// Deserializer for https://www.pathofexile.com/api/trade/data/leagues
    /// </summary>
    public sealed class LeagueList {
        public List<Dictionary<string, string>> result { get; set; }
    }

    /// <summary>
    /// Deserializer for Github's latest release API
    /// </summary>
    public sealed class ReleaseObject {
        public sealed class AssetObject {
            public string name { get; set; }
            public string size { get; set; }
            public string browser_download_url { get; set; }
        }

        public string html_url { get; set; }
        public string tag_name { get; set; }
        public string name { get; set; }
        public List<AssetObject> assets { get; set; }
    }

    //-----------------------------------------------------------------------------------------------------------
    // Eye-candy for pretty code
    //-----------------------------------------------------------------------------------------------------------

    public sealed class Prices : Dictionary<String, Entry> {

    }

    public sealed class PoeStatsEntryList : List<PoeStatsEntry> {

    }

    public sealed class PoeNinjasEntryDict {
        public List<PoeNinjaEntry> lines { get; set; }
    }
}
