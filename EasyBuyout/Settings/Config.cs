using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyBuyout.Settings.Source;

namespace EasyBuyout.Settings
{
    public class Config
    {
        //-----------------------------------------------------------------------------------------------------------
        // Readonly
        //-----------------------------------------------------------------------------------------------------------

        public readonly SourceSite Source;
        public readonly string     ProgramTitle   = "EasyBuyout";
        public readonly string     ProgramVersion = "v1.1.2";

        public readonly string LeagueApiUrl          = "https://www.pathofexile.com/api/trade/data/leagues";
        public readonly string ManualLeagueDisplay   = "<Manually specify>";
        public readonly string GithubReleaseApi      = "https://api.github.com/repos/siegrest/EasyBuyout/releases";
        public readonly int    LiveUpdateDelayMs     = 600000; // Update prices every x milliseconds
        private         string _selectedLeague       = "Standard";
        private         string _notePrefix           = "~price";
        private         int    _lowerPricePercentage = -5;
        private         int    _pasteDelay           = 140;
        private         int    _pricePrecision       = 0;
        private         bool   _flagSendNote         = true;
        private         bool   _flagSendEnter        = false;
        private         bool   _flagLiveUpdate       = true;

        //-----------------------------------------------------------------------------------------------------------
        // Dynamic
        //-----------------------------------------------------------------------------------------------------------

        public string NotePrefix
        {
            get => _notePrefix;
            set
            {
                _notePrefix = value;
                SaveFile();
            }
        }

        private const string PreferencesIni = "Preferences.ini";

        public string SelectedLeague
        {
            get => _selectedLeague;
            set
            {
                _selectedLeague = value;
                SaveFile();
            }
        }

        public int LowerPricePercentage
        {
            get => _lowerPricePercentage;
            set
            {
                _lowerPricePercentage = value;
                SaveFile();
            }
        }

        public int PasteDelay
        {
            get => _pasteDelay;
            set
            {
                _pasteDelay = value;
                SaveFile();
            }
        }

        public int ClipboardWriteDelay { get; set; } = 4;

        public int PricePrecision
        {
            get => _pricePrecision;
            set
            {
                _pricePrecision = value;
                SaveFile();
            }
        }

        public bool FlagSendNote
        {
            get => _flagSendNote;
            set
            {
                _flagSendNote = value;
                SaveFile();
            }
        }

        public bool FlagSendEnter
        {
            get => _flagSendEnter;
            set
            {
                _flagSendEnter = value;
                SaveFile();
            }
        }

        public bool FlagLiveUpdate
        {
            get => _flagLiveUpdate;
            set
            {
                _flagLiveUpdate = value;
                SaveFile();
            }
        }

        public bool FlagRun { get; set; } = false;

        public Config()
        {
            // Initialize data sources
            Source = new SourceSite()
            {
                    Name = "Poe.Ninja",
                    SourceApis = new List<SourceApi>()
                    {
                            new SourceApi()
                            {
                                    Display = "Currency",
                                    Url = "https://poe.ninja/api/data/currencyoverview?league={league}&type={category}",
                                    Categories = new List<string>()
                                    {
                                            "Currency", "Fragment"
                                    }
                            },
                            new SourceApi()
                            {
                                    Display = "Items",
                                    Url     = "https://poe.ninja/api/data/itemoverview?league={league}&type={category}",
                                    Categories = new List<string>()
                                    {
                                            "Resonator", "Incubator", "UniqueMap", "Essence", "Scarab", "Fossil", "Oil",
                                            "DeliriumOrb",
                                            "DivinationCard", "Prophecy",
                                            "Map", "UniqueArmour", "UniqueJewel", "UniqueFlask", "UniqueWeapon",
                                            "UniqueAccessory", "SkillGem"
                                    }
                            }
                    }
            };
            if (File.Exists(PreferencesIni))
            {
                try
                {
                    string[] lines = File.ReadAllLines(PreferencesIni);
                    _selectedLeague       = lines[0];
                    _notePrefix           = lines[1];
                    _pricePrecision       = int.Parse(lines[2]);
                    _lowerPricePercentage = int.Parse(lines[3]);
                    _pasteDelay           = int.Parse(lines[4]);
                    _flagSendNote         = bool.Parse(lines[5]);
                    _flagSendEnter        = bool.Parse(lines[6]);
                    _flagLiveUpdate       = bool.Parse(lines[7]);
                }
                catch (Exception e) { }
            }
        }

        private void SaveFile()
        {
            var items = new object[]
            {
                    _selectedLeague,
                    _notePrefix,
                    _pricePrecision,
                    _lowerPricePercentage,
                    _pasteDelay,
                    _flagSendNote,
                    _flagSendEnter,
                    _flagLiveUpdate
            };
            File.WriteAllLines(PreferencesIni, items.Select(o => o.ToString()));
        }
    }
}