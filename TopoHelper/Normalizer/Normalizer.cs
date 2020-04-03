using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;

namespace TopoHelper.Normalizer
{
    internal static class Normalizer
    {
        #region Public Methods

        public static NormalizerPoint DeNormalize(this NormalizerPoint point, double minX, double minY)
        {
            // DeNormalize X and Y input
            return new NormalizerPoint(point.X + minX, point.Y + minY, point.Z);
        }

        public static Point2d DeNormalize(this Point2d point, double minX, double minY)
        {
            // DeNormalize X and Y input
            return new Point2d(point.X + minX, point.Y + minY);
        }

        public static Point3d DeNormalize(this Point3d point, double minX, double minY)
        {
            // DeNormalize X and Y input
            return new Point3d(point.X + minX, point.Y + minY, point.Z);
        }

        public static IEnumerable<NormalizerPoint> DeNormalize(this IEnumerable<NormalizerPoint> pointsList, double minX, double minY)
        {
            // DeNormalize X and Y input
            return pointsList.Select(point => point.DeNormalize(minX, minY));
        }

        public static Point2d DivideAnamorphosis(this Point2d point, int anamorphosis)
        {
            return new Point2d(point.X, point.Y / anamorphosis);
        }

        public static Point3d DivideAnamorphosis(this Point3d point, int anamorphosis)
        {
            return new Point3d(point.X, point.Y / anamorphosis, 0);
        }

        public static IEnumerable<Point2d> DivideAnamorphosis(this IEnumerable<Point2d> unsegmentedInput, int anamorphosis)
        {
            return unsegmentedInput.Select(x => x.DivideAnamorphosis(anamorphosis));
        }

        public static Point2d MultiplyAnamorphosis(this Point2d point, int anamorphosis)
        {
            return new Point2d(point.X, point.Y * anamorphosis);
        }

        public static Point3d MultiplyAnamorphosis(this Point3d point, int anamorphosis)
        {
            return new Point3d(point.X, point.Y * anamorphosis, 0);
        }

        public static IEnumerable<Point2d> MultiplyAnamorphosis(this IEnumerable<Point2d> unsegmentedInput, int anamorphosis)
        {
            return unsegmentedInput.Select(x => x.MultiplyAnamorphosis(anamorphosis));
        }

        public static IEnumerable<NormalizerPoint> Normalize(this IList<NormalizerPoint> pointsList, out double minX, out double minY,
                           double offset = .0)
        {
            offset = Math.Abs(offset);

            // Calculating minimum
            minX = pointsList.Min(p => p.X) - offset;
            minY = pointsList.Min(p => p.Y) - offset;

            // Normalize X and Y input
            var x = minX;
            var y = minY;
            return pointsList.Select(point => new NormalizerPoint(point.X - x, point.Y - y, point.Z));
        }

        public static IEnumerable<Point2d> Normalize2d(this IList<NormalizerPoint> pointsList, out double minX, out double minY,
                           double offset = .0)
        {
            offset = Math.Abs(offset);

            // Calculating minimum
            minX = pointsList.Min(p => p.X) - offset;
            minY = pointsList.Min(p => p.Y) - offset;

            // Normalize X and Y input
            var x = minX;
            var y = minY;
            return pointsList.Select(point => new Point2d(point.X - x, point.Y - y));
        }

        public static IEnumerable<NormalizerPoint> RotatePointsList(this IList<NormalizerPoint> pointsList, double proposedRotation)
        {
            return pointsList.Select(point => point.RotateAroundOrigin(proposedRotation));
        }

        #endregion
    }
}