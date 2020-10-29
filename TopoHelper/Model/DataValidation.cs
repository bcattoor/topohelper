using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using TopoHelper.Properties;

// ReSharper disable MemberCanBePrivate.Global

namespace TopoHelper.Model
{
    internal static class DataValidation
    {
        #region Private Fields

        private const string CountNotEqual = "Both rail-axis polyline3d should have the same amount of vertices's.";

        private const string DirectionNotEqual = "Both rail-axis polyline3d should have the same direction.";

        private const string ItemCount = "Both rail-axis polyline3d should have at least 2 vertices's.";

        private const string RailMaxDistance = "You have exceeded the maximum distance allowed rail measured point to rail measured point.";

        private const string RailMinDistance = "You have exceeded the minimum distance allowed rail measured point to rail measured point.";

        /// <summary>
        /// My 2d plane to work in.
        /// </summary>
        private static readonly Plane MyPlaneWcs = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));

        private static readonly Settings _settingsDefault = Settings.Default;

        #endregion

        #region Public Methods

        public static bool ValidateInput(IList<Point3d> firstList, IList<Point3d> secondList, out string exceptionMessage)
        {
            if (firstList is null || !firstList.Any())
            {
                throw new ArgumentNullException(nameof(secondList));
            }

            if (secondList is null || !secondList.Any())
            {
                throw new ArgumentNullException(nameof(firstList));
            }

            if (firstList.Count != secondList.Count)
            { exceptionMessage = CountNotEqual; return false; }

            if (firstList.Count < 2)
            { exceptionMessage = ItemCount; return false; }

            if (!ValidateInputApparentVectorAngles(firstList, secondList))
            { exceptionMessage = DirectionNotEqual; return false; }

            if (!ValidateInputGaugeMinDistances(firstList, secondList, out int error))
            { exceptionMessage = RailMinDistance + $" Index on first polyline: {error}"; return false; }


            if (!ValidateInputGaugeMaxDistances(firstList, secondList, out error))
            { exceptionMessage = RailMaxDistance + $" Index on first polyline: {error}"; return false; }

            exceptionMessage = null;
            return true;
        }

        private static bool ValidateInputApparentVectorAngles(IList<Point3d> firstList, IList<Point3d> secondList)
        {
            var l1Sp = firstList.ElementAt(0).Convert2d(MyPlaneWcs);
            // var l1_ep = firstList.Last().Convert2d(myPlaneWCS);
            var l2Sp = secondList.ElementAt(0).Convert2d(MyPlaneWcs);
            var l2Ep = secondList.Last().Convert2d(MyPlaneWcs);
            // the start-point start-point distance needs to be smaller than the
            // start-point end-point distance if this is not the case, the two
            // lines are not in same direction.
            var d1 = l1Sp.GetDistanceTo(l2Sp);
            var d2 = l1Sp.GetDistanceTo(l2Ep);
            if (d1 < d2) return true;

            return false;
        }

        private static bool ValidateInputGaugeMaxDistances(IList<Point3d> firstList, IList<Point3d> secondList, out int indexErrorFirstList)
        {
            indexErrorFirstList = -1;
            for (var i = 0; i < firstList.Count; i++)
            {
                indexErrorFirstList = i;
                var leftpoint = firstList.ElementAt(i);
                var rightpoint = secondList.ElementAt(i);
                var distance = leftpoint.DistanceTo(rightpoint);
                if (distance > _settingsDefault.DataValidation_LeftrailToRightRail_Tolerance + _settingsDefault.DataValidation_LeftrailToRightRail_Maximum)
                    return false;
            }
            return true;
        }

        private static bool ValidateInputGaugeMinDistances(IList<Point3d> firstList, IList<Point3d> secondList, out int indexErrorFirstList)
        {
            indexErrorFirstList = -1;
            for (var i = 0; i < firstList.Count; i++)
            {
                indexErrorFirstList = i;
                var leftpoint = firstList.ElementAt(i);
                var rightpoint = secondList.ElementAt(i);
                var distance = leftpoint.DistanceTo(rightpoint);
                if (distance < 1.435 - _settingsDefault.DataValidation_LeftrailToRightRail_Tolerance)
                    return false;
            }

            return true;
        }

        public static bool ValidatePointsToPolylineSettings(out string message)
        {
            if (_settingsDefault.PointsTo3DPolyline_MinimumPointDistance < 0)
            {
                message = $"The variable {nameof(_settingsDefault.PointsTo3DPolyline_MinimumPointDistance)} should not be smaller than 0.";
                return false;
            }

            if (_settingsDefault.PointsTo3DPolyline_MaximumPointDistance < _settingsDefault.PointsTo3DPolyline_MinimumPointDistance + _settingsDefault.__APP_EPSILON)
            {
                message = $"The variable {nameof(_settingsDefault.PointsTo3DPolyline_MaximumPointDistance)} should not be smaller than variable {nameof(_settingsDefault.PointsTo3DPolyline_MinimumPointDistance)}.";
                return false;
            }

            message = null;
            return true;
        }

        #endregion
    }
}