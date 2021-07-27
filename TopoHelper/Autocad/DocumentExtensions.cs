using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopoHelper.Autocad
{
    public static class DocumentExtensions
    {
        #region Public Methods

        public static Point3d? Get3dPoint(this Editor editor)
        {
            var promptResult = editor.GetPoint(new Autodesk.AutoCAD.EditorInput.PromptPointOptions("Please select a point: "));
            if (promptResult.Status != PromptStatus.OK)
                return null;

            return promptResult.Value;
        }

        public static IEnumerable<Point3d> Select3dPoints(this Editor editor)
        {
            var myPLlist = new List<Point3d>();
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
                        return null;

                    foreach (var id in selectedObjects.GetObjectIds())
                    {
                        var objectX = transAction.GetObject(id, OpenMode.ForRead);
                        if (!(objectX is DBPoint)) continue;

                        var dBPoint = objectX as DBPoint;
                        myPLlist.Add(dBPoint.Position);
                    }
                }
                if (myPLlist.Any())
                    editor.WriteMessage("\r\n\t=>A set has been selected with " + myPLlist.Count + " points.");
                return myPLlist;
            }
        }

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
            return ObjectId.Null;
        }

        public static IEnumerable<Point3d> Select3dPolyline(this Editor editor, string message, out ObjectId id)
        {
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

                IEnumerable<Point3d> arrRes;
                if (res.Status == PromptStatus.OK)
                {
                    arrRes = editor.Document.Database.GetPointsFromPolyline(res.ObjectId);
                }
                else
                {
                    id = ObjectId.Null;
                    return null;
                }

                Debug.Assert(arrRes != null, nameof(arrRes) + " != null");

                var point3ds = arrRes as Point3d[] ?? arrRes.ToArray();
                if (point3ds.Any())
                    editor.WriteMessage("\r\n\t=>Polyline3d has been selected with " + point3ds.Length + " vertices.");

                id = res.ObjectId;
                return point3ds;
            }
        }

        #endregion
    }
}