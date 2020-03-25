using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using PostSharp.Patterns.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TopoHelper.Autocad
{
    internal static class UserInteraction
    {
        public static ObjectId Select3dPolyline([NotNull] this Editor editor, [NotEmpty] string message)
        {
            //Setting some options
            PromptEntityOptions mypromptOption = new PromptEntityOptions(message)
            {
                AllowNone = false,
                AllowObjectOnLockedLayer = true
            };
            mypromptOption.SetRejectMessage("\r\n\t=>Only objects of type <Polyline 3D> are allowed.");
            mypromptOption.AddAllowedClass(typeof(Polyline3d), false);

            //Making a new result and adding options to it
            PromptEntityResult res = editor.GetEntity(mypromptOption);

            if (res.Status == PromptStatus.OK)
            {
                return res.ObjectId;
            }
            throw new Exception("\r\n\t=> Selected object is not supported for this function.");
        }

        public static IEnumerable<Point3d> Select3dPolyline([NotNull] this Editor editor, [NotEmpty]string message, out ObjectId id)
        {
            IEnumerable<Point3d> arrRes = null;

            //' Start a transaction
            using (OpenCloseTransaction transAction = editor.Document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                //Setting some options
                PromptEntityOptions mypromptOption = new PromptEntityOptions(message)
                {
                    AllowNone = false,
                    AllowObjectOnLockedLayer = true
                };
                mypromptOption.SetRejectMessage("\r\n\t=>Only objects of type <Polyline 3D> are allowed.");
                mypromptOption.AddAllowedClass(typeof(Polyline3d), false);

                //Making a new result and adding options to it
                PromptEntityResult res = editor.GetEntity(mypromptOption);

                if (res.Status == PromptStatus.OK)
                {
                    arrRes = editor.Document.Database.GetPointsFromPolyline(res.ObjectId);
                }
                if (arrRes != null && arrRes.Any())
                    editor.WriteMessage("\r\n\t=>Polyline3d has been selected with " + arrRes.Count() + " vertices's.");

                id = res.ObjectId;
                return arrRes;
            }

            throw new InvalidOperationException("Something went wrong when handling the selection.");
        }

        public static IEnumerable<Point3d> Select3dPoints([NotNull] this Editor editor, out List<ObjectId> ids)
        {
            List<Point3d> myPLlist = new List<Point3d>();
            ids = new List<ObjectId>();
            //' Start a transaction
            using (OpenCloseTransaction transAction = editor.Document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                //Setting some options
                PromptSelectionOptions mypromptOption = new PromptSelectionOptions()
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
                        if (objectX is DBPoint)
                        {
                            DBPoint dBPoint = objectX as DBPoint;
                            myPLlist.Add(dBPoint.Position);
                            ids.Add(dBPoint.ObjectId);
                        }
                    }
                }
                if (myPLlist != null && myPLlist.Any())
                    editor.WriteMessage("\r\n\t=>A set has been selected with " + myPLlist.Count + " points.");
                return myPLlist;
            }

            throw new InvalidOperationException("Something went wrong when handling the selection.");
        }
    }
}