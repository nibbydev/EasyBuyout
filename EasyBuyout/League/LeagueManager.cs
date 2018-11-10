using EasyBuyout.Settings;
using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;

namespace EasyBuyout.League {
    public class LeagueManager {
        private readonly Config _config;
        private readonly Action<string, MainWindow.Flair> _logAction;
        private readonly WebClient _webClient;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        /// <param name="webClient"></param>
        /// <param name="logAction"></param>
        public LeagueManager(Config config, WebClient webClient, Action<string, MainWindow.Flair> logAction) {
            _config = config;
            _webClient = webClient;
            _logAction = logAction;
        }

        /// <summary>
        /// Get list of active leagues
        /// </summary>
        public string[] GetLeagueList() {
            var leagueEntries = DownloadLeagueList();

            if (leagueEntries == null) {
                return null;
            }

            var leagues = new string[leagueEntries.Count];

            for (var i = 0; i < leagueEntries.Count; i++) {
                leagues[i] = leagueEntries[i].id;
            }

            return leagues;
        }

        /// <summary>
        /// Downloads active leagues from PoE's official API
        /// </summary>
        /// <returns>List of active leagues or null on failure</returns>
        private List<LeagueEntry> DownloadLeagueList() {
            // WebClient can only handle one connection per instance. It is bad practice to have multiple WebClients.
            while (_webClient.IsBusy) {
                System.Threading.Thread.Sleep(10);
            }

            try {
                var jsonString = _webClient.DownloadString(_config.LeagueApiUrl);
                return new JavaScriptSerializer().Deserialize<LeagueParcel>(jsonString).result;
            } catch (Exception ex) {
                _logAction(ex.ToString(), MainWindow.Flair.Error);
                return null;
            }
        }
    }
}