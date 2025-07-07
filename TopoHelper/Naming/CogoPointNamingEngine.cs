using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Autodesk.Civil.DatabaseServices;
using Infrabel.AutodeskPlatform.TopoHelper.Properties;

namespace Infrabel.AutodeskPlatform.TopoHelper.Naming
{
    /// <summary>
    /// Represents the complete set of rules for COGO point naming.
    /// </summary>
    [Serializable]
    public class CogoPointNamingSettings
    {
        public List<PrefixPattern> PrefixPatterns { get; set; }
        public List<DescriptionMapping> DescriptionLookupTable { get; set; }
        public string DefaultPattern { get; set; }

        public CogoPointNamingSettings()
        {
            PrefixPatterns = new List<PrefixPattern>
            {
                new PrefixPattern("CATA", "CATA-{Counter:3}"),
                new PrefixPattern("CATB", "CATB-{Counter:3}")
            };
            DescriptionLookupTable = new List<DescriptionMapping>
            {
                new DescriptionMapping("TREE", "TR-{Counter:3}"),
                new DescriptionMapping("SIGN", "SGN-{Easting:4}")
            };
            DefaultPattern = "{Description}-{Counter:4}";
        }
    }

    /// <summary>
    /// Defines a specific naming pattern for a description prefix (e.g., "CATA").
    /// </summary>
    [Serializable]
    public class PrefixPattern
    {
        public string Prefix { get; set; }
        public string Pattern { get; set; }

        public PrefixPattern() { }
        public PrefixPattern(string prefix, string pattern)
        {
            Prefix = prefix;
            Pattern = pattern;
        }
    }

    /// <summary>
    /// Maps a full point description to a specific naming pattern.
    /// </summary>
    [Serializable]
    public class DescriptionMapping
    {
        public string Description { get; set; }
        public string Pattern { get; set; }

        public DescriptionMapping() { }
        public DescriptionMapping(string description, string pattern)
        {
            Description = description;
            Pattern = pattern;
        }
    }

    /// <summary>
    /// Main engine for generating COGO point names based on a set of rules.
    /// </summary>
    public static class CogoPointNamingEngine
    {
        /// <summary>
        /// Generates a unique name for a CogoPoint based on defined settings.
        /// </summary>
        public static string GeneratePointName(CogoPoint cogoPoint, CogoPointNamingSettings settings, HashSet<string> existingNames)
        {
            string pattern = GetPattern(cogoPoint.RawDescription, settings);
            string baseName = ParsePattern(pattern, cogoPoint);
            
            // Ensure uniqueness
            string finalName = baseName;
            int counter = 1;
            while (existingNames.Contains(finalName, StringComparer.OrdinalIgnoreCase))
            {
                finalName = $"{baseName}_{counter++}";
            }

            // Ensure compliance with AutoCAD limitations (e.g., length)
            if (finalName.Length > 255) // A reasonable limit
            {
                finalName = finalName.Substring(0, 255);
            }

            return finalName;
        }

        private static string GetPattern(string description, CogoPointNamingSettings settings)
        {
            // 1. Check for prefix match
            foreach (var prefixRule in settings.PrefixPatterns)
            {
                if (description.StartsWith(prefixRule.Prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return prefixRule.Pattern;
                }
            }

            // 2. Check for exact description match in lookup table
            foreach (var lookupRule in settings.DescriptionLookupTable)
            {
                if (description.Equals(lookupRule.Description, StringComparison.OrdinalIgnoreCase))
                {
                    return lookupRule.Pattern;
                }
            }
            
            // 3. Return default pattern
            return settings.DefaultPattern;
        }

        private static string ParsePattern(string pattern, CogoPoint cogoPoint)
        {
            var result = pattern;
            
            // Simple counter token (can be expanded later)
            result = Regex.Replace(result, @"{Counter:(\d)}", m => cogoPoint.PointNumber.ToString().PadLeft(int.Parse(m.Groups[1].Value), '0'));

            // Coordinate tokens
            result = Regex.Replace(result, @"{Easting:(\d)}", m => cogoPoint.Easting.ToString("F0").TakeLast(int.Parse(m.Groups[1].Value)));
            result = Regex.Replace(result, @"{Northing:(\d)}", m => cogoPoint.Northing.ToString("F0").TakeLast(int.Parse(m.Groups[1].Value)));
            result = Regex.Replace(result, @"{Elevation:(\d)}", m => cogoPoint.Elevation.ToString("F0").TakeLast(int.Parse(m.Groups[1].Value)));
            
            // Description token
            result = result.Replace("{Description}", cogoPoint.RawDescription);

            return result;
        }

        private static string TakeLast(this string source, int count)
        {
            if (source.Length > count)
                return source.Substring(source.Length - count);
            return source;
        }

        #region Settings Serialization

        public static CogoPointNamingSettings LoadSettings()
        {
            var xmlConfig = Settings.Default.CogoPointNamingConfiguration;
            if (string.IsNullOrWhiteSpace(xmlConfig))
            {
                return new CogoPointNamingSettings(); // Return default settings
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(CogoPointNamingSettings));
                using (StringReader reader = new StringReader(xmlConfig))
                {
                    return (CogoPointNamingSettings)serializer.Deserialize(reader);
                }
            }
            catch (Exception)
            {
                // If deserialization fails, return defaults
                return new CogoPointNamingSettings();
            }
        }

        public static void SaveSettings(CogoPointNamingSettings settings)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(CogoPointNamingSettings));
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, settings);
                Settings.Default.CogoPointNamingConfiguration = writer.ToString();
                Settings.Default.Save();
            }
        }

        #endregion
    }
}