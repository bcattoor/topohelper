using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace TopoHelper.AutoCAD
{
    internal static class DatabaseCreateEntityExtensions
    {
        #region Private Fields

        // ReSharper disable once UnusedMember.Local
        private static readonly Plane MyPlaneWcs = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));

        #endregion

        #region Public Methods

        public static ObjectId Create2dPolyline(this Database database, IEnumerable<Point3d> points, string layerName = null,
            short layerColor = 256, short entityCollor = 256, double elevation = .0, double startwidth = .0, double endwidth = .0)
        {
            using (var transAction = database.TransactionManager.StartOpenCloseTransaction())
            {
                // Get the layer table from the drawing
                using (var blockTable = (BlockTable)transAction.GetObject(database.BlockTableId, OpenMode.ForRead))
                {
                    var blockTableRecord = (BlockTableRecord)transAction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    //create the layer
                    using (var pl2d = new Polyline())

                    {
                        pl2d.SetDatabaseDefaults();
                        var point3ds = points as Point3d[] ?? points.ToArray();
                        for (var i = 0; i < point3ds.Count(); i++)
                        {
                            pl2d.AddVertexAt(i, point3ds.ElementAt(i).T2d(), .0, startwidth, endwidth);
                        }
                        pl2d.Elevation = elevation;
                        pl2d.ColorIndex = entityCollor;
                        pl2d.LinetypeScale = .5;
                        pl2d.LineWeight = LineWeight.ByLayer;

                        if (layerName != null)
                        {
                            database.CreateLayer(layerName, layerColor, "");
                            pl2d.Layer = layerName;
                        }

                        pl2d.ColorIndex = entityCollor;
                        var id = blockTableRecord.AppendEntity(pl2d);
                        transAction.AddNewlyCreatedDBObject(pl2d, true);
                        transAction.Commit();
                        return id;
                    }
                }
            }
        }

        public static ObjectId Create3dPolyline(this Database database, IEnumerable<Point3d> points, string layer = null, int layerColor = 256, short entityCollor = 256)
        {
            using (var transAction = database.TransactionManager.StartOpenCloseTransaction())
            {
                // Get the layer table from the drawing
                using (var blocktable = (BlockTable)transAction.GetObject(database.BlockTableId, OpenMode.ForRead))
                {
                    var blockTableRecord = (BlockTableRecord)transAction.GetObject(blocktable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    ObjectId id;
                    //create the layer
                    using (var pl3d = new Polyline3d(Poly3dType.SimplePoly, new Point3dCollection(points.ToArray()), false))

                    {
                        pl3d.SetDatabaseDefaults();

                        if (layer != null)
                        {
                            database.CreateLayer(layer, (short)layerColor, "");
                            pl3d.Layer = layer;
                        }

                        pl3d.ColorIndex = entityCollor;
                        id = blockTableRecord.AppendEntity(pl3d);
                        transAction.AddNewlyCreatedDBObject(pl3d, true);
                        transAction.Commit();
                        return id;
                    }
                }
            }
        }

        public static ObjectId CreateLayer(this Database database, string layName, short colorIndex, string description, bool layerIsLocked = false, bool isPlottable = true, bool isHidden = false, LineWeight lineWeight = LineWeight.ByLineWeightDefault)
        {
            using (var transAction = database.TransactionManager.StartOpenCloseTransaction())
            {
                // Get the layer table from the drawing
                using (var layerTable = (LayerTable)transAction.GetObject(database.LayerTableId, OpenMode.ForRead))
                {
                    //make sure name is valid according to the SymbolUtilityService
                    SymbolUtilityServices.ValidateSymbolName(layName, false);

                    if (layerTable.Has(layName))
                    {
                        //we return the object-id of the layer, we have the layer already... (so you could say it has been created)
                        return layerTable[layName];
                    }

                    ObjectId id;
                    //create the layer
                    using (var newLayerTableRecord = new LayerTableRecord())

                    {
                        // Add the new layer to the layer table
                        layerTable.UpgradeOpen();

                        id = layerTable.Add(newLayerTableRecord);

                        transAction.AddNewlyCreatedDBObject(newLayerTableRecord, true);
                        // ... and set its properties
                        newLayerTableRecord.Name = layName;
                        newLayerTableRecord.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);
                        newLayerTableRecord.Description = description;
                        newLayerTableRecord.IsLocked = layerIsLocked;
                        newLayerTableRecord.IsHidden = isHidden;
                        newLayerTableRecord.IsPlottable = isPlottable;
                        newLayerTableRecord.LineWeight = lineWeight;
                        transAction.Commit();
                    }
                    return id;
                }
            }
        }

        public static ObjectId CreatePoint(this Database database, Point3d p, string layerName = null, short layerColor = 256, short entityCollor = 256)
        {
            using (var transAction = database.TransactionManager.StartOpenCloseTransaction())
            {
                // Get the layer table from the drawing
                using (var blocktable = (BlockTable)transAction.GetObject(database.BlockTableId, OpenMode.ForRead))
                {
                    var blockTableRecord = (BlockTableRecord)transAction.GetObject(blocktable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    ObjectId id;
                    //create the entity
                    using (var dbPoint = new DBPoint(p))

                    {
                        dbPoint.SetDatabaseDefaults();

                        if (layerName != null)
                        {
                            database.CreateLayer(layerName, layerColor, "");
                            dbPoint.Layer = layerName;
                        }

                        dbPoint.ColorIndex = entityCollor;
                        id = blockTableRecord.AppendEntity(dbPoint);
                        transAction.AddNewlyCreatedDBObject(dbPoint, true);
                        transAction.Commit();
                        return id;
                    }
                }
            }
        }

        public static IEnumerable<ObjectId> CreatePoints(this Database database, IEnumerable<Point3d> points, string layerName = null, short layerColor = 256, short entityCollor = 256)
        {
            if (points is null)
                throw new ArgumentNullException(nameof(points));

            using (var transAction = database.TransactionManager.StartOpenCloseTransaction())
            {
                // Get the layer table from the drawing
                using (var blockTable = (BlockTable)transAction.GetObject(database.BlockTableId, OpenMode.ForRead))
                {
                    var blockTableRecord = (BlockTableRecord)transAction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    var point3ds = points as Point3d[] ?? points.ToArray();
                    var count = point3ds.Count();
                    var ids = new ObjectId[count];
                    //create the layer
                    for (var i = 0; i < count; i++)
                    {
                        using (var dbPoint = new DBPoint(point3ds.ElementAt(i)))

                        {
                            dbPoint.SetDatabaseDefaults();

                            if (layerName != null)
                            {
                                database.CreateLayer(layerName, layerColor, "");
                                dbPoint.Layer = layerName;
                            }

                            dbPoint.ColorIndex = entityCollor;
                            ids[i] = blockTableRecord.AppendEntity(dbPoint);
                            transAction.AddNewlyCreatedDBObject(dbPoint, true);
                        }
                    }
                    transAction.Commit();
                    return ids;
                }
            }
        }

        #endregion
    }
}