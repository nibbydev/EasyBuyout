using System;
using System.Linq;
using System.Text;
using EasyBuyout.Prices;

namespace EasyBuyout.Item
{
    public class Key
    {
        public string Name, TypeLine, Variation;
        public int    FrameType;
        public int?   Links, MapTier, GemLevel, GemQuality;
        public bool?  GemCorrupted;


        private static string[] SimplifiedCurrencies = {"Oil", "Essence", "DeliriumOrb"};
        public Key() { }

        /// <summary>
        /// Poe.ninja's API is a bit retarded so everything gets fixed here
        /// </summary>
        /// <param name="category"></param>
        /// <param name="entry"></param>
        public Key(string category, PoeNinjaEntry entry)
        {
            // Currency is handled in a different API format, yeah bc it's retarded -.-
            if (category.Equals("Currency"))
            {
                Name      = entry.currencyTypeName;
                FrameType = 5;
                return;
            }
            foreach (var s in SimplifiedCurrencies)
                if (s == category)
                {
                    Name      = entry.name;
                    FrameType = 5;
                    return;
                }

            // Fragments are handled in a different API format
            if (category.Equals("Fragment"))
            {
                Name      = entry.currencyTypeName;
                FrameType = entry.currencyTypeName.Contains("Splinter") ? 5 : 0;
                return;
            }


            Name      = entry.name;
            TypeLine  = entry.baseType;
            FrameType = entry.itemClass;

            Links        = entry.links;
            MapTier      = entry.mapTier;
            GemLevel     = entry.gemLevel;
            GemQuality   = entry.gemQuality;
            GemCorrupted = entry.corrupted;
            Variation    = entry.variant;

            // For gems
            if (FrameType == 4)
            {
                // Gems have variants?
                Variation = null;
            }

            // For non-maps
            if (MapTier == 0)
            {
                MapTier = null;
            }
            else if (FrameType != 3)
            {
                // Maps have random frametypes
                FrameType = 0;
                // Can't determine map series from item data so map variation is unusable for us
                Variation = null;
            }

            // For items with variations
            if (Variation != null)
            {
                Variation = FixPoeNinjaVariant(Name, Variation);
            }


            // For non- 6L and 5L
            if (Links == 0)
            {
                Links = null;
            }

            // For non-gems
            if (FrameType != 4)
            {
                GemLevel     = null;
                GemQuality   = null;
                GemCorrupted = null;
            }

            // For normal maps
            if (MapTier == 0 && FrameType == 0)
            {
                // Name is type line for some reason
                TypeLine = null;
                // Variant refers to map type
                Variation = null;
            }
        }

        private string FixPoeNinjaVariant(string name, string variant)
        {
            if (variant == null)
            {
                return null;
            }

            switch (name)
            {
                case "Atziri's Splendour":
                    switch (variant)
                    {
                        case "Armour/ES":         return "ar/es";
                        case "ES":                return "es";
                        case "Armour/Evasion":    return "ar/ev";
                        case "Armour/ES/Life":    return "ar/es/li";
                        case "Evasion/ES":        return "ev/es";
                        case "Armour/Evasion/ES": return "ar/ev/es";
                        case "Evasion":           return "ev";
                        case "Evasion/ES/Life":   return "ev/es/li";
                        case "Armour":            return "ar";
                    }

                    break;

                case "Yriel's Fostering":
                    switch (variant)
                    {
                        case "Bleeding": return "ursa";
                        case "Poison":   return "snake";
                        case "Maim":     return "rhoa";
                    }

                    break;

                case "Volkuur's Guidance":
                    switch (variant)
                    {
                        case "Lightning": return "lightning";
                        case "Fire":      return "fire";
                        case "Cold":      return "cold";
                    }

                    break;

                case "Lightpoacher":
                case "Shroud of the Lightless":
                case "Bubonic Trail":
                case "Tombfist":
                case "Command of the Pit":
                case "Hale Negator":
                    switch (variant)
                    {
                        case "2 Jewels": return "2 sockets";
                        case "1 Jewel":  return "1 socket";
                    }

                    break;

                case "Vessel of Vinktar":
                    switch (variant)
                    {
                        case "Added Attacks": return "attacks";
                        case "Added Spells":  return "spells";
                        case "Penetration":   return "penetration";
                        case "Conversion":    return "conversion";
                    }

                    break;

                case "Doryani's Invitation":
                    switch (variant)
                    {
                        case null: // Bug on poe.ninja's end
                        case "Physical": return "physical";
                        case "Fire":      return "fire";
                        case "Cold":      return "cold";
                        case "Lightning": return "lightning";
                    }

                    break;

                case "Impresence":
                    switch (variant)
                    {
                        case "Chaos":     return "chaos";
                        case "Physical":  return "physical";
                        case "Fire":      return "fire";
                        case "Cold":      return "cold";
                        case "Lightning": return "lightning";
                    }

                    break;

                case "The Beachhead":
                    return MapTier.ToString();
            }

            Console.WriteLine($"no var match for {this} var {variant}");

            return null;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (typeof(Key) != obj.GetType())
            {
                return false;
            }

            var other = (Key) obj;

            if (FrameType != other.FrameType)
            {
                return false;
            }

            if (!Name?.Equals(other.Name) ?? (other.Name != null))
            {
                return false;
            }

            if (!TypeLine?.Equals(other.TypeLine) ?? (other.TypeLine != null))
            {
                return false;
            }

            if (!Variation?.Equals(other.Variation) ?? (other.Variation != null))
            {
                return false;
            }

            if (!Links?.Equals(other.Links) ?? (other.Links != null))
            {
                return false;
            }

            if (!GemLevel?.Equals(other.GemLevel) ?? (other.GemLevel != null))
            {
                return false;
            }

            if (!GemQuality?.Equals(other.GemQuality) ?? (other.GemQuality != null))
            {
                return false;
            }

            if (!MapTier?.Equals(other.MapTier) ?? (other.MapTier != null))
            {
                return false;
            }

            if (!GemCorrupted?.Equals(other.GemCorrupted) ?? (other.GemCorrupted != null))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hash = 3;

            hash = 53 * hash + (Name?.GetHashCode()      ?? 0);
            hash = 53 * hash + (TypeLine?.GetHashCode()  ?? 0);
            hash = 53 * hash + (Variation?.GetHashCode() ?? 0);
            hash = 53 * hash + (Links.GetHashCode());
            hash = 53 * hash + (GemLevel.GetHashCode());
            hash = 53 * hash + (GemQuality.GetHashCode());
            hash = 53 * hash + (MapTier.GetHashCode());
            hash = 53 * hash + (GemCorrupted.GetHashCode());
            hash = 53 * hash + FrameType;

            return hash;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append($"{Name}");

            if (TypeLine != null)
            {
                builder.Append($", {TypeLine}");
            }

            if (Variation != null)
            {
                builder.Append($" ({Variation})");
            }

            return builder.ToString();
        }

        public string ToFullString()
        {
            var builder = new StringBuilder();

            builder.Append($"name:'{Name}'");

            if (TypeLine != null)
            {
                builder.Append($",type:'{TypeLine}'");
            }

            builder.Append($",frame:'{FrameType}'");

            if (Variation != null)
            {
                builder.Append($",var:'{Variation}'");
            }

            if (Links != null)
            {
                builder.Append($",links:'{Links}'");
            }

            if (MapTier != null)
            {
                builder.Append($",tier:'{MapTier}'");
            }

            if (GemLevel != null)
            {
                builder.Append($",lvl:'{GemLevel}'");
            }

            if (GemQuality != null)
            {
                builder.Append($",quality:'{GemQuality}'");
            }

            if (GemCorrupted != null)
            {
                builder.Append($",corrupted:'{GemCorrupted}'");
            }

            return builder.ToString();
        }
    }
}