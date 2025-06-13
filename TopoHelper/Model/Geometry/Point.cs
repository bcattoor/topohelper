using Autodesk.AutoCAD.Geometry;
using System.Globalization;

namespace Infrabel.AutodeskPlatform.TopoHelper.Model.Geometry
{
    public class Point
    {
        #region Public Constructors

        public Point(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Point(Point3d p)
        {
            X = p.X; Y = p.Y; Z = p.Z;
        }

        #endregion

        #region Public Properties

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        #endregion

        #region Public Methods

        public Point3d ToPoint3d()
        {
            return new Point3d(X, Y, Z);
        }

        public override string ToString()
        {
            return $"{X.ToString(CultureInfo.CurrentCulture)};{Y.ToString(CultureInfo.CurrentCulture)};{Z.ToString(CultureInfo.CurrentCulture)}";
        }

        #endregion
    }

    public static class PointExtensions
    {
        /// <summary>
        /// Calculates the midpoint between the current object, and a new provided point3d.
        /// </summary>
        /// <param name="p1">Current point.</param>
        /// <param name="p2">Point to measure to</param>
        /// <returns>Mid between p1 and p2 as 3dPoint</returns>
        public static Point3d GetMidpointTo3dPoint(this Point3d p1, Point3d p2)
        {
            return Calculations.Basic.GetMidpointTo3dPoint(new Point(p1), new Point(p2)).ToPoint3d();
        }
    }
}