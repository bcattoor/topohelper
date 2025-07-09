//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using Autodesk.Civil.DatabaseServices;
//using Infrabel.AutodeskPlatform.TopoHelper.Properties;
//using System.Xml.Serialization;

//namespace Infrabel.AutodeskPlatform.TopoHelper.Model.Naming
//{
//    [Serializable]
//    public class CogoPointNamingSettings
//    {
//        public List<PrefixPattern> PrefixPatterns { get; set; } = new List<PrefixPattern>();
//        public List<DescriptionMapping> DescriptionLookupTable { get; set; } = new List<DescriptionMapping>();
//        public string DefaultPattern { get; set; } = "{Description}-{Counter:4}";
//    }

//    [Serializable]
//    public class PrefixPattern
//    {
//        public string Prefix { get; set; }
//        public string Pattern { get; set; }
//        public PrefixPattern() { }
//        public PrefixPattern(string prefix, string pattern) { Prefix = prefix; Pattern = pattern; }
//    }

//    [Serializable]
//    public class DescriptionMapping
//    {
//        public string Description { get; set; }
//        public string Pattern { get; set; }
//        public DescriptionMapping() { }
//        public DescriptionMapping(string description, string pattern) { Description = description; Pattern = pattern; }
//    }

//    public static class CogoPointNamingEngine
//    {
//        public static string GeneratePointName(CogoPoint cogoPoint, CogoPointNamingSettings settings, HashSet<string> existingNames)
//        {
//            string pattern = GetPattern(cogoPoint.RawDescription, settings);
//            string baseName = ParsePattern(pattern, cogoPoint);
//            string finalName = baseName;
//            int counter = 1;

//            while (existingNames.Contains(finalName, StringComparer.OrdinalIgnoreCase))
//            {
//                finalName = $"{baseName}_{counter++}";
//            }

//            if (finalName.Length > 255) finalName = finalName.Substring(0, 255);

//            return finalName;
//        }

//        private static string GetPattern(string description, CogoPointNamingSettings settings)
//        {
//            if (!string.IsNullOrEmpty(description))
//            {
//                var prefixRule = settings.PrefixPatterns.FirstOrDefault(p => description.StartsWith(p.Prefix, StringComparison.OrdinalIgnoreCase));
//                if (prefixRule != null) return prefixRule.Pattern;

//                var lookupRule = settings.DescriptionLookupTable.FirstOrDefault(l => description.Equals(l.Description, StringComparison.OrdinalIgnoreCase));
//                if (lookupRule != null) return lookupRule.Pattern;
//            }
//            return settings.DefaultPattern;
//        }

//        private static string ParsePattern(string pattern, CogoPoint cogoPoint)
//        {
//            var result = pattern;
//            var pointNumberStr = cogoPoint.PointNumber.ToString();

//            result = Regex.Replace(result, @"{Counter:(\d+)}", m => pointNumberStr.PadLeft(int.Parse(m.Groups[1].Value), '0'));
//            result = Regex.Replace(result, @"{Easting:(\d+)}", m => cogoPoint.Easting.ToString("F0").TakeLast(int.Parse(m.Groups[1].Value)));
//            result = Regex.Replace(result, @"{Northing:(\d+)}", m => cogoPoint.Northing.ToString("F0").TakeLast(int.Parse(m.Groups[1].Value)));
//            result = Regex.Replace(result, @"{Elevation:(\d+)}", m => cogoPoint.Elevation.ToString("F0").TakeLast(int.Parse(m.Groups[1].Value)));
//            result = result.Replace("{Description}", cogoPoint.RawDescription);

//            return result;
//        }

//        private static string TakeLast(this string source, int count) => source.Length > count ? source.Substring(source.Length - count) : source;

//        public static CogoPointNamingSettings LoadSettings()
//        {
//            var xmlConfig = Settings.Default.CogoPointNamingConfiguration;
//            if (string.IsNullOrWhiteSpace(xmlConfig))
//            {
//                var defaultSettings = new CogoPointNamingSettings();
//                defaultSettings.PrefixPatterns.Add(new PrefixPattern("CATA", "CATA-{Counter:3}"));
//                defaultSettings.PrefixPatterns.Add(new PrefixPattern("CATB", "CATB-{Counter:3}"));
//                return defaultSettings;
//            }
//            try
//            {
//                XmlSerializer serializer = new XmlSerializer(typeof(CogoPointNamingSettings));
//                using (var reader = new StringReader(xmlConfig))
//                {
//                    return (CogoPointNamingSettings)serializer.Deserialize(reader);
//                }
//            }
//            catch { return new CogoPointNamingSettings(); }
//        }

//        public static void SaveSettings(CogoPointNamingSettings settings)
//        {
//            var serializer = new XmlSerializer(typeof(CogoPointNamingSettings));
//            using (var writer = new StringWriter())
//            {
//                serializer.Serialize(writer, settings);
//                Settings.Default.CogoPointNamingConfiguration = writer.ToString();
//                Settings.Default.Save();
//            }
//        }
//    }
//}
