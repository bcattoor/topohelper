using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
//todo: using Simplifynet; #disabled until original source code is found
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using TopoHelper;
using TopoHelper.AutoCAD;
using TopoHelper.CommandImplementations;
using TopoHelper.Csv;
using TopoHelper.Model;
using TopoHelper.Model.Calculations;
using TopoHelper.Model.Results;
using TopoHelper.Properties;
using TopoHelper.UserControls;
using TopoHelper.UserControls.ViewModels;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = System.Exception;
using Point = TopoHelper.Model.Geometry.Point;

[assembly: CommandClass(typeof(Commands))]

namespace TopoHelper
{
    // ReSharper disable once PartialTypeWithSinglePart (Commands classes need to be partial?)
    public static partial class Commands
    {
        #region Public Fields

        public const double Tolerance = 0.000000001d;

        #endregion

        #region Private Fields

        private const string Calculating = "\r\nCalculating, please wait ...";
        private const string DataValuationErrorOccurred = "\r\n\t=> A data validation error has occurred: ";
        private const string FunctionCanceled = "\r\n\t=> Function has been canceled.";
        private const string LeftRail = "\r\nPlease select the polyline3d you would like to use for the left rail.";
        private const string RightRail = "\r\nPlease select the polyline3d you would like to use for the right rail.";
        private const string Select3dPolyLine = "\r\nSelect a 3d-polyline: ";
        private const string SelectionNotUnique = "\r\nYou selected the same polyline twice, why?";

        // ReSharper disable once UnusedMember.Local
        private static readonly Plane MyPlaneWcs = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));

        private static readonly Settings SettingsDefault = Settings.Default;
        private static ObjectId IAMTopo_AlignAngleOfBlockLastSelectedPolylineId = ObjectId.Null;
        private static string Patern;

        #endregion

        #region Public Properties

        public static SettingsUserControl MySettingsView { get; private set; }
        public static SettingsViewModel MySettingsViewModel { get; private set; }
        public static PaletteSet PaletteSet { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// This command is used by user to align an inserted blocks angle
        /// property to a selected polyline.
        /// </summary>
        [CommandMethod("IAMTopo_AlignAngleOfBlock", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        public static void IAMTopo_AlignAngleOfBlock()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            var editor = document.Editor;
            var database = document.Database;

            try
            {
                if (IAMTopo_AlignAngleOfBlockLastSelectedPolylineId == ObjectId.Null)
                {// Select the polyline
                    var promptEntityOptions = new PromptEntityOptions("\nSelect 3D-polyline to align to.");
                    promptEntityOptions.SetRejectMessage("\nInvalid selection...");
                    promptEntityOptions.AddAllowedClass(typeof(Polyline3d), true);
                    promptEntityOptions.AllowObjectOnLockedLayer = true;
                    var promptResult = editor.GetEntity(promptEntityOptions);
                    if (PromptStatus.OK != promptResult.Status)
                        return;

                    IAMTopo_AlignAngleOfBlockLastSelectedPolylineId = promptResult.ObjectId;
                }
                else
                {
                    editor.WriteMessage("Using last selected entity..., use the [IAMTopo_AlignAngleOfBlock_ResetEntity] to reset this.\r\n");
                }

                // Select the blockreference to set angle
                var peo2 = new PromptEntityOptions("\nSelect the BlockReference.");
                peo2.SetRejectMessage("\nInvalid selection...");
                peo2.AddAllowedClass(typeof(BlockReference), true);
                var promptResult2 = editor.GetEntity(peo2);
                if (PromptStatus.OK != promptResult2.Status)
                    return;

                using (Transaction transaction = database.TransactionManager.StartOpenCloseTransaction())
                {
                    AlignAngleOfBlock.IAMTopo_AlignAngleOfBlock(
                        transaction,
                        promptResult2.ObjectId,
                        IAMTopo_AlignAngleOfBlockLastSelectedPolylineId,
                        SettingsDefault.AlignAngleOfBlock_AddAngleValue_TimesPI,
                        SettingsDefault.AlignAngleOfBlock_DynamicPropertyName);
                    transaction.Commit();
                }
            }
            catch (System.Exception exception) { HandleUnexpectedException(exception); }
        }

        [CommandMethod("IAMTopo_PlaceTextOnLineWithLength", CommandFlags.Modal)]
        public static void IAP_PlaceTextOnLineWithLength()
        {
            try
            {
                CommandImplementations.PlaceTextOnLineWithLength.ExcecuteCommand(false, FunctionCanceled);
            }
            catch (System.Exception exception)
            {
                HandleUnexpectedException(exception);
            }
        }

        /// <summary>
        /// This command is used by user to reset the objectid
        /// </summary>
        [CommandMethod("IAMTopo_AlignAngleOfBlock_ResetEntity", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        public static void IAMTopo_AlignAngleOfBlock_ResetEntity()
        {
            IAMTopo_AlignAngleOfBlockLastSelectedPolylineId = ObjectId.Null;
        }

        [CommandMethod("IAMTopo_CleanNonSurveyVertexFromPolyline", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        public static void IAMTopo_CleanNonSurveyVertexFromPolyline()
        {
            var document = Application.DocumentManager.MdiActiveDocument;

            var database = document.Database;
            var editor = document.Editor;
            try
            {
                //Clear selection
                editor.SetImpliedSelection(Array.Empty<ObjectId>());

                var promptOptions = new PromptEntityOptions("Please select one of the points that are on the polyline to clean.")
                {
                    AllowObjectOnLockedLayer = true,
                    AllowNone = false
                };

                // Select an entity that is on the points-layer
                var promptEntityResult = editor.GetEntity(promptOptions);

                if (promptEntityResult.Status == PromptStatus.Cancel)
                    throw new Exception(FunctionCanceled);
                if (promptEntityResult.Status != PromptStatus.OK)
                    throw new Exception("Something went wrong during the selection.");

                // Read the layer info from the selected entity
                var layerName = database.GetLayerNameFromEntityObjectId(promptEntityResult.ObjectId);

                // Make sure object is supported (we support points and block inserts
                if (!promptEntityResult.ObjectId.ObjectClass.DxfName.Equals("INSERT", StringComparison.OrdinalIgnoreCase)
                    && !promptEntityResult.ObjectId.ObjectClass.DxfName.Equals("POINT", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Selected object not supported by function.");
                // Get all entities on that layer, of selected type, in model-space
                var pointIdsOnLayer = editor.GetEntityIdsOnLayer(layerName, "MODEL", promptEntityResult.ObjectId.ObjectClass.DxfName);

                // Go get the positions of all those points/inserts
                var pointsToUseAsFilter = database.GetPoints(pointIdsOnLayer);

                // Making a new list of points, filtered by available points

                // Select what polyline to clean
                var selectedObjectId = editor.Select3dPolyline(Select3dPolyLine);
                if (selectedObjectId == ObjectId.Null)
                {
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }
                editor.SetImpliedSelection(new[] { selectedObjectId });

                var pointsFromPolyline = database.GetPointsFromPolyline(selectedObjectId);
                var pointsFromPolylineEArray = pointsFromPolyline as Point3d[] ?? pointsFromPolyline.ToArray();

                var newPolylineListOfPoint = pointsFromPolylineEArray.ToList().FindAll(vertex =>
                {
                    var res = pointsToUseAsFilter.Find(x => x.IsEqualTo(vertex,
                        new Tolerance(Settings.Default.__APP_EPSILON,
                            Settings.Default.__APP_EPSILON)));
                    return !res.Equals(Point3d.Origin);
                });

                editor.WriteMessage("\r\n\t=>Polyline has been selected with " + pointsFromPolylineEArray.Length + " vertices's.\r\n");

                database.Create3dPolyline(newPolylineListOfPoint);

                // Report
                editor.WriteMessage("\r\n\t=>Polyline has been created with " + newPolylineListOfPoint.Count + " vertices's.");
            }
            catch (System.Exception exception) { HandleUnexpectedException(exception); }
            finally
            {
                // clear selection
                editor.SetImpliedSelection(Array.Empty<ObjectId>());
            }
        }

        /// <summary>
        /// This method calculates the distance between 2 selected 3D polylines.
        /// </summary>
        [CommandMethod("IAMTopo_DistanceBetween2Polylines", CommandFlags.DocReadLock | CommandFlags.NoUndoMarker)]
        public static void IAMTopo_DistanceBetween2Polylines()
        {
            var document = Application.DocumentManager.MdiActiveDocument;

            var database = document.Database;

            var editor = document.Editor;
            try
            {
                #region Selection

                // Let user select a right rail end a left rail
                editor.SetImpliedSelection(Array.Empty<ObjectId>()); // clear selection

                var pl1Id = editor.Select3dPolyline("\r\nSelect first polyline.");
                if (pl1Id == ObjectId.Null)
                {
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }
                editor.SetImpliedSelection(new[] { pl1Id });
                var pl2Id = editor.Select3dPolyline("\r\nSelect second polyline.");
                if (pl1Id == ObjectId.Null)
                {
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }
                editor.SetImpliedSelection(new[] { pl1Id, pl2Id });

                // Make sure we did not select the same polyline twice
                if (pl1Id.Equals(pl2Id))
                {
                    editor.WriteMessage(SelectionNotUnique);
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }

                #endregion

                #region Calculation

                editor.WriteMessage(Calculating);
                var distanceResult = DistanceBetween3dPolylines.CalculateDistanceBetween3dPolylines(database, pl1Id, pl2Id);
                var distanceBetween2PolylinesSectionResults = distanceResult as DistanceBetween2PolylinesSectionResult[] ?? distanceResult.ToArray();
                if (!distanceBetween2PolylinesSectionResults.Any())
                {
                    throw new InvalidOperationException("We failed to calculate any result.");
                }

                #endregion

                #region Write Result

                var csv = ReadWrite.Instance;
                csv.FilePath = SettingsDefault.DistanceBetween2Polylines_PathToCsvFile;
                // create csv from result
                csv.WriteDistanceBetween2PolylinesResult(distanceBetween2PolylinesSectionResults);
                editor.WriteMessage($"Result was written to CSV file {csv.FilePath}");

                #endregion
            }
            catch (System.Exception exception) { HandleUnexpectedException(exception); }
            finally
            {
                // clear selection
                editor.SetImpliedSelection(Array.Empty<ObjectId>());
            }
        }

        /// <summary>
        /// This command is used by user to increment an attribute within an
        /// inserted block property to a selected polyline.
        /// </summary>
        [CommandMethod("IAMTopo_IncrementAttribute", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        public static void IAMTopo_IncrementAttribute()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            var ed = document.Editor;
            var db = document.Database;

            // Select the blockreference to set angle
            var peo2 = new PromptEntityOptions("\nSelect the BlockReference.");
            peo2.SetRejectMessage("\nInvalid selection...");
            peo2.AddAllowedClass(typeof(BlockReference), true);

            var blockSelection = ed.GetEntity(peo2);

            while (blockSelection.Status == PromptStatus.OK)
            {
                if (PromptStatus.OK != blockSelection.Status)
                    return;

                Patern = SettingsDefault.IncrementAttribute_Pattern;
                try
                {
                    using (Transaction transaction = db.TransactionManager.StartOpenCloseTransaction())
                    {
                        IncrementAttribute.ExecuteIncrementsByPatern(transaction, blockSelection.ObjectId, ref Patern, SettingsDefault.IncrementAttribute_Name);
                        transaction.Commit();
                        // Save used pater to settings
                        SettingsDefault.IncrementAttribute_Pattern = Patern;
                        SettingsDefault.Save();

                        if (SettingsDefault.IncrementAttribute_ChainAlign)
                            if (IAMTopo_AlignAngleOfBlockLastSelectedPolylineId != null)
                                AlignAngleOfBlock.IAMTopo_AlignAngleOfBlock(
                                    transaction,
                                    blockSelection.ObjectId,
                                    IAMTopo_AlignAngleOfBlockLastSelectedPolylineId,
                                    SettingsDefault.AlignAngleOfBlock_AddAngleValue_TimesPI,
                                    SettingsDefault.AlignAngleOfBlock_DynamicPropertyName);
                            else throw new Exception("You cannot chain the allign command, without setting the polyline first. Use the [IAMTopo_AlignAngleOfBlock] command to set this.");
                    }
                }
                catch (System.Exception exception) { HandleUnexpectedException(exception); }
                // reselect next block
                blockSelection = ed.GetEntity(peo2);
            }
        }

        [CommandMethod("IAMTopo_JoinPolyline")]
        public static void IAMTopo_JoinPolyline()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            var ed = document.Editor;
            var db = document.Database;
            var peo1 = new PromptEntityOptions(Select3dPolyLine);
            const string InvalidSelection = "\nInvalid selection...";
            peo1.SetRejectMessage(InvalidSelection);
            peo1.AddAllowedClass(typeof(Polyline), true);
            peo1.AddAllowedClass(typeof(Polyline2d), true);
            peo1.AddAllowedClass(typeof(Polyline3d), true);

            // TODO: When the first selected entity is a line, we need to convert it to a polyline before we can continue.
            // TODO: When two 2D-polylines are selected, we need to check if the are coplanar, if not we need to convert both objects to a 3d-polyline
            // TODO: When 2D polyline is converted to a 3D polyline we need to make sure arc's in that polyline are segmented!
            // TODO: When two 2D-polylines are selected, we need to check if the are coplanar, if not we can equal elevation to source and join them as 2D polyline
            peo1.AddAllowedClass(typeof(Line), true);

            var promptResult = ed.GetEntity(peo1);
            if (PromptStatus.OK != promptResult.Status)
                return;

            var selectedEntityObjectId = promptResult.ObjectId;
            ed.SetImpliedSelection(new[] { selectedEntityObjectId });
            var peo2 = new PromptEntityOptions(Select3dPolyLine);
            peo2.SetRejectMessage(InvalidSelection);
            peo2.AddAllowedClass(typeof(Line), true);
            peo2.AddAllowedClass(typeof(Polyline), true);
            peo2.AddAllowedClass(typeof(Polyline), true);
            peo2.AddAllowedClass(typeof(Polyline2d), true);
            peo2.AddAllowedClass(typeof(Polyline3d), true);
            promptResult = ed.GetEntity(peo2);
            if (PromptStatus.OK != promptResult.Status)
                return;
            // clear selection Implication
            ed.SetImpliedSelection(new ObjectId[] { });
            var joinId = promptResult.ObjectId;
            try
            {
                using (Transaction transaction = db.TransactionManager.StartOpenCloseTransaction())
                {
                    using (var sourcePolyline = transaction.GetObject(selectedEntityObjectId, OpenMode.ForWrite) as Curve)
                    using (var polylineToAdd = transaction.GetObject(joinId, OpenMode.ForWrite) as Curve)
                    {
                        if (sourcePolyline == null || polylineToAdd == null) throw new NullReferenceException("sourcePolyline == null || polylineToAdd == null");

                        var startPointCurve1 = sourcePolyline.StartPoint;
                        var startPointCurve2 = polylineToAdd.StartPoint;
                        var endPointCurve1 = sourcePolyline.EndPoint;
                        var endPointCurve2 = polylineToAdd.EndPoint;

                        var distance = new List<Tuple<string, double>>
                        {
                            new Tuple<string, double>("sp1:sp2", startPointCurve1.DistanceTo(startPointCurve2)),
                            new Tuple<string, double>("sp1:ep2", startPointCurve1.DistanceTo(endPointCurve2)),
                            new Tuple<string, double>("ep1:sp2", endPointCurve1.DistanceTo(startPointCurve2)),
                            new Tuple<string, double>("ep1:ep2", endPointCurve1.DistanceTo(endPointCurve2))
                        };

                        var result = distance.OrderBy(i => i.Item2).FirstOrDefault();

                        System.Diagnostics.Trace.Assert(result != null, nameof(result) + " != null");
                        switch (result.Item1)
                        {
                            case "sp1:sp2":
                                { sourcePolyline.JoinEntity(new Line(startPointCurve1, startPointCurve2)); break; }
                            case "sp1:ep2":
                                { sourcePolyline.JoinEntity(new Line(startPointCurve1, endPointCurve2)); break; }
                            case "ep1:sp2":
                                { sourcePolyline.JoinEntity(new Line(endPointCurve1, startPointCurve2)); break; }
                            case "ep1:ep2":
                                { sourcePolyline.JoinEntity(new Line(endPointCurve1, endPointCurve2)); break; }
                        }

                        sourcePolyline.JoinEntity(polylineToAdd);

                        // If user wants to delete source entities
                        if (SettingsDefault.JoinPolyline_DeleteSelectedEntities) polylineToAdd.Erase();

                        transaction.Commit();

                        var vertexCount = sourcePolyline.EndParam + 1;
                        ed.WriteMessage($"\n\rBoth lines were joined together." +
                            $"\n\r\tVertexes: {vertexCount}\n\r\t" +
                            $"Gap distance: {result.Item2:F6}");
                    }
                }
            }
            catch (System.Exception exception) { HandleUnexpectedException(exception); }
        }

        [CommandMethod("IAMTopo_OpenRailView", CommandFlags.Modal)]
        public static void IAMTopo_OpenRailView()
        {
            try
            {
                var currentDocument = Autodesk.AutoCAD.ApplicationServices.
                    Core.Application.DocumentManager.MdiActiveDocument;
                var editor = currentDocument.Editor;
                var promptResult = editor.GetPoint(new Autodesk.AutoCAD.EditorInput.PromptPointOptions("Select a location."));
                if (promptResult.Status != PromptStatus.OK)
                    return;

                var selectedPoint = promptResult.Value;
                Process.Start(string.Format(
                    @"http://georamses/GeoRamses/ImajnetViewer.aspx?COORDX={0}&COORDY={1}&LOCALE=nl",
                    Math.Floor(selectedPoint.X),
                    Math.Floor(selectedPoint.Y)));
            }
            catch (System.Exception exception) { HandleUnexpectedException(exception); }
        }

        /// <summary>
        /// This command is used by user to generate a 3d-polyline from a set of
        /// selected points, it will connect the points with that line, using
        /// points found inside buffer, wit a min, and max buffer radius.
        /// </summary>
        [CommandMethod("IAMTopo_PointsToPolyline", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        public static void IAMTopo_PointsToPolyline()
        {
            var document = Application.DocumentManager.MdiActiveDocument;

            var database = document.Database;

            var editor = document.Editor;
            try
            {
                // lets set some basic values
                var minimumDistance = SettingsDefault.PointsTo3DPolyline_MinimumPointDistance; // drawing units
                var maxDistance = SettingsDefault.PointsTo3DPolyline_MaximumPointDistance; // drawing units

                if (!DataValidation.ValidatePointsToPolylineSettings(out var msg))
                {
                    editor.WriteMessage(msg);
                    return;
                }

                // Let user select a set of points
                editor.SetImpliedSelection(Array.Empty<ObjectId>()); // clear selection
                var points = editor.Select3dPoints();
                if (points == null)
                {
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }
                editor.WriteMessage(Calculating);
                var point3ds = points as Point3d[] ?? points.ToArray();
                var result = ClosestPointsList.Calculate(point3ds.Select(a => new Point(a)).ToList(), new Point(point3ds.First()), minimumDistance, maxDistance);

                if (result.Count() < 2)
                {
                    editor.WriteMessage($"\r\nPolyline was not created, too few vertices's ({result.Count()}) found.");
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }

                // create a 3-dimensional polyline for track-center-line
                database.Create3dPolyline(result.Select(x => x.ToPoint3d()).ToArray());

                editor.WriteMessage($"\r\nPolyline created with {result.Count()} vertices's.");
            }
            catch (System.Exception exception) { HandleUnexpectedException(exception); }
            finally
            {
                // clear selection
                editor.SetImpliedSelection(Array.Empty<ObjectId>());
            }
        }

        [CommandMethod("IAMTopo_RailsToRailwayCenterLine", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        public static void IAMTopo_RailsToRailwayCenterLine()
        {
            var document = Application.DocumentManager.MdiActiveDocument;

            var database = document.Database;

            var editor = document.Editor;
            try
            {
                #region Selection

                // Let user select a right rail and a left rail
                editor.SetImpliedSelection(Array.Empty<ObjectId>()); // clear selection

                var leftRailId1 = editor.Select3dPolyline(LeftRail);

                if (leftRailId1 == ObjectId.Null)
                {
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }

                editor.SetImpliedSelection(new[] { leftRailId1 });
                var rightRailPoints = editor.Select3dPolyline(RightRail, out var rightRailId);

                if (rightRailId == ObjectId.Null)
                {
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }
                editor.SetImpliedSelection(new[] { leftRailId1, rightRailId });
                // Make sure we did not select the same polyline twice
                if (leftRailId1.Equals(rightRailId))
                {
                    editor.WriteMessage(SelectionNotUnique);
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }

                var leftRailPoints = database.GetPointsFromPolyline(leftRailId1);
                if (leftRailPoints is null)
                {
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }

                #endregion

                #region Validation

                // Validate input before trying to calculate railway-axis.
                var point3dsLeft = leftRailPoints as Point3d[] ?? leftRailPoints.ToArray();
                var point3dsRight = rightRailPoints as Point3d[] ?? rightRailPoints.ToArray();
                if (!DataValidation.ValidateInput(point3dsLeft, point3dsRight, out var error))
                {
                    editor.WriteMessage(DataValuationErrorOccurred + error);
                    return;
                }

                #endregion

                editor.WriteMessage(Calculating);

                #region CSD Correcting Survey Displacement

                IEnumerable<CalculateDisplacementSectionResult> correctedResult = null;
                // take away the survey errors of the points
                if (SettingsDefault.RailsToRailwayCenterLine_Use_CalculateSurveyCorrection)
                    correctedResult = SurveyCorrecting.CalculateDisplacement(point3dsLeft, point3dsRight);

                #endregion

                #region Calculate Centerline

                // Calculate railway-alignment-center-line
                IList<MeasuredSectionResult> sections = null;
                IEnumerable<CalculateDisplacementSectionResult> calculateDisplacementSectionResults = null;
                if (correctedResult != null)
                {
                    calculateDisplacementSectionResults = correctedResult as CalculateDisplacementSectionResult[] ?? correctedResult.ToArray();

                    sections =
                       Rails2RailwayCenterLine.CalculateRailwayCenterLine(
                           calculateDisplacementSectionResults.Select(s => s.LeftRailPoint).ToList(),
                           calculateDisplacementSectionResults.Select(s => s.RightRailPoint).ToList())
                       ;
                }
                else
                {
                    sections = Rails2RailwayCenterLine.CalculateRailwayCenterLine(point3dsLeft, point3dsRight);
                }

                #endregion

                #region Draw Result To AutoCAD

                var trackAxis3DPoints = sections.Select(x => x.TrackAxisPoint).ToArray();

                // DRAW CENTER-LINE POLYLINES AND POINTS
                if (SettingsDefault.RailsToRailwayCenterLine_Draw2DPolyline_CenterLine)
                    // create a 2-dimensional polyline for track-center-line 2D
                    database.Create2dPolyline(
                        points: trackAxis3DPoints,
                        layerName: SettingsDefault.LayerNamePrefix_2DObjects + SettingsDefault.RailsToRailwayCenterLine_LayerNameCenterline,
                        layerColor: SettingsDefault.RailsToRailwayCenterLine_LayerColorOfCenterline3DPolyLine);

                if (SettingsDefault.RailsToRailwayCenterLine_Draw3DPolyline_CenterLine)
                    // create a 3-dimensional polyline for track-center-line 3D
                    database.Create3dPolyline(trackAxis3DPoints,
                    SettingsDefault.LayerNamePrefix_3DObjects + SettingsDefault.RailsToRailwayCenterLine_LayerNameCenterline,
                    SettingsDefault.RailsToRailwayCenterLine_LayerColorOfCenterline3DPolyLine);

                if (SettingsDefault.RailsToRailwayCenterLine_DrawCenterline3DPoints)
                    // create 3-dimensional points
                    database.CreatePoints(
                        points: trackAxis3DPoints,
                        layerName: SettingsDefault.LayerNamePrefix_3DObjects + SettingsDefault.RailsToRailwayCenterLine_LayerNameCenterLine3DPoints,
                        layerColor: SettingsDefault.RailsToRailwayCenterLine_LayerColorCenterline3DPoints);

                if (SettingsDefault.RailsToRailwayCenterLine_DrawCenterline2DPoints)
                    // create 2-dimensional points
                    database.CreatePoints(
                        points: trackAxis3DPoints.Select(p => p.T2d().T3d(0)).ToArray(),
                        layerName: SettingsDefault.LayerNamePrefix_2DObjects + SettingsDefault.RailsToRailwayCenterLine_LayerNameCenterLine3DPoints);

                if (SettingsDefault.RailsToRailwayCenterLine_Use_CalculateSurveyCorrection)
                {
                    // DRAW CSD - RAILS
                    if (SettingsDefault.CalculateSurveyCorrection_Draw3DPolyline_Rails)
                    {
                        // RIGHT --> create a 3-dimensional polyline for correctedResult
                        database.Create3dPolyline(calculateDisplacementSectionResults.Select(s => s.LeftRailPoint),
                        SettingsDefault.LayerNamePrefix_3DObjects + SettingsDefault.CalculateSurveyCorrection_LayerNamePolylines_Rails,
                        SettingsDefault.CalculateSurveyCorrection_LayerColorPolyline_Rails);

                        // LEFT --> create a 3-dimensional polyline for correctedResult
                        database.Create3dPolyline(calculateDisplacementSectionResults.Select(s => s.RightRailPoint),
                        SettingsDefault.LayerNamePrefix_3DObjects + SettingsDefault.CalculateSurveyCorrection_LayerNamePolylines_Rails,
                        SettingsDefault.CalculateSurveyCorrection_LayerColorPolyline_Rails);
                    }

                    if (SettingsDefault.CalculateSurveyCorrection_Draw3DPoints_Rails)
                    { // create 3-dimensional points
                        database.CreatePoints(
                            points: calculateDisplacementSectionResults.Select(s => s.LeftRailPoint),
                            layerName: SettingsDefault.LayerNamePrefix_3DObjects + SettingsDefault.CalculateSurveyCorrection_LayerNamePoints_Rails,
                            layerColor: SettingsDefault.CalculateSurveyCorrection_LayerColorPoints_Rails);

                        database.CreatePoints(
                             points: calculateDisplacementSectionResults.Select(s => s.RightRailPoint),
                             layerName: SettingsDefault.LayerNamePrefix_3DObjects + SettingsDefault.CalculateSurveyCorrection_LayerNamePoints_Rails,
                             layerColor: SettingsDefault.CalculateSurveyCorrection_LayerColorPoints_Rails);
                    }
                }

                #endregion

                #region Write Result

                var result = WriteResultToFile(calculateDisplacementSectionResults, sections);
                editor.WriteMessage(result);

                #endregion
            }
            catch (System.Exception exception) { HandleUnexpectedException(exception); }
            finally
            {
                // clear selection
                editor.SetImpliedSelection(Array.Empty<ObjectId>());
            }
        }

        /// <summary>
        /// This command is used by user to generate a 3d-polyline from a set of
        /// selected points, it will connect the points with that line, using
        /// points found inside buffer, with a min, and max buffer radius.
        /// </summary>
        [CommandMethod("IAMTopo_Settings", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        public static void IAMTopo_Settings()
        {
            var document = Application.DocumentManager.MdiActiveDocument;

            // var database = document.Database;

            var editor = document.Editor;
            try
            {
                if (PaletteSet == null)
                {
                    PaletteSet = new PaletteSet(
                        "", "", new Guid("1c50e559-c891-4041-a5ac-cd6e479473ec"));
                    MySettingsViewModel = new SettingsViewModel
                    {
                        // Hide pane when canceled is clicked.
                        OnCancel = () => { PaletteSet.Visible = false; }
                    };
                    MySettingsView = new SettingsUserControl
                    {
                        DataContext = MySettingsViewModel
                    };

                    PaletteSet.MinimumSize = new Size(240, 225);
                    PaletteSet.Size = new Size(800, 400);
                    PaletteSet.Name = "Topohelper settings menu";
                    PaletteSet.AddVisual("Topohelper settings menu", MySettingsView, true);
                    PaletteSet.Style = PaletteSetStyles.Snappable |
                                       PaletteSetStyles.UsePaletteNameAsTitleForSingle |
                                       PaletteSetStyles.ShowAutoHideButton |
                                       PaletteSetStyles.ShowPropertiesMenu;
                }

                // now display the palette set if not yet visible, else hide
                PaletteSet.Visible = !PaletteSet.Visible;
                if (PaletteSet.Visible && MySettingsViewModel.ReloadSettingsCommand.CanExecute(null))
                    MySettingsViewModel.ReloadSettingsCommand.Execute(null);
            }
            catch (System.Exception exception) { HandleUnexpectedException(exception); }
        }

        [CommandMethod("IAMTopo_SimplifyPolyline", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        public static void IAMTopo_SimplifyPolyline()
        {
            var document = Application.DocumentManager.MdiActiveDocument;

            var database = document.Database;
            var editor = document.Editor;
            try
            {
                //? This function is disabled until we have found the original source code
                /*
                //Clear selection
                editor.SetImpliedSelection(new ObjectId[0]);

                //Making a new result and adding options to it
                var selectedObjectId = editor.Select3dPolyline(Select3dPolyLine);

                if (selectedObjectId == ObjectId.Null)
                {
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }

                editor.SetImpliedSelection(new[] { selectedObjectId });

                var points = database.GetPointsFromPolyline(selectedObjectId);
                var enumerable = points as Point3d[] ?? points.ToArray();
                editor.WriteMessage("\r\n\t=>Polyline has been selected with " + enumerable.Length + " vertices's.\r\n");

                // Make up our list
                var simplePoints = enumerable.Select(p => new Simplifynet.Point(p.X, p.Y, p.Z)).ToArray();
                editor.WriteMessage(Calculating);
                // Simplify polyline
                var utility = new SimplifyUtility3D();

                var r = utility.Simplify(simplePoints, SettingsDefault.SimplifyPolyline_Tolerance, SettingsDefault.SimplifyPolyline_UseHighPrecision);
                if (r == null || r.Count <= 2)
                    throw new InvalidOperationException("We could not calculate sufficient points.");

                database.Create3dPolyline(r.Select(x => new Point3d(x.X, x.Y, x.Z)));

                // Report
                editor.WriteMessage("\r\n\t=>Polyline has been created with " + r.Count + " vertices's.");*/
            }
            catch (System.Exception exception) { HandleUnexpectedException(exception); }
            finally
            {
                // clear selection
                editor.SetImpliedSelection(Array.Empty<ObjectId>());
            }
        }

        [CommandMethod("IAMTopo_OpenGeoramses", CommandFlags.Modal)]
        public static void IAMTopo_OpenGeoramses()
        {
            try
            {
                var currentDocument = Autodesk.AutoCAD.ApplicationServices.
                    Core.Application.DocumentManager.MdiActiveDocument;
                var editor = currentDocument.Editor;
                var promptResult = editor.GetPoint(new Autodesk.AutoCAD.EditorInput.PromptPointOptions("Select a location."));
                if (promptResult.Status != PromptStatus.OK)
                    return;

                var selectedPoint = promptResult.Value;
                Process.Start(string.Format(
                    @"http://georamses/GeoRamses/Default.aspx?x={0}&y={1}&scale=1000",
                    Math.Floor(selectedPoint.X),
                    Math.Floor(selectedPoint.Y)));
            }
            catch (System.Exception exception) { HandleUnexpectedException(exception); }
        }

        [CommandMethod("IAMTope_OffsetPolyLine")]
        public static void IAMTope_OffsetPolyLine()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            var ed = document.Editor;
            var db = document.Database;
            // CivilDocument cdoc = CivilApplication.ActiveDocument;

            //select ROW polyline prompt
            var promptEntityOptions = new PromptEntityOptions("\nSelect ROW polyline:");
            promptEntityOptions.SetRejectMessage("\nSelected entity is not a curve");
            promptEntityOptions.AddAllowedClass(typeof(Curve), false);
            promptEntityOptions.AllowNone = true;
            var per = ed.GetEntity(promptEntityOptions);
            if (per.Status == PromptStatus.Cancel) return;

            while (per.Status == PromptStatus.OK)
            {
                using (DocumentLock dl = document.LockDocument())
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        //prompt to specify offset distance
                        var promptDistanceOptions = new PromptDistanceOptions("\nSpecify the offset distance:")
                        {
                            AllowNegative = false,
                            AllowZero = false,
                            DefaultValue = 1,
                            UseDefaultValue = true
                        };
                        var promptDoubleOptionsResult = ed.GetDistance(promptDistanceOptions);
                        var distance = promptDoubleOptionsResult.Value;
                        if (promptDoubleOptionsResult.Status == PromptStatus.Cancel) return;

                        //prompt to select side
                        var sidePoint = ed.GetPoint("\nSelect side to offset to:");
                        if (sidePoint.Status == PromptStatus.Cancel) return;

                        var polyline = (Polyline)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                        var closestPointTo = polyline.GetClosestPointTo(sidePoint.Value, false);

                        var offsetCurvesCollection1 = polyline.GetOffsetCurves(distance);
                        var offsetCurvesCollection2 = polyline.GetOffsetCurves(-distance);
                        Curve curve1 = null;
                        Curve curve2 = null;
                        var p1 = new Point3d();
                        var p2 = new Point3d();
                        var distance1 = 0d;
                        var distance2 = 0d;
                        for (var i = 0; i < offsetCurvesCollection1.Count; i++)
                        {
                            curve1 = (Curve)offsetCurvesCollection1[i];
                            curve2 = (Curve)offsetCurvesCollection2[i];
                            p1 = curve1.GetClosestPointTo(sidePoint.Value, false);
                            p2 = curve2.GetClosestPointTo(sidePoint.Value, false);
                            distance1 = p1.DistanceTo(sidePoint.Value);
                            distance2 = p2.DistanceTo(sidePoint.Value);

                            if (distance1 < distance2)
                            {
                                var id = db.Create2dPolyline(curve1);
                            }
                            else
                            {
                                var id = db.Create2dPolyline(curve2);
                            }
                        }
                        tr.Commit();
                    }
                }
                per = ed.GetEntity(promptEntityOptions);
            }
        }

        /// <summary>
        /// This command weeds two polylines so that the resulting polylines
        /// have fewer vertexes, but all vertexes are perpendiculary projected
        /// from one polyline to the other, also a minimum distance is used to
        /// make sure a point exists per minimum distance value (aka:
        /// "WeedPolyline_MinDistance" in settings).
        /// </summary>
        //    [CommandMethod("IAMTopo_WeedPolyline", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        //    public static void IAMTopo_WeedPolyline()
        //    {
        //        Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\r\nThis function has not yet been properly implemented." +
        //" It lacks definition and a well described purpose.");

        //var document = Application.DocumentManager.MdiActiveDocument;

        //var database = document.Database;

        //var editor = document.Editor;
        //try
        //{
        //    // Let user select a right rail and a left rail
        //    editor.SetImpliedSelection(Array.Empty<ObjectId>()); // clear selection

        // var plId1 = editor.Select3dPolyline(Select3dPolyLine); var plId2
        // = editor.Select3dPolyline(Select3dPolyLine);
        // editor.WriteMessage(Calculating); var points =
        // database.GetPointsFrom2PolylinesWithPointWeeding(plId1, plId2, SettingsDefault.WeedPolyline_MinDistance);

        // if (points.Item1 is null) { editor.WriteMessage(FunctionCanceled);
        // return; } if (points.Item2 is null) {
        // editor.WriteMessage(FunctionCanceled); return; }

        // // create a 3D polyline database.Create3dPolyline(points.Item1,
        // SettingsDefault.WeedPolyline_LayerName, SettingsDefault.WeedPolyline_LayerColor);

        //    database.Create3dPolyline(points.Item2, SettingsDefault.WeedPolyline_LayerName, SettingsDefault.WeedPolyline_LayerColor);
        //}
        //catch (System.Exception exception) { HandleUnexpectedException(exception); }
        //finally
        //{
        //    // clear selection
        //    editor.SetImpliedSelection(Array.Empty<ObjectId>());
        //}
        //}

        #endregion

        #region Private Methods

        private static void HandleUnexpectedException(System.Exception exception)
        {
            var currentDocument = Autodesk.AutoCAD.ApplicationServices.
                Core.Application.DocumentManager.MdiActiveDocument;
            string msg = "";
#if DEBUG
            msg = exception.Message + ".\r\n" + exception.Source + ".\r\n" + exception.StackTrace + ".\r\n" + exception.TargetSite + ".\r\n";
#else
            msg = exception.Message;
#endif
            currentDocument?.Editor?.WriteMessage(msg);

            System.Diagnostics.Trace.TraceError(exception.Message);
        }

        private static string WriteResultToFile(IEnumerable<CalculateDisplacementSectionResult> correctedResult, IList<MeasuredSectionResult> sections)
        {
            var result = new StringBuilder(Environment.NewLine);
            if (SettingsDefault.RailsToRailwayCenterLine_WriteResultToCSVFile)
            {
                var csv = ReadWrite.Instance;
                csv.FilePath = SettingsDefault.RailsToRailwayCenterLine_PathToCSVFile;
                // create csv from result
                csv.WriteMeasuredSections(sections);
                result.AppendLine($"Rails to Railway Center-Line, result: CSV-file has been written to: {csv.FilePath}");
            }

            if (!SettingsDefault.RailsToRailwayCenterLine_Use_CalculateSurveyCorrection) return result.ToString();
            var csv2 = ReadWrite.Instance;
            csv2.FilePath = SettingsDefault.CalculateSurveyCorrection_PathToCsvFile;
            // create csv from result
            csv2.WriteCalculateDisplacementResult(correctedResult);
            result.AppendLine($"\nCalculate survey correction, result: CSV-file has been written to: {csv2.FilePath}");
            return result.ToString();
        }

        #endregion
    }
}