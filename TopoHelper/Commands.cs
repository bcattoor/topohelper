using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using TopoHelper;
using TopoHelper.Autocad;
using TopoHelper.CommandImplementations;
using TopoHelper.Model;
using TopoHelper.Model.Calculations;
using TopoHelper.Model.Results;
using TopoHelper.Properties;
using TopoHelper.UserControls;
using TopoHelper.UserControls.ViewModels;

[assembly: CommandClass(typeof(Commands))]

namespace TopoHelper
{
    public static partial class Commands
    {
        #region Private Fields

        private const string MessageDataVallidationErrorOccurred = "\r\n\t=> A data validation error has occurred: ";
        private const string MessageFunctionCanceled = "\r\n\t=> Function has been canceled.";
        private const string MessageLeftRail = "\r\nPlease select the polyline3d you would like to use for the left rail.";
        private const string MessageRightRail = "\r\nPlease select the polyline3d you would like to use for the right rail.";
        private const string MessageSelect3dPolyLine = "\r\nSelect a 3d-polyline: ";
        private const string MessageSelectionNotUnique = "\r\nYou selected the same polyline twice, why?";
        private const string MessageCalculating = "\r\nCalculating, please wait ...";
        private static readonly Settings settings_default = Properties.Settings.Default;

        #endregion

        #region Public Properties

        public static SettingsUserControl MySettingsView { get; private set; }
        public static SettingsViewModel MySettingsViewModel { get; private set; }
        public static PaletteSet PaletteSet { get; private set; }

        #endregion

        #region Public Methods

        [CommandMethod("IAMTopo_DistanceBetween2Polylines", CommandFlags.DocReadLock | CommandFlags.NoUndoMarker)]
        public static void IAMTopo_DistanceBetween2Polylines()
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;

            var database = document.Database;

            var editor = document.Editor;
            try
            {
                #region Selection

                // Let user select a right rail end a left rail
                editor.SetImpliedSelection(Array.Empty<ObjectId>()); // clear selection

                var pl1Id = editor.Select3dPolyline("\r\nSelect first polyline.");

                editor.SetImpliedSelection(new ObjectId[] { pl1Id });
                var pl2Id = editor.Select3dPolyline("\r\nSelect second polyline.");

                editor.SetImpliedSelection(new ObjectId[] { pl1Id, pl2Id });

                // Make sure we did not select the same polyline twice
                if (pl1Id.Equals(pl2Id))
                {
                    editor.WriteMessage(MessageSelectionNotUnique);
                    editor.WriteMessage(MessageFunctionCanceled);
                    return;
                }

                #endregion

                #region Calculation

                editor.WriteMessage(MessageCalculating);
                var distanceResult = DistanceBetween3dPolylines.CalculateDistanceBetween3dPolylines(database, pl1Id, pl2Id);
                if (!distanceResult.Any())
                {
                    throw new InvalidOperationException("We failed to calculate any result.");
                }

                #endregion

                #region Write Result

                var csv = new Csv.ReadWrite
                {
                    FilePath = settings_default.IO_FILE_DBPL_CSV,
                    Delimiter = settings_default.IO_FILE_DBPL_CSV_DELIMITER
                };
                // create csv from result
                csv.WriteDistanceBetween2PolylinesResult(distanceResult);

                #endregion
            }
            catch (System.Exception Exception)
            {
                editor.WriteMessage("\r\n" + Exception.Message);
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
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;

            var database = document.Database;

            var editor = document.Editor;
            try
            {
                // lets set some basic values
                var minimumDistance = settings_default.DIST_MIN_PTP; // drawing units
                var maxDistance = settings_default.DIST_MIN_PTP; // drawing units

                if (!DataValidation.ValidatePointsToPolylineSettings(out var msg))
                {
                    editor.WriteMessage(msg);
                    return;
                }

                // Let user select a set of points
                editor.SetImpliedSelection(Array.Empty<ObjectId>()); // clear selection
                var pnts = editor.Select3dPoints(out List<ObjectId> ptsIds);
                if (pnts is null)
                {
                    editor.WriteMessage(MessageFunctionCanceled);
                    return;
                }
                editor.WriteMessage(MessageCalculating);
                var result = ClosestPointsList.Calculate(pnts.Select(a => new Model.Geometry.Point(a)).ToList(), new Model.Geometry.Point(pnts.First()), minimumDistance, maxDistance);

                // create a 3-dimensional polyline for track-centerline
                database.Create3dPolyline(result.Select(x => x.ToPoint3d()).ToArray());
            }
            catch (System.Exception Exception)
            {
                editor.WriteMessage("\r\n" + Exception.Message);
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
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;

            var database = document.Database;

            var editor = document.Editor;
            try
            {
                #region Selection

                // Let user select a right rail end a left rail
                editor.SetImpliedSelection(Array.Empty<ObjectId>()); // clear selection

                var leftRailId1 = editor.Select3dPolyline(MessageLeftRail);

                editor.SetImpliedSelection(new ObjectId[] { leftRailId1 });
                var rightRailPoints = editor.Select3dPolyline(MessageRightRail, out ObjectId rightRailId);

                if (rightRailPoints is null)
                {
                    editor.WriteMessage(MessageFunctionCanceled);
                    return;
                }
                editor.SetImpliedSelection(new ObjectId[] { leftRailId1, rightRailId });
                // Make sure we did not select the same polyline twice
                if (leftRailId1.Equals(rightRailId))
                {
                    editor.WriteMessage(MessageSelectionNotUnique);
                    editor.WriteMessage(MessageFunctionCanceled);
                    return;
                }

                var leftRailPoints = database.GetPointsFromPolyline(leftRailId1);
                if (leftRailPoints is null)
                {
                    editor.WriteMessage(MessageFunctionCanceled);
                    return;
                }

                #endregion

                #region Validation

                // Validate input before trying to calculate railway-axis.
                if (!DataValidation.ValidateInput(leftRailPoints, rightRailPoints, out string error))
                {
                    editor.WriteMessage(MessageDataVallidationErrorOccurred + error);
                    return;
                }

                #endregion

                editor.WriteMessage(MessageCalculating);

                #region CSD Correcting Survey Displacement

                IEnumerable<CalculateDisplacementSectionResult> correctedResult = null;
                // take away the survey errors of the points
                if (settings_default.CALCULATE_CSD)
                    correctedResult = Model.Calculations.SurveyCorrecting.CalculateDisplacement(leftRailPoints, rightRailPoints);

                #endregion

                #region Calculate Centerline

                // Calculate railway-alignment-center-line
                IList<MeasuredSectionResult> sections = null;

                if (settings_default.CALCULATE_CSD)
                    sections = Rails2RailwayCenterLine.CalculateRailwayCenterLine(correctedResult.Select(s => s.LeftRailPoint), correctedResult.Select(s => s.RightRailPoint));
                else
                    sections = Rails2RailwayCenterLine.CalculateRailwayCenterLine(leftRailPoints, rightRailPoints);

                #endregion

                #region Draw Result To AutoCAD

                var trackAxis3DPoints = sections.Select(x => x.TrackAxisPoint).ToArray();

                // DRAW CENTERLINE POLYLINES AND POINTS
                if (settings_default.DRAW_2D_R2R_CL_PL)
                    // create a 2-dimensional polyline for track-centerline 2D
                    database.Create2dPolyline(
                        points: trackAxis3DPoints,
                        layerName: settings_default.LAY_NAME_PREFIX_2D + settings_default.LAY_NAME_R2R_CL,
                        layerColor: settings_default.LAY_COL_R2R_CL_PL);

                if (settings_default.DRAW_3D_R2R_CL_PL)
                    // create a 3-dimensional polyline for track-centerline 3D
                    database.Create3dPolyline(trackAxis3DPoints,
                    settings_default.LAY_NAME_PREFIX_3D + settings_default.LAY_NAME_R2R_CL,
                    settings_default.LAY_COL_R2R_CL_PL);

                if (settings_default.DRAW_3D_R2R_CL_PNTS)
                    // create 3-dimensional points
                    database.CreatePoints(
                        points: trackAxis3DPoints,
                        layerName: settings_default.LAY_NAME_PREFIX_3D + settings_default.LAY_NAME_R2R_CL_PNTS,
                        layerColor: settings_default.LAY_COL_R2D_CL_PNTS);

                if (settings_default.DRAW_2D_R2R_CL_PNTS)
                    // create 2-dimensional points
                    database.CreatePoints(
                        points: trackAxis3DPoints.Select(p => p.T2d().T3d(0)).ToArray(),
                        layerName: settings_default.LAY_NAME_PREFIX_2D + settings_default.LAY_NAME_R2R_CL_PNTS);

                if (settings_default.CALCULATE_CSD)
                {
                    // DRAW CSD - RAILS
                    if (settings_default.DRAW_3D_CSD_RAILS_PL)
                    {
                        // RIGHT --> create a 3-dimensional polyline for correctedResult
                        database.Create3dPolyline(correctedResult.Select(s => s.LeftRailPoint),
                        settings_default.LAY_NAME_PREFIX_3D + settings_default.LAY_NAME_CSD_RAILS_PL,
                        settings_default.LAY_COL_CSD_RAILS_PL);

                        // LEFT --> create a 3-dimensional polyline for correctedResult
                        database.Create3dPolyline(correctedResult.Select(s => s.RightRailPoint),
                        settings_default.LAY_NAME_PREFIX_3D + settings_default.LAY_NAME_CSD_RAILS_PL,
                        settings_default.LAY_COL_CSD_RAILS_PL);
                    }

                    if (settings_default.DRAW_3D_CSD_RAILS_PNTS)
                    { // create 3-dimensional points
                        database.CreatePoints(
                            points: correctedResult.Select(s => s.LeftRailPoint),
                            layerName: settings_default.LAY_NAME_PREFIX_3D + settings_default.LAY_NAME_CSD_RAILS_PNTS,
                            layerColor: settings_default.LAY_COL_CSD_RAILS_PNTS);

                        database.CreatePoints(
                             points: correctedResult.Select(s => s.RightRailPoint),
                             layerName: settings_default.LAY_NAME_PREFIX_3D + settings_default.LAY_NAME_CSD_RAILS_PNTS,
                             layerColor: settings_default.LAY_COL_CSD_RAILS_PNTS);
                    }
                }

                #endregion

                #region Write Result

                WriteResultToFile(correctedResult, sections);

                #endregion
            }
            catch (System.Exception Exception)
            {
                editor.WriteMessage("\r\n" + Exception.Message);
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
        [CommandMethod("IAMTopo_Settings", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        public static void IAMTopo_Settings()
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;

            var database = document.Database;

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
                        OnCancel = new Action(() => { PaletteSet.Visible = false; })
                    };
                    MySettingsView = new SettingsUserControl
                    {
                        DataContext = MySettingsViewModel
                    };

                    PaletteSet.MinimumSize = new System.Drawing.Size(240, 225);
                    PaletteSet.Size = new System.Drawing.Size(800, 400);
                    PaletteSet.Name = "Topohelper settings menu";
                    PaletteSet.AddVisual("Topohelper settings menu", MySettingsView, true);
                    PaletteSet.Style = PaletteSetStyles.Snappable |
                                       PaletteSetStyles.UsePaletteNameAsTitleForSingle |
                                       PaletteSetStyles.ShowAutoHideButton |
                                       PaletteSetStyles.ShowPropertiesMenu;
                }

                // now display the palette set if not yet visible, else hide
                if (PaletteSet.Visible == true)
                { PaletteSet.Visible = false; }
                else
                {
                    PaletteSet.Visible = true;
                }
            }
            catch (System.Exception Exception)
            {
                editor.WriteMessage("\r\n" + Exception.Message);
            }
            finally
            {
            }
        }

        [CommandMethod("IAMTopo_SimplefyPolyline", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        public static void IAMTopo_SimplefyPolyline()
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;

            var database = document.Database;
            var editor = document.Editor;
            try
            {
                //Clear selection
                editor.SetImpliedSelection(new ObjectId[0]);

                //Making a new result and adding options to it
                var selectedObjectId = editor.Select3dPolyline(MessageSelect3dPolyLine);
                editor.SetImpliedSelection(new ObjectId[] { selectedObjectId });

                var points = database.GetPointsFromPolyline(selectedObjectId);
                if (points is null && points?.Count() < 1)
                {
                    editor.WriteMessage(MessageFunctionCanceled);
                    return;
                }
                else
                    editor.WriteMessage("\r\n\t=>Polyline has been selected with " + points.Count() + " vertices's.\r\n");

                // Make up our list
                var simplePoints = points.Select(p => new Simplifynet.Point(p.X, p.Y, p.Z)).ToArray();
                editor.WriteMessage(MessageCalculating);
                // Simplify polyline
                var utility = new Simplifynet.SimplifyUtility3D();

                var r = utility.Simplify(simplePoints, settings_default.SYMPPL_TOLERANCE, settings_default.SYMPPL_HIGH_PRICISION);
                if (r == null || r.Count() <= 2)
                    throw new System.InvalidOperationException("We could not calculate sufficient points.");

                _ = database.Create3dPolyline(r.Select(x => new Point3d(x.X, x.Y, x.Z)));

                // Report
                editor.WriteMessage("\r\n\t=>Polyline has been created with " + r.Count() + " vertices's.");
            }
            catch (System.Exception Exception)
            {
                editor.WriteMessage("\r\n" + Exception.Message);
            }
            finally
            {
                // clear selection
                editor.SetImpliedSelection(Array.Empty<ObjectId>());
            }
        }

        [CommandMethod("IAMTopo_WeedPolyline", CommandFlags.DocExclusiveLock | CommandFlags.NoMultiple)]
        public static void IAMTopo_WeedPolyline()
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;

            var database = document.Database;

            var editor = document.Editor;
            try
            {
                // Let user select a right rail end a left rail
                editor.SetImpliedSelection(Array.Empty<ObjectId>()); // clear selection

                var plId1 = editor.Select3dPolyline(MessageSelect3dPolyLine);
                var plId2 = editor.Select3dPolyline(MessageSelect3dPolyLine);
                editor.WriteMessage(MessageCalculating);
                var points = database.GetPointsFrom2PolylinesWithPointWeeding(plId1, plId2, settings_default.PLW_WEEDING_DISTANCE);

                if (points.Item1 is null)
                {
                    editor.WriteMessage(MessageFunctionCanceled);
                    return;
                }
                if (points.Item2 is null)
                {
                    editor.WriteMessage(MessageFunctionCanceled);
                    return;
                }

                // create a 3-dimensional polyline for track-centerline 3D
                database.Create3dPolyline(points.Item1);

                database.Create3dPolyline(points.Item2);
            }
            catch (System.Exception Exception)
            {
                editor.WriteMessage("\r\n" + Exception.Message);
            }
            finally
            {
                // clear selection
                editor.SetImpliedSelection(Array.Empty<ObjectId>());
            }
        }

        [CommandMethod("IAMTopo_JoinPline")]
        static public void JoinPolylines()
        {
            var document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            var ed = document.Editor;
            var db = document.Database;
            var peo1 = new PromptEntityOptions("\nSelect source polyline : ");
            peo1.SetRejectMessage("\nInvalid selection...");
            peo1.AddAllowedClass(typeof(Polyline), true);
            peo1.AddAllowedClass(typeof(Polyline2d), true);
            peo1.AddAllowedClass(typeof(Polyline3d), true);

            // TODO: When the first selected entity is a line, we need to convert it to a polyline before we can continue.
            // TODO: When two 2D-polylines are selected, we need to check if the are coplanar, if not we need to convert both objects to a 3d-polyline
            // TODO: When 2D polyline is converted to a 3D polyline we need to make sure arc's in that polyline are segmented!
            // TODO: When two 2D-polylines are selected, we need to check if the are coplanar, if not we can equal elevation to source and join them as 2D polyline
            peo1.AddAllowedClass(typeof(Line), true);

            var pEntrs = ed.GetEntity(peo1);
            if (PromptStatus.OK != pEntrs.Status)
                return;
            var srcId = pEntrs.ObjectId;
            var peo2 = new PromptEntityOptions("\nSelect polyline to join : ");
            peo2.SetRejectMessage("\nInvalid selection...");
            peo2.AddAllowedClass(typeof(Line), true);
            peo2.AddAllowedClass(typeof(Polyline), true);
            peo2.AddAllowedClass(typeof(Polyline), true);
            peo2.AddAllowedClass(typeof(Polyline2d), true);
            peo2.AddAllowedClass(typeof(Polyline3d), true);
            pEntrs = ed.GetEntity(peo2);
            if (PromptStatus.OK != pEntrs.Status)
                return;
            var joinId = pEntrs.ObjectId;
            try
            {
                using (Transaction transaction = db.TransactionManager.StartOpenCloseTransaction())
                {
                    using (var sourcePolyline = transaction.GetObject(srcId, OpenMode.ForWrite) as Curve)
                    using (var polylineToAdd = transaction.GetObject(joinId, OpenMode.ForWrite) as Curve)
                    {
                        var startPointCurve1 = sourcePolyline.StartPoint;
                        var startPointCurve2 = polylineToAdd.StartPoint;
                        var endPointCurve1 = sourcePolyline.EndPoint;
                        var endPointCurve2 = polylineToAdd.EndPoint;

                        List<Tuple<string /*action */, double /* value */ >> distance = new List<Tuple<string, double>>
                        {
                            new Tuple<string, double>("sp1:sp2", startPointCurve1.DistanceTo(startPointCurve2)),
                            new Tuple<string, double>("sp1:ep2", startPointCurve1.DistanceTo(endPointCurve2)),
                            new Tuple<string, double>("ep1:sp2", endPointCurve1.DistanceTo(startPointCurve2)),
                            new Tuple<string, double>("ep1:ep2", endPointCurve1.DistanceTo(endPointCurve2))
                        };

                        var result = distance.OrderBy(i => i.Item2).FirstOrDefault();

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
                            default:
                                break;
                        }

                        sourcePolyline.JoinEntity(polylineToAdd);

                        // If user wants to delete source entities
                        if (settings_default.DELETE_JPL_ENTITIES) polylineToAdd.Erase();

                        transaction.Commit();
                        ed.WriteMessage($"\n\rBoth lines were joined together.\n\r\t Distance measured between start-point and end-point is {result.Item2.ToString("F6")}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage(ex.Message);
            }
        }

        #endregion

        #region Private Methods

        private static void WriteResultToFile(IEnumerable<CalculateDisplacementSectionResult> correctedResult, IList<MeasuredSectionResult> sections)
        {
            if (settings_default.LOG_R2R_CSV)
            {
                var csv = new Csv.ReadWrite
                {
                    FilePath = settings_default.IO_FILE_R2R_CSV,
                    Delimiter = settings_default.IO_FILE_R2R_CSV_DELIMITER
                };
                // create csv from result
                csv.WriteMeasuredSections(sections);
            }

            if (settings_default.CALCULATE_CSD)
            {
                var csv2 = new Csv.ReadWrite
                {
                    FilePath = settings_default.IO_FILE_CSD_CSV,
                    Delimiter = settings_default.IO_FILE_CSD_CSV_DELIMITER
                };
                // create csv from result
                csv2.WriteCalculateDisplacementResult(correctedResult);
            }
        }

        #endregion
    }
}