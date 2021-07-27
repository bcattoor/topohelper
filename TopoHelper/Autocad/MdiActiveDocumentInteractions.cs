using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopoHelper.Autocad
{
    public static class MdiActiveDocumentInteractions
    {
        /// <summary>
        /// This will set the CurrentDocument's UCS to world and store the
        /// previous UCS.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="previousUcsName" /> is <see langword="null" />
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// We could not create the UCS table record.
        /// </exception>
        /// <param name="returnToPreviousUcs">
        /// if this is true, we will return to the previously saved UCS, instead
        /// of going to wordUCS
        /// </param>
        /// <param name="previousUcsName">    
        /// the name of the UCS tablerecord, this is where the previeus UCS will
        /// be stored at.
        /// </param>
        public static void SetWcs(string previousUcsName, bool returnToPreviousUcs = false)
        {
            if (previousUcsName == null) throw new ArgumentNullException("previousUcsName");

            // Get the current document and database
            var acDoc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            var acCurDb = acDoc.Database;

            using (acDoc.LockDocument())
            {
                using (var acTrans = acCurDb.TransactionManager.StartOpenCloseTransaction())
                {
                    // Open the UCS table for read
                    UcsTable acUcsTbl;

                    using (acUcsTbl = acTrans.GetObject(acCurDb.UcsTableId, OpenMode.ForRead) as UcsTable)
                    {
                        UcsTableRecord acUcsTblRec;

                        // Check to see if the "New_UCS" UCS table record
                        // exists, we create it if it does not exist
                        if (acUcsTbl != null && acUcsTbl.Has(previousUcsName) == false)
                        {
                            using (acUcsTblRec = new UcsTableRecord())
                            {
                                acUcsTblRec.Name = previousUcsName;

                                // Open the UCSTable for write
                                acUcsTbl.UpgradeOpen();

                                // Add the new UCS table record
                                acUcsTbl.Add(acUcsTblRec);
                                acTrans.AddNewlyCreatedDBObject(acUcsTblRec, true);
                            }
                        }

                        if (acUcsTbl == null)
                            throw new InvalidOperationException("We could not create the UCS table record.");

                        using (acUcsTblRec = acTrans.GetObject(acUcsTbl[previousUcsName],
                            OpenMode.ForWrite) as UcsTableRecord)
                        {
                            var ucs = acDoc.Editor.CurrentUserCoordinateSystem;
                            if (!returnToPreviousUcs)
                            {
                                // Save the previous UCS
                                if (acUcsTblRec != null)
                                {
                                    acUcsTblRec.Origin = ucs.CoordinateSystem3d.Origin;
                                    acUcsTblRec.XAxis = ucs.CoordinateSystem3d.Xaxis;
                                    acUcsTblRec.YAxis = ucs.CoordinateSystem3d.Yaxis;
                                }

                                // Set the ucs to the world ucs
                                var newUcsMat = Matrix3d.AlignCoordinateSystem(new Point3d(0, 0, 0), new Vector3d(1, 0, 0), new Vector3d(0, 1, 0), new Vector3d(0, 0, 1), new Point3d(0, 0, 0), new Vector3d(1, 0, 0), new Vector3d(0, 1, 0), new Vector3d(0, 0, 1));
                                acDoc.Editor.CurrentUserCoordinateSystem = newUcsMat;
                            }
                            else
                            {
                                // resore previous saved UCS
                                if (acUcsTblRec != null)
                                {
                                    var cs = new CoordinateSystem3d(acUcsTblRec.Origin, acUcsTblRec.XAxis, acUcsTblRec.YAxis);
                                    var matrix = Matrix3d.AlignCoordinateSystem(ucs.CoordinateSystem3d.Origin, ucs.CoordinateSystem3d.Xaxis, ucs.CoordinateSystem3d.Yaxis, ucs.CoordinateSystem3d.Zaxis, cs.Origin, cs.Xaxis, cs.Yaxis, cs.Zaxis);
                                    acDoc.Editor.CurrentUserCoordinateSystem = matrix;
                                }
                            }
                        }
                    }

                    // Save the new objects to the database
                    acTrans.Commit();
                }
            }
        }
    }
}