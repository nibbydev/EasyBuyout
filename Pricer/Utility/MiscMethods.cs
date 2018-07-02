using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Net;

namespace EasyBuyout.Utility {
    class MiscMethods {
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
