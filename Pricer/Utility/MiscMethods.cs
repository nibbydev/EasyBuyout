using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Net;

namespace Pricer.Utility {
    class MiscMethods {
        /// <summary>
        /// Get list of active leagues from the official PoE website
        /// </summary>
        /// <returns>List of active leagues</returns>
        public static string[] GetLeagueList(WebClient webClient) {
            try {
                // Download JSON-encoded string
                string jsonString = webClient.DownloadString("https://www.pathofexile.com/api/trade/data/leagues");

                // Deserialize JSON string
                LeagueList leagueList = new JavaScriptSerializer().Deserialize<LeagueList>(jsonString);

                // Init returnString
                string[] returnList = new string[leagueList.result.Count];

                // Add all values from temp list to returnString list
                for (int i = 0; i < leagueList.result.Count; i++) {
                    returnList[i] = leagueList.result[i]["id"];
                }

                return returnList;
            } catch (Exception ex) {
                Console.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Encodes text in base64, used for https://poeprices.info API calls
        /// </summary>
        /// <param name="text">Raw item data</param>
        /// <returns>Base64 encoded raw item data</returns>
        public static string Base64Encode(string text) {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(text);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
