using System;
using System.Text.RegularExpressions;

namespace Pricer {
    public class Item {
        public string rarity, name, type, key = "";
        public string[] splitRaw;
        public int stackSize;
        public volatile bool discard = false;
        public int gem_q; // Only available for gems
        public string raw;
        public int errorCode;

        public Item(string raw) {
            // All Ctrl+C'd item data must contain "--------" or "Rarity:"
            if (!raw.Contains("--------")) return;
            if (!raw.Contains("Rarity: ")) return;

            // Add to raw
            this.raw = raw;

            // Format the data and split it into an array
            ParseRaw();

            // Checks what type of item is present and calls appropriate methods
            ParseData();
        }

        /**
         * Parser methods
        **/

        // Converts input into an array, replaces newlines with delimiters, splits groups
        private void ParseRaw() {
            // Expects input in the form of (whatever copied itemdata looks like):
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

            string formattedRaw = raw.Replace("--------", "::");
            formattedRaw = formattedRaw.Replace("\r\n", "|");
            formattedRaw = formattedRaw.Remove(formattedRaw.Length - 1);

            // At this point the input looks like this:
            // "Rarity: Normal|Majestic Plate|::|Armour: 530|::|Requirements:|Level: 53|Str: 144|::|Sockets: B-R |::|Item Level: 100"

            splitRaw = formattedRaw.Split(new string[] { "|::|" }, StringSplitOptions.None);

            // Then we get something like this:
            // {
            //     "Rarity: Normal|Majestic Plate",
            //     "Armour: 530",
            //     "Requirements:|Level: 53|Str: 144",
            // 	   "Sockets: B-R ",
            // 	   "Item Level: 100"
            // }
        }

        // Checks what data is present and then calls parse methods
        private void ParseData() {
            // Index 0 will always contain rarity/name/type info
            ParseData_Rarity(splitRaw[0]);

            // Do some error code assignment
            foreach (string line in splitRaw) { 
                if (line.Contains("Unidentified")) {
                    errorCode = 1;
                    break;
                } else if (line.Contains("Note: ")) {
                    errorCode = 2;
                    break;
                }
            }

            // Call methods based on item type
            switch (rarity) {
                case "Gem":
                    Parse_GemData();
                    break;
                case "Divination Card":
                    Parse_DivinationData();
                    break;
                case "Unique":
                    Parse_Unique(); 
                    break;
                case "Currency": // Contains essence and currency
                    Parse_Currency();
                    break;
                default:
                    Parse_Default();
                    break;
            }
        }

        // Makes specific key when item is a gem
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

            // Store the quality for later use
            gem_q = quality;

            // Last line will contain "Note:" if item has a note
            bool isCorrupted = splitRaw[splitRaw.Length - 1].Contains("Corrupted");

            // Different sources have different gem categories
            if (Settings.source.ToLower() == "poe.ovh") {
                // Special gems have special needs
                if (name.Contains("Empower") || name.Contains("Enlighten") || name.Contains("Enhance")) {
                    if (quality < 6) quality = 0;
                    else if (quality < 16) quality = 10;
                    else if (quality >= 16) quality = 20;

                    // Quality doesn't matter for lvl 3 and 4
                    if (level > 2) quality = 0;
                } else {
                    if (level < 7) level = 1;           // 1  = 1,2,3,4,5,6
                    else if (level < 19) level = 10;    // 10 = 7,8,9,10,11,12,13,14,15,16,17,18
                    else if (level < 21) level = 20;    // 20 = 19,20
                                                        // 21 = 21

                    if (quality < 7) quality = 0;           // 0  = 0,1,2,3,4,5,6
                    else if (quality < 18) quality = 10;    // 10 = 7,8,9,10,11,12,13,14,15,16,17
                    else if (quality < 23) quality = 20;    // 20 = 16,17,18,19,20,21,22
                                                            // 23 = 23

                    // Gets rid of specific gems
                    if (level < 20 && quality > 20) quality = 20;           // |4| 1|23|1 and |4|10|23|1
                    else if (level == 21 && quality < 20) quality = 0;      // |4|21|10|1

                    if (level < 20 && quality < 20) isCorrupted = false;
                    if (key.Contains("Vaal")) isCorrupted = true;
                }
            } else if (Settings.source.ToLower() == "poe.ninja") {
                // Special gems have special needs
                if (name.Contains("Empower") || name.Contains("Enlighten") || name.Contains("Enhance")) {
                    if (level == 1) {
                        // Quality can only be 0(1,2,3,4,5,6,7) otherwise set it to -1
                        if (quality < 8) quality = 0;
                        else quality = -1;
                    } else if (level == 2) {
                        // Quality can be either 0(1,2,3,4,5,6,7) or 20(17,18,19,20) otherwise set it to -1
                        if (quality < 8) {
                            quality = 0;
                            isCorrupted = false;
                        } else if (quality > 16) {
                            quality = 20;
                            isCorrupted = true;
                        } else quality = -1;
                    } else if (level == 3) {
                        // Quality can be either 0(1,2,3,4,5,6,7) or 20(17,18,19,20) otherwise set it to -1
                        if (quality < 8) {
                            quality = 0;
                            isCorrupted = true;
                        } else if (quality > 16) {
                            quality = 20;
                            isCorrupted = false;
                        } else quality = -1;
                    } else if (level == 4) {
                        quality = 0;
                    }
                } else {
                    if (key.Contains("Vaal")) {
                        // Level can only be 20(17,18,19,20) otherwise set it to -1
                        if (level > 16) level = 20;
                        else level = -1;

                        // Quality can be either 0(1,2,3,4,5,6,7) or 20(17,18,19,20) otherwise set it to -1
                        if (quality < 8) quality = 0;
                        else if (quality > 16) quality = 20;
                        else quality = -1;
                    } else {
                        // Level can be either 1(1,2,3,4,5,6,7) 20(17,18,19,20) or 21(21) otherwise set it to -1
                        if (level < 8) level = 1;
                        else if (level > 16 && level < 21) level = 20;
                        else if (level != 21) level = -1;

                        // Match quality
                        switch (level) {
                            case 1:
                                // Quality can only be 20(17,18,19,20) otherwise set it to -1
                                if (quality > 16) {
                                    isCorrupted = true;
                                    quality = 20;
                                } else quality = -1;
                                break;
                            case 20:
                                // Quality can be either 0(1,2,3,4,5,6,7) or 20(17,18,19,20) or 23(23) otherwise set it to -1
                                if (quality < 8) quality = 0;
                                else if (quality > 16 && quality < 23) quality = 20;
                                else if (quality == 23) quality = 23;
                                else quality = -1;
                                break;
                            case 21:
                                // Quality can be either 0(1,2,3,4,5,6,7) or 20(17,18,19,20) otherwise set it to -1
                                if (quality < 8) quality = 0;
                                else if (quality > 16) quality = 20;
                                else quality = -1;
                                break;
                            default:
                                break;
                        }
                    }
                }

                // Poe.ninja does not have enough data to price most gems
                if (quality < 0 || level < 0) {
                    errorCode = 3;
                    discard = true;
                }
            }

            // Build key in the format of "<gem name>|4|<lvl>|<quality>"
            key = name + "|4|" + level + "|" + quality;

            // Add corruption to key
            if (isCorrupted) key += "|1";
            else key += "|0";
        }

        // Makes specific key when item is a divination card
        private void Parse_DivinationData() {
            // Build key in the format of "A Mother's Parting Gift|6"
            key = name + "|" + 6;
        }

        // Makes specific key when item is a currency
        private void Parse_Currency() {
            // Build key in the format of "Alchemy Shard|5"
            key = name + "|" + 5;
        }

        // Makes specific key when item is unique
        private void Parse_Unique() {
            // Can't price unidentified uniques
            if (errorCode == 1) {
                discard = true;
                return;
            }
            
            // Format base key
            key = name + "|" + type + "|3";

            // Find what index socket data has
            int i;
            for (i = 0; i < splitRaw.Length; i++) if (splitRaw[i].StartsWith("Sockets:")) break;

            // Decide whether to add link suffix or not
            if (splitRaw.Length > i) {
                int links = ParseData_Links(splitRaw[i]);
                if (links > 4) key += "|" + links + "L";
            }

            // Check if the item has special variants
            key += ParseData_Variant();
        }

        // Makes specific key when item is of normal rarity
        // (this includes maps, fragments, prophecies and maybe others aswell)
        private void Parse_Default() {
            int frameType = 0;

            // Loop through lines, checking if the item contains prophecy text
            // (They have frameType 8 but Ctrl+C shows them as "Normal")
            for (int i = splitRaw.Length - 1; i > 0; i--) {
                if (splitRaw[i].Contains("Right-click to add this prophecy to your character")) {
                    frameType = 8;
                    break;
                } else if (splitRaw[i].Contains("Travel to this Map by using it in the Templar Laboratory or a personal Map Device")) {
                    frameType = 0;
                    
                    if (rarity == "Rare") {
                        name = type;
                        type = null;
                    }

                    if (name.Contains("Superior ")) name = name.Remove(0, 9);

                    break;
                } else if (rarity == "Normal") {
                    frameType = 0;
                } else if (rarity == "Magic") {
                    frameType = 1;
                } else if (rarity == "Rare") {
                    frameType = 2;
                }
            }

            // Start building the database key
            key = name;

            // If the item has a type, add it to the key
            if (type != null) key += "|" + type;

            // Append frameType to the key
            key += "|" + frameType;
        }

        // Takes input as "Item Level: 55" or "Sockets: B - B B " and returns "55" or "B - B B"
        private string TrimProperty(string str) {
            // Convert "Sockets: B - B B " -> "B - B B"
            int tempIndex = str.IndexOf(' ') + 1;
            return str.Substring(tempIndex, str.Length - tempIndex).Trim();
        }

        /**
         * Parse methods
        **/

        // Takes input as "Rarity: Gem|Faster Attacks Support"
        private void ParseData_Rarity(string str) {
            // Examples of item rarity categories:
            //     Div cards: "Rarity: Divination Card|Rain of Chaos"
            //     Gem: "Rarity: Gem|Faster Attacks Support"
            //     Unique: "Rarity: Unique|Inpulsa's Broken Heart|Sadist Garb"

            // Rarity and item name will always be under data[0]
            string[] splitStr = str.Split('|');

            // Get rarity. [0] is "Rarity: Magic"
            rarity = TrimProperty(splitStr[0]);

            // Get name. [1] is "Majestic Plate" or "Radiating Samite Gloves of the Penguin"
            name = splitStr[1];

            // If item has a type, add it to the key
            if (splitStr.Length > 2) type = splitStr[2];
        }

        // Converts "Sockets: B G-R-R-G-R " -> "B G-R-R-G-R"
        private int ParseData_Links(string str) {
            // Remove useless info from socket data
            str = TrimProperty(str);

            int[] links = { 1, 1, 1, 1, 1 };
            int counter = 0;

            // Loop through, counting links
            for (int i = 1; i < str.Length; i += 2) {
                if (str[i] != '-') counter++;
                else links[counter]++;
            }

            // Reuse, reduce, recycle
            counter = 0;

            // Find largest element
            foreach (int link in links) if (link > counter) counter = link;

            // Return largest link
            return counter;
        }

        // Expects input in the form of "Stack Size: 2/20"
        private void ParseData_StackSize(string str) {
            // "Stack Size: 2,352/10" -> "Stack Size: 2352/10"
            if(str.Contains(",")) str = str.Remove(str.IndexOf(','), 1);

            // Remove spaces: "Stack Size: 11/20" -> "StackSize:11/20"
            str = String.Join(null, str.Split(' '));

            // Extract stack size: "StackSize:11/20" -> "11"
            string stackSize = str.Substring(str.IndexOf(':') + 1, str.IndexOf('/') - str.IndexOf(':') - 1);

            this.stackSize = Int32.Parse(stackSize);
        }

        // Special items have multiple variants, distinguish them
        private string ParseData_Variant() {
            // Find the index of "Item Level:"
            int exModIndex;
            for (exModIndex = 0; exModIndex < splitRaw.Length; exModIndex++)
                if (splitRaw[exModIndex].StartsWith("Item Level:")) break;


            // Check variation
            switch (name) {
                case "Atziri's Splendour":
                    string[] splitExplicitMods = splitRaw[exModIndex + 1].Split('|');
                    
                    // Do some magic :)
                    switch (String.Join("#", Regex.Split(splitExplicitMods[0], @"\d+"))) {
                        case "#% increased Armour, Evasion and Energy Shield":
                            return "|var:ar/ev/es";
                        case "#% increased Armour and Energy Shield":
                            if (splitExplicitMods[1].Contains("Life")) return "|var:ar/es/li";
                            else return "|var:ar/es";
                        case "#% increased Evasion and Energy Shield":
                            if (splitExplicitMods[1].Contains("Life")) return "|var:ev/es/li";
                            else return "|var:ev/es";
                        case "#% increased Armour and Evasion":
                            return  "|var:ar/ev";
                        case "#% increased Armour":
                            return "|var:ar";
                        case "#% increased Evasion Rating":
                            return "|var:ev";
                        case "+# to maximum Energy Shield":
                            return "|var:es";
                        default:
                            break;
                    }
                    break;

                case "Vessel of Vinktar":
                    foreach (string mod in splitRaw[exModIndex + 1].Split('|')) {
                        if (mod.Contains("to Spells"))
                            return "|var:spells";
                        else if (mod.Contains("to Attacks"))
                            return "|var:attacks";
                        else if (mod.Contains("Converted to"))
                            return "|var:conversion";
                        else if (mod.Contains("Penetrates"))
                            return "|var:penetration";
                    }
                    break;

                case "Doryani's Invitation":
                    foreach (string mod in splitRaw[exModIndex + 2].Split('|')) {
                        if (mod.Contains("Lightning Damage"))
                            return "|var:lightning";
                        else if (mod.Contains("Fire Damage"))
                            return "|var:fire";
                        else if (mod.Contains("Cold Damage"))
                            return "|var:cold";
                        else if (mod.Contains("Physical Damage"))
                            return "|var:physical";
                    }
                    break;

                case "Yriel's Fostering":
                    foreach (string mod in splitRaw[exModIndex + 1].Split('|')) {
                        if (mod.Contains("Chaos Damage"))
                            return "|var:chaos";
                        else if (mod.Contains("Physical Damage"))
                            return "|var:physical";
                        else if (mod.Contains("Attack and Movement"))
                            return "|var:speed";
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
                            return "|var:lightning";
                        else if (mod.Contains("Fire Damage"))
                            return "|var:fire";
                        else if (mod.Contains("Cold Damage"))
                            return "|var:cold";
                    }
                    break;

                case "Impresence":
                    foreach (string mod in splitRaw[exModIndex + 2].Split('|')) {
                        if (mod.Contains("Lightning Damage"))
                            return "|var:lightning";
                        else if (mod.Contains("Fire Damage"))
                            return "|var:fire";
                        else if (mod.Contains("Cold Damage"))
                            return "|var:cold";
                        else if (mod.Contains("Physical Damage"))
                            return "|var:physical";
                        else if (mod.Contains("Chaos Damage"))
                            return "|var:chaos";
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
                            return "|var:2";
                        case "Has 1 Abyssal Socket":
                            return "|var:1";
                    }

                    break;
                default:
                    return "";
            }

            // Nothing matched, eh?
            Console.WriteLine("Unmatched variant item: " + raw);
            return "";
        }
    }
}
