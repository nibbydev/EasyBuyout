using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBuyout {
    public static class Settings {
        public static readonly string[] sourceList = { "Poe.ninja", "Poe-stats.com" };
        public const string programTitle = "EasyBuyout";
        public const string programVersion = "v1.0.23.2";
        public const string activeWindowTitle = "Path of Exile";

        // League manager
        public const string poeLeagueAPI = "https://www.pathofexile.com/api/trade/data/leagues";
        public const string manualLeagueDisplay = "<Manually specify>";

        // Updater
        public const string githubReleaseAPI = "https://api.github.com/repos/siegrest/Pricer/releases";
        public const bool flag_updaterEnabled = true; // Set to false to disable updater

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
