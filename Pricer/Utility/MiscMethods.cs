using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace Pricer.Utility {
    class MiscMethods {
        /// <summary>
        /// Get list of active leagues from the official PoE website
        /// </summary>
        /// <returns>List of active leagues</returns>
        public static string[] GetLeagueList() {
            try {
                // Download JSON-encoded string
                string jsonString = MainWindow.webClient.DownloadString("https://www.pathofexile.com/api/trade/data/leagues");

                // Deserialize JSON string
                Dictionary<string, List<Dictionary<string, string>>> tempDict = new JavaScriptSerializer()
                    .Deserialize<Dictionary<string, List<Dictionary<string, string>>>>(jsonString);

                // Init returnString
                string[] returnList = new string[tempDict["result"].Count];

                // Add all values from temp list to returnString list
                int counter = 0;
                foreach (Dictionary<string, string> item in tempDict["result"]) {
                    returnList[counter] = item["id"];
                    counter++;
                }

                return returnList;
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }

            // If an error occured return null
            return null;
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
