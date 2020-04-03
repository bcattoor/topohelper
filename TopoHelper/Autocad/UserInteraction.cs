using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TopoHelper.AutoCAD
{
    internal static class UserInteraction
    {
        public static ObjectId Select3dPolyline(this Editor editor, string message)
        {
            //Setting some options
            var mypromptOption = new PromptEntityOptions(message)
            {
                AllowNone = false,
                AllowObjectOnLockedLayer = true
            };
            mypromptOption.SetRejectMessage("\r\n\t=>Only objects of type <Polyline 3D> are allowed.");
            mypromptOption.AddAllowedClass(typeof(Polyline3d), false);

            //Making a new result and adding options to it
            var res = editor.GetEntity(mypromptOption);

            if (res.Status == PromptStatus.OK)
            {
                return res.ObjectId;
            }
            throw new Exception("\r\n\t=> Selected object is not supported for this function.");
        }

        public static IEnumerable<Point3d> Select3dPolyline(this Editor editor, string message, out ObjectId id)
        {
            IEnumerable<Point3d> arrRes = null;

            //' Start a transaction
            using (/*var transAction =*/ editor.Document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                //Setting some options
                var myPromptOption = new PromptEntityOptions(message)
                {
                    AllowNone = false,
                    AllowObjectOnLockedLayer = true
                };
                myPromptOption.SetRejectMessage("\r\n\t=>Only objects of type <Polyline 3D> are allowed.");
                myPromptOption.AddAllowedClass(typeof(Polyline3d), false);

                //Making a new result and adding options to it
                var res = editor.GetEntity(myPromptOption);

                if (res.Status == PromptStatus.OK)
                {
                    arrRes = editor.Document.Database.GetPointsFromPolyline(res.ObjectId);
                }

                Debug.Assert(arrRes != null, nameof(arrRes) + " != null");

                var point3ds = arrRes as Point3d[] ?? arrRes.ToArray();
                if (point3ds.Any())
                    editor.WriteMessage("\r\n\t=>Polyline3d has been selected with " + point3ds.Length + " vertices.");

                id = res.ObjectId;
                return point3ds;
            }
        }

        public static IEnumerable<Point3d> Select3dPoints(this Editor editor, out List<ObjectId> ids)
        {
            var myPLlist = new List<Point3d>();
            ids = new List<ObjectId>();
            //' Start a transaction
            using (var transAction = editor.Document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                //Setting some options
                var mypromptOption = new PromptSelectionOptions
                {
                    AllowDuplicates = false,
                    RejectObjectsFromNonCurrentSpace = true,
                    AllowSubSelections = false,
                    RejectObjectsOnLockedLayers = false,
                };

                //Making a new result and adding options to it
                var res = editor.GetSelection(mypromptOption);

                if (res.Status == PromptStatus.OK)
                {
                    var selectedObjects = res.Value;
                    if (selectedObjects.Count == 0)
                        throw new Exception("\r\n\t=> No objects were selected.");

                    foreach (var id in selectedObjects.GetObjectIds())
                    {
                        var objectX = transAction.GetObject(id, OpenMode.ForRead);
                        if (!(objectX is DBPoint)) continue;

                        var dBPoint = objectX as DBPoint;
                        myPLlist.Add(dBPoint.Position);
                        ids.Add(dBPoint.ObjectId);
                    }
                }
                if (myPLlist.Any())
                    editor.WriteMessage("\r\n\t=>A set has been selected with " + myPLlist.Count + " points.");
                return myPLlist;
            }
        }
    }
}