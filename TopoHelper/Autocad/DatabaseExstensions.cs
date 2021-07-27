using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopoHelper.Autocad
{
    public static class DatabaseExstensions
    {
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
    }
}