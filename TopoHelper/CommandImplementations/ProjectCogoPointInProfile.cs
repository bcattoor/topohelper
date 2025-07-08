using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.DatabaseServices.Styles;
using System;
using System.Collections.Generic;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Infrabel.AutodeskPlatform.TopoHelper.CommandImplementations
{
    internal static class ProjectCogoPointInProfile
    {
        private const string FunctionCanceled = "\r\n\t=> Functie is geannuleerd.";

        /// <summary>
        /// Voert het commando uit om CogoPoints naar een ProfileView te projecteren
        /// </summary>
        public static void ExecuteCommand()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            var database = document.Database;
            var editor = document.Editor;
            var civilDocument = CivilApplication.ActiveDocument;

            try
            {
                // Stap 1: Selecteer een ProfileView
                var profileViewId = SelectProfileView(editor);
                if (profileViewId == ObjectId.Null)
                {
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }

                // Stap 2: Selecteer CogoPoints
                var cogoPointIds = SelectCogoPoints(editor);
                if (cogoPointIds == null || cogoPointIds.Count == 0)
                {
                    editor.WriteMessage(FunctionCanceled);
                    return;
                }

                // Stap 3: Configuratie voor projectie
                var config = new ProfileProjectionConfig
                {
                    ElevationSource = ElevationSourceType.UseObjectElevation
                };

                // Stap 4: Projecteer de punten
                using (var transaction = database.TransactionManager.StartTransaction())
                {
                    var profileView = transaction.GetObject(profileViewId, OpenMode.ForRead) as ProfileView;
                    if (profileView == null)
                    {
                        editor.WriteMessage("\r\nKon de ProfileView niet openen.");
                        return;
                    }

                    // Haal de alignment op die bij de ProfileView hoort
                    var alignmentId = profileView.AlignmentId;
                    var alignment = transaction.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                    if (alignment == null)
                    {
                        editor.WriteMessage("\r\nKon de Alignment niet openen.");
                        return;
                    }

                    // Haal de beschikbare projectie stijlen op
                    var projectionStyleId = GetDefaultProjectionStyleId(civilDocument);
                    var labelStyleId = GetDefaultLabelStyleId(civilDocument);

                    // Projecteer elk punt
                    int successCount = 0;
                    foreach (ObjectId cogoPointId in cogoPointIds)
                    {
                        try
                        {
                            var cogoPoint = transaction.GetObject(cogoPointId, OpenMode.ForRead) as CogoPoint;
                            if (cogoPoint == null) continue;

                            // Controleer of het punt binnen het bereik van de ProfileView valt
                            var point3d = new Point3d(cogoPoint.Easting, cogoPoint.Northing, cogoPoint.Elevation);
                            var station = alignment.GetStationAtPoint(point3d);
                            
                            if (station < profileView.StationStart || station > profileView.StationEnd)
                            {
                                editor.WriteMessage($"\r\nPunt {cogoPoint.PointNumber} valt buiten het bereik van de ProfileView.");
                                continue;
                            }

                            // Maak de projectie aan
                            var projection = ProfileProjection.Create(
                                profileViewId,
                                cogoPointId,
                                projectionStyleId,
                                labelStyleId);

                            if (projection != null)
                            {
                                projection.ElevationSource = config.ElevationSource;
                                successCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            editor.WriteMessage($"\r\nFout bij projecteren van punt: {ex.Message}");
                        }
                    }

                    transaction.Commit();
                    editor.WriteMessage($"\r\n{successCount} van {cogoPointIds.Count} punten succesvol geprojecteerd naar de ProfileView.");
                }
            }
            catch (Exception ex)
            {
                editor.WriteMessage($"\r\nFout: {ex.Message}");
            }
        }

        /// <summary>
        /// Selecteert een ProfileView uit de tekening
        /// </summary>
        private static ObjectId SelectProfileView(Editor editor)
        {
            var options = new PromptEntityOptions("\r\nSelecteer een ProfileView: ");
            options.SetRejectMessage("\r\nDit is geen ProfileView.");
            options.AddAllowedClass(typeof(ProfileView), false);

            var result = editor.GetEntity(options);
            return result.Status == PromptStatus.OK ? result.ObjectId : ObjectId.Null;
        }

        /// <summary>
        /// Selecteert CogoPoints uit de tekening
        /// </summary>
        private static ObjectIdCollection SelectCogoPoints(Editor editor)
        {
            var options = new PromptSelectionOptions();
            options.MessageForAdding = "\r\nSelecteer CogoPoints om te projecteren: ";
            options.AllowSubSelections = false;
            options.SingleOnly = false;

            var filter = new SelectionFilter(new[] {
                new TypedValue((int)DxfCode.Start, "AECC_COGO_POINT")
            });

            var result = editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
                return new ObjectIdCollection();

            var objectIdCollection = new ObjectIdCollection();
            foreach (var objectId in result.Value.GetObjectIds())
            {
                objectIdCollection.Add(objectId);
            }
            
            return objectIdCollection;
        }

        /// <summary>
        /// Haalt de standaard projectie stijl op
        /// </summary>
        private static ObjectId GetDefaultProjectionStyleId(CivilDocument civilDocument)
        {
            try
            {
                var styleCollection = civilDocument.Styles.ProfileViewStyles.ProjectionStyles;
                if (styleCollection.Count > 0)
                {
                    // Gebruik de eerste beschikbare stijl
                    return styleCollection[0];
                }
            }
            catch { /* Negeer fouten en gebruik de standaard stijl */ }
            
            return ObjectId.Null; // Gebruik de standaard stijl
        }

        /// <summary>
        /// Haalt de standaard label stijl op
        /// </summary>
        private static ObjectId GetDefaultLabelStyleId(CivilDocument civilDocument)
        {
            try
            {
                var styleCollection = civilDocument.Styles.LabelStyles.ProfileViewLabelStyles.ProjectionLabelStyles;
                if (styleCollection.Count > 0)
                {
                    // Gebruik de eerste beschikbare stijl
                    return styleCollection[0];
                }
            }
            catch { /* Negeer fouten en gebruik de standaard stijl */ }
            
            return ObjectId.Null; // Gebruik de standaard stijl
        }
    }

    /// <summary>
    /// Configuratie voor het projecteren van punten naar een ProfileView
    /// </summary>
    internal class ProfileProjectionConfig
    {
        public ObjectId ProjectionStyleId { get; set; } = ObjectId.Null;
        public ObjectId LabelStyleId { get; set; } = ObjectId.Null;
        public ElevationSourceType ElevationSource { get; set; } = ElevationSourceType.UseObjectElevation;
        public ObjectId SurfaceId { get; set; } = ObjectId.Null; // Voor surface elevation
        public double ManualElevationOffset { get; set; } = 0.0;
    }
}
