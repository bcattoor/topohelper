using Autodesk.AutoCAD.Geometry;
using PostSharp.Patterns.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TopoHelper.Model.Results;
using TopoHelper.Normalizer;
using TopoHelper.Properties;

namespace TopoHelper.CommandImplementations
{
    public static class Rails2RailwayCenterLine
    {
        #region Private Fields

        private static readonly object _SectionsLock = 1;
        private static readonly Plane myPlaneWCS = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));
        private static Settings settings_default = Properties.Settings.Default;

        #endregion

        #region Internal Methods

        /// <summary>
        /// This function calculates a <seealso cref="IList{MeasuredSection}" />
        /// of Type <see cref="MeasuredSection" />.
        /// </summary>
        /// <param name="leftRailPoints">  Points lying on the right rail. </param>
        /// <param name="rightRailPoints"> Points lying on the right rail. </param>
        /// <param name="preferedType">   
        /// Here you can pass a preferred way of calculating the
        /// railway-centerline hight by supplying the Axis0CallculationType.
        /// </param>
        /// <returns> A <seealso cref="IList{MeasuredSection}" /> </returns>
        internal static IList<MeasuredSectionResult> CalculateRailwayCenterLine(
            [NotNull]IEnumerable<Point3d> leftRailPoints,
            [NotNull]IEnumerable<Point3d> rightRailPoints)
        {
            //+ Normalize input before calculation
            var normalizerInput = new List<Point3d>();
            normalizerInput.AddRange(leftRailPoints);
            normalizerInput.AddRange(rightRailPoints);
            var normalizerOutput = Normalizer.Normalizer.Normalize(normalizerInput.Select(point =>
            new NormalizerPoint(point)), out double minX, out double minY).ToList();

            //+ Calculate data for all sections
            double centerLineChainage = 0;

            int itemCount = leftRailPoints.Count();
            MeasuredSectionResult[] sections = new MeasuredSectionResult[itemCount];

            Parallel.For(0, itemCount, (i) =>
            //for (int i = 0; i < itemCount; i++)
            {
                var right_i = itemCount + i;
                //? The n_ prefix is used to clarify we are using a normalized variable.
                var n_LeftRailPoint = normalizerOutput[i];
                var n_RightRailPoint = normalizerOutput[right_i];

                var section = new MeasuredSectionResult(leftRailPoints.ElementAt(i), rightRailPoints.ElementAt(i));

                section.SetCant(section.LeftRailMeasuredPoint.Z, section.RightRailMeasuredPoint.Z);

                //+ line0 calculation, and set cant direction
                // to find our x-and-y- coordinates we need a 3d line
                // starting at the highest rail, and ending at lowest rail.
                // railHead2RailHeadLineSegment3d = short-name variable r2rLineSeg3d
                using (var r2rLineSeg3d = new LineSegment3d(n_LeftRailPoint.ToPoint3d(), n_RightRailPoint.ToPoint3d()))
                {
                    //var dist = r2rLineSeg3d.GetLength(0, 0.5, settings_default.APP_EPSILON);

                    // Set Gauge
                    section.Gauge = r2rLineSeg3d.Length;

                    //? De-normalize point!
                    //var para = r2rLineSeg3d.GetParameterAtLength(0, dist, true, settings_default.APP_EPSILON);
                    var trackCenterlinePoint = r2rLineSeg3d.EvaluatePoint(.5).DeNormalize(minX, minY);

                    //? Here we have the real axis calculated!
                    //- notice how we use the Z-value of the lowest rail to create the height center point.
                    section.TrackAxisPoint = new Point3d(
                        trackCenterlinePoint.X, trackCenterlinePoint.Y,
                        r2rLineSeg3d.StartPoint.Z < r2rLineSeg3d.EndPoint.Z ?
                        r2rLineSeg3d.StartPoint.Z :
                        r2rLineSeg3d.EndPoint.Z);
                }
                lock (_SectionsLock)
                {
                    sections[i] = section;
                }
            });

            for (int i = 0; i < itemCount; i++)
            {
                // calculate chainage to the previously calculated point in the
                // 2-dimensional space.
                if (i != 0)
                    centerLineChainage += sections[i - 1].TrackAxisPoint.Convert2d(myPlaneWCS)
                        .GetDistanceTo(sections[i].TrackAxisPoint.Convert2d(myPlaneWCS));

                // Set chainage on section object
                sections[i].Chainage = centerLineChainage;
            }

            return sections;
        }

        #endregion
    }
}