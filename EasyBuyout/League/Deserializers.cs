using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBuyout.League {
    /// <summary>
    /// Deserializer for https://www.pathofexile.com/api/trade/data/leagues
    /// </summary>
    public sealed class LeagueParcel {
        public List<LeagueEntry> result { get; set; }
    }

    /// <summary>
    /// Deserializer for https://www.pathofexile.com/api/trade/data/leagues
    /// </summary>
    public sealed class LeagueEntry {
        public string text { get; set; }
        public string id { get; set; }
    }
}
