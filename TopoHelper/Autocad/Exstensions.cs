﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PostSharp.Patterns.Contracts;
using System;
using System.Collections.Generic;

namespace TopoHelper.Autocad
{
    internal static class Exstensions
    {
        #region Public Methods

        public static IEnumerable<Point3d> GetPointsFromPolyline([NotNull]this Database database, ObjectId id)
        {
            using (OpenCloseTransaction transAction = database.TransactionManager.StartOpenCloseTransaction())
            {
                object objectX = transAction.GetObject(id, OpenMode.ForRead);
                switch (objectX.GetType().ToString())
                {
                    case "Autodesk.AutoCAD.DatabaseServices.Polyline3d":
                        {
                            //Get a list of 3D points in parameter order
                            using (Polyline3d pl1 = (Polyline3d)objectX)
                            {
                                // We get curve here, its faster to get the
                                // parameters and point on a non database object
                                var curve = pl1.GetGeCurve(new Tolerance(1e-10, 1e-10));
                                var polylineEndParam = (int)pl1.EndParam;

                                // Initialize list with dimensions specified (speed)
                                Point3d[] arrRes = new Point3d[polylineEndParam + 1];

                                for (int i = 0; i <= polylineEndParam; i++)
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

        public static IEnumerable<Point3d> GetPointsFromPolylineWithPointWeeding([NotNull] this Database database, ObjectId id, [GreaterThan(.0)] double weeding)
        {
            // Weeding, we only add the point when weeding factor has been satisfied.

            using (OpenCloseTransaction transAction = database.TransactionManager.StartOpenCloseTransaction())
            {
                object objectX = transAction.GetObject(id, OpenMode.ForRead);
                switch (objectX.GetType().ToString())
                {
                    case "Autodesk.AutoCAD.DatabaseServices.Polyline3d":
                        {
                            //Get a list of 3D points in parameter order
                            using (Polyline3d pl1 = (Polyline3d)objectX)
                            {
                                // We get curve here, its faster to get the
                                // parameters and point on a non database object
                                var curve = pl1.GetGeCurve(new Tolerance(1e-10, 1e-10));

                                // Initialize list with dimensions specified (speed)
                                var bounds = (int)Math.Ceiling(pl1.Length / weeding) + 1;
                                Point3d[] arrRes = new Point3d[bounds];
                                var arrCounter = 0;

                                for (double i = 0.0; i < pl1.Length; i += weeding)
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

        public static Tuple<IEnumerable<Point3d>, IEnumerable<Point3d>> GetPointsFrom2PolylinesWithPointWeeding([NotNull] this Database database, ObjectId id1, ObjectId id2, [GreaterThan(.0)] double weeding)
        {
            // Weeding, we only add the point when weeding factor has been satisfied.

            using (OpenCloseTransaction transAction = database.TransactionManager.StartOpenCloseTransaction())
            {
                object object1 = transAction.GetObject(id1, OpenMode.ForRead);
                object object2 = transAction.GetObject(id2, OpenMode.ForRead);

                if (object1.GetType().ToString() != "Autodesk.AutoCAD.DatabaseServices.Polyline3d" ||
                    object2.GetType().ToString() != "Autodesk.AutoCAD.DatabaseServices.Polyline3d")
                    throw new Exception("\r\n\t=> Selected object is not supported for this function.");

                //Get a list of 3D points in parameter order
                using (Polyline3d pl1 = (Polyline3d)object1)
                using (Polyline3d pl2 = (Polyline3d)object2)
                {
                    // We get curve here, its faster to get the parameters and
                    // point on a non database object
                    var curve1 = pl1.GetGeCurve(new Tolerance(1e-10, 1e-10));
                    var curve2 = pl2.GetGeCurve(new Tolerance(1e-10, 1e-10));

                    // Initialize list with dimensions specified (speed)
                    var bounds = (int)Math.Ceiling(pl1.Length / weeding) + 1;
                    Point3d[] arrRes1 = new Point3d[bounds];
                    Point3d[] arrRes2 = new Point3d[bounds];
                    var arrCounter = 0;

                    // For each point on PL1 we find the closest point on PL2
                    // and create a tuple of the result.
                    for (double i = 0.0; i < pl1.Length; i += weeding)
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