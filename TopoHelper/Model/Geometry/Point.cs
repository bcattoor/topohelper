using System.Globalization;
using Autodesk.AutoCAD.Geometry;

namespace TopoHelper.Model.Geometry
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
}