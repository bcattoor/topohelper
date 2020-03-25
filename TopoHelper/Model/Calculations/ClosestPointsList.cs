using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using TopoHelper.Model.Geometry;

namespace TopoHelper.Model.Calculations
{
    internal class ClosestPointsList
    {
        #region Public Methods

        public static List<Point> Calculate(List<Point> pointList, Point startPoint, double minimumPointDistance, double maximumPointDistance)
        {
            if (minimumPointDistance > maximumPointDistance)
                throw new InvalidOperationException("The maximumPointDistance buffer should be bigger than the minimumPointDistance buffer!");

            // This is used to create the resulting polyline
            var vertexList = new List<Point>
            {
                // add start-point as first!
                startPoint
            };

            while (pointList.Count > 0)
            {
                // Do we need to ignore point that are too close points are
                // consider being too close when the candidate point is inside
                // the rectangle
                var minDistanceRectaglePoint1 = new Point(startPoint.X - minimumPointDistance, startPoint.Y - minimumPointDistance, startPoint.Z);
                var minDistanceRectaglePoint2 = new Point(startPoint.X + minimumPointDistance, startPoint.Y + minimumPointDistance, startPoint.Z);

                var maxDistanceRectaglePoint1 = new Point(startPoint.X - maximumPointDistance, startPoint.Y - maximumPointDistance, startPoint.Z);
                var maxDistanceRectaglePoint2 = new Point(startPoint.X + maximumPointDistance, startPoint.Y + maximumPointDistance, startPoint.Z);

                // remove points that are too close, also remove them from the point-list
                var pointsRemoved = pointList.RemoveAll(a => Calculations.IsInsideRectangle(minDistanceRectaglePoint1, minDistanceRectaglePoint2, a));

                // If no point are within limit, just break out, and return result
                if (pointsRemoved == pointList.Count())
                    break;

                // Lets create a list with points that are within our buffer
                var localPoints = pointList.Where(a => Calculations.IsInsideRectangle(maxDistanceRectaglePoint1, maxDistanceRectaglePoint2, a)).ToList();

                // If no point are within limit, just break out, and return result
                if (!localPoints.Any())
                    break;

                // Find point closest to the start-point
                var x = localPoints.MinBy(listPoint =>
                {
                    return Calculations.CalculatePoweredDistance(
                        startPoint.X, startPoint.Y, startPoint.Z,
                        listPoint.X, listPoint.Y, listPoint.Z);
                }).First();

                // add it as vertex
                vertexList.Add(x);

                // remove it from point-list
                pointList.Remove(x);

                // set start-point to closest point
                startPoint = x;
                // repeat
            }

            return vertexList;
        }

        #endregion
    }
}