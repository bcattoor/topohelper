using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Simplifynet;
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
using Exception = System.Exception;
using Point = TopoHelper.Model.Geometry.Point;

[assembly: CommandClass(typeof(Commands))]

namespace TopoHelper
{
    // ReSharper disable once PartialTypeWithSinglePart (Commands classes need to be partial?)
    public static partial class Commands
    {
        #region Private Fields

        private const string Calculating = "\r\nCalculating, please wait ...";
        private const string DataValuationErrorOccurred = "\r\n\t=> A data validation error has occurred: ";
        private const string FunctionCanceled = "\r\n\t=> Function has been canceled.";
        private const string LeftRail = "\r\nPlease select the polyline3d you would like to use for the left rail.";
        private const string RightRail = "\r\nPlease select the polyline3d you would like to use for the right rail.";
        private const string Select3dPolyLine = "\r\nSelect a 3d-polyline: ";
        private const string SelectionNotUnique = "\r\nYou selected the same polyline twice, why?";
        public const double Tolerance = 0.000000001d;
        private static string Patern;

        // ReSharper disable once UnusedMember.Local
        private static readonly Plane MyPlaneWcs = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));

        private static readonly Settings SettingsDefault = Settings.Default;
        private static ObjectId IAMTopo_AlignAngleOfBlockLastSelectedPolylineId = ObjectId.Null;

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
                catch (Exception ex)
                {
                    ed.WriteMessage(ex.Message);
                }
                // reselect next block
                blockSelection = ed.GetEntity(peo2);
            }
        }

        /// <summary>
        /// This command is used by user to align an inserted blocks angle
        /// property to a selected polyline.
        /// </summary>
        [CommandMethod("IAMTopo_AlignAngleOfBlock", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        public static void IAMTopo_AlignAngleOfBlock()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            var ed = document.Editor;
            var db = document.Database;

            try
            {
                if (IAMTopo_AlignAngleOfBlockLastSelectedPolylineId == ObjectId.Null)
                {// Select the polyline
                    var peo1 = new PromptEntityOptions("\nSelect 3D-polyline to align to.");
                    peo1.SetRejectMessage("\nInvalid selection...");
                    peo1.AddAllowedClass(typeof(Polyline3d), true);
                    peo1.AllowObjectOnLockedLayer = true;
                    var promptResult = ed.GetEntity(peo1);
                    if (PromptStatus.OK != promptResult.Status)
                        return;

                    IAMTopo_AlignAngleOfBlockLastSelectedPolylineId = promptResult.ObjectId;
                }
                else
                {
                    ed.WriteMessage("Using last selected entity..., use the [IAMTopo_AlignAngleOfBlock_ResetEntity] to reset this.\r\n");
                }

                // Select the blockreference to set angle
                var peo2 = new PromptEntityOptions("\nSelect the BlockReference.");
                peo2.SetRejectMessage("\nInvalid selection...");
                peo2.AddAllowedClass(typeof(BlockReference), true);
                var promptResult2 = ed.GetEntity(peo2);
                if (PromptStatus.OK != promptResult2.Status)
                    return;

                using (Transaction transaction = db.TransactionManager.StartOpenCloseTransaction())
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
            catch (Exception ex)
            {
                ed.WriteMessage(ex.Message);
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

                // Select an entity that is on the points-layer
                var promptEntityResult = editor.GetEntity("Please select one of the points that are on the polyline to clean.");

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
                var pointToUseAsFilter = database.GetPoints(pointIdsOnLayer);

                // Making a new list of points, filtered by available points

                // Select what polyline to clean
                var selectedObjectId = editor.Select3dPolyline(Select3dPolyLine);
                editor.SetImpliedSelection(new[] { selectedObjectId });

                var pointsFromPolyline = database.GetPointsFromPolyline(selectedObjectId);
                var pointsFromPolylineEArray = pointsFromPolyline as Point3d[] ?? pointsFromPolyline.ToArray();

                var newPolylineListOfPoint = pointsFromPolylineEArray.ToList().FindAll(vertex =>
                {
                    var res = pointToUseAsFilter.Find(x => x.IsEqualTo(vertex,
                        new Tolerance(Settings.Default.__APP_EPSILON,
                            Settings.Default.__APP_EPSILON)));
                    return !res.Equals(Point3d.Origin);
                });

                editor.WriteMessage("\r\n\t=>Polyline has been selected with " + pointsFromPolylineEArray.Length + " vertices's.\r\n");

                database.Create3dPolyline(newPolylineListOfPoint);

                // Report
                editor.WriteMessage("\r\n\t=>Polyline has been created with " + newPolylineListOfPoint.Count + " vertices's.");
            }
            catch (Exception exception)
            {
                editor.WriteMessage("\r\n" + exception.Message);
            }
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

                editor.SetImpliedSelection(new[] { pl1Id });
                var pl2Id = editor.Select3dPolyline("\r\nSelect second polyline.");

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
                csv.Delimiter = SettingsDefault.DistanceBetween2Polylines_CSVFile_Delimiter;
                // create csv from result
                csv.WriteDistanceBetween2PolylinesResult(distanceBetween2PolylinesSectionResults);
                editor.WriteMessage($"Result was written to CSV file {csv.FilePath}");

                #endregion
            }
            catch (Exception exception)
            {
                editor.WriteMessage("\r\n" + exception.Message);
            }
            finally
            {
                // clear selection
                editor.SetImpliedSelection(Array.Empty<ObjectId>());
            }
        }

        [CommandMethod("IAMTopo_JoinPolyline")]
        public static void IAMTopo_JoinPolyline()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            var ed = document.Editor;
            var db = document.Database;
            var peo1 = new PromptEntityOptions("\nSelect source polyline (select a point close to gap!): ");
            peo1.SetRejectMessage("\nInvalid selection...");
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
            var peo2 = new PromptEntityOptions("\nSelect polyline to join (select a point close to gap!): ");
            peo2.SetRejectMessage("\nInvalid selection...");
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

                        Debug.Assert(result != null, nameof(result) + " != null");
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
                        ed.WriteMessage($"\n\rBoth lines were joined together.\n\r\t Distance measured between start-point and end-point is {result.Item2:F6}");
                    }
                }
            }
            catch (Exception ex)
            {
                ed.WriteMessage(ex.Message);
            }
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
                var maxDistance = SettingsDefault.PointsTo3DPolyline_MinimumPointDistance; // drawing units

                if (!DataValidation.ValidatePointsToPolylineSettings(out var msg))
                {
                    editor.WriteMessage(msg);
                    return;
                }

                // Let user select a set of points
                editor.SetImpliedSelection(Array.Empty<ObjectId>()); // clear selection
                var points = editor.Select3dPoints(out var _);
                if (points is null)
                {
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }
                editor.WriteMessage(Calculating);
                var point3ds = points as Point3d[] ?? points.ToArray();
                var result = ClosestPointsList.Calculate(point3ds.Select(a => new Point(a)).ToList(), new Point(point3ds.First()), minimumDistance, maxDistance);

                // create a 3-dimensional polyline for track-center-line
                database.Create3dPolyline(result.Select(x => x.ToPoint3d()).ToArray());
            }
            catch (Exception exception)
            {
                editor.WriteMessage("\r\n" + exception.Message);
            }
            finally
            {
                // clear selection
                editor.SetImpliedSelection(Array.Empty<ObjectId>());
            }
        }

        [CommandMethod("IAMTopo_Rails2RailwayCenterLine", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        public static void IAMTopo_Rails2RailwayCenterLine()
        {
            var document = Application.DocumentManager.MdiActiveDocument;

            var database = document.Database;

            var editor = document.Editor;
            try
            {
                #region Selection

                // Let user select a right rail end a left rail
                editor.SetImpliedSelection(Array.Empty<ObjectId>()); // clear selection

                var leftRailId1 = editor.Select3dPolyline(LeftRail);

                editor.SetImpliedSelection(new[] { leftRailId1 });
                var rightRailPoints = editor.Select3dPolyline(RightRail, out var rightRailId);

                if (rightRailPoints is null)
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
                if (SettingsDefault.Rails2RailwayCenterLine_Use_CalculateSurveyCorrection)
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
                if (SettingsDefault.Rails2RailwayCenterLine_Draw2DPolyline_CenterLine)
                    // create a 2-dimensional polyline for track-center-line 2D
                    database.Create2dPolyline(
                        points: trackAxis3DPoints,
                        layerName: SettingsDefault.LayerNamePrefix_2DObjects + SettingsDefault.Rails2RailwayCenterLine_LayerNameCenterline,
                        layerColor: SettingsDefault.Rails2RailwayCenterLine_LayerColorOfCenterline3DPolyLine);

                if (SettingsDefault.Rails2RailwayCenterLine_Draw3DPolyline_CenterLine)
                    // create a 3-dimensional polyline for track-center-line 3D
                    database.Create3dPolyline(trackAxis3DPoints,
                    SettingsDefault.LayerNamePrefix_3DObjects + SettingsDefault.Rails2RailwayCenterLine_LayerNameCenterline,
                    SettingsDefault.Rails2RailwayCenterLine_LayerColorOfCenterline3DPolyLine);

                if (SettingsDefault.Rails2RailwayCenterLine_DrawCenterline3DPoints)
                    // create 3-dimensional points
                    database.CreatePoints(
                        points: trackAxis3DPoints,
                        layerName: SettingsDefault.LayerNamePrefix_3DObjects + SettingsDefault.Rails2RailwayCenterLine_LayerNameCenterLine3DPoints,
                        layerColor: SettingsDefault.Rails2RailwayCenterLine_LayerColorCenterline3DPoints);

                if (SettingsDefault.Rails2RailwayCenterLine_DrawCenterline2DPoints)
                    // create 2-dimensional points
                    database.CreatePoints(
                        points: trackAxis3DPoints.Select(p => p.T2d().T3d(0)).ToArray(),
                        layerName: SettingsDefault.LayerNamePrefix_2DObjects + SettingsDefault.Rails2RailwayCenterLine_LayerNameCenterLine3DPoints);

                if (SettingsDefault.Rails2RailwayCenterLine_Use_CalculateSurveyCorrection)
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
            catch (Exception exception)
            {
                editor.WriteMessage("\r\n" + exception.Message);
            }
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
            catch (Exception exception)
            {
                editor.WriteMessage("\r\n" + exception.Message);
            }
        }

        [CommandMethod("IAMTopo_SimplifyPolyline", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        public static void IAMTopo_SimplifyPolyline()
        {
            var document = Application.DocumentManager.MdiActiveDocument;

            var database = document.Database;
            var editor = document.Editor;
            try
            {
                //Clear selection
                editor.SetImpliedSelection(new ObjectId[0]);

                //Making a new result and adding options to it
                var selectedObjectId = editor.Select3dPolyline(Select3dPolyLine);
                editor.SetImpliedSelection(new[] { selectedObjectId });

                var points = database.GetPointsFromPolyline(selectedObjectId);
                var enumerable = points as Point3d[] ?? points.ToArray();
                editor.WriteMessage("\r\n\t=>Polyline has been selected with " + enumerable.Length + " vertices's.\r\n");

                // Make up our list
                var simplePoints = enumerable.Select(p => new Simplifynet.Point(p.X, p.Y, p.Z)).ToArray();
                editor.WriteMessage(Calculating);
                // Simplify polyline
                var utility = new SimplifyUtility3D();

                var r = utility.Simplify(simplePoints, SettingsDefault.SYMPPL_TOLERANCE, SettingsDefault.SYMPPL_HIGH_PRICISION);
                if (r == null || r.Count <= 2)
                    throw new InvalidOperationException("We could not calculate sufficient points.");

                database.Create3dPolyline(r.Select(x => new Point3d(x.X, x.Y, x.Z)));

                // Report
                editor.WriteMessage("\r\n\t=>Polyline has been created with " + r.Count + " vertices's.");
            }
            catch (Exception exception)
            {
                editor.WriteMessage("\r\n" + exception.Message);
            }
            finally
            {
                // clear selection
                editor.SetImpliedSelection(Array.Empty<ObjectId>());
            }
        }

        /// <summary>
        /// This command weeds two polylines so that the resulting polylines
        /// have fewer vertexes, but all vertexes are perpendiculary projected
        /// from one polyline to the other, also a minimum distance is used to
        /// make sure a point exists per minimum distance value (aka:
        /// "WeedPolyline_MinDistance" in settings).
        /// </summary>
        [CommandMethod("IAMTopo_WeedPolyline", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        public static void IAMTopo_WeedPolyline()
        {
            var document = Application.DocumentManager.MdiActiveDocument;

            var database = document.Database;

            var editor = document.Editor;
            try
            {
                // Let user select a right rail end a left rail
                editor.SetImpliedSelection(Array.Empty<ObjectId>()); // clear selection

                var plId1 = editor.Select3dPolyline(Select3dPolyLine);
                var plId2 = editor.Select3dPolyline(Select3dPolyLine);
                editor.WriteMessage(Calculating);
                var points = database.GetPointsFrom2PolylinesWithPointWeeding(plId1, plId2, SettingsDefault.WeedPolyline_MinDistance);

                if (points.Item1 is null)
                {
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }
                if (points.Item2 is null)
                {
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }

                // create a 3D polyline
                database.Create3dPolyline(points.Item1, SettingsDefault.WeedPolyline_LayerName, SettingsDefault.WeedPolyline_LayerColor);

                database.Create3dPolyline(points.Item2, SettingsDefault.WeedPolyline_LayerName, SettingsDefault.WeedPolyline_LayerColor);
            }
            catch (Exception exception)
            {
                editor.WriteMessage("\r\n" + exception.Message);
            }
            finally
            {
                // clear selection
                editor.SetImpliedSelection(Array.Empty<ObjectId>());
            }
        }

        #endregion

        #region Private Methods

        private static string WriteResultToFile(IEnumerable<CalculateDisplacementSectionResult> correctedResult, IList<MeasuredSectionResult> sections)
        {
            var result = new StringBuilder(Environment.NewLine);
            if (SettingsDefault.Rails2RailwayCenterLine_WriteResultToCSVFile)
            {
                var csv = ReadWrite.Instance;
                csv.FilePath = SettingsDefault.Rails2RailwayCenterLine_PathToCSVFile;
                csv.Delimiter = SettingsDefault.Rails2RailwayCenterLine_CSVFile_Delimiter;
                // create csv from result
                csv.WriteMeasuredSections(sections);
                result.AppendLine($"Rails to Railway Center-Line, result: CSV-file has been written to: {csv.FilePath}");
            }

            if (!SettingsDefault.Rails2RailwayCenterLine_Use_CalculateSurveyCorrection) return result.ToString();
            var csv2 = ReadWrite.Instance;
            csv2.FilePath = SettingsDefault.CalculateSurveyCorrection_PathToCsvFile;
            csv2.Delimiter = SettingsDefault.CalculateSurveyCorrection_CSVFile_Delimiter;
            // create csv from result
            csv2.WriteCalculateDisplacementResult(correctedResult);
            result.AppendLine($"\nCalculate survey correction, result: CSV-file has been written to: {csv2.FilePath}");
            return result.ToString();
        }

        #endregion
    }
}