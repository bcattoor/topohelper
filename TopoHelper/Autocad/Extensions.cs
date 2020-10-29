using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Windows.Documents;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

namespace TopoHelper.AutoCAD
{
    internal static class Extensions
    {
        #region Public Methods

        public static ObjectId GetLayerIdFromEntityObjectId(this Database database, ObjectId id)
        {
            using (var transAction = database.TransactionManager.StartOpenCloseTransaction())
            {
                object objectX = transAction.GetObject(id, OpenMode.ForRead);

                if (objectX is Entity entity)

                    return entity.LayerId;

                throw new Exception("\r\n\t=> Selected object is not supported for this function.");
            }
        }

        public static string GetLayerNameFromEntityObjectId(this Database database, ObjectId id)
        {
            using (var transAction = database.TransactionManager.StartOpenCloseTransaction())
            {
                object objectX = transAction.GetObject(id, OpenMode.ForRead);

                if (objectX is Entity entity)

                    return entity.Layer;

                throw new Exception("\r\n\t=> Selected object is not supported for this function.");
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="editor">    Current editor </param>
        /// <param name="layerName"> Layer to filter by </param>
        /// <param name="spaceName"> Space to filter by: Paper or Model </param>
        /// <param name="dxfName">   Dxf type to filter by: INSERT or POINT </param>
        /// <returns> Object id's of filtered result. </returns>
        public static ObjectIdCollection GetEntityIdsOnLayer(this Editor editor, string layerName, string spaceName, string dxfName)
        {
            // Build a filter list so that only entities on the specified layer
            // are selected

            var psr = editor.SelectAll(new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, dxfName),
                new TypedValue((int)DxfCode.LayerName, layerName),
                new TypedValue((int)DxfCode.LayoutName, spaceName)
            }));
            return psr.Status == PromptStatus.OK ?
                new ObjectIdCollection(psr.Value.GetObjectIds())
                : new ObjectIdCollection();
        }

        public static IEnumerable<Point3d> GetPointsFromPolyline(this Database database, ObjectId id)
        {
            using (var transAction = database.TransactionManager.StartOpenCloseTransaction())
            {
                object objectX = transAction.GetObject(id, OpenMode.ForRead);
                switch (objectX.GetType().ToString())
                {
                    case "Autodesk.AutoCAD.DatabaseServices.Polyline3d":
                        {
                            //Get a list of 3D points in parameter order
                            using (var pl1 = (Polyline3d)objectX)
                            {
                                // We get curve here, its faster to get the
                                // parameters and point on a non database object
                                var curve = pl1.GetGeCurve(new Tolerance(1e-10, 1e-10));
                                var polylineEndParam = (int)pl1.EndParam;

                                // Initialize list with dimensions specified (speed)
                                var arrRes = new Point3d[polylineEndParam + 1];

                                for (var i = 0; i <= polylineEndParam; i++)
                                {
                                    var distOnPl = pl1.GetDistanceAtParameter(i);
                                    arrRes[i] = curve.EvaluatePoint(curve.GetParameterAtLength(0, distOnPl, true, 1e-10));
                                }
                                transAction.Commit();
                                return arrRes;
                            }
                        }
                    default: throw new Exception("\r\n\t=> Selected object is not supported for this function.");
                }
            }
        }

        public static List<Point3d> GetPoints(this Database database, ObjectIdCollection ids)
        {
            var result = new List<Point3d>();

            using (var transAction = database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    object objectX = transAction.GetObject(id, OpenMode.ForRead);
                    switch (objectX.GetType().ToString())
                    {
                        case "Autodesk.AutoCAD.DatabaseServices.BlockReference":
                            {
                                //Get a list of 3D points in parameter order
                                using (var insert = (BlockReference)objectX)
                                {
                                    result.Add(insert.Position);
                                }
                                break;
                            }
                        case "Autodesk.AutoCAD.DatabaseServices.DBPoint":
                            {
                                //Get a list of 3D points in parameter order
                                using (var insert = (DBPoint)objectX)
                                {
                                    result.Add(insert.Position);
                                }
                                break;
                            }
                        default: throw new Exception("\r\n\t=> Selected object is not supported for this function.");
                    }
                }
            }

            return result;
        }

        public static IEnumerable<Point3d> GetPointsFromPolylineWithPointWeeding(this Database database, ObjectId id, double weeding)
        {
            // Weeding, we only add the point when weeding factor has been satisfied.

            using (var transAction = database.TransactionManager.StartOpenCloseTransaction())
            {
                object objectX = transAction.GetObject(id, OpenMode.ForRead);
                switch (objectX.GetType().ToString())
                {
                    case "Autodesk.AutoCAD.DatabaseServices.Polyline3d":
                        {
                            //Get a list of 3D points in parameter order
                            using (var pl1 = (Polyline3d)objectX)
                            {
                                // We get curve here, its faster to get the
                                // parameters and point on a non database object
                                var curve = pl1.GetGeCurve(new Tolerance(1e-10, 1e-10));

                                // Initialize list with dimensions specified (speed)
                                var bounds = (int)Math.Ceiling(pl1.Length / weeding) + 1;
                                var arrRes = new Point3d[bounds];
                                var arrCounter = 0;

                                for (var i = 0.0; i < pl1.Length; i += weeding)
                                {
                                    var x = curve.EvaluatePoint(curve.GetParameterAtLength(0, i, true, 1e-10));
                                    arrRes[arrCounter++] = x;
                                }
                                arrRes[arrCounter] = pl1.EndPoint;
                                transAction.Commit();
                                return arrRes;
                            }
                        }
                    default: throw new Exception("\r\n\t=> Selected object is not supported for this function.");
                }
            }
        }

        public static Tuple<IEnumerable<Point3d>, IEnumerable<Point3d>> GetPointsFrom2PolylinesWithPointWeeding(this Database database, ObjectId id1, ObjectId id2, double weeding)
        {
            // Weeding, we only add the point when weeding factor has been satisfied.
            throw new NotImplementedException("This function has not yet been properly implemented." +
                " It lacks definition and a well described purpose.");

            using (var transAction = database.TransactionManager.StartOpenCloseTransaction())
            {
                object object1 = transAction.GetObject(id1, OpenMode.ForRead);
                object object2 = transAction.GetObject(id2, OpenMode.ForRead);

                if (object1.GetType().ToString() != "Autodesk.AutoCAD.DatabaseServices.Polyline3d" ||
                    object2.GetType().ToString() != "Autodesk.AutoCAD.DatabaseServices.Polyline3d")
                    throw new Exception("\r\n\t=> Selected object is not supported for this function.");

                //Get a list of 3D points in parameter order
                using (var pl1 = (Polyline3d)object1)
                using (var pl2 = (Polyline3d)object2)
                {
                    // We get curve here, its faster to get the parameters and
                    // point on a non database object
                    var curve1 = pl1.GetGeCurve(new Tolerance(1e-10, 1e-10));
                    var curve2 = pl2.GetGeCurve(new Tolerance(1e-10, 1e-10));

                    // Initialize list with dimensions specified (speed)
                    var bounds = (int)Math.Ceiling(pl1.Length / weeding) + 1;
                    var arrRes1 = new Point3d[bounds];
                    var arrRes2 = new Point3d[bounds];
                    var arrCounter = 0;

                    // For each point on PL1 we find the closest point on PL2
                    // and create a tuple of the result.
                    for (var i = 0.0; i < pl1.Length; i += weeding)
                    {
                        var p1 = curve1.EvaluatePoint(curve1.GetParameterAtLength(0, i, true, 1e-10));
                        var p2 = curve2.GetClosestPointTo(p1, new Tolerance(1e-10, 1e-10)).Point;
                        arrRes1[arrCounter] = p1;
                        arrRes2[arrCounter++] = p2;
                    }
                    arrRes1[arrCounter] = pl1.EndPoint;
                    arrRes2[arrCounter] = curve2.GetClosestPointTo(pl1.EndPoint, new Tolerance(1e-10, 1e-10)).Point;
                    transAction.Commit();
                    return new Tuple<IEnumerable<Point3d>, IEnumerable<Point3d>>(arrRes1, arrRes2);
                }
            }
        }

        public static Point2d T2d(this Point3d p)
        {
            return new Point2d(p.X, p.Y);
        }

        public static Point3d T3d(this Point2d p, double elevation)
        {
            return new Point3d(p.X, p.Y, elevation);
        }

        #endregion
    }
}