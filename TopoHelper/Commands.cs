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

        private const string DataValuationErrorOccurred = "\r\n\t=> A data validation error has occurred: ";
        private const string FunctionCanceled = "\r\n\t=> Function has been canceled.";
        private const string LeftRail = "\r\nPlease select the polyline3d you would like to use for the left rail.";
        private const string RightRail = "\r\nPlease select the polyline3d you would like to use for the right rail.";
        private const string Select3dPolyLine = "\r\nSelect a 3d-polyline: ";
        private const string SelectionNotUnique = "\r\nYou selected the same polyline twice, why?";
        private const string Calculating = "\r\nCalculating, please wait ...";
        private static readonly Settings SettingsDefault = Settings.Default;

        #endregion

        #region Public Properties

        public static SettingsUserControl MySettingsView { get; private set; }
        public static SettingsViewModel MySettingsViewModel { get; private set; }
        public static PaletteSet PaletteSet { get; private set; }

        #endregion

        #region Public Methods

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
                csv.FilePath = SettingsDefault.IO_FILE_DBPL_CSV;
                csv.Delimiter = SettingsDefault.IO_FILE_DBPL_CSV_DELIMITER;
                // create csv from result
                csv.WriteDistanceBetween2PolylinesResult(distanceBetween2PolylinesSectionResults);

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
                var minimumDistance = SettingsDefault.DIST_MIN_PTP; // drawing units
                var maxDistance = SettingsDefault.DIST_MIN_PTP; // drawing units

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
                if (SettingsDefault.CALCULATE_CSD)
                    correctedResult = SurveyCorrecting.CalculateDisplacement(point3dsLeft, point3dsRight);

                #endregion

                #region Calculate Centerline

                // Calculate railway-alignment-center-line

                Debug.Assert(correctedResult != null, nameof(correctedResult) + " != null");

                var calculateDisplacementSectionResults = correctedResult as CalculateDisplacementSectionResult[] ?? correctedResult.ToArray();

                var sections = SettingsDefault.CALCULATE_CSD ?
                    Rails2RailwayCenterLine.CalculateRailwayCenterLine(
                        calculateDisplacementSectionResults.Select(s => s.LeftRailPoint).ToList(),
                        calculateDisplacementSectionResults.Select(s => s.RightRailPoint).ToList())
                    : Rails2RailwayCenterLine.CalculateRailwayCenterLine(point3dsLeft, point3dsRight);

                #endregion

                #region Draw Result To AutoCAD

                var trackAxis3DPoints = sections.Select(x => x.TrackAxisPoint).ToArray();

                // DRAW CENTERLINE POLYLINES AND POINTS
                if (SettingsDefault.DRAW_2D_R2R_CL_PL)
                    // create a 2-dimensional polyline for track-center-line 2D
                    database.Create2dPolyline(
                        points: trackAxis3DPoints,
                        layerName: SettingsDefault.LAY_NAME_PREFIX_2D + SettingsDefault.LAY_NAME_R2R_CL,
                        layerColor: SettingsDefault.LAY_COL_R2R_CL_PL);

                if (SettingsDefault.DRAW_3D_R2R_CL_PL)
                    // create a 3-dimensional polyline for track-center-line 3D
                    database.Create3dPolyline(trackAxis3DPoints,
                    SettingsDefault.LAY_NAME_PREFIX_3D + SettingsDefault.LAY_NAME_R2R_CL,
                    SettingsDefault.LAY_COL_R2R_CL_PL);

                if (SettingsDefault.DRAW_3D_R2R_CL_PNTS)
                    // create 3-dimensional points
                    database.CreatePoints(
                        points: trackAxis3DPoints,
                        layerName: SettingsDefault.LAY_NAME_PREFIX_3D + SettingsDefault.LAY_NAME_R2R_CL_PNTS,
                        layerColor: SettingsDefault.LAY_COL_R2D_CL_PNTS);

                if (SettingsDefault.DRAW_2D_R2R_CL_PNTS)
                    // create 2-dimensional points
                    database.CreatePoints(
                        points: trackAxis3DPoints.Select(p => p.T2d().T3d(0)).ToArray(),
                        layerName: SettingsDefault.LAY_NAME_PREFIX_2D + SettingsDefault.LAY_NAME_R2R_CL_PNTS);

                if (SettingsDefault.CALCULATE_CSD)
                {
                    // DRAW CSD - RAILS
                    if (SettingsDefault.DRAW_3D_CSD_RAILS_PL)
                    {
                        // RIGHT --> create a 3-dimensional polyline for correctedResult
                        database.Create3dPolyline(calculateDisplacementSectionResults.Select(s => s.LeftRailPoint),
                        SettingsDefault.LAY_NAME_PREFIX_3D + SettingsDefault.LAY_NAME_CSD_RAILS_PL,
                        SettingsDefault.LAY_COL_CSD_RAILS_PL);

                        // LEFT --> create a 3-dimensional polyline for correctedResult
                        database.Create3dPolyline(calculateDisplacementSectionResults.Select(s => s.RightRailPoint),
                        SettingsDefault.LAY_NAME_PREFIX_3D + SettingsDefault.LAY_NAME_CSD_RAILS_PL,
                        SettingsDefault.LAY_COL_CSD_RAILS_PL);
                    }

                    if (SettingsDefault.DRAW_3D_CSD_RAILS_PNTS)
                    { // create 3-dimensional points
                        database.CreatePoints(
                            points: calculateDisplacementSectionResults.Select(s => s.LeftRailPoint),
                            layerName: SettingsDefault.LAY_NAME_PREFIX_3D + SettingsDefault.LAY_NAME_CSD_RAILS_PNTS,
                            layerColor: SettingsDefault.LAY_COL_CSD_RAILS_PNTS);

                        database.CreatePoints(
                             points: calculateDisplacementSectionResults.Select(s => s.RightRailPoint),
                             layerName: SettingsDefault.LAY_NAME_PREFIX_3D + SettingsDefault.LAY_NAME_CSD_RAILS_PNTS,
                             layerColor: SettingsDefault.LAY_COL_CSD_RAILS_PNTS);
                    }
                }

                #endregion

                #region Write Result

                WriteResultToFile(calculateDisplacementSectionResults, sections);

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
        /// have fewer vertexes, but all vertexes are perpendicular
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
                database.Create3dPolyline(points.Item1);

                database.Create3dPolyline(points.Item2);
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
        public static void JoinPolylines()
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
                        if (SettingsDefault.DELETE_JPL_ENTITIES) polylineToAdd.Erase();

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

        #endregion

        #region Private Methods

        private static void WriteResultToFile(IEnumerable<CalculateDisplacementSectionResult> correctedResult, IList<MeasuredSectionResult> sections)
        {
            if (SettingsDefault.LOG_R2R_CSV)
            {
                var csv = ReadWrite.Instance;
                csv.FilePath = SettingsDefault.IO_FILE_R2R_CSV;
                csv.Delimiter = SettingsDefault.IO_FILE_R2R_CSV_DELIMITER;
                // create csv from result
                csv.WriteMeasuredSections(sections);
            }

            if (!SettingsDefault.CALCULATE_CSD) return;
            var csv2 = ReadWrite.Instance;
            csv2.FilePath = SettingsDefault.IO_FILE_CSD_CSV;
            csv2.Delimiter = SettingsDefault.IO_FILE_CSD_CSV_DELIMITER;
            // create csv from result
            csv2.WriteCalculateDisplacementResult(correctedResult);
        }

        #endregion
    }
}