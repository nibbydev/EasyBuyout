using System.Collections.Generic;
using EasyBuyout.Settings.Source;

namespace EasyBuyout.Settings {
    public class Config {
        //-----------------------------------------------------------------------------------------------------------
        // Readonly
        //-----------------------------------------------------------------------------------------------------------

        public readonly SourceSite Source;
        public readonly string ProgramTitle = "EasyBuyout";
        public readonly string ProgramVersion = "v1.1";

        public readonly string LeagueApiUrl = "https://www.pathofexile.com/api/trade/data/leagues";
        public readonly string ManualLeagueDisplay = "<Manually specify>";
        public readonly string GithubReleaseApi = "https://api.github.com/repos/siegrest/EasyBuyout/releases";
        public readonly int LiveUpdateDelayMs = 600000; // Update prices every x milliseconds

        //-----------------------------------------------------------------------------------------------------------
        // Dynamic
        //-----------------------------------------------------------------------------------------------------------

        public string NotePrefix { get; set; } = "~b/o";
        public string SelectedLeague { get; set; }
        public int LowerPricePercentage { get; set; } = 0;
        public int PasteDelay { get; set; } = 120;
        public int ClipboardWriteDelay { get; set; } = 4;
        public int PricePrecision { get; set; } = 1;

        public bool FlagSendNote { get; set; } = true;
        public bool FlagSendEnter { get; set; } = true;
        public bool FlagShowOverlay { get; set; } = false;
        public bool FlagLiveUpdate { get; set; } = true;
        public bool FlagRun { get; set; } = false;

        public Config() {
            // Initialize data sources
            Source = new SourceSite() {
                Name = "Poe.Ninja",
                SourceApis = new List<SourceApi>() {
                    new SourceApi() {
                        Display = "Currency",
                        Url = "https://poe.ninja/api/data/currencyoverview?league={league}&type={category}",
                        Categories = new List<string>() {
                            "Currency", "Fragment"
                        }
                    },
                    new SourceApi() {
                        Display = "Items",
                        Url = "https://poe.ninja/api/data/itemoverview?league={league}&type={category}",
                        Categories = new List<string>() {
                            "UniqueArmour", "Essence", "DivinationCard", "Prophecy", "UniqueMap",
                            "Map", "UniqueJewel", "UniqueFlask", "UniqueWeapon", "UniqueAccessory", "SkillGem"
                        }
                    }
                }
            };
        }
    }
}