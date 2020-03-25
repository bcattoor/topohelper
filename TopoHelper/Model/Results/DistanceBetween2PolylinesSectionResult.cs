using Autodesk.AutoCAD.Geometry;

namespace TopoHelper.Model.Results
{
    public class DistanceBetween2PolylinesSectionResult
    {
        #region Public Constructors

        public DistanceBetween2PolylinesSectionResult(
            double distanceOnPolyline,
            double deltaZ, double deltaXY2d, double deltaXY3d,
            Point3d fromPoint, Point3d toPoint)
        {
            FromPoint = fromPoint;
            ToPoint = toPoint;
            Chainage = distanceOnPolyline;
            DeltaZ = deltaZ;
            DeltaXY2d = deltaXY2d;
            DeltaXY3d = deltaXY3d;
        }

        #endregion

        #region Public Properties

        public double Chainage { get; private set; }
        public double DeltaXY2d { get; private set; }
        public double DeltaXY3d { get; private set; }
        public double DeltaZ { get; private set; }
        public Point3d FromPoint { get; private set; }
        public Point3d ToPoint { get; private set; }

        #endregion
    }
}