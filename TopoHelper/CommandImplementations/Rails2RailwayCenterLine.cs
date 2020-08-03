using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TopoHelper.Model.Results;
using TopoHelper.Normalizer;

namespace TopoHelper.CommandImplementations
{
    public static class Rails2RailwayCenterLine
    {
        #region Private Fields

        private static readonly object SectionsLock = 1;
        private static readonly Plane MyPlaneWcs = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));

        #endregion

        #region Internal Methods

        /// <summary>
        /// This function calculates a <seealso cref="IList{MeasuredSection}" />
        /// of Type <see cref="MeasuredSectionResult" />.
        /// </summary>
        /// <param name="leftRailPoints">  Points laying on the right rail. </param>
        /// <param name="rightRailPoints"> Points laying on the right rail. </param>
        /// <returns> A <seealso cref="IList{MeasuredSectionResult}" /> </returns>
        internal static IList<MeasuredSectionResult> CalculateRailwayCenterLine(
            IList<Point3d> leftRailPoints,
            IList<Point3d> rightRailPoints)
        {
            //+ Normalize input before calculation
            var normalizerInput = new List<Point3d>();

            normalizerInput.AddRange(leftRailPoints);

            normalizerInput.AddRange(rightRailPoints);
            var normalizerOutput = normalizerInput.Select(point =>
                new NormalizerPoint(point)).ToList().Normalize(out var minX, out var minY).ToList();

            //+ Calculate data for all sections
            double centerLineChainage = 0;

            var itemCount = leftRailPoints.Count;
            var sections = new MeasuredSectionResult[itemCount];

            Parallel.For(0, itemCount, i =>
            //for (int i = 0; i < itemCount; i++)
            {
                var rightI = itemCount + i;
                //? The n_ prefix is used to clarify we are using a normalized variable.
                var nLeftRailPoint = normalizerOutput[i];
                var nRightRailPoint = normalizerOutput[rightI];

                var section = new MeasuredSectionResult(leftRailPoints.ElementAt(i), rightRailPoints.ElementAt(i));

                section.SetCant(section.LeftRailMeasuredPoint.Z, section.RightRailMeasuredPoint.Z);

                //+ line0 calculation, and set cant direction
                // to find our x-and-y- coordinates we need a 3d line
                // starting at the highest rail, and ending at lowest rail.
                // railHead2RailHeadLineSegment3d = short-name variable r2rLineSeg3d
                using (var r2RLineSeg3d = new LineSegment3d(nLeftRailPoint.ToPoint3d(), nRightRailPoint.ToPoint3d()))
                {
                    // Set Gauge
                    section.Gauge = r2RLineSeg3d.Length;

                    //? De-normalize point!
                    var trackCenterLinePoint = r2RLineSeg3d.EvaluatePoint(.5).DeNormalize(minX, minY);

                    //? Here we have the real axis calculated!
                    //- notice how we use the Z-value of the lowest rail to create the height center point.
                    section.TrackAxisPoint = new Point3d(
                        trackCenterLinePoint.X, trackCenterLinePoint.Y,
                        r2RLineSeg3d.StartPoint.Z < r2RLineSeg3d.EndPoint.Z ?
                        r2RLineSeg3d.StartPoint.Z :
                        r2RLineSeg3d.EndPoint.Z);
                }
                lock (SectionsLock)
                {
                    sections[i] = section;
                }
            });

            for (var i = 0; i < itemCount; i++)
            {
                // calculate chainage to the previously calculated point in the
                // 2-dimensional space.
                if (i != 0)
                    centerLineChainage += sections[i - 1].TrackAxisPoint.Convert2d(MyPlaneWcs)
                        .GetDistanceTo(sections[i].TrackAxisPoint.Convert2d(MyPlaneWcs));

                // Set chainage on section object
                sections[i].Chainage = centerLineChainage;
            }

            return sections;
        }

        #endregion
    }
}