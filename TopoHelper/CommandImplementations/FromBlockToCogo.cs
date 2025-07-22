using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.Civil.Settings;
using Infrabel.AutodeskPlatform.AutoCADCommon.BlockScanner;
using Infrabel.AutodeskPlatform.AutoCADCommon.Interactions;
using Infrabel.AutodeskPlatform.TopoHelper.Model.Naming;
using Infrabel.AutodeskPlatform.TopoHelper.Properties;
using MoreLinq;
using BlockReference = Autodesk.AutoCAD.DatabaseServices.BlockReference;
using ObjectId = Autodesk.AutoCAD.DatabaseServices.ObjectId;


namespace Infrabel.AutodeskPlatform.TopoHelper.CommandImplementations
{
    internal static class FromBlockToCogo
    {
        #region Configuration Classes

        private class StyleConfiguration
        {
            public Dictionary<ClassificationsEnum, List<PointStyleMapping>> StyleMappings { get; set; }
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
        private static Dictionary<ObjectId, ObjectId> CreateCogoPointsFromBlocks(List<ClassificationObject> blocks)
        {
            if (!blocks.Any())
                throw new System.Exception("No blocks given, we need at least one block to call this function..");


            return AddCogoPoints(blocks);
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
        private static ClassificationObject CreateClassificationFromBlock(IapBlock iAPBlock, BlockReference blockReffrence)
        {
            return new ClassificationObject(iAPBlock);
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
        /// Voltooi de conversie details met resultaten
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
            Dictionary<ClassificationsEnum, List<PointStyleMapping>> styleMappings,
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
            var newName = Model.Naming.CogoPointNamingEngine.GeneratePointName(cgPoint, namingSettings, existingNames);
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

        public static void ExecuteCommand(List<ObjectId> selectedBlockIds)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var civDoc = CivilApplication.ActiveDocument;

            if (!selectedBlockIds.Any()) throw new System.Exception("No blocks found.");

            // Stap 2: Extraheer block eigenschappen
            List<IapBlock> iAPBlocksWithPropAndAttr;
            using (var tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                iAPBlocksWithPropAndAttr = ExtractBlockProperties(selectedBlockIds, db, doc, tr);
                tr.Commit();
            }

            if (!iAPBlocksWithPropAndAttr.Any()) throw new System.Exception("No blocks found.");

            // Stap 3 Clasifficeer de IAPBlocken volgens de clasifficatie logica

            var classifications = new List<ClassificationObject>();
            foreach (var blk in iAPBlocksWithPropAndAttr)
            {
                classifications.Add(new ClassificationObject(blk));
            }

            // Stap 3: Maak CogoPoints aan
            var cogoPointMapping = CreateCogoPointsFromBlocks(classifications);

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

        public static Dictionary<ObjectId, ObjectId> AddCogoPoints(List<ClassificationObject> blocks)
        {
            var result = new Dictionary<ObjectId, ObjectId>();
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (blocks == null || !blocks.Any())
                return result;

            // Extract data
            var locations = new Point3dCollection(blocks.Select(b => b.Block.InsertionPoint3D).ToArray());
            var originalBlockIds = blocks.Select(b => b.ObjectId).ToList();

            using (var tr = doc.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var civilDoc = CivilApplication.ActiveDocument;

                // Haal default point en label style namen op via feature-instellingen
                var pointSettings = civilDoc.Settings.GetSettings<SettingsPoint>();
                var defaultPointStyleName = pointSettings?.Styles.Point.Value;
                var defaultLabelStyleName = pointSettings?.Styles.PointLabel.Value;

                // Haal bijbehorende ObjectIds uit de stijlencollecties; vang fouten af
                var defaultPointStyleId = ObjectId.Null;
                var defaultLabelStyleId = ObjectId.Null;

                try
                {
                    if (!string.IsNullOrEmpty(defaultPointStyleName) &&
                        civilDoc.Styles.PointStyles.Contains(defaultPointStyleName))
                        defaultPointStyleId = civilDoc.Styles.PointStyles[defaultPointStyleName];
                }
                catch
                {
                    // fallback naar Null, of todo:log voor troubleshooting
                    
                }

                try
                {
                    if (!string.IsNullOrEmpty(defaultLabelStyleName) &&
                        civilDoc.Styles.LabelStyles.PointLabelStyles.LabelStyles.Contains(defaultLabelStyleName))
                        defaultLabelStyleId =
                            civilDoc.Styles.LabelStyles.PointLabelStyles.LabelStyles[defaultLabelStyleName];
                }
                catch
                {
                    // fallback naar Null, of todo:log voor troubleshooting
                }

                // Voeg COGO-punten toe zonder user interaction
                var newPointIds = civilDoc.CogoPoints.Add(
                    locations,
                    string.Empty, // lege beschrijving bij inizialisatie
                    false,
                    false,
                    true
                );

                // Mapping blokken naar nieuwe punten
                for (var i = 0; i < newPointIds.Count; i++)
                {
                    if (i >= originalBlockIds.Count) continue;
                    var cogoPoint = tr.GetObject(newPointIds[i], OpenMode.ForWrite) as CogoPoint;
                    if (cogoPoint != null)
                    {
                        // Vul properties in
                        var blockClass = blocks[i];
                        cogoPoint.PointName = blockClass.NewCogoPointName;
                        cogoPoint.RawDescription = blockClass.NewCogoPointRawDescription;
                        cogoPoint.LayerId = Layers.CreateLayer(blockClass.NewCogoPointLayerName, 0, "");

                        // Style instellen (point en label)
                        if (!defaultPointStyleId.IsNull)
                            cogoPoint.StyleId = defaultPointStyleId;
                        if (!defaultLabelStyleId.IsNull)
                            cogoPoint.LabelStyleId = defaultLabelStyleId;
                    }

                    result.Add(originalBlockIds[i], newPointIds[i]);
                }

                tr.Commit();
            }

            return result;
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
            var lastChar = input.Last();
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
            foreach (var cogoId in civDoc.CogoPoints)
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


        public enum ClassificationsEnum { UnknownEntity, KnownByLayer, KnownByAttibuteTag, KnownByRealBlockName }
        public class PointStyleMapping
        {
            public ClassificationsEnum Classification { get; set; }
            public string Identifier { get; set; }
            public string PointStyleName { get; set; }
            public string Description { get; set; }
            public int Priority { get; set; }
        }

        public class ConversionDetails
        {
            public string BlockName { get; set; }
            public string BlockLayer { get; set; }
            public ClassificationsEnum Classification { get; set; }
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
            public Dictionary<ClassificationsEnum, int> ConversionsByClassification => Details.GroupBy(d => d.Classification).ToDictionary(g => g.Key, g => g.Count());
        }

        internal class ClassificationObject
        {
            public ObjectId ObjectId { get { return IapBlock.Id; } }
            public ClassificationsEnum Classification { get; private set; }

            /// <summary>
            /// Dit is de text die gebruikt wordt om bij de projectie van een punt in het profileVew de zoals gedefinieerd in de projectie stijl I-AM_CAT(in infrabel template)
            /// </summary>
            public string NewCogoPointName { get; private set; } = string.Empty;
            public string NewCogoPointLayerName { get; private set; } = string.Empty;
            public string NewCogoPointRawDescription { get; private set; } = string.Empty;
            public string NewCogoPointStyleName { get; private set; } = "<default>";
            public string NewCogoPointPointLabelStyleName { get; private set; } = "<default>";
            public List<IapBlockAttributes> AttributeTags { get { return IapBlock?.Attributes; } }
            private IapBlock IapBlock { get; }
            public IapBlock Block => IapBlock;


            public ClassificationObject(IapBlock iApBlock)
            {
                IapBlock = iApBlock ?? throw new ArgumentNullException(nameof(iApBlock));
                var allKnownBlocks =
                     _knownBlocksCat
                         .Concat(_knownBlocksKp)
                         .Concat(_knownBlocksHp)
                         .Concat(_knownBlocksSignal)
                         .Concat(_knownBlocksSwitch)
                         .ToList();

                // Eerst classificeren we het object
                Classification = Classify(iApBlock, allKnownBlocks);

                // En nu kunnen we de eigenschappen instellen aan de hand van de classificatie
                FillPropertiesByClassification(this, _knownBlocksCat, _knownBlocksKp, _knownBlocksHp, _knownBlocksSignal, _knownBlocksSwitch);
            }

            private static void FillPropertiesByClassification(ClassificationObject classification, List<string> knownBlocksCat, List<string> knownBlocksKp, List<string> knownBlocksHp, List<string> knownBlocksSignal, List<string> knownBlocksSwitch)
            {
                var sw = classification.Classification;

                switch (sw)
                {
                    case ClassificationsEnum.UnknownEntity:
                        FillPropertiesUnknownEntity(classification); break;
                    case ClassificationsEnum.KnownByLayer:
                        FillPropertiesKnownByLayer(classification); break;
                    case ClassificationsEnum.KnownByAttibuteTag:
                        FillPropertiesKnownByAttributeTag(classification); break;
                    case ClassificationsEnum.KnownByRealBlockName:
                        FillPropertiesKnownByRealBlockName(classification, knownBlocksCat, knownBlocksKp, knownBlocksHp, knownBlocksSignal, knownBlocksSwitch);
                        break;

                }

            }


            // TODO Get this from setting instead of hardcoding
            // these are the BlockDefinition Names.
            private readonly List<string> _knownBlocksCat = new List<string>() { "CAT", "811", "969_GoujonRep_Stifbout" };
            private readonly List<string> _knownBlocksKp = new List<string>() { "KP", "" };
            private readonly List<string> _knownBlocksHp = new List<string>() { "HP", "" };
            private readonly List<string> _knownBlocksSignal = new List<string>() { "Signal", "" };
            private readonly List<string> _knownBlocksSwitch = new List<string>() { "Switch", "" };


            private static void FillPropertiesKnownByRealBlockName(ClassificationObject classification, List<string> knownBlocksCat, List<string> knownBlocksKp, List<string> knownBlocksHp, List<string> knownBlocksSignal, List<string> knownBlocksSwitch)
            {
                // this means we can use hardcoded convertion rules, because we know the block, and know what to expect
                // ea. If this is a Hectometer Palen, Kilometer Palen, Katenapalen, Switches, Seinen, AluminiumLassen, enz...
                // opmeter kunnen op veschillende manieren hun block genoemd hebben, dus in de instellingen kunnen we
                // deze mappen 'custom name', map to Kp, Hp, Cat, Signal, Switch
                // Bijvoorbeeld: Bij een switch moeten we 4 punten toevoegen als cogopoint
                // Deze implementeren is PRIO 1!!

                //
                // a CAT
                if (knownBlocksCat.Contains(classification.IapBlock.BlockName))
                {
                    classification.NewCogoPointLayerName = "C3D_COGO_E_CAT";
                    classification.NewCogoPointName =
                        classification.AttributeTags.Find(x => x.Tag.Equals("PUNTNUMMER")).Text;
                    // Bij catena's moeten we er voor zorgen dat wanneer de naam achteraan een even nummer is, de punt
                    // CATB (rechts van het spoor) heeft in de beschrijving, anders is het CATA(links van het spoor)
                    var last = CheckLastDigit(classification.NewCogoPointName);
                    switch (last)
                    {
                        case LastDigit.NotANumber:
                            { classification.NewCogoPointRawDescription = ("CAT " + classification.NewCogoPointName).Trim(); break; }
                        case LastDigit.Even:
                            { classification.NewCogoPointRawDescription = ("CATB " + classification.NewCogoPointName).Trim(); break; }
                        case LastDigit.Odd:
                            { classification.NewCogoPointRawDescription = ("CATA " + classification.NewCogoPointName).Trim(); break; }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }



            private static void FillPropertiesKnownByAttributeTag(ClassificationObject classification)
            {
                throw new NotImplementedException();
            }

            private static void FillPropertiesKnownByLayer(ClassificationObject classification)
            {
                throw new NotImplementedException();
            }

            private static void FillPropertiesUnknownEntity(ClassificationObject classification)
            {
                throw new NotImplementedException();
            }

            private static ClassificationsEnum Classify(IapBlock iApBlock, List<string> allKnownBlocks)
            {
                // We get all know block names from the hardcoded values
                var knownRealBlockNames = allKnownBlocks;
                //var knownLayers = DeserializeStringCollection(Settings.Default.FromBlockToCogo_Known_Layer_Names_ToSetCogoProperties);
                //var knownAttributeNames = DeserializeStringCollection(Settings.Default.FromBlockToCogo_Known_Block_Attributes_ToSetCogoProperties);
                //var attributeTags = iApBlock.Attributes;
                // Controleer of ObjectName voorkomt in de lijst van bekende bloknamen
                if (!string.IsNullOrEmpty(iApBlock.RawAutoCadName) &&
                    knownRealBlockNames.Any(knownName => string.Equals(knownName, iApBlock.RawAutoCadName, StringComparison.OrdinalIgnoreCase)))
                { return ClassificationsEnum.KnownByRealBlockName; }

                //// Controleer of een van de attribuutTags voorkomt in de lijst van bekende attribuutnamen
                //else if (attributeTags != null && attributeTags.Any() &&
                //     attributeTags.Any(attr => !string.IsNullOrEmpty(attr.Tag) &&
                //                       knownAttributeNames.Any(name => string.Equals(name, attr.Tag, StringComparison.OrdinalIgnoreCase))))
                //{ return ClassificationsEnum.KnownByAttibuteTag; }

                //// Controleer of LayerName voorkomt in de lijst van bekende laagnamen
                //else if (!string.IsNullOrEmpty(iApBlock.Layer) &&
                //     _knownLayers.Any(layer => string.Equals(layer, iApBlock.Layer, StringComparison.OrdinalIgnoreCase)))
                //{ return ClassificationsEnum.KnownByLayer; }

                // Geen gekend object, dus behandelen we deze als unknown
                return ClassificationsEnum.UnknownEntity;


            }

        }

        private static class PointStyleMappingManager
        {
            public static Dictionary<ClassificationsEnum, List<PointStyleMapping>> LoadStyleMappingsFromSettings()
            {
                return LoadStyleMappingsFromXml(Settings.Default.FromBlockToCogo_StyleMappingConfiguration);
            }
            private static Dictionary<ClassificationsEnum, List<PointStyleMapping>> LoadStyleMappingsFromXml(string xmlConfig)
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
                            if (System.Enum.TryParse<ClassificationsEnum>(node.SelectSingleNode("Classification")?.InnerText, out var classification) &&
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
            public static void ValidateStyleMappings(Dictionary<ClassificationsEnum, List<PointStyleMapping>> styleMappings, PointStyleCollection pointStyles)
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
            private static Dictionary<ClassificationsEnum, List<PointStyleMapping>> GetDefaultStyleMappings()
            {
                // Provide some sensible defaults
                var mappings = new List<PointStyleMapping>
                {
                   new PointStyleMapping { Classification = ClassificationsEnum.KnownByRealBlockName, Identifier = "HP", PointStyleName = "Standard", Description = "CATA", Priority = 1 },
                   new PointStyleMapping { Classification = ClassificationsEnum.KnownByRealBlockName, Identifier = "HP", PointStyleName = "Standard", Description = "CATB", Priority = 1 },
                   new PointStyleMapping { Classification = ClassificationsEnum.UnknownEntity, Identifier = "*", PointStyleName = "Standard", Description = "822", Priority = 99 }
                };
                return mappings.GroupBy(m => m.Classification).ToDictionary(g => g.Key, g => g.ToList());
            }
            public static (ObjectId styleId, string description) GetStyleForClassification(ClassificationObject classification, Dictionary<ClassificationsEnum, List<PointStyleMapping>> styleMappings, PointStyleCollection pointStyles, ObjectId fallbackStyleId)
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
                //switch (classification.Classification)
                //{
                //    case ClassificationsEnum.KnownByRealBlockName: return classification.NewCogoPointName;
                //    case ClassificationsEnum.KnownByLayer: return classification.NewLayerName;
                //    case ClassificationsEnum.KnownByAttibuteTag: return classification.AttributeNames..FirstOrDefault();
                //    default: return "*";
                //} 
                throw new NotImplementedException("This function should be removed!");
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


