using Autodesk.AutoCAD.Geometry;

namespace TopoHelper.Model.Results
{
    public class DistanceBetween2PolylinesSectionResult
    {
        #region Public Constructors

        public DistanceBetween2PolylinesSectionResult(
            double distanceOnPolyline,
            double deltaZ, double deltaXy2d, double deltaXy3d,
            Point3d fromPoint, Point3d toPoint)
        {
            FromPoint = fromPoint;
            ToPoint = toPoint;
            Chainage = distanceOnPolyline;
            DeltaZ = deltaZ;
            DeltaXy2d = deltaXy2d;
            DeltaXy3d = deltaXy3d;
        }

        #endregion

        #region Public Properties

        public double Chainage { get; private set; }
        public double DeltaXy2d { get; private set; }
        public double DeltaXy3d { get; private set; }
        public double DeltaZ { get; private set; }
        public Point3d FromPoint { get; private set; }
        public Point3d ToPoint { get; private set; }

        #endregion
    }
}