using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pricer {
    public static class Settings {
        public static readonly string[] sourceList = { "Poe-stats.com", "Poe.ninja" };
        public const string programTitle = "Item pricer";
        public const string programVersion = "v1.0.17";
        public const string activeWindowTitle = "Path of Exile";
        public const string programReleaseAPI = "https://api.github.com/repos/siegrest/Pricer/releases";

        // Situational flags
        public static volatile bool flag_clipBoardPaste = false;

        public static volatile bool flag_run = false;
        public static volatile bool flag_sendNote = true;
        public static volatile bool flag_sendEnter = true;
        public static volatile bool flag_fallback = true;
        public static volatile bool flag_showOverlay = false;
        public static volatile bool flag_runOnRightClick = true;
        public static int pasteDelay = 120;

        public static string prefix = "~b/o";
        public static int lowerPricePercentage; // ?? needed ??
        public static string league;
        public static string source;

        public static readonly string[] poeNinjaKeys = {
            "Currency", "UniqueArmour", "Fragment", "Essence", "DivinationCards", "Prophecy", "UniqueMap",
            "Map", "UniqueJewel", "UniqueFlask", "UniqueWeapon", "UniqueAccessory", "SkillGem"
        };

        public static readonly string[] poeStatsKeys = {
            "gems", "maps", "prophecy", "currency", "weapons", "armour", "accessories",
            "jewels", "cards", "flasks", "essence"
        };
    }
}
