using EasyBuyout.League;
using EasyBuyout.Settings;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using EasyBuyout.Item;
using EasyBuyout.Settings.Source;

namespace EasyBuyout.Prices {
    /// <summary>
    /// Handles downloading, managing and translating price data from various websites
    /// </summary>
    public class PriceManager {
        private readonly JavaScriptSerializer _javaScriptSerializer;
        private readonly WebClient _webClient;
        private readonly Action<string, MainWindow.Flair> _log;
        private readonly Dictionary<Key, Entry> _entryMap;
        private Timer _liveUpdateTask;
        private readonly Config _config;

        // Accessing SettingsWindow's progressbar
        private readonly Action _incProgressBar;
        private readonly Action<int> _configureProgressBar;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        /// <param name="webClient"></param>
        /// <param name="log"></param>
        /// <param name="incProgressBar"></param>
        /// <param name="configureProgressBar"></param>
        public PriceManager(Config config, WebClient webClient, Action<string, MainWindow.Flair> log,
            Action incProgressBar, Action<int> configureProgressBar) {
            _config = config;
            _webClient = webClient;
            _log = log;
            _incProgressBar = incProgressBar;
            _configureProgressBar = configureProgressBar;

            _javaScriptSerializer = new JavaScriptSerializer {MaxJsonLength = int.MaxValue};
            _entryMap = new Dictionary<Key, Entry>();
        }

        //-----------------------------------------------------------------------------------------------------------
        // Data download
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Picks download source depending on source selection
        /// </summary>
        public void Download(string league) {
            var categoryCount = 0;
            foreach (var api in _config.Source.SourceApis) {
                categoryCount += api.Categories.Count;
            }

            _configureProgressBar(categoryCount);

            _entryMap.Clear();
            foreach (var api in _config.Source.SourceApis) {
                Download2(api, league);
            }

            _liveUpdateTask?.Dispose();
            if (_config.FlagLiveUpdate) {
                _liveUpdateTask = new Timer(LiveUpdate, null, _config.LiveUpdateDelayMs, _config.LiveUpdateDelayMs);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceApi"></param>
        /// <param name="league"></param>
        private void Download2(SourceApi sourceApi, string league) {
            foreach (var category in sourceApi.Categories) {
                _log($"Fetching: {category} for {league}", MainWindow.Flair.Info);

                try {
                    var url = sourceApi.Url.Replace("{league}", league).Replace("{category}", category);
                    var jsonString = _webClient.DownloadString(url);

                    // Deserialize
                    var entryDict = _javaScriptSerializer.Deserialize<PoeNinjasEntryDict>(jsonString);

                    if (entryDict == null) {
                        _log($"[{league}] Reply was null for {category}", MainWindow.Flair.Error);
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
                } finally {
                    _incProgressBar();
                }
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
            Download(_config.SelectedLeague);
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