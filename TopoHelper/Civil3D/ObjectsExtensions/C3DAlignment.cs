using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using Infrabel.AutodeskPlatform.AutoCADCommon;

namespace Infrabel.AutodeskPlatform.TopoHelper.Civil3D.ObjectsExtensions
{
    internal static class C3DAlignment
    {
        public static void RenameAlignment(this Alignment alignment, string newName)
        {
            using (Transaction transaction = alignment.Database.TransactionManager.StartOpenCloseTransaction())
            {
                alignment.UpgradeOpen();
                alignment.Name = newName;
                alignment.DowngradeOpen();
                transaction.Commit();
            }
        }

        public static Alignment RenameLayer(this Alignment alignment, string newName)
        {
            return null;
            
        }
    }
}
