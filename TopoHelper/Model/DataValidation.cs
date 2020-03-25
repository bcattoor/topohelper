using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using TopoHelper.Properties;

namespace TopoHelper.Model
{
    internal static class DataValidation
    {
        #region Private Fields

        private const string MessageCountNotEqual = "Both rail-axis polyline3d should have the same amount of vertices's.";

        private const string MessageDirectionNotEqual = "Both rail-axis polyline3d should have the same direction.";

        private const string MessageItemCount = "Both rail-axis polyline3d should have at least 2 vertices's.";

        private const string MessageRailMaxDistance = "You have exceeded the maximum distance allowed rail measured point to rail measured point.";

        private const string MessageRailMinDistance = "You have exceeded the minimum distance allowed rail measured point to rail measured point.";

        /// <summary>
        /// My 2d plane to work in.
        /// </summary>
        private static readonly Plane myPlaneWCS = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));

        private static Settings settings_default = Properties.Settings.Default;

        #endregion

        #region Public Methods

        public static bool ValidateInput(IEnumerable<Point3d> firstList, IEnumerable<Point3d> secondList, out string exeptionMessage)
        {
            if (firstList is null || !firstList.Any())
            {
                throw new ArgumentNullException(nameof(secondList));
            }

            if (secondList is null || !secondList.Any())
            {
                throw new ArgumentNullException(nameof(firstList));
            }

            if (firstList.Count() != secondList.Count())
            { exeptionMessage = MessageCountNotEqual; return false; }

            if (firstList.Count() < 2)
            { exeptionMessage = MessageItemCount; return false; }

            if (!ValidateInputApparentVectorAngles(firstList, secondList))
            { exeptionMessage = MessageDirectionNotEqual; return false; }

            if (!ValidateInputGaugeMinDistances(firstList, secondList))
            { exeptionMessage = MessageRailMinDistance; return false; }

            if (!ValidateInputGaugeMaxDistances(firstList, secondList))
            { exeptionMessage = MessageRailMaxDistance; return false; }

            exeptionMessage = null;
            return true;
        }

        public static bool ValidateInputApparentVectorAngles(IEnumerable<Point3d> firstList, IEnumerable<Point3d> secondList)
        {
            var l1_sp = firstList.ElementAt(0).Convert2d(myPlaneWCS);
            // var l1_ep = firstList.Last().Convert2d(myPlaneWCS);
            var l2_sp = secondList.ElementAt(0).Convert2d(myPlaneWCS);
            var l2_ep = secondList.Last().Convert2d(myPlaneWCS);
            // the start-point start-point distance needs to be smaller than the
            // start-point end-point distance if this is not the case, the two
            // lines are not in same direction.
            var d1 = l1_sp.GetDistanceTo(l2_sp);
            var d2 = l1_sp.GetDistanceTo(l2_ep);
            if (d1 < d2) return true;

            return false;
        }

        public static bool ValidateInputGaugeMaxDistances(IEnumerable<Point3d> firstList, IEnumerable<Point3d> secondList)
        {
            for (int i = 0; i < firstList.Count(); i++)
            {
                var railtoRailLine = firstList.ElementAt(i).DistanceTo(secondList.ElementAt(i));
                if (railtoRailLine > settings_default.DV_LRTRR_MAX_VALUE)
                    return false;
            }
            return true;
        }

        public static bool ValidateInputGaugeMinDistances(IEnumerable<Point3d> firstList, IEnumerable<Point3d> secondList)
        {
            for (int i = 0; i < firstList.Count(); i++)
            {
                var railtoRailLine = firstList.ElementAt(i).DistanceTo(secondList.ElementAt(i));
                if (railtoRailLine < 1.435 - settings_default.DV_LRTRR_TOLERANCE)
                    return false;
            }

            return true;
        }

        public static bool ValidatePointsToPolylineSettings(out string message)
        {
            if (settings_default.DIST_MIN_PTP < 0)
            {
                message = $"The variable {nameof(settings_default.DIST_MIN_PTP)} should not be smaller than 0.";
                return false;
            }

            if (settings_default.DIST_MAX_PTP < settings_default.DIST_MIN_PTP + settings_default.APP_EPSILON)
            {
                message = $"The variable {nameof(settings_default.DIST_MAX_PTP)} should not be smaller than variable {nameof(settings_default.DIST_MIN_PTP)}.";
                return false;
            }

            message = null;
            return true;
        }

        #endregion
    }
}