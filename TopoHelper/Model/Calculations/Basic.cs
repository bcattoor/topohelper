using System;
using TopoHelper.Model.Geometry;

namespace TopoHelper.Model.Calculations
{
    internal static class Basic
    {
        #region Public Methods

        public static double CalculatePoweredDistance(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2) + Math.Pow(z2 - z1, 2);
        }

        // function to find if given point lies inside a given rectangle or not.
        public static bool IsInsideRectangle(Point p1, Point p2, Point p)
        {
            if (p.X > p1.X && p.X < p2.X &&
                p.Y > p1.Y && p.Y < p2.Y)
                return true;

            return false;
        }

        /// <summary>
        /// Calculates the midpoint between the two points, and return a new point3d.
        /// </summary>
        /// <param name="p1">Point one</param>
        /// <param name="p2">Point two</param>
        /// <returns>Mid between p1 and p2 as Point></returns>
        public static Point GetMidpointTo3dPoint(Point p1, Point p2)
        {
            // midpoint : (x1, y1, z1) and (x2, y2, z1) is (x1+x2 )/2,(y1+y2 )/2,(z1+z2 )/2.
            return new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2, (p1.Z + p2.Z) / 2);
        }

        #endregion
    }
}