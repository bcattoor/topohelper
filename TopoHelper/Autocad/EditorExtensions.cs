using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopoHelper.Autocad
{
    public static class EditorExtensions
    {        /// <summary>
             /// </summary> <param name="editor"> Current editor </param> <param
             /// name="layerName"> Layer to filter by </param> <param
             /// name="spaceName"> Space to filter by: Paper or Model </param>
             /// <param name="dxfName"> Dxf type to filter by: INSERT or POINT
             /// </param> <returns> Object id's of filtered result. </returns>
        public static ObjectIdCollection GetEntityIdsOnLayer(this Editor editor, string layerName, string spaceName, string dxfName)
        {
            // Build a filter list so that only entities on the specified layer
            // are selected

            var psr = editor.SelectAll(new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, dxfName),
                new TypedValue((int)DxfCode.LayerName, layerName),
                new TypedValue((int)DxfCode.LayoutName, spaceName)
            }));
            return psr.Status == PromptStatus.OK ?
                new ObjectIdCollection(psr.Value.GetObjectIds())
                : new ObjectIdCollection();
        }
    }
}