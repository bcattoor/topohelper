using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Infrabel.AutodeskPlatform.AutoCADCommon.BlockScanner;
using Infrabel.AutodeskPlatform.TopoHelper.Naming;
using Infrabel.AutodeskPlatform.TopoHelper.Properties;
using MoreLinq;

namespace Infrabel.AutodeskPlatform.TopoHelper.CommandImplementations
{
    internal static class FromBlockToCogo
    {
        private static (ObjectId styleId, string description) SetCogoPointPropertiesWithReporting(
            CogoPoint cgPoint,
            BlockReference blockRef,
            ClassificationObject classification,
            CogoPointNamingSettings namingSettings,
            Dictionary<Classifications, List<PointStyleMapping>> styleMappings,
            PointStyleCollection pointStyles,
            ObjectId fallbackStyleId,
            ObjectId labelStyleId,
            HashSet<string> existingNames)
        {
            // First, determine the style and description from mappings.
            var (styleId, description) = PointStyleMappingManager.GetStyleForClassification(classification, styleMappings, pointStyles, fallbackStyleId);

            // If the mapping doesn't provide a description, use a fallback.
            if (string.IsNullOrEmpty(description))
            {
                var lastDigitResult = CheckLastDigit(blockRef.Name); // Fallback to checking block name
                if (lastDigitResult == LastDigit.Odd)
                    description = "CATA";
                else if (lastDigitResult == LastDigit.Even)
                    description = "CATB";
                else
                    description = "822"; // Ultimate fallback
            }
            cgPoint.RawDescription = description;

            // Now, generate the point name using the new engine, which can use the description.
            string newName = CogoPointNamingEngine.GeneratePointName(cgPoint, namingSettings, existingNames);
            cgPoint.PointName = newName;
            existingNames.Add(newName);

            // Set Layer
            cgPoint.Layer = AutoCADCommon.Interactions.Layers.GetValidLayerNameOrCurrent(blockRef.Layer);

            // Set Styles
            if (styleId != ObjectId.Null)
                cgPoint.StyleId = styleId;
            if (labelStyleId != ObjectId.Null)
                cgPoint.LabelStyleId = labelStyleId;

            return (styleId, description);
        }

        public static void ExecuteCommand(string defaultLabelStyleName)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var civDoc = CivilApplication.ActiveDocument;

            // Load Naming Settings once
            var namingSettings = CogoPointNamingEngine.LoadSettings();

            List<IapBlock> iAPBlocksReadyToConvert;
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                PromptSelectionOptions pso = new PromptSelectionOptions { MessageForAdding = "\nSelect blocks: " };
                SelectionFilter filter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "INSERT") });
                PromptSelectionResult psr = doc.Editor.GetSelection(pso, filter);

                if (psr.Status != PromptStatus.OK) return;
                var selectedBlockIds = psr.Value.GetObjectIds().ToList();
                if (!selectedBlockIds.Any()) return;

                iAPBlocksReadyToConvert = BlockScanner.GetPropertiesOfBlocksById(selectedBlockIds, db, doc, null, tr).ToList();
                tr.Commit();
            }

            var newCogoPoints = AddCogoPoints(new Point3dCollection(iAPBlocksReadyToConvert.Select(b => b.InsertionPoint3D).ToArray()), iAPBlocksReadyToConvert.Select(c => c.Id).ToList(), "CogoPoint");

            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                var styleMappings = PointStyleMappingManager.LoadStyleMappingsFromSettings();
                PointStyleMappingManager.ValidateStyleMappings(styleMappings, civDoc.Styles.PointStyles); // Warnings are handled inside

                var fallbackStyleId = GetPointStyleIdWithFallback(civDoc.Styles.PointStyles, Settings.Default.FromBlockToCogo_DefaultPointStyleName, Settings.Default.FromBlockToCogo_FallbackPointStyleName);
                var labelStyleId = GetLabelStyleIdByName(civDoc.Styles.LabelStyles.PointLabelStyles.LabelStyles, defaultLabelStyleName);
                var existingNames = GetAllCogoPointNames(civDoc, tr);
                var conversionReport = new ConversionReport();

                foreach (var block in iAPBlocksReadyToConvert)
                {
                    var blockRef = (BlockReference)tr.GetObject(block.Id, OpenMode.ForRead, false, true);
                    if (blockRef == null) continue;

                    var classification = new ClassificationObject(block.Id, blockRef.Name, blockRef.Layer, block.Attributes.Select(a => a.Tag).ToList());
                    var cgPoint = (CogoPoint)tr.GetObject(newCogoPoints[block.Id], OpenMode.ForWrite);

                    var details = new ConversionDetails
                    {
                        BlockName = blockRef.Name,
                        BlockLayer = blockRef.Layer,
                        Classification = classification.Classification,
                        OriginalPosition = block.InsertionPoint3D
                    };

                    var (finalStyleId, appliedDescription) = SetCogoPointPropertiesWithReporting(cgPoint, blockRef, classification,
                        namingSettings, styleMappings, civDoc.Styles.PointStyles, fallbackStyleId, labelStyleId, existingNames);

                    details.AppliedStyleId = finalStyleId;
                    details.AppliedDescription = appliedDescription;
                    details.FinalPointName = cgPoint.PointName;
                    details.FinalPosition = cgPoint.Location;
                    conversionReport.Details.Add(details);
                }

                tr.Commit();
                // DisplayConversionReport(doc.Editor, conversionReport, civDoc.Styles.PointStyles, db.TransactionManager.StartOpenCloseTransaction());
            }
        }

        #region Utility and Helper Methods (Unchanged)

        private static void DisplayConversionReport(Editor editor, ConversionReport report, PointStyleCollection pointStyles, Transaction tr)
        {
            editor.WriteMessage("\n" + new string('=', 60));
            editor.WriteMessage("\n*** BLOCK TO COGO POINT CONVERSION REPORT ***");
            editor.WriteMessage("\n" + new string('=', 60));
            editor.WriteMessage($"\nConversion completed at: {report.ConversionTime:yyyy-MM-dd HH:mm:ss}");
            editor.WriteMessage($"\nTotal points converted: {report.TotalConverted}");

            editor.WriteMessage("\n\nConversions by Classification:");
            foreach (var kvp in report.ConversionsByClassification.OrderBy(k => k.Key))
            {
                editor.WriteMessage($"\n  {kvp.Key}: {kvp.Value} points");
            }

            if (report.Details.Any())
            {
                editor.WriteMessage("\n\nDetailed Conversion Results:");
                editor.WriteMessage("\nPoint Name | Block Name | Layer | Classification | Style | Description");
                editor.WriteMessage(new string('-', 80));

                foreach (var detail in report.Details.OrderBy(d => d.FinalPointName))
                {
                    var styleName = GetStyleNameById(pointStyles, detail.AppliedStyleId, tr);
                    editor.WriteMessage($"\n{detail.FinalPointName,-10} | {detail.BlockName,-10} | {detail.BlockLayer,-10} | {detail.Classification,-15} | {styleName,-10} | {detail.AppliedDescription}");
                }
            }

            var styleUsage = report.Details.GroupBy(d => d.AppliedStyleId).ToDictionary(g => g.Key, g => g.Count());
            if (styleUsage.Any())
            {
                editor.WriteMessage("\n\nPoint Style Usage:");
                foreach (var kvp in styleUsage.OrderByDescending(x => x.Value))
                {
                    var styleName = GetStyleNameById(pointStyles, kvp.Key, tr);
                    editor.WriteMessage($"\n  {styleName}: {kvp.Value} points");
                }
            }

            editor.WriteMessage("\n" + new string('=', 60));
            tr.Dispose();
        }

        private static string GetStyleNameById(PointStyleCollection pointStyles, ObjectId styleId, Transaction tr)
        {
            if (styleId == ObjectId.Null) return "No style set.";

            if (pointStyles.Contains(styleId))
            {
                var style = tr.GetObject(styleId, OpenMode.ForRead) as PointStyle;
                if (style != null)
                    return style.Name;

            }
            return "No style set.";
        }

        public static Dictionary<ObjectId, ObjectId> AddCogoPoints(Point3dCollection locations, List<ObjectId> originalBlockIds, string description = "")
        {
            if (locations == null || !locations.Cast<Point3d>().Any()) throw new ArgumentNullException(nameof(locations));
            if (originalBlockIds == null || !originalBlockIds.Any()) throw new ArgumentNullException(nameof(originalBlockIds));
            if (locations.Count != originalBlockIds.Count) throw new ArgumentException("Location and ID counts must match.");

            var returnDictionary = new Dictionary<ObjectId, ObjectId>();
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (var tr = doc.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var civilDoc = CivilApplication.ActiveDocument;
                ObjectIdCollection newPointIds = civilDoc.CogoPoints.Add(locations, description, false, false, true);
                for (int i = 0; i < newPointIds.Count; i++)
                {
                    returnDictionary.Add(originalBlockIds[i], newPointIds[i]);
                }
                tr.Commit();
            }
            return returnDictionary;
        }

        private static ObjectId GetPointStyleIdWithFallback(PointStyleCollection pointStyles, string primaryStyleName, string fallbackStyleName)
        {
            try { if (!string.IsNullOrEmpty(primaryStyleName) && pointStyles.Contains(primaryStyleName)) return pointStyles[primaryStyleName]; } catch { }
            try { if (!string.IsNullOrEmpty(fallbackStyleName) && pointStyles.Contains(fallbackStyleName)) return pointStyles[fallbackStyleName]; } catch { }
            if (pointStyles.Count > 0) return pointStyles[0];
            return ObjectId.Null;
        }

        private static ObjectId GetLabelStyleIdByName(LabelStyleCollection labelStyles, string styleName)
        {
            if (!string.IsNullOrEmpty(styleName) && labelStyles.Contains(styleName))
                return labelStyles[styleName];
            return ObjectId.Null;
        }

        private static LastDigit CheckLastDigit(string input)
        {
            if (string.IsNullOrEmpty(input)) return LastDigit.NotANumber;
            char lastChar = input.Last();
            if (char.IsDigit(lastChar))
            {
                return (int.Parse(lastChar.ToString()) % 2 == 0) ? LastDigit.Even : LastDigit.Odd;
            }
            return LastDigit.NotANumber;
        }


        private static HashSet<string> GetAllCogoPointNames(CivilDocument civDoc, Transaction tr)
        {
            if (tr == null)
                throw new ArgumentNullException(nameof(tr));

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ObjectId cogoId in civDoc.CogoPoints)
            {
                var cogo = (CogoPoint)tr.GetObject(cogoId, OpenMode.ForRead, false, true);
                if (cogo != null && !string.IsNullOrEmpty(cogo.PointName))
                    names.Add(cogo.PointName);
            }
            return names;
        }

        #endregion

        #region Nested Classes (Unchanged)
        private enum LastDigit { NotANumber, Even, Odd }
        public enum Classifications { Unknown, KnownByLayer, KnownByAttibuteName, KnownByRealBlockName }
        public class PointStyleMapping
        {
            public Classifications Classification { get; set; }
            public string Identifier { get; set; }
            public string PointStyleName { get; set; }
            public string Description { get; set; }
            public int Priority { get; set; }
        }
        public class StyleMappingValidationResult
        {
            public List<string> Warnings { get; set; } = new List<string>();
            public List<string> Errors { get; set; } = new List<string>();
            public bool IsValid => !Errors.Any();
        }

        public class ConversionDetails
        {
            public string BlockName { get; set; }
            public string BlockLayer { get; set; }
            public Classifications Classification { get; set; }
            public Point3d OriginalPosition { get; set; }
            public Point3d FinalPosition { get; set; }
            public string FinalPointName { get; set; }
            public ObjectId AppliedStyleId { get; set; }
            public string AppliedDescription { get; set; }
        }

        public class ConversionReport
        {
            public List<ConversionDetails> Details { get; set; } = new List<ConversionDetails>();
            public DateTime ConversionTime { get; } = DateTime.Now;
            public int TotalConverted => Details.Count;
            public Dictionary<Classifications, int> ConversionsByClassification => Details.GroupBy(d => d.Classification).ToDictionary(g => g.Key, g => g.Count());
        }

        class ClassificationObject
        {
            public ObjectId ObjectId { get; }
            public Classifications Classification { get; }
            public string ObjectName { get; }
            public string LayerName { get; }
            public List<string> AttributeNames { get; }
            private static readonly List<string> KnownRealBlockNames = new List<string>() { "KP", "HP", "CAT" };
            private static readonly List<string> KnownLayers = new List<string>() { "0", "410_pile_axis", "173_pond_edge" };
            private static readonly List<string> KnownAttributeNames = new List<string>() { "NR", "NR", "NAAM" };

            public ClassificationObject(ObjectId objectId, string objectName, string layerName, List<string> attributeNames)
            {
                ObjectId = objectId;
                ObjectName = objectName ?? string.Empty;
                LayerName = layerName ?? string.Empty;
                AttributeNames = attributeNames ?? new List<string>();
                Classification = Classify();
            }

            private Classifications Classify()
            {
                if (KnownRealBlockNames.Contains(ObjectName, StringComparer.OrdinalIgnoreCase)) return Classifications.KnownByRealBlockName;
                if (AttributeNames.Any(attr => KnownAttributeNames.Contains(attr, StringComparer.OrdinalIgnoreCase))) return Classifications.KnownByAttibuteName;
                if (KnownLayers.Contains(LayerName, StringComparer.OrdinalIgnoreCase)) return Classifications.KnownByLayer;
                return Classifications.Unknown;
            }
        }

        private static class PointStyleMappingManager
        {
            public static Dictionary<Classifications, List<PointStyleMapping>> LoadStyleMappingsFromSettings()
            {
                return LoadStyleMappingsFromXml(Settings.Default.FromBlockToCogo_StyleMappingConfiguration);
            }
            private static Dictionary<Classifications, List<PointStyleMapping>> LoadStyleMappingsFromXml(string xmlConfig)
            {
                if (string.IsNullOrWhiteSpace(xmlConfig)) return GetDefaultStyleMappings();
                try
                {
                    var doc = new System.Xml.XmlDocument();
                    doc.LoadXml(xmlConfig);
                    var mappings = new List<PointStyleMapping>();
                    var mappingNodes = doc.SelectNodes("//Mapping");
                    if (mappingNodes != null)
                    {
                        foreach (System.Xml.XmlNode node in mappingNodes)
                        {
                            if (System.Enum.TryParse<Classifications>(node.SelectSingleNode("Classification")?.InnerText, out var classification) &&
                                int.TryParse(node.SelectSingleNode("Priority")?.InnerText, out var priority))
                            {
                                mappings.Add(new PointStyleMapping
                                {
                                    Classification = classification,
                                    Identifier = node.SelectSingleNode("Identifier")?.InnerText ?? "*",
                                    PointStyleName = node.SelectSingleNode("PointStyleName")?.InnerText,
                                    Description = node.SelectSingleNode("Description")?.InnerText,
                                    Priority = priority
                                });
                            }
                        }
                    }
                    return mappings.GroupBy(m => m.Classification).ToDictionary(g => g.Key, g => g.ToList());
                }
                catch { return GetDefaultStyleMappings(); }
            }
            public static void ValidateStyleMappings(Dictionary<Classifications, List<PointStyleMapping>> styleMappings, PointStyleCollection pointStyles)
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) return;

                foreach (var mapping in styleMappings.SelectMany(kvp => kvp.Value))
                {
                    if (string.IsNullOrEmpty(mapping.PointStyleName) || !pointStyles.Contains(mapping.PointStyleName))
                    {
                        doc.Editor.WriteMessage($"\nStyle Mapping Warning: Point style '{mapping.PointStyleName ?? "null"}' not found for {mapping.Classification} mapping '{mapping.Identifier}'.");
                    }
                }
            }
            private static Dictionary<Classifications, List<PointStyleMapping>> GetDefaultStyleMappings()
            {
                // Provide some sensible defaults
                var mappings = new List<PointStyleMapping>
                {
                   new PointStyleMapping { Classification = Classifications.KnownByRealBlockName, Identifier = "KP", PointStyleName = "Standard", Description = "CATA", Priority = 1 },
                   new PointStyleMapping { Classification = Classifications.KnownByRealBlockName, Identifier = "HP", PointStyleName = "Standard", Description = "CATB", Priority = 1 },
                   new PointStyleMapping { Classification = Classifications.Unknown, Identifier = "*", PointStyleName = "Standard", Description = "822", Priority = 99 }
                };
                return mappings.GroupBy(m => m.Classification).ToDictionary(g => g.Key, g => g.ToList());
            }
            public static (ObjectId styleId, string description) GetStyleForClassification(ClassificationObject classification, Dictionary<Classifications, List<PointStyleMapping>> styleMappings, PointStyleCollection pointStyles, ObjectId fallbackStyleId)
            {
                if (styleMappings.TryGetValue(classification.Classification, out var mappings))
                {
                    var identifier = GetIdentifierForClassification(classification);
                    var bestMatch = mappings.OrderBy(m => m.Priority)
                                            .FirstOrDefault(m => m.Identifier == "*" || m.Identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase));
                    if (bestMatch != null)
                    {
                        var styleId = TryGetPointStyleByName(pointStyles, bestMatch.PointStyleName);
                        if (styleId != ObjectId.Null)
                        {
                            return (styleId, bestMatch.Description);
                        }
                    }
                }
                return (fallbackStyleId, null);
            }
            private static string GetIdentifierForClassification(ClassificationObject classification)
            {
                switch (classification.Classification)
                {
                    case Classifications.KnownByRealBlockName: return classification.ObjectName;
                    case Classifications.KnownByLayer: return classification.LayerName;
                    case Classifications.KnownByAttibuteName: return classification.AttributeNames.FirstOrDefault();
                    default: return "*";
                }
            }
            private static ObjectId TryGetPointStyleByName(PointStyleCollection pointStyles, string styleName)
            {
                try { if (!string.IsNullOrEmpty(styleName) && pointStyles.Contains(styleName)) return pointStyles[styleName]; } catch { }
                return ObjectId.Null;
            }
        }
        #endregion
    }
}

namespace Infrabel.AutodeskPlatform.TopoHelper.Naming
{
    [Serializable]
    public class CogoPointNamingSettings
    {
        public List<PrefixPattern> PrefixPatterns { get; set; } = new List<PrefixPattern>();
        public List<DescriptionMapping> DescriptionLookupTable { get; set; } = new List<DescriptionMapping>();
        public string DefaultPattern { get; set; } = "{Description}-{Counter:4}";
    }

    [Serializable]
    public class PrefixPattern
    {
        public string Prefix { get; set; }
        public string Pattern { get; set; }
        public PrefixPattern() { }
        public PrefixPattern(string prefix, string pattern) { Prefix = prefix; Pattern = pattern; }
    }

    [Serializable]
    public class DescriptionMapping
    {
        public string Description { get; set; }
        public string Pattern { get; set; }
        public DescriptionMapping() { }
        public DescriptionMapping(string description, string pattern) { Description = description; Pattern = pattern; }
    }

    public static class CogoPointNamingEngine
    {
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

        public static void SaveSettings(CogoPointNamingSettings settings)
        {
            var serializer = new XmlSerializer(typeof(CogoPointNamingSettings));
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, settings);
                Settings.Default.CogoPointNamingConfiguration = writer.ToString();
                Settings.Default.Save();
            }
        }
    }
}
