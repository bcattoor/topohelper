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
using Infrabel.AutodeskPlatform.TopoHelper.Model;
using Infrabel.AutodeskPlatform.TopoHelper.Model.Naming;
using Infrabel.AutodeskPlatform.TopoHelper.Properties;
using MoreLinq;

namespace Infrabel.AutodeskPlatform.TopoHelper.CommandImplementations
{
    internal static class FromBlockToCogo
    {
        #region Configuration Classes
        
        private class StyleConfiguration
        {
            public Dictionary<Classifications, List<PointStyleMapping>> StyleMappings { get; set; }
            public ObjectId FallbackStyleId { get; set; }
            public ObjectId LabelStyleId { get; set; }
            public PointStyleCollection PointStyles { get; set; }
        }
        
        #endregion

        #region Main Refactored Functions

        /// <summary>
        /// Selecteert blocks van de gebruiker
        /// </summary>
        private static List<ObjectId> SelectBlocksFromUser(Editor editor)
        {
            var pso = new PromptSelectionOptions { MessageForAdding = "\nSelecteer blocks: " };
            var filter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "INSERT") });
            var psr = editor.GetSelection(pso, filter);
            
            if (psr.Status != PromptStatus.OK) 
                return new List<ObjectId>();
                
            var selectedBlockIds = psr.Value.GetObjectIds().ToList();
            return selectedBlockIds.Any() ? selectedBlockIds : new List<ObjectId>();
        }

        /// <summary>
        /// Extraheert eigenschappen van geselecteerde blocks
        /// </summary>
        private static List<IapBlock> ExtractBlockProperties(List<ObjectId> blockIds, Database database, Document document, Transaction transaction)
        {
            if (!blockIds.Any())
                throw new System.Exception(nameof(blockIds) + " has no items in it.");
                
            return BlockScanner.GetPropertiesOfBlocksById(blockIds, database, document, null, transaction).ToList();
        }

        /// <summary>
        /// Maakt CogoPoints aan op basis van block data
        /// </summary>
        private static Dictionary<ObjectId, ObjectId> CreateCogoPointsFromBlocks(List<IapBlock> blocks)
        {
            if (!blocks.Any())
                return new Dictionary<ObjectId, ObjectId>();
                
            var locations = new Point3dCollection(blocks.Select(b => b.InsertionPoint3D).ToArray());
            var blockIds = blocks.Select(c => c.Id).ToList();
            
            return AddCogoPoints(locations, blockIds, "CogoPoint");
        }

        /// <summary>
        /// Initialiseert style configuratie voor de conversie
        /// </summary>
        private static StyleConfiguration InitializeStyleConfiguration(CivilDocument civilDocument, string defaultLabelStyleName)
        {
            var styleMappings = PointStyleMappingManager.LoadStyleMappingsFromSettings();
            PointStyleMappingManager.ValidateStyleMappings(styleMappings, civilDocument.Styles.PointStyles);
            
            var fallbackStyleId = GetPointStyleIdWithFallback(
                civilDocument.Styles.PointStyles, 
                Settings.Default.FromBlockToCogo_DefaultPointStyleName, 
                Settings.Default.FromBlockToCogo_FallbackPointStyleName);
                
            var labelStyleId = GetLabelStyleIdByName(
                civilDocument.Styles.LabelStyles.PointLabelStyles.LabelStyles, 
                defaultLabelStyleName);
            
            return new StyleConfiguration
            {
                StyleMappings = styleMappings,
                FallbackStyleId = fallbackStyleId,
                LabelStyleId = labelStyleId,
                PointStyles = civilDocument.Styles.PointStyles
            };
        }

        /// <summary>
        /// Converteert een enkel block naar een CogoPoint
        /// </summary>
        private static ConversionDetails ConvertSingleBlockToCogoPoint(
            IapBlock block, 
            Dictionary<ObjectId, ObjectId> cogoPointMapping,
            StyleConfiguration styleConfig,
            CogoPointNamingSettings namingSettings,
            HashSet<string> existingNames,
            Transaction transaction)
        {
            var blockRef = (BlockReference)transaction.GetObject(block.Id, OpenMode.ForRead, false, true);
            if (blockRef == null) 
                return null;

            var classification = CreateClassificationFromBlock(block, blockRef);
            var cgPoint = (CogoPoint)transaction.GetObject(cogoPointMapping[block.Id], OpenMode.ForWrite);

            var details = CreateInitialConversionDetails(block, blockRef, classification);

            var (finalStyleId, appliedDescription) = SetCogoPointPropertiesWithReporting(
                cgPoint, blockRef, classification, namingSettings, 
                styleConfig.StyleMappings, styleConfig.PointStyles, 
                styleConfig.FallbackStyleId, styleConfig.LabelStyleId, existingNames);

            CompleteConversionDetails(details, finalStyleId, appliedDescription, cgPoint);
            
            return details;
        }

        /// <summary>
        /// Verwerkt alle block conversies
        /// </summary>
        private static ConversionReport ProcessBlockConversions(
            List<IapBlock> blockProperties,
            Dictionary<ObjectId, ObjectId> cogoPointMapping,
            StyleConfiguration styleConfig,
            CogoPointNamingSettings namingSettings,
            CivilDocument civDoc,
            Database database)
        {
            var conversionReport = new ConversionReport();

            using (var tr = database.TransactionManager.StartOpenCloseTransaction())
            {
                var existingNames = GetAllCogoPointNames(civDoc, tr);
                
                foreach (var block in blockProperties)
                {
                    var conversionDetails = ConvertSingleBlockToCogoPoint(
                        block, cogoPointMapping, styleConfig, namingSettings, existingNames, tr);
                        
                    if (conversionDetails != null)
                        conversionReport.Details.Add(conversionDetails);
                }
                tr.Commit();
            }
            
            return conversionReport;
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Maakt een classificatie object aan op basis van block data
        /// </summary>
        private static ClassificationObject CreateClassificationFromBlock(IapBlock block, BlockReference blockRef)
        {
            return new ClassificationObject(
                block.Id, 
                blockRef.Name, 
                blockRef.Layer, 
                block.Attributes.Select(blockN => blockN.Tag).ToList());
        }

        /// <summary>
        /// Maakt initiële conversie details aan
        /// </summary>
        private static ConversionDetails CreateInitialConversionDetails(IapBlock block, BlockReference blockRef, ClassificationObject classification)
        {
            return new ConversionDetails
            {
                BlockName = blockRef.Name,
                BlockLayer = blockRef.Layer,
                Classification = classification.Classification,
                OriginalPosition = block.InsertionPoint3D
            };
        }

        /// <summary>
        /// Voltooit de conversie details met resultaten
        /// </summary>
        private static void CompleteConversionDetails(ConversionDetails details, ObjectId finalStyleId, string appliedDescription, CogoPoint cgPoint)
        {
            details.AppliedStyleId = finalStyleId;
            details.AppliedDescription = appliedDescription;
            details.FinalPointName = cgPoint.PointName;
            details.FinalPosition = cgPoint.Location;
        }

        #endregion

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
            string newName = Naming.CogoPointNamingEngine.GeneratePointName(cgPoint, namingSettings, existingNames);
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

            // Stap 1: Selecteer blocks van gebruiker
            var selectedBlockIds = SelectBlocksFromUser(doc.Editor);
            if (!selectedBlockIds.Any()) return;

            // Stap 2: Extraheer block eigenschappen
            List<IapBlock> blockProperties;
            using (var tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                blockProperties = ExtractBlockProperties(selectedBlockIds, db, doc, tr);
                tr.Commit();
            }
            
            if (!blockProperties.Any()) return;

            // Stap 3: Maak CogoPoints aan
            var cogoPointMapping = CreateCogoPointsFromBlocks(blockProperties);

            // Stap 4: Configureer styles en naming
            var styleConfig = InitializeStyleConfiguration(civDoc, defaultLabelStyleName);
            var namingSettings = CogoPointNamingEngine.LoadSettings();
            
            // Stap 5: Converteer elk block naar CogoPoint
            var conversionReport = ProcessBlockConversions(
                blockProperties, cogoPointMapping, styleConfig, namingSettings, civDoc, db);

            // Stap 6: Toon resultaten (optioneel)
            DisplayConversionReport(doc.Editor, conversionReport, civDoc.Styles.PointStyles, db.TransactionManager.StartOpenCloseTransaction());
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
                try
                {
                    var knownRealBlockNames = DeserializeStringCollection(Settings.Default.FromBlockToCogo_KnownRealBlockNames_ToSetCogoProperties);
                    var knownLayers = DeserializeStringCollection(Settings.Default.FromBlockToCogo_Known_Layer_Names_ToSetCogoProperties);
                    var knownAttributeNames = DeserializeStringCollection(Settings.Default.FromBlockToCogo_Known_Block_Attributes_ToSetCogoProperties);

                    // Controleer of ObjectName voorkomt in de lijst van bekende bloknamen
                    if (!string.IsNullOrEmpty(ObjectName) && 
                        knownRealBlockNames.Any(name => string.Equals(name, ObjectName, StringComparison.OrdinalIgnoreCase)))
                        return Classifications.KnownByRealBlockName;
                    
                    // Controleer of een van de attribuutnamen voorkomt in de lijst van bekende attribuutnamen
                    if (AttributeNames != null && AttributeNames.Any() && 
                        AttributeNames.Any(attr => !string.IsNullOrEmpty(attr) && 
                                          knownAttributeNames.Any(name => string.Equals(name, attr, StringComparison.OrdinalIgnoreCase))))
                        return Classifications.KnownByAttibuteName;
                    
                    // Controleer of LayerName voorkomt in de lijst van bekende laagnamen
                    if (!string.IsNullOrEmpty(LayerName) && 
                        knownLayers.Any(layer => string.Equals(layer, LayerName, StringComparison.OrdinalIgnoreCase)))
                        return Classifications.KnownByLayer;
                    
                    return Classifications.Unknown;
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine($"Fout bij classificeren van object: {ex.Message}");
                    throw ex;
                }
            }

            private static List<string> DeserializeStringCollection(string xmlString)
            {
                if (string.IsNullOrEmpty(xmlString))
                    return new List<string>();

                try
                {
                    var serializer = new System.Xml.Serialization.XmlSerializer(typeof(string[]));
                    using (var reader = new System.IO.StringReader(xmlString))
                    {
                        var array = (string[])serializer.Deserialize(reader);
                        if (array == null)
                            return new List<string>();
                            
                        // Zorg ervoor dat alle items strings zijn (niet chars)
                        return array.Select(item => item?.ToString() ?? string.Empty).ToList();
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine($"Fout bij deserialiseren van string collection: {ex.Message}");
                    throw ex;
                }
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


