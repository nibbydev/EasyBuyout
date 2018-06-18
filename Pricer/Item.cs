using System;
using System.Text.RegularExpressions;

namespace Pricer {
    public class Item {
        public string key;
        public int errorCode;
        public volatile bool discard;

        private readonly string raw;
        private readonly string[] splitRaw;

        // Keybuilding variables
        private int links, frame;
        private string rarity, variant, name, type;
        private int level, quality, corrupted;

        public Item(string raw) {
            // All Ctrl+C'd item data must contain "--------" or "Rarity:"
            if (!raw.Contains("--------") || !raw.Contains("Rarity: ")) {
                errorCode = 1;
                return;
            }

            this.raw = raw;

            // Format the data and split it into a list
            splitRaw = SplitParseRaw(raw);
        }

        //-----------------------------------------------------------------------------------------------------------
        // Main parsing methods
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts input into an array, replaces newlines with delimiters, splits groups
        /// </summary>
        /// <param name="raw">Raw item data string</param>
        /// <returns>Formatted list of itemdata groups</returns>
        private static string[] SplitParseRaw(string raw) {
            // Expects input in the form of whatever copied itemdata looks like:
            //     Rarity: Normal
            //     Majestic Plate
            //     --------
            //     Armour: 530
            //     --------
            //     Requirements:
            //     Level: 53
            //     Str: 144
            //     --------
            //     Sockets: B-R 
            //     --------
            //     Item Level: 100

            // Convert it to a string like this:
            // "Rarity: Normal|Majestic Plate|::|Armour: 530|::|Requirements:|Level: 53|Str: 144|::|Sockets: B-R |::|Item Level: 100"

            string formattedRaw = raw.Replace("--------", "::");
            formattedRaw = formattedRaw.Replace("\r\n", "|");
            formattedRaw = formattedRaw.Remove(formattedRaw.Length - 1);

            // Then get something like this:
            // {
            //     "Rarity: Normal|Majestic Plate",
            //     "Armour: 530",
            //     "Requirements:|Level: 53|Str: 144",
            // 	   "Sockets: B-R ",
            // 	   "Item Level: 100"
            // }

            return formattedRaw.Split(new string[] { "|::|" }, StringSplitOptions.None);
        }

        /// <summary>
        /// Checks what data is present and then calls specific parse methods
        /// </summary>
        public void ParseData() {
            // Check if item is pricable
            foreach (string line in splitRaw) { 
                if (line.StartsWith("Unidentified")) {
                    errorCode = 2;
                    break;
                } else if (line.StartsWith("Note: ")) {
                    errorCode = 3;
                    break;
                }
            }

            // Find name, type and rarity
            string[] splitLine = splitRaw[0].Split('|');
            rarity = TrimProperty(splitLine[0]);
            name = splitLine[1];
            if (splitLine.Length > 2) type = splitLine[2];

            // Call specific parse methods based on item rarity
            switch (rarity) {
                case "Gem":
                    frame = 4;
                    Parse_GemData();
                    break;

                case "Divination Card":
                    frame = 6;
                    break;

                case "Unique":
                    frame = 3;

                    // Can't price unidentified uniques
                    if (errorCode == 2) {
                        discard = true;
                        return;
                    }

                    // Check if the item has more than 4 links
                    links = ParseData_Sockets();
                    // Check if the item has special variants
                    variant = ParseData_Variant();

                    break;

                case "Currency":
                    frame = 5;
                    break;

                default:
                    frame = Parse_DefaultData();
                    // Unknown rarity
                    if (frame == -1) errorCode = 5;
                    break;
            }

            key = BuildKey();
        }

        /// <summary>
        /// Constructs a key for the item
        /// </summary>
        /// <returns></returns>
        private string BuildKey() {
            string key = name;
            if (type != null) key += ":" + type;
            key += "|" + frame;

            if (frame == 4) {
                key += "|l:" + level;
                key += "|q:" + quality;
                key += "|c:" + corrupted;
            } else {
                if (links > 4) key += "|links:" + links;
                if (variant != null) key += "|var:" + variant;
            }

            return key;
        }

        //-----------------------------------------------------------------------------------------------------------
        // Specific parsing methods
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Parses item data when item is a gem
        /// </summary>
        private void Parse_GemData() {
            int level = 0, quality = 0;

            // Index 2 in data will contain gem info (if its a gem)
            foreach (string s in splitRaw[1].Split('|')) {
                if (s.Contains("Level:")) {
                    level = Int32.Parse(new Regex(@"\d+").Match(s).Value);
                } else if (s.Contains("Quality:")) {
                    quality = Int32.Parse(new Regex(@"\d+").Match(s).Value);
                }
            }

            // Last line will contain "Note:" if item has a note
            bool isCorrupted = splitRaw[splitRaw.Length - 1].Contains("Corrupted");

            // Special gems have special needs
            if (name.Contains("Empower") || name.Contains("Enlighten") || name.Contains("Enhance")) {
                if (quality < 10) quality = 0;
                else quality = 20;

                // Quality doesn't matter for lvl 3 and 4
                if (level > 2) level = 0;
            } else {
                if (level < 15) level = 1;          // 1  = 1,2,3,4,5,6,7,8,9,10,11,12,13,14
                else if (level < 21) level = 20;    // 20 = 15,16,17,18,19,20
                                                    // 21 = 21

                if (quality < 17) quality = 0;          // 0  = 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16
                else if (quality < 22) quality = 20;    // 20 = 17,18,19,20,21
                                                        // 22,23 = 23
                // Gets rid of specific gems
                if (level < 20 && quality > 20) quality = 20;         // |4| 1|23|1 and |4|10|23|1
                else if (level == 21 && quality < 20) quality = 0;    // |4|21|10|1

               
                if (!name.Contains("Vaal")) {
                    if (level < 20 && quality < 20) isCorrupted = false;
                }
            }

            this.level = level;
            this.quality = quality;
            this.corrupted = isCorrupted ? 1 : 0;
        }

        /// <summary>
        /// Parses item data when item is of normal rarity
        /// Includes maps, fragments, prophecies and maybe others
        /// </summary>
        /// <returns>Item's frametype</returns>
        private int Parse_DefaultData() {
            // Loop through lines, checking if the item contains prophecy or map text
            foreach (string line in splitRaw) {
                if (line.Contains("Right-click to add this prophecy to your character")) {
                    return 8;
                } else if (line.Contains("Travel to this Map by using it in the Templar Laboratory or a personal Map Device")) {
                    // Count rare maps as normal
                    if (rarity == "Rare") {
                        name = type;
                        type = null;
                    }

                    if (name.Contains("Superior ")) name = name.Remove(0, 9);

                    return 0;
                } else if (line.Equals("Relic Unique")) {
                    return 9;
                }
            }

            if (rarity == "Normal") return 0;
            else if (rarity == "Magic") return 1;
            else if (rarity == "Rare") return 2;
            else return -1;
        }

        //-----------------------------------------------------------------------------------------------------------
        // Generic parsing methods
        //-----------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Finds the index of the element the element in the haystack that starts with the needle
        /// </summary>
        /// <param name="needle">String to search for</param>
        /// <param name="haystack">Array to search it in</param>
        /// <returns></returns>
        private static int FindIndexOf(string needle, string[] haystack) {
            for (int i = 0; i < haystack.Length; i++) {
                if (haystack[i].StartsWith(needle)) {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Parses socket data if present and finds largest link
        /// </summary>
        private int ParseData_Sockets() {
            // Find what index socket data is under
            int index = FindIndexOf("Sockets:", splitRaw);
            // If it's still 0 then item doesn't have socket data
            if (index == -1) return 0;
            // Assign socket string: "Sockets: B G-R-R-G-R " -> "B G-R-R-G-R"
            string socketData = TrimProperty(splitRaw[index]);

            int[] tempLinks = { 1, 1, 1, 1, 1 };
            int counter = 0;

            // Loop through, counting links
            for (int i = 1; i < socketData.Length; i += 2) {
                if (socketData[i] != '-') counter++;
                else tempLinks[counter]++;
            }

            // Find largest link
            foreach (int tempLink in tempLinks) {
                if (tempLink > 4) return tempLink;
            }

            // If largest < 5
            return 0;
        }

        /// <summary>
        /// As some specific items have multiple variants, distinguish them
        /// </summary>
        /// <returns>Variant string</returns>
        private string ParseData_Variant() {
            // Find the index of "Item Level:"
            int exModIndex = FindIndexOf("Item Level:", splitRaw);
            // Couldn't find index of item level
            if (exModIndex == -1) return null;

            // Check variation
            switch (name) {
                case "Atziri's Splendour":
                    string[] splitExplicitMods = splitRaw[exModIndex + 1].Split('|');

                    switch (String.Join("#", Regex.Split(splitExplicitMods[0], @"\d+"))) {
                        case "#% increased Armour, Evasion and Energy Shield":
                            return "ar/ev/es";
                        case "#% increased Armour and Energy Shield":
                            if (splitExplicitMods[1].Contains("Life")) return "ar/es/li";
                            else return "ar/es";
                        case "#% increased Evasion and Energy Shield":
                            if (splitExplicitMods[1].Contains("Life")) return "ev/es/li";
                            else return "ev/es";
                        case "#% increased Armour and Evasion":
                            return "ar/ev";
                        case "#% increased Armour":
                            return "ar";
                        case "#% increased Evasion Rating":
                            return "ev";
                        case "+# to maximum Energy Shield":
                            return "es";
                        default:
                            break;
                    }
                    break;

                case "Vessel of Vinktar":
                    foreach (string mod in splitRaw[exModIndex + 1].Split('|')) {
                        if (mod.Contains("to Spells"))
                            return "spells";
                        else if (mod.Contains("to Attacks"))
                            return "attacks";
                        else if (mod.Contains("Converted to"))
                            return "conversion";
                        else if (mod.Contains("Penetrates"))
                            return "penetration";
                    }
                    break;

                case "Doryani's Invitation":
                    foreach (string mod in splitRaw[exModIndex + 2].Split('|')) {
                        if (mod.Contains("Lightning Damage"))
                            return "lightning";
                        else if (mod.Contains("Fire Damage"))
                            return "fire";
                        else if (mod.Contains("Cold Damage"))
                            return "cold";
                        else if (mod.Contains("Physical Damage"))
                            return "physical";
                    }
                    break;

                case "Yriel's Fostering":
                    foreach (string mod in splitRaw[exModIndex + 1].Split('|')) {
                        if (mod.Contains("Chaos Damage"))
                            return "snake";
                        else if (mod.Contains("Physical Damage"))
                            return "ursa";
                        else if (mod.Contains("Attack and Movement"))
                            return "rhoa";
                    }
                    break;

                case "Volkuur's Guidance":
                    // Figure out under what index are explicit mods located (cause of enchantments)
                    for (int i = exModIndex; i < splitRaw.Length; i++) {
                        if (splitRaw[i].Contains("to maximum Life")) {
                            exModIndex = i;
                            break;
                        }
                    }

                    // Figure out item variant
                    foreach (string mod in splitRaw[exModIndex].Split('|')) {
                        if (mod.Contains("Lightning Damage"))
                            return "lightning";
                        else if (mod.Contains("Fire Damage"))
                            return "fire";
                        else if (mod.Contains("Cold Damage"))
                            return "cold";
                    }
                    break;

                case "Impresence":
                    foreach (string mod in splitRaw[exModIndex + 2].Split('|')) {
                        if (mod.Contains("Lightning Damage"))
                            return "lightning";
                        else if (mod.Contains("Fire Damage"))
                            return "fire";
                        else if (mod.Contains("Cold Damage"))
                            return "cold";
                        else if (mod.Contains("Physical Damage"))
                            return "physical";
                        else if (mod.Contains("Chaos Damage"))
                            return "chaos";
                    }
                    break;

                case "Lightpoacher":
                case "Shroud of the Lightless":
                case "Bubonic Trail":
                case "Tombfist":
                    // Figure out under what index are explicit mods located (cause of enchantments)
                    for (int i = exModIndex; i < splitRaw.Length; i++) {
                        if (splitRaw[i].Contains("Abyssal Socket")) {
                            exModIndex = i;
                            break;
                        }
                    }

                    // Check how many abyssal sockets the item has
                    switch (splitRaw[exModIndex].Split('|')[0]) {
                        case "Has 2 Abyssal Sockets":
                            return "2 sockets";
                        case "Has 1 Abyssal Socket":
                            return "1 socket";
                    }

                    break;

                case "The Beachhead":
                    // Find the line from itemdata that contains map tier info (e.g "Map Tier: 15")
                    foreach (string line in splitRaw) {
                        foreach (string prop in line.Split('|')) {
                            if (prop.StartsWith("Map Tier: ")) {
                                // Return tier info (e.g "15")
                                return prop.Substring(prop.IndexOf(' ', prop.IndexOf(' ') + 1) + 1);
                            }
                        }
                    }
                    break;

                case "Combat Focus":
                    foreach (string mod in splitRaw[exModIndex + 2].Split('|')) {
                        if (mod.Contains("choose Fire"))
                            return "fire";
                        else if (mod.Contains("choose Cold"))
                            return "cold";
                        else if (mod.Contains("choose Lightning"))
                            return "lightning";
                    }
                    break;

                default:
                    break;
            }

            // Nothing matched, eh?
            return null;
        }

        /// <summary>
        /// Takes input as "Item Level: 55" or "Sockets: B - B B " and returns "55" or "B - B B"
        /// </summary>
        /// <param name="str">"Sockets: B - B B "</param>
        /// <returns>"B - B B"</returns>
        private static string TrimProperty(string str) {
            int index = str.IndexOf(' ') + 1;
            return str.Substring(index, str.Length - index).Trim();
        }

        //-----------------------------------------------------------------------------------------------------------
        // Getters and setters
        //-----------------------------------------------------------------------------------------------------------

        public int GetFrame() {
            return frame;
        }

        public string GetRaw() {
            return raw;
        }
    }
}
