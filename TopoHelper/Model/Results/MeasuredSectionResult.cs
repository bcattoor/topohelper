using System;
using Autodesk.AutoCAD.Geometry;
using TopoHelper.Model.Enums;

namespace TopoHelper.Model.Results
{
    internal class MeasuredSectionResult
    {
        #region Private Fields

        // ReSharper disable once UnusedMember.Local
        private static readonly Plane MyPlaneWcs = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));

        #endregion

        #region Public Constructors

        public MeasuredSectionResult(Point3d leftRailMeasuredPoint, Point3d rightRailMeasuredPoint)
        {
            LeftRailMeasuredPoint = leftRailMeasuredPoint;
            RightRailMeasuredPoint = rightRailMeasuredPoint;
            //CurvatureCurveLeft = curvatureCurveLeft;
            //CurvatureCurveRight = curvatureCurveRight;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The actual cant that has been measured by the operator. This is an
        /// absolute value use the cant direction to determine what rail to raise.
        /// </summary>
        public double Cant { get; private set; }

        /// <summary>
        /// The direction of the cant depends on the direction of the track,
        /// when a left rail is raised it is CW, else it is CCW.
        /// </summary>
        public CantDirection CantDirection { get; private set; }

        public double Chainage { get; set; }
        //public CircularArc2d CurvatureCurveLeft { get; private set; }
        //public CircularArc2d CurvatureCurveRight { get; private set; }

        /// <summary>
        /// Gauge is the distance measured between the two points provided by
        /// the operator. It is the rail-to-rail distance.
        /// </summary>
        public double Gauge { get; set; } = double.NaN;

        public Point3d LeftRailMeasuredPoint { get; }

        //public bool RailIsInsideRailOfCurveLeft { get; set; } = false;
        //public bool RailIsInsideRailOfCurveRight { get; set; } = false;
        public Point3d RightRailMeasuredPoint { get; }

        public Point3d TrackAxisPoint { get; set; }

        #endregion

        #region Public Methods

        public void SetCant(double leftRailHeight, double rightRailHeight)
        {
            Cant = Math.Abs(leftRailHeight - rightRailHeight);
            // Set cant direction
            if (leftRailHeight >= rightRailHeight)
                CantDirection = CantDirection.Clockwise;
            else
                CantDirection = CantDirection.CounterClockwise;
        }

        #endregion

        #region Internal Methods

        internal string ToDebugString()
        {
            return $"Chainage({Chainage}); LeftRail({LeftRailMeasuredPoint}); RightRail({RightRailMeasuredPoint}); Gauge({Gauge});";
        }

        #endregion
    }
}