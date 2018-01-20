using System;
using System.Text.RegularExpressions;

namespace Pricer {
    class Item {
        public string rarity, name;
        public string[] mods, data;
        public int[] sockets; // socket data: 0=red 1=green 2=blue 3=white 4=abyss 5=misc
        public volatile bool match = false;
        public int stackSize;
        public bool corrupted = false;
        public string key;
        public bool hasPrice = false;
        public int level, quality;

        public Item(string raw) {
            // All Ctrl+C'd item data must contain "--------" or "Rarity:", otherwise it is not an item
            if (!raw.Contains("--------"))
                return;
            else if (!raw.Contains("Rarity:"))
                return;

            // Call parser methods
            ParseInput(raw);
            ParseGemData();
            BuildDataBaseKey();
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
            // Store the index to allow us to get explcitmods location
            int ilvl_index = 0;

            for (int i = 0; i < data.Length; i++) {
                if (data[i].Contains("Rarity:")) {
                    ParseData_Rarity(data[i]); // Done
                } else if (data[i].Contains("Sockets:")) {
                    ParseData_Socket(data[i]); // Done
                } else if (data[i].Contains("Item Level:")) {
                    ilvl_index = i;
                } else if (data[i].Contains("Stack Size:")) {
                    ParseData_StackSize(data[i]); // Done
                }
            }

            // Int32.Parse(new Regex(@"\d+").Match(splitStr[i]).Value)

            if (rarity == "Rare" || rarity == "Magic") { 
                if (data.Length == ilvl_index + 1) {
                    ParseData_Mods(data[data.Length - 1]);
                } else {
                    data[ilvl_index + 1] += "|" + data[data.Length - 1];
                    ParseData_Mods(data[ilvl_index + 1]);
                }
            }
        }

        // Checks what data is present and then calls parse methods
        private void ParseGemData() {
            string[] splitStr = data[0].Split('|');
            this.rarity = TrimProperty(splitStr[0]);
            this.name = splitStr[1];

            foreach (string s in data[1].Split('|')) {
                if (s.Contains("Level:")) {
                    this.level = Int32.Parse(new Regex(@"\d+").Match(s).Value);
                } else if (s.Contains("Quality:")) {
                    this.quality = Int32.Parse(new Regex(@"\d+").Match(s).Value);
                }
            }

            // Last line will contain "Note:" if item has a note
            if (data[data.Length - 1].Contains("Note:")) {
                this.hasPrice = true;

                // Second to last line will contain "corrupted" if gem is corrupted and has a note
                if (data[data.Length - 2].Contains("Corrupted")) this.corrupted = true;
            } else {
                // Last line will contain "corrupted" if gem is corrupted
                if (data[data.Length - 1].Contains("Corrupted")) this.corrupted = true;
            }
        }

        // Abyss|gems:skill|Abyssal Cry|4|0|20|0
        private void BuildDataBaseKey() {
            int temp_level, temp_quality;

            if (level < 20 && level > 18)
                temp_level = 20;
            else if (level > 19) 
                temp_level = level;
            else
                temp_level = 0;

            if (quality < 22 && quality > 17)
                temp_quality = 20;
            else if (quality > 21)
                temp_quality = 23;
            else
                temp_quality = 0;
            
            this.key = name + "|4|" + temp_level + "|" + temp_quality;

            if (corrupted) key += "|" + 1; else key += "|" + 0;
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

        // Takes input as "Rarity: Magic|Radiating Samite Gloves of the Penguin"
        private void ParseData_Rarity(string str) {
            // Examples of item rarity categories:
            // Normal:
            //     Rarity: Normal
            //     Majestic Plate
            // Magic:
            //     Rarity: Magic
            //     Vaporous Diamond Ring of the Penguin
            // Rare: 
            //     Rarity: Rare
            //     Dire Brand
            //     Spike-Point Arrow Quiver


            // Rarity and item name will always be under data[0]
            string[] splitStr = str.Split('|');

            // Get rarity. [0] is "Rarity: Magic"
            this.rarity = TrimProperty(splitStr[0]);

            // Get name. [1] is "Majestic Plate" or "Radiating Samite Gloves of the Penguin"
            this.name = splitStr[1];

            // If item is rare, [1] will have name and [2] item type
            if (splitStr.Length > 2) this.name += " " + splitStr[2];
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

        // Expects input in the form of "Requirements:|Level: 47|Int: 68"
        private void ParseData_Mods(string str) {
            string[] splitStr = str.Split('|');
            mods = new string[splitStr.Length];

            // "+27 to maximum Energy Shield"
            for (int i = 0; i < splitStr.Length; i++) {
                MatchCollection matches = new Regex(@"\d+").Matches(splitStr[i]);

                string values = "";
                foreach (Match match in matches) {
                    splitStr[i] = splitStr[i].Replace(match.Value, "#");
                    values += "|" + match.Value;
                }

                mods[i] = splitStr[i] + values;
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
