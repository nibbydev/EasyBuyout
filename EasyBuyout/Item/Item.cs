using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EasyBuyout.Item
{
    public class Item
    {
        public readonly List<string> Errors = new List<string>();
        public volatile bool         Discard;
        public readonly Key          Key = new Key();

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="rawInput">Copied item data string</param>
        public Item(string rawInput)
        {
            var splitRaw = SplitRawInput(rawInput);
            // If any errors occured in split method
            if (Discard) return;

            // Process item data
            ParseItemData(splitRaw);
        }

        //-----------------------------------------------------------------------------------------------------------
        // Main parsing methods
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Converts input into an array, replaces newlines with delimiters, splits groups
        /// </summary>
        /// <returns>Formatted list of item data groups</returns>
        private string[] SplitRawInput(string rawInput)
        {
            // All Ctrl+C'd item data must contain "--------" or "Rarity:"
            if (!rawInput.Contains("--------") || !rawInput.Contains("Rarity: "))
            {
                Errors.Add("Clipboard contents did not match Ctrl+C'd item data pattern");
                Discard = true;
                return null;
            }

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

            var formattedRaw = rawInput.Replace("--------", "::");
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

            var splitRaw = formattedRaw.Split(new[] {"|::|"}, StringSplitOptions.None);

            // Check validity of item data
            foreach (var line in splitRaw)
            {
                if (line.StartsWith("Unidentified"))
                {
                    Errors.Add("Item is unidentified");
                    Discard = true;
                    break;
                }

                if (line.StartsWith("Note: "))
                {
                    Errors.Add("Cannot add buyout notes to items that already have them");
                    Discard = true;
                    break;
                }
            }

            return splitRaw;
        }

        /// <summary>
        /// Checks what data is present and then calls specific parse methods
        /// </summary>
        public void ParseItemData(string[] splitRaw)
        {
            // Find name, type and rarity
            var nameLine  = splitRaw[0].Split('|');
            var itemClass = TrimProperty(nameLine[0]);
            var rarity    = TrimProperty(nameLine[1]);

            Key.Name     = nameLine[2];
            Key.TypeLine = nameLine.Length > 3 ? nameLine[3] : null;

            if (rarity.Equals("Rare"))
            {
                Key.Name     = Key.TypeLine;
                Key.TypeLine = null;
            }

            // Find item's frame type
            Key.FrameType = Parse_FrameType(splitRaw, rarity);

            if (itemClass.Equals("Maps"))
            {
                var itemInfo = splitRaw[1].Split('|');
                Key.MapTier = int.Parse(TrimProperty(itemInfo[0]));
                return;

            }
            // If any errors occured in parse function
            if (Discard) return;

            switch (Key.FrameType)
            {
                case 3:
                    // Find largest link group, if present
                    Key.Links = ParseSockets(splitRaw);
                    // Find unique item variant, if present
                    Key.Variation = ParseVariant(splitRaw, Key.Name);
                    break;
                case 4:
                    ParseGemData(splitRaw, Key.Name);
                    break;
            }
        }

        private static string GetItemClass(string classSubString)
        {
            var index     = classSubString.IndexOf(':');
            return classSubString.Substring(index, classSubString.Length - index);
        }

        /// <summary>
        /// Extract frame type from item data
        /// </summary>
        /// <param name="splitRaw"></param>
        /// <param name="rarity"></param>
        /// <returns></returns>
        private int Parse_FrameType(string[] splitRaw, string rarity)
        {
            if (rarity == null)
            {
                // Shouldn't run
                Discard = true;
                Errors.Add("Invalid rarity");
            }

            switch (rarity)
            {
                case "Unique":
                    return 3;
                case "Gem":
                    return 4;
                case "Currency":
                    return 5;
                case "Fragment":
                    return 0;
                case "Divination Card":
                    return 6;
            }

            // Loop through lines checking for specific strings
            foreach (var line in splitRaw)
            {
                if (line.Contains("Right-click to add this prophecy"))
                {
                    // Prophecy
                    return 8;
                }

                if (line.Contains("Travel to this Map by using it in"))
                {
                    // Standardized maps
                    return 0;
                }

                if (line.Equals("Relic Unique"))
                {
                    // Relics
                    return 9;
                }
            }

            // If other methods failed use frame type 0
            switch (rarity)
            {
                case "Normal":
                case "Magic":
                case "Rare":
                    return 0;
            }

            // Could not determine the frame type
            Discard = true;
            Errors.Add("Unknown frame type");
            return -1;
        }

        //-----------------------------------------------------------------------------------------------------------
        // Specific parsing methods
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Parses socket data if present and finds largest link
        /// </summary>
        /// <param name="splitRaw"></param>
        /// <returns>Socket count (5 or 6) or null</returns>
        private static int? ParseSockets(string[] splitRaw)
        {
            // Find what line index socket data is under
            var lineIndex = FindIndexOf("Sockets:", splitRaw);

            // Item doesn't have socket data
            if (lineIndex == -1) return null;

            // Get socket line: "Sockets: B G-R-R-G-R " -> "B G-R-R-G-R" (note the extra whitespace prefix)
            var socketData = TrimProperty(splitRaw[lineIndex]);

            // Create array for counting link groups
            int[] links   = {1, 1, 1, 1, 1};
            var   counter = 0;

            // Count links
            for (var i = 1; i < socketData.Length; i += 2)
            {
                if (socketData[i] == '-')
                    links[counter]++;
                else
                    counter++;
            }

            // Find largest link
            var largestLink = links.Concat(new[] {0}).Max();

            // Return largest link or null
            return largestLink > 4 ? (int?) largestLink : null;
        }

        /// <summary>
        /// As some specific items have multiple variants, distinguish them
        /// </summary>
        /// <returns>Variant string or null if none</returns>
        private static string ParseVariant(string[] splitRaw, string name)
        {
            // Find what line index "Item Level:" is under
            var exModIndex = FindIndexOf("Item Level:", splitRaw);

            // Couldn't find index of item level
            if (exModIndex == -1) return null;

            // Explicit mods are located under "Item Level:" line
            var explicitMods = splitRaw[exModIndex + 1].Split('|');

            // Check variation
            switch (name)
            {
                case "Atziri's Splendour":
                    // Take the first explicit mod and replace variables with constants
                    var genericMod = string.Join("#", Regex.Split(explicitMods[0], @"\d+"));

                    // Compare the first generic explicit mod to the preset definitions
                    switch (genericMod)
                    {
                        case "#% increased Armour, Evasion and Energy Shield":
                            return "ar/ev/es";
                        case "#% increased Armour and Energy Shield":
                            return explicitMods[1].Contains("Life") ? "ar/es/li" : "ar/es";
                        case "#% increased Evasion and Energy Shield":
                            return explicitMods[1].Contains("Life") ? "ev/es/li" : "ev/es";
                        case "#% increased Armour and Evasion":
                            return "ar/ev";
                        case "#% increased Armour":
                            return "ar";
                        case "#% increased Evasion Rating":
                            return "ev";
                        case "+# to maximum Energy Shield":
                            return "es";
                    }

                    break;

                case "Vessel of Vinktar":
                    foreach (var mod in explicitMods)
                    {
                        if (mod.Contains("to Spells")) return "spells";
                        if (mod.Contains("to Attacks")) return "attacks";
                        if (mod.Contains("Penetrates")) return "penetration";
                        if (mod.Contains("Converted to")) return "conversion";
                    }

                    break;

                case "Doryani's Invitation":
                    // Explicit mods for Doryani's Invitation are located 2 lines under the "Item Level:" line
                    explicitMods = splitRaw[exModIndex + 2].Split('|');

                    foreach (var mod in explicitMods)
                    {
                        if (mod.Contains("Lightning Damage")) return "lightning";
                        if (mod.Contains("Physical Damage")) return "physical";
                        if (mod.Contains("Fire Damage")) return "fire";
                        if (mod.Contains("Cold Damage")) return "cold";
                    }

                    break;

                case "Yriel's Fostering":
                    foreach (var mod in explicitMods)
                    {
                        if (mod.Contains("Chaos Damage")) return "snake";
                        if (mod.Contains("Physical Damage")) return "ursa";
                        if (mod.Contains("Attack and Movement")) return "rhoa";
                    }

                    break;

                case "Volkuur's Guidance":
                    // Figure out on what line explicit mods are located (due to enchantments)
                    for (int i = exModIndex; i < splitRaw.Length; i++)
                    {
                        if (splitRaw[i].Contains("to maximum Life"))
                        {
                            explicitMods = splitRaw[i].Split('|');
                            break;
                        }
                    }

                    // Figure out item variant
                    foreach (var mod in explicitMods)
                    {
                        if (mod.Contains("Lightning Damage")) return "lightning";
                        if (mod.Contains("Fire Damage")) return "fire";
                        if (mod.Contains("Cold Damage")) return "cold";
                    }

                    break;

                case "Impresence":
                    // Explicit mods for Impresence are located 2 lines under the "Item Level:" line
                    explicitMods = splitRaw[exModIndex + 2].Split('|');

                    foreach (var mod in explicitMods)
                    {
                        if (mod.Contains("Lightning Damage")) return "lightning";
                        if (mod.Contains("Physical Damage")) return "physical";
                        if (mod.Contains("Chaos Damage")) return "chaos";
                        if (mod.Contains("Fire Damage")) return "fire";
                        if (mod.Contains("Cold Damage")) return "cold";
                    }

                    break;

                case "Lightpoacher":
                case "Shroud of the Lightless":
                case "Bubonic Trail":
                case "Tombfist":
                case "Command of the Pit":
                case "Hale Negator":
                    // Figure out on what line explicit mods are located (due to enchantments)
                    for (int i = exModIndex; i < splitRaw.Length; i++)
                    {
                        if (splitRaw[i].Contains("Abyssal Socket"))
                        {
                            explicitMods = splitRaw[i].Split('|');
                            break;
                        }
                    }

                    // Check how many abyssal sockets the item has
                    switch (explicitMods[0])
                    {
                        case "Has 2 Abyssal Sockets":
                            return "2 sockets";
                        case "Has 1 Abyssal Socket":
                            return "1 socket";
                    }

                    break;

                case "The Beachhead":
                    // Find the line that contains map tier info (e.g "Map Tier: 15")
                    foreach (var line in splitRaw)
                    {
                        var subLine = line.Split('|');

                        foreach (var prop in subLine)
                        {
                            if (prop.StartsWith("Map Tier: "))
                            {
                                // Return tier info (e.g "15")
                                return prop.Substring(prop.IndexOf(' ', prop.IndexOf(' ') + 1) + 1);
                            }
                        }
                    }

                    break;

                case "Combat Focus":
                    // Explicit mods for Combat Focus are located 2 lines under the "Item Level:" line
                    explicitMods = splitRaw[exModIndex + 2].Split('|');

                    foreach (var mod in explicitMods)
                    {
                        if (mod.Contains("choose Lightning")) return "lightning";
                        if (mod.Contains("choose Cold")) return "cold";
                        if (mod.Contains("choose Fire")) return "fire";
                    }

                    break;
            }

            // Nothing matched, eh?
            return null;
        }

        /// <summary>
        /// Extracts gem information from item data
        /// </summary>
        private void ParseGemData(string[] splitRaw, string name)
        {
            var  pattern = new Regex(@"\d+");
            int? level   = null;
            int  quality = 0;

            // Second line will contain gem data
            var gemLines = splitRaw[1].Split('|');

            // Find gem level and quality
            foreach (var line in gemLines)
            {
                if (line.Contains("Level:"))
                {
                    level = int.Parse(pattern.Match(line).Value);
                }
                else if (line.Contains("Quality:"))
                {
                    quality = int.Parse(pattern.Match(line).Value);
                }
            }

            if (level == null)
            {
                Discard = true;
                Errors.Add("Couldn't extract gem data");
                return;
            }

            // Find gem corrupted status
            var corrupted = splitRaw[splitRaw.Length - 1].Contains("Corrupted");

            // Special gems have special needs
            if (name.Contains("Empower") || name.Contains("Enlighten") || name.Contains("Enhance"))
            {
                quality = quality < 10 ? 0 : 20;
                // Quality doesn't matter for lvl 3 and 4
                if (level > 2) quality = 0;
            }
            else
            {
                // Logically speaking, a lvl 17 gem is closer to lvl1 in terms of value compared to lvl20
                if (level < 18)
                {
                    level = 1;
                }
                else if (level < 21)
                {
                    level = 20;
                }

                if (quality < 17)
                {
                    quality = 0;
                }
                else if (quality < 22)
                {
                    quality = 20;
                }
                else
                {
                    quality = 23;
                }

                // Gets rid of specific gems (lvl:1 quality:23-> lvl:1 quality:20)
                if (level < 20 && quality > 20)
                {
                    quality = 20;
                }

                if (level > 20 || quality > 20)
                {
                    corrupted = true;
                }
                else if (name.Contains("Vaal"))
                {
                    corrupted = true;
                }
            }

            Key.GemLevel     = level;
            Key.GemQuality   = quality;
            Key.GemCorrupted = corrupted;
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
        private static int FindIndexOf(string needle, string[] haystack)
        {
            for (int i = 0; i < haystack.Length; i++)
            {
                if (haystack[i].StartsWith(needle))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Takes input as "Item Level: 55" or "Sockets: B - B B " and returns "55" or "B - B B"
        /// </summary>
        /// <param name="str">"Sockets: B - B B "</param>
        /// <returns>"B - B B"</returns>
        private static string TrimProperty(string str)
        {
            var index = str.IndexOf(':')           + 2;
            return str.Substring(index, str.Length - index).Trim();
        }
    }
}