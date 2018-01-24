using System;
using System.Text.RegularExpressions;

namespace Pricer {
    class Item {
        public string rarity, name, type, key = "";
        public string[] data;
        public int[] sockets; // socket data: 0=red 1=green 2=blue 3=white 4=abyss 5=misc
        public int stackSize;
        public volatile bool discard = false;
        public int gem_q; // Only available for gems

        public Item(string raw) {
            // All Ctrl+C'd item data must contain "--------" or "Rarity:", otherwise it is not an item
            if (!raw.Contains("--------"))
                return;
            else if (!raw.Contains("Rarity:"))
                return;

            // Format the data and split it into an array
            ParseInput(raw);

            // Checks what type of item is present and calls appropriate methods
            ParseData();
        }

        /**
         * Parser methods
        **/

        // Converts input into an array, replaces newlines with delimiters, splits groups
        private void ParseInput(string raw) {
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

            raw = raw.Replace("--------", "::");
            raw = raw.Replace("\r\n", "|");
            raw = raw.Remove(raw.Length - 1);

            // At this point the input looks like this:
            // "Rarity: Normal|Majestic Plate|::|Armour: 530|::|Requirements:|Level: 53|Str: 144|::|Sockets: B-R |::|Item Level: 100"

            data = raw.Split(new string[] { "|::|" }, StringSplitOptions.None);

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
            ParseData_Rarity(data[0]);

            // Can't price unidentified items atm
            foreach(string line in data) { 
                if (line.Contains("Unidentified")) {
                    discard = true;
                    return;
                }
            }

            // It's pretty difficult to overwrite an existing note/price
            if (data[data.Length - 1].Contains("Note:")) {
                discard = true;
                return;
            }

            // Call methods based on item type
            switch (rarity) {
                case "Gem":
                    Parse_GemData2();
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

            // Add itemtype to key if needed
            if (type != null) key += "|" + type;
        }

        // Makes specific key when item is a gem
        private void Parse_GemData() {
            int level = 0, quality = 0, corrupted = 0;
            
            // Index 2 in data will contain gem info (if its a gem)
            foreach (string s in data[1].Split('|')) {
                if (s.Contains("Level:")) {
                    level = Int32.Parse(new Regex(@"\d+").Match(s).Value);
                } else if (s.Contains("Quality:")) {
                    quality = Int32.Parse(new Regex(@"\d+").Match(s).Value);
                }
            }

            // Last line will contain "Note:" if item has a note
            if (data[data.Length - 1].Contains("Corrupted")) corrupted = 1;

            // 19,20 = 20
            if (level < 20 && level > 18)
                level = 20;
            // 18,17,16.. = 0
            else if (level < 19)
                level = 0;

            // 21,20,19,18 = 20
            if (quality < 22 && quality > 17)
                quality = 20;
            // 22,23 = 23
            else if (quality > 21)
                quality = 23;
            // 17,16,15.. = 0
            else
                quality = 0;

            // Build key in the format of "Abyssal Cry|4|0|20|0"
            key = name + "|4|" + level + "|" + quality + "|" + corrupted;
        }

        // Makes specific key when item is a gem
        private void Parse_GemData2() {
            int level = 0, quality = 0, corrupted = 0;

            // Index 2 in data will contain gem info (if its a gem)
            foreach (string s in data[1].Split('|')) {
                if (s.Contains("Level:")) {
                    level = Int32.Parse(new Regex(@"\d+").Match(s).Value);
                } else if (s.Contains("Quality:")) {
                    quality = Int32.Parse(new Regex(@"\d+").Match(s).Value);
                }
            }

            // Store the quality for later use
            gem_q = quality;

            // Last line will contain "Note:" if item has a note
            if (data[data.Length - 1].Contains("Corrupted")) corrupted = 1;

            // 22,23 = 23
            if (quality > 21)
                quality = 23;
            // 17,16,15..11 = 20
            else if (quality > 10)
                quality = 20;
            // 10,9,8...0 = 0
            else
                quality = 0;
            
            // No need to round levels for special gems
            if (!name.Contains("Empower") && !name.Contains("Enlighten") && !name.Contains("Enhance")) {
                // 19,20 = 20
                if (level > 18 && level < 21)
                    level = 20;
                // 18,17,16.. = 0
                else if (level < 19)
                    level = 1;
            }

            // Build key in the format of "Abyssal Cry|4|0|20|0"
            key = name + "|4|" + level + "|" + quality + "|" + corrupted;
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
            key = name + "|" + type + "|3";
            type = null;
        }

        // Makes specific key when item is of normal rarity
        // (this includes maps, fragments, prophecies and maybe others aswell)
        private void Parse_Default() {
            int frameType = 0;

            // Loop through lines, checking if the item contains prophecy text
            // (They have frameType 8 but Ctrl+C shows them as "Normal")
            for (int i = data.Length - 1; i > 0; i--) {
                if (data[i].Contains("Right-click to add this prophecy to your character")) {
                    frameType = 8;
                    break;
                } else if (data[i].Contains("Travel to this Map by using it in the Templar Laboratory or a personal Map Device")) {
                    frameType = 0;
                    
                    if (rarity == "Rare") {
                        name = type;
                        type = null;
                    }

                    if (name.Contains("Superior ")) name = name.Remove(0, 9);

                    break;
                }
            }

            // Seems like it was a prophecy afterall. Build key in the format of "A Call into the Void|8"
            key = name + "|" + frameType;
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

        // Expects input in the form of "Sockets: B - B B "
        private void ParseData_Socket(string str) {
            // Converts "Sockets: B - B B " -> "B - B B"
            str = TrimProperty(str);

            this.sockets = new int[6];

            // Loop through "B - B B", counting socket colors
            for (int i = 0; i < (str.Length + 1); i += 2) {
                switch (str[i]) {
                    case 'R':
                        this.sockets[0]++;
                        break;
                    case 'G':
                        this.sockets[1]++;
                        break;
                    case 'B':
                        this.sockets[2]++;
                        break;
                    case 'W':
                        this.sockets[3]++;
                        break;
                    case 'A':
                        this.sockets[4]++;
                        break;
                    default:
                        this.sockets[5]++;
                        break;
                }
            }
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
    }
}
