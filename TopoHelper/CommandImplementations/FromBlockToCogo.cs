using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using MoreLinq;

namespace Infrabel.AutodeskPlatform.TopoHelper.CommandImplementations
{
    internal static class FromBlockToCogo
    {
        private static string[] CogoPointDescriptions = new string[] { "CATA", "CATB", "822" };
        private static string[] CogoPointNummerAttibute = new string[] { "nr", "puntnummer", "naam", "nr", "nummer" };

        // Retrieves the 'PUNTNUMMER' attribute value from a block by searching through its attributes
        // Returns null if the attribute is not found
        private static string GetPuntNummer(IapBlock block)
        {
            return block.Attributes
                .Where(a => a.Tag != null && CogoPointNummerAttibute.Contains(a.Tag, StringComparer.OrdinalIgnoreCase))
                .Select(a => a.Text)
                .FirstOrDefault();
        }

        // Configures all properties of a CogoPoint based on the source block reference
        // Sets the point name (ensuring uniqueness), description based on last digit rules,
        // layer, and style properties. The description is set to:
        // - "CATA" if the last digit is odd
        // - "CATB" if the last digit is even
        // - "822" if the last character is not a number
        private static void SetCogoPointProperties(
            CogoPoint cgPoint,
            BlockReference blockRef,
            IapBlock block,
            ObjectId styleId,
            ObjectId labelStyleId,
            HashSet<string> existingNames,
            string defaultLayereName)
        {
            var baseName = GetPuntNummer(block) ?? string.Empty;
            var newName = baseName;
            int suffix = 1;
            // Ensure unique name
            while (!string.IsNullOrEmpty(newName) && existingNames.Contains(newName))
            {
                newName = $"{baseName}_{suffix}";
                suffix++;
            }
            cgPoint.PointName = newName;
            if (!string.IsNullOrEmpty(newName))
                existingNames.Add(newName);

            if (!string.IsNullOrEmpty(newName))
            {
                var lastDigitResult = CheckLastDigit(newName);
                if (lastDigitResult == LastDigitIsEvenOrOddResult.Odd)
                    cgPoint.RawDescription = "CATA";
                else if (lastDigitResult == LastDigitIsEvenOrOddResult.Even)
                    cgPoint.RawDescription = "CATB";
                else
                    cgPoint.RawDescription = "822";
            }
            else
            {
                cgPoint.RawDescription = string.Empty;
            }

            // Use defaultLayereName if provided, otherwise use blockRef.Layer if not null/empty, otherwise use current layer
            string layerName = !string.IsNullOrEmpty(defaultLayereName) ? defaultLayereName :
                             (!string.IsNullOrEmpty(blockRef.Layer) ? blockRef.Layer :
                             Application.DocumentManager.MdiActiveDocument.Database.Clayer.ToString());
            cgPoint.Layer = layerName;

            if (styleId != ObjectId.Null)
                cgPoint.StyleId = styleId;
            if (labelStyleId != ObjectId.Null)
                cgPoint.LabelStyleId = labelStyleId;
        }

        /// <summary>
        /// Executes the command to convert selected blocks to CogoPoints.
        /// </summary>
        /// <param name="defaultLabelStyleName">The name of the default label style to apply to the CogoPoints.</param>
        /// <param name="defaultLayereName">The name of the default layer to assign to the CogoPoints.</param>
        public static void ExecuteCommand(string defaultLabelStyleName, string defaultLayereName)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var civDoc = CivilApplication.ActiveDocument;

            List<IapBlock> iAPBlocksReadyToConvert;
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                // Prompt user to select multiple blocks
                PromptSelectionOptions pso = new PromptSelectionOptions();
                pso.MessageForAdding = "\nSelect blocks: ";
                SelectionFilter filter = new SelectionFilter(new[]
                {
                    new TypedValue((int)DxfCode.Start, "INSERT") // BlockReference
                });
                PromptSelectionResult psr = doc.Editor.GetSelection(pso, filter);

                if (psr.Status != PromptStatus.OK)
                {
                    return;
                }

                var selectedBlockIds = psr.Value.GetObjectIds().ToList();
                if (selectedBlockIds.Count == 0)
                {
                    return;
                }

                // Get all block names without filtering for uniqueness
                var selectedBlockNames = new Dictionary<ObjectId, string>();
                foreach (var blockId in selectedBlockIds)
                {
                    var blockRef = (BlockReference)tr.GetObject(blockId, OpenMode.ForRead);
                    if (blockRef.Name != null)
                        selectedBlockNames.Add(blockId, blockRef.Name);
                }

                // Get properties and materialize the collection before transaction ends
                iAPBlocksReadyToConvert = BlockScanner.GetPropertiesOfBlocksById(
                    selectedBlockIds,
                    db,
                    doc,
                    null,
                    tr).ToList();  // Materialize the collection

                tr.Commit();
            }


            // Create new CogoPoinst at the block's positions and safe ID's
            var newCogoPoints = AddCogoPoints(new Point3dCollection(iAPBlocksReadyToConvert.Select(b => b.InsertionPoint3D).ToArray()), iAPBlocksReadyToConvert.Select(c => c.Id).ToList(), "CogoPoint");

            // Batch all CogoPoint property writes into a single transaction
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                // Lookup style and label only once
                var styleId = GetPointStyleIdByName(civDoc.Styles.PointStyles, "822_overhead_line_pole_anchoring", tr);
                var labelStyleId = GetLabelStyleIdByName(civDoc.Styles.LabelStyles.PointLabelStyles.LabelStyles, defaultLabelStyleName, tr);
                // Collect all existing CogoPoint names once
                var existingNames = GetAllCogoPointNames(civDoc, tr);
                foreach (var block in iAPBlocksReadyToConvert)
                {
                    var blockRef = (BlockReference)tr.GetObject(block.Id, OpenMode.ForRead);

                    if (blockRef != null && blockRef.BlockTableRecord != ObjectId.Null)
                    {
                        CogoPoint cgPoint = (CogoPoint)tr.GetObject(newCogoPoints[block.Id], OpenMode.ForWrite);
                        SetCogoPointProperties(cgPoint, blockRef, block, styleId, labelStyleId, existingNames, defaultLayereName);
                    }
                }
                tr.Commit();
            }
        }

        /// <summary>
        /// Creates new CogoPoints at the specified locations and maintains a mapping between the original block IDs and the newly created CogoPoint IDs.
        /// </summary>
        /// <param name="locations">A collection of 3D points where the CogoPoints will be created.</param>
        /// <param name="originalBlockIds">A list of original block IDs corresponding to the locations.</param>
        /// <param name="description">An optional description for the CogoPoints.</param>
        /// <returns>A dictionary where the key is the original block ID and the value is the new CogoPoint ID.</returns>
        /// <exception cref="ArgumentNullException">Thrown when locations or originalBlockIds is null.</exception>
        /// <exception cref="ArgumentException">Thrown when locations or originalBlockIds is empty, or when their counts do not match.</exception>
        public static Dictionary<ObjectId/*block id*/, ObjectId/*cogopoint id*/> AddCogoPoints(Point3dCollection locations, List<ObjectId> originalBlockIds, string description = "")
        {
            if (locations == null)
                throw new ArgumentNullException(nameof(locations), "Locations collection cannot be null");
            if (originalBlockIds == null)
                throw new ArgumentNullException(nameof(originalBlockIds), "Original block IDs collection cannot be null");
            if (locations.Count == 0)
                throw new ArgumentException("Locations collection cannot be empty", nameof(locations));
            if (originalBlockIds.Count == 0)
                throw new ArgumentException("Original block IDs collection cannot be empty", nameof(originalBlockIds));
            if (locations.Count != originalBlockIds.Count)
                throw new ArgumentException("Locations and original block IDs collections must have the same length");

            var returnDictionary = new Dictionary<ObjectId, ObjectId>();


            // Get the current document and database
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;


            // Start a transaction
            using (Transaction trans = db.TransactionManager.StartOpenCloseTransaction())
            {
                // Get Civil 3D database
                CivilDocument civilDoc = CivilApplication.ActiveDocument;

                // Add the new COGO points to the collection
                ObjectIdCollection newPointId = civilDoc.CogoPoints.Add(locations, description, false, false, true);

                // Commit the transaction
                trans.Commit();
                for (int i = 0; i < newPointId.Count; i++)
                {
                    returnDictionary.Add(originalBlockIds[i], newPointId[i]);
                }
            }

            return returnDictionary;

        }

        private static ObjectId GetPointStyleIdByName(PointStyleCollection pointStyles, string styleName, Transaction tr)
        {
            if (string.IsNullOrEmpty(styleName))
                throw new ArgumentException("Style name cannot be null or empty", nameof(styleName));
            // Use the indexer if available, otherwise fall back to LINQ
            return pointStyles[styleName];
        }

        // Searches through the LabelStyleCollection to find a label style with the specified name
        // Returns the ObjectId of the matching label style, or ObjectId.Null if not found
        private static ObjectId GetLabelStyleIdByName(LabelStyleCollection labelStyles, string styleName, Transaction tr)
        {

            foreach (ObjectId styleId in labelStyles)
            {
                using (LabelStyle labelStyle = (LabelStyle)tr.GetObject(styleId, OpenMode.ForRead))
                {
                    if (labelStyle.Name == styleName)
                    {
                        return styleId;
                    }
                }
            }

            return ObjectId.Null;
        }

        // Analyzes the last character of a string to determine if it's an even number, odd number, or not a number
        // Returns LastDigitIsEvenOrOddResult enum value indicating the result of the analysis
        private static LastDigitIsEvenOrOddResult CheckLastDigit(string input)
        {
            if (string.IsNullOrEmpty(input))
            {

                return LastDigitIsEvenOrOddResult.NotANumber;
            }

            string lastChar = input.Substring(input.Length - 1, 1);

            if (char.IsDigit(lastChar[0]))
            {
                int lastDigit = int.Parse(lastChar);
                if (lastDigit % 2 == 0)
                {

                    return LastDigitIsEvenOrOddResult.Even;
                }
                else
                {

                    return LastDigitIsEvenOrOddResult.Odd;
                }
            }
            else
            {

                return LastDigitIsEvenOrOddResult.NotANumber;
            }
        }

        private enum LastDigitIsEvenOrOddResult
        {
            NotANumber = 0,
            Even = 2,
            Odd = 1,

        }

        // Collects all existing CogoPoint names from the Civil document into a HashSet for efficient lookup
        // Uses case-insensitive comparison to ensure uniqueness regardless of case
        private static HashSet<string> GetAllCogoPointNames(CivilDocument civDoc, Transaction tr)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ObjectId cogoId in civDoc.CogoPoints)
            {
                var cogo = (CogoPoint)tr.GetObject(cogoId, OpenMode.ForRead);
                names.Add(cogo.PointName);
            }
            return names;
        }
    }

}
