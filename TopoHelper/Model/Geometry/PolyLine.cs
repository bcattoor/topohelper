using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TopoHelper.Model.Geometry
{
    /// <summary>
    /// My implementation of the polyline class, with some extra needed features.
    /// </summary>
    public class Topo_PolyLine3d /*No inheritance/sharing of the AutoCAD database object! We need to be able to dispose of original!*/
    {
        readonly Polyline3d baseReference = null;

        public Topo_PolyLine3d(Polyline3d baseReference)
        {
            this.baseReference = baseReference ?? throw new ArgumentNullException(nameof(baseReference));
        }

        /// <summary>
        /// Setting the position from this function opens a transaction with the current MDI active document. If u do not want this, use the function SetPositionOfVertex and use its overload parameter to set the document yourself.
        /// </summary>
        public  Point3d StartPoint { get => baseReference.StartPoint; set => SetPositionOfVertex((int)baseReference.StartParam, value); }

        public  Point3d EndPoint { get => baseReference.EndPoint; set => SetPositionOfVertex((int)baseReference.EndParam, value); }

        /// <summary>
        /// Get the PolylineVertex3d's object id by index. ReadOnly!
        /// </summary>
        /// <param name="index">Integer index, zero based.</param>
        /// <returns></returns>
        public ObjectId this[int index]
        {
            get => baseReference.Cast<ObjectId>().ElementAt(index);
        }

        public void SetPositionOfVertex(int index, Point3d position, Autodesk.AutoCAD.ApplicationServices.Document doc = null)
        {
            var docToUse = doc ?? Application.DocumentManager.MdiActiveDocument;

            using (Transaction tr = docToUse.TransactionManager.StartOpenCloseTransaction())
            {

                using (PolylineVertex3d vt = tr.GetObject(this[index], OpenMode.ForWrite) as PolylineVertex3d)

                {
                    vt.Position = position;
                }

                tr.Commit();
            }
        }
    }
}
