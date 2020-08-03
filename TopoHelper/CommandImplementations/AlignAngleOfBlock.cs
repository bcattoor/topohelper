using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace TopoHelper.CommandImplementations
{
    internal class AlignAngleOfBlock
    {
        #region Private Fields

        private const double Tolerance = Commands.Tolerance;

        #endregion

        #region Public Methods

        /// <summary>
        /// This command is used by user to align an inserted blocks angle
        /// property to a selected polyline.
        /// </summary>
        public static void IAMTopo_AlignAngleOfBlock(Transaction transaction, ObjectId blockId, ObjectId polyline3dId, double piMultiplyer, string dynamicPropertyNameAngle)
        {
            using (var sourceBlockReference = transaction.GetObject(blockId, OpenMode.ForWrite) as BlockReference)
            using (var polyline = transaction.GetObject(polyline3dId, OpenMode.ForRead) as Curve)
            {
                var closestPointToInsertion = polyline.GetClosestPointTo(sourceBlockReference.Position, false);

                // find out what parameter to use to get right direction
                var paraClosestPoint = polyline.GetParameterAtPoint(closestPointToInsertion);
                var beginDefPara = Math.Floor(paraClosestPoint);
                var endDefPara = Math.Ceiling(paraClosestPoint);

                if (beginDefPara.Equals(endDefPara))
                {
                    if (Math.Abs(endDefPara) < Tolerance)
                        endDefPara += 1;
                    else
                    {
                        beginDefPara -= 1;
                    }
                }

                // get points at parameter
                var startDirectionLine = polyline.GetPointAtParameter(beginDefPara);
                var endDirectionLine = polyline.GetPointAtParameter(endDefPara);
                var extraRotation = piMultiplyer * Math.PI;
                var angle = GetAngleBetween2Points(startDirectionLine, endDirectionLine) + extraRotation;

                if (!sourceBlockReference.IsDynamicBlock)
                    throw new Exception("This is not a dynamic block.");

                foreach (DynamicBlockReferenceProperty property in sourceBlockReference.DynamicBlockReferencePropertyCollection)
                {
                    if (property.PropertyName == dynamicPropertyNameAngle && !property.ReadOnly)
                    { property.Value = angle; break; }
                }
            }
        }

        #endregion

        #region Private Methods

        private static double GetAngleBetween2Points(Point3d p1, Point3d p2)
        {
            return GetAngleBetween2Points(p1.X, p2.X, p1.Y, p2.Y);
        }

        private static double GetAngleBetween2Points(double px1, double px2, double py1, double py2)
        {
            // Negate X and Y values
            var pxRes = px2 - px1;

            var pyRes = py2 - py1;
            double angle;

            // Calculate the angle
            if (Math.Abs(pxRes) < Tolerance)
            {
                if (Math.Abs(pxRes) < Tolerance)
                    angle = 0.0;
                else if (pyRes > 0.0)
                    angle = Math.PI / 2.0;
                else
                    angle = Math.PI * 3.0 / 2.0;
            }
            else if (Math.Abs(pyRes) < Tolerance)
            {
                if (pxRes > 0.0)
                    angle = 0.0;
                else
                    angle = Math.PI;
            }
            else
            {
                if (pxRes < 0.0)
                    angle = Math.Atan(pyRes / pxRes) + Math.PI;
                else if (pyRes < 0.0)
                    angle = Math.Atan(pyRes / pxRes) + (2 * Math.PI);
                else
                    angle = Math.Atan(pyRes / pxRes);
            }

            return angle;
        }

        #endregion
    }
}