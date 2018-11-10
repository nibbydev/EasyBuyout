using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBuyout.Prices {
    /// <summary>
    /// Used to populate local database
    /// </summary>
    public sealed class Entry {
        public double Value { get; set; }
        public int Quantity { get; set; }

        public Entry(double value, int quantity) {
            Value = value;
            Quantity = quantity;
        }

        public Entry(Entry input) {
            Value = input.Value;
            Quantity = input.Quantity;
        }
    }

    /// <summary>
    /// Used to deserialize http://poe.ninja API calls
    /// </summary>
    public sealed class PoeNinjaEntry {
        public int? id { get; set; }
        public string name { get; set; }
        public int count { get; set; }
        public string baseType { get; set; }
        public int itemClass { get; set; }
        public string currencyTypeName { get; set; }
        public string variant { get; set; }
        public int? links { get; set; }
        public int mapTier { get; set; }

        public double? chaosValue { get; set; }
        public double? chaosEquivalent { get; set; }

        public bool corrupted { get; set; }
        public int gemLevel { get; set; }
        public int gemQuality { get; set; }

        public double GetValue() {
            return chaosValue ?? chaosEquivalent ?? 0;
        }
    }

    /// <summary>
    /// Eye-candy for pretty code
    /// </summary>
    public sealed class PoeNinjasEntryDict {
        public List<PoeNinjaEntry> lines { get; set; }
    }
}
