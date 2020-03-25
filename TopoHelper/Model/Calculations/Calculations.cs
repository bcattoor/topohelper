using System;
using TopoHelper.Model.Geometry;

namespace TopoHelper.Model.Calculations
{
    internal static class Calculations
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

        #endregion
    }
}