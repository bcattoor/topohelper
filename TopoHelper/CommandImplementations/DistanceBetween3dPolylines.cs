using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Threading.Tasks;
using TopoHelper.AutoCAD;
using TopoHelper.Model.Results;

namespace TopoHelper.CommandImplementations
{
    public static class DistanceBetween3dPolylines
    {
        #region Private Fields

        private static readonly object ArrayLock = 0;
        private static readonly object CurveLock = 0;

        #endregion

        #region Public Methods

        public static IEnumerable<DistanceBetween2PolylinesSectionResult> CalculateDistanceBetween3dPolylines(Database database, ObjectId pl1Id, ObjectId pl2Id)
        {
            Curve3d /*curve1,*/ curve2;
            Point3d[] parameterListPoints;
            double[] parameterListLength;
            using (var transAction = database.TransactionManager.StartTransaction())
            {
                object objectX1 = transAction.GetObject(pl1Id, OpenMode.ForRead);
                object objectX2 = transAction.GetObject(pl2Id, OpenMode.ForRead);

                //Get a list of 3D points in parameter order
                int polyline1EndParam;
                using (var pl1 = (Polyline3d)objectX1)
                using (var pl2 = (Polyline3d)objectX2)
                {
                    // We get curve here, its faster to get the parameters and
                    // point on a non database object curve1 =
                    // pl1.GetGeCurve(new Tolerance(1e-10, 1e-10));
                    curve2 = pl2.GetGeCurve(new Tolerance(1e-10, 1e-10));
                    polyline1EndParam = (int)pl1.EndParam;
                    parameterListPoints = new Point3d[polyline1EndParam + 1];
                    parameterListLength = new double[polyline1EndParam + 1];
                    for (var y = 0; y <= polyline1EndParam; y++)
                    {
                        parameterListPoints[y] = pl1.GetPointAtParameter(y);
                        parameterListLength[y] = pl1.GetDistanceAtParameter(y);
                    }
                }

                // Initialize list with dimensions specified (speed)
                var arrRes = new DistanceBetween2PolylinesSectionResult[polyline1EndParam + 1];

                //for (int i = 0; i <= polyline1EndParam; i++)
                Parallel.For(0, polyline1EndParam + 1, i =>
                {
                    Point3d pointOnCurve1, pointOnCurve2;
                    lock (CurveLock)
                    {
                        pointOnCurve1 = parameterListPoints[i];
                        pointOnCurve2 = curve2.GetClosestPointTo(pointOnCurve1).Point;
                    }

                    // Add result to array
                    lock (ArrayLock)
                        arrRes[i] = new DistanceBetween2PolylinesSectionResult(
                                parameterListLength[i],
                                pointOnCurve1.Z - pointOnCurve2.Z,
                                pointOnCurve1.T2d().GetDistanceTo(pointOnCurve2.T2d()),
                                pointOnCurve1.DistanceTo(pointOnCurve2),
                                pointOnCurve1, pointOnCurve2);
                });
                transAction.Commit();
                return arrRes;
            }
        }

        #endregion
    }
}