using EasyBuyout.Settings;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using EasyBuyout.Item;

namespace EasyBuyout.Prices {
    /// <summary>
    /// Handles downloading, managing and translating price data from various websites
    /// </summary>
    public class PriceManager {
        private readonly JavaScriptSerializer _javaScriptSerializer;
        private readonly WebClient _webClient;
        private readonly Action<string, MainWindow.Flair> _log;
        private readonly Dictionary<Key, Entry> _entryMap;
        private readonly Config _config;
        private Timer _liveUpdateTask;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        /// <param name="webClient"></param>
        /// <param name="log"></param>
        public PriceManager(Config config, WebClient webClient, Action<string, MainWindow.Flair> log) {
            _config = config;
            _webClient = webClient;
            _log = log;

            _javaScriptSerializer = new JavaScriptSerializer {MaxJsonLength = int.MaxValue};
            _entryMap = new Dictionary<Key, Entry>();
        }

        //-----------------------------------------------------------------------------------------------------------
        // Data download
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Picks download source depending on source selection
        /// </summary>
        public void Download() {
            if (_config.SelectedLeague == null) {
                return;
            }

            _entryMap.Clear();
            foreach (var api in _config.Source.SourceApis) {
                foreach (var category in api.Categories) {
                    _log($"Fetching: {category}", MainWindow.Flair.Info);

                    try {
                        var url = api.Url.Replace("{league}", _config.SelectedLeague).Replace("{category}", category);
                        var jsonString = _webClient.DownloadString(url);

                        // Deserialize
                        var entryDict = _javaScriptSerializer.Deserialize<PoeNinjasEntryDict>(jsonString);

                        if (entryDict == null) {
                            _log($"[{_config.SelectedLeague}] Reply was null for {category}", MainWindow.Flair.Error);
                            break;
                        }

                        // Add all entries
                        foreach (var line in entryDict.lines) {
                            var key = new Key(category, line);

                            if (_entryMap.ContainsKey(key)) {
                                Console.WriteLine($"Duplicate key for {key}");
                                continue;
                            }
                            _entryMap.Add(new Key(category, line), new Entry(line.GetValue(), line.count));
                        }
                    } catch (Exception ex) {
                        _log(ex.ToString(), MainWindow.Flair.Error);
                    }
                }
            }

            _liveUpdateTask?.Dispose();
            if (_config.FlagLiveUpdate) {
                _liveUpdateTask = new Timer(LiveUpdate, null, _config.LiveUpdateDelayMs, _config.LiveUpdateDelayMs);
            }
        }

        /// <summary>
        /// Periodically downloads price data
        /// </summary>
        /// <param name="state">Literally null</param>
        private void LiveUpdate(object state) {
            if (!_config.FlagLiveUpdate) {
                return;
            }

            _log("Updating prices", MainWindow.Flair.Info);
            Download();
            _log("Prices updated", MainWindow.Flair.Info);
        }

        //-----------------------------------------------------------------------------------------------------------
        // Generic methods
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Get Entry instances associated with provided keys.
        /// Objects are ordered as the provided keys
        /// </summary>
        /// <param name="key"></param>
        /// <returns>List of Entry objects</returns>
        public Entry GetEntry(Key key) {
            _entryMap.TryGetValue(key, out var entry);
            return entry;
        }
    }
}