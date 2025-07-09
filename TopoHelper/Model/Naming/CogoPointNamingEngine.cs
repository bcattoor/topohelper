using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Autodesk.Civil.DatabaseServices;
using Infrabel.AutodeskPlatform.TopoHelper.Properties;

namespace Infrabel.AutodeskPlatform.TopoHelper.Model.Naming
{

    #region COGO Naming Engine

    public static class CogoPointNamingEngine
    {

        public static void SaveSettings(CogoPointNamingSettings settings)
        {
            var settingsToSave = new CogoPointNamingSettings_Serializable
            {
                PrefixPatterns = settings.PrefixPatterns.ToList(),
                DescriptionLookupTable = settings.DescriptionLookupTable.ToList(),
                DefaultPattern = settings.DefaultPattern
            };

            var serializer = new XmlSerializer(typeof(CogoPointNamingSettings_Serializable));
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, settingsToSave);
                Settings.Default.CogoPointNamingConfiguration = writer.ToString();
                Settings.Default.Save();
            }
        }

        public static string GeneratePointName(CogoPoint cogoPoint, CogoPointNamingSettings settings, HashSet<string> existingNames)
        {
            string pattern = GetPattern(cogoPoint.RawDescription, settings);
            string baseName = ParsePattern(pattern, cogoPoint);
            string finalName = baseName;
            int counter = 1;

            while (existingNames.Contains(finalName, StringComparer.OrdinalIgnoreCase))
            {
                finalName = $"{baseName}_{counter++}";
            }

            if (finalName.Length > 255) finalName = finalName.Substring(0, 255);

            return finalName;
        }

        private static string GetPattern(string description, CogoPointNamingSettings settings)
        {
            if (!string.IsNullOrEmpty(description))
            {
                var prefixRule = settings.PrefixPatterns.FirstOrDefault(p => description.StartsWith(p.Prefix, StringComparison.OrdinalIgnoreCase));
                if (prefixRule != null) return prefixRule.Pattern;

                var lookupRule = settings.DescriptionLookupTable.FirstOrDefault(l => description.Equals(l.Description, StringComparison.OrdinalIgnoreCase));
                if (lookupRule != null) return lookupRule.Pattern;
            }
            return settings.DefaultPattern;
        }

        private static string ParsePattern(string pattern, CogoPoint cogoPoint)
        {
            var result = pattern;
            var pointNumberStr = cogoPoint.PointNumber.ToString();

            result = Regex.Replace(result, @"{Counter:(\d+)}", m => pointNumberStr.PadLeft(int.Parse(m.Groups[1].Value), '0'));
            result = Regex.Replace(result, @"{Easting:(\d+)}", m => cogoPoint.Easting.ToString("F0").TakeLast(int.Parse(m.Groups[1].Value)));
            result = Regex.Replace(result, @"{Northing:(\d+)}", m => cogoPoint.Northing.ToString("F0").TakeLast(int.Parse(m.Groups[1].Value)));
            result = Regex.Replace(result, @"{Elevation:(\d+)}", m => cogoPoint.Elevation.ToString("F0").TakeLast(int.Parse(m.Groups[1].Value)));
            result = result.Replace("{Description}", cogoPoint.RawDescription);

            return result;
        }

        private static string TakeLast(this string source, int count) => source.Length > count ? source.Substring(source.Length - count) : source;

        public static CogoPointNamingSettings LoadSettings()
        {
            var xmlConfig = Settings.Default.CogoPointNamingConfiguration;
            if (string.IsNullOrWhiteSpace(xmlConfig))
            {
                var defaultSettings = new CogoPointNamingSettings();
                defaultSettings.PrefixPatterns.Add(new PrefixPattern("CATA", "CATA-{Counter:3}"));
                defaultSettings.PrefixPatterns.Add(new PrefixPattern("CATB", "CATB-{Counter:3}"));
                return defaultSettings;
            }
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(CogoPointNamingSettings));
                using (var reader = new StringReader(xmlConfig))
                {
                    return (CogoPointNamingSettings)serializer.Deserialize(reader);
                }
            }
            catch { return new CogoPointNamingSettings(); }
            }
        }

        #endregion

    }
