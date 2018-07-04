using EasyBuyout.Settings;
using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;

namespace EasyBuyout.League {
    public class LeagueManager {
        private readonly WebClient webClient;
        private string[] leagues;
        private string selectedLeague;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="webClient"></param>
        public LeagueManager(WebClient webClient) {
            this.webClient = webClient;
        }

        /// <summary>
        /// Get list of active leagues
        /// </summary>
        public void Run() {
            MainWindow.Log("Updating league list...", 0);

            List<LeagueEntry> leagueEntries = DownloadLeagueList();
            if (leagueEntries == null) {
                MainWindow.Log("Unable to update leagues", 2);
                return;
            }

            leagues = new string[leagueEntries.Count];

            for (int i = 0; i < leagueEntries.Count; i++) {
                leagues[i] = leagueEntries[i].id;
            }

            MainWindow.Log("League list updated", 0);
        }

        /// <summary>
        /// Downloads active leagues from PoE's official API
        /// </summary>
        /// <returns>List of active leagues or null on failure</returns>
        private List<LeagueEntry> DownloadLeagueList() {
            // WebClient can only handle one connection per instance.
            // It is bad practice to have multiple WebClients.
            while (webClient.IsBusy) System.Threading.Thread.Sleep(10);

            try {
                string jsonString = webClient.DownloadString(Config.poeLeagueAPI);
                return new JavaScriptSerializer().Deserialize<LeagueParcel>(jsonString).result;
            } catch (Exception ex) {
                Console.WriteLine(ex);
                return null;
            }
        }

        //-----------------------------------------------------------------------------------------------------------
        // Getters and setters
        //-----------------------------------------------------------------------------------------------------------

        public void SetSelectedLeague(string league) {
            selectedLeague = league;
        }

        public string[] GetLeagues() {
            return leagues;
        }

        public string GetSelectedLeague() {
            return selectedLeague;
        }
    }
}
