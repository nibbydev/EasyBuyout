using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBuyout.Settings {
    public static class Config {
        public static readonly string[] sourceList = { "Poe.ninja", "Poe-stats.com" };
        public const string programTitle = "EasyBuyout";
        public const string programVersion = "v1.0.24.1";
        public const string activeWindowTitle = "Path of Exile";

        // League manager
        public const string poeLeagueAPI = "https://www.pathofexile.com/api/trade/data/leagues";
        public const string manualLeagueDisplay = "<Manually specify>";

        // Updater
        public const string githubReleaseAPI = "https://api.github.com/repos/siegrest/EasyBuyout/releases";
        public const bool flag_updaterEnabled = true; // Set to false to disable updater

        // Pricemanager
        public const int liveUpdateDelayMS = 1000 * 60 * 10; // Update prices every x milliseconds
        public const double liveUpdateInactiveMin = 15; // Don't download new prices if program was last used > x minutes ago
        public static readonly string[] poeNinjaKeys = {
            "Currency", "UniqueArmour", "Fragment", "Essence", "DivinationCards", "Prophecy", "UniqueMap",
            "Map", "UniqueJewel", "UniqueFlask", "UniqueWeapon", "UniqueAccessory", "SkillGem", "HelmetEnchant"
        };
        public static readonly string[] poeStatsKeys = {
            "gems", "maps", "prophecy", "currency", "weapons", "armour", "accessories",
            "jewels", "cards", "flasks", "essence", "enchantments"
        };
    }
}
