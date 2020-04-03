using Autodesk.AutoCAD.Geometry;
using System;
using TopoHelper.Properties;

namespace TopoHelper.Normalizer
{
    internal struct NormalizerPoint
    {
        #region Private Fields

        private static Settings _settingsDefault = Settings.Default;

        #endregion

        #region Public Constructors

        public NormalizerPoint(double x, double y, double z)
        {
            Y = y;
            X = x;
            Z = z;
        }

        public NormalizerPoint(Point2d p, double z)
        {
            Y = p.Y;
            X = p.X;
            Z = z;
        }

        public NormalizerPoint(Point3d p)
        {
            Y = p.Y;
            X = p.X;
            Z = p.Z;
        }

        #endregion

        #region Public Properties

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns angle in radians, the point makes apposed to the origin x-axes.
        /// </summary>
        /// <returns> Returns angle in radians. </returns>
        /// <exception cref="ArgumentException">
        /// Before getting this angle, make sure your dataset has been normalized.
        /// </exception>
        public double GetAngleToOrigin()
        {
            const double halfPi = Math.PI / 2d;

            // When the point is laying on the origin, return 0.0
            if (Math.Abs(X) < _settingsDefault.APP_EPSILON && Math.Abs(Y) < _settingsDefault.APP_EPSILON)
            {
                throw new ArgumentException("You can not measure an angle of a point that is laying on the origin.");
            }

            if (Math.Abs(X) < _settingsDefault.APP_EPSILON)
            {
                if (Y > 0.0d)

                    // 0 degrees
                    return 0d;

                // 270 degrees
                return Math.PI + halfPi;
            }
            if (!(Math.Abs(Y) < _settingsDefault.APP_EPSILON)) return Math.Atan2(Y, X);
            // 180 degrees of 90
            return X > 0.0d ? halfPi : Math.PI;
        }

        public NormalizerPoint RotateAroundOrigin(double angle)
        {
            // When the point is laying on the origin, return 0.0
            if (Math.Abs(X) < _settingsDefault.APP_EPSILON && Math.Abs(Y) < _settingsDefault.APP_EPSILON)
            {
                throw new ArgumentException("You can not rotate a point that is on the origin.");
            }

            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);

            var newX = X * cos - Y * sin;
            var newY = X * sin + Y * cos;

            return new NormalizerPoint(newX, newY, Z);
        }

        public string ToAutocadString()
        {
            return $"Point {X},{Y},{Z} ";
        }

        public Point2d ToPoint2d()
        {
            return new Point2d(X, Y);
        }

        public Point3d ToPoint3d()
        {
            return new Point3d(X, Y, Z);
        }

        #endregion
    }
}