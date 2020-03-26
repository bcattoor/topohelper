using Autodesk.AutoCAD.Geometry;

namespace TopoHelper.Model.Results
{
    public class CalculateDisplacementSectionResult
    {
        #region Public Properties

        public double Cant { get; internal set; }
        public double Chainage { get; internal set; }
        public double DisplacementSectionXy { get; internal set; }
        public double DisplacementSectionZ { get; internal set; }
        public double Gauge { get; internal set; }
        public Point3d LeftRailPoint { get; set; }
        public Point3d OriginalLeftRailPoint { get; set; }
        public Point3d OriginalRightRailPoint { get; set; }
        public Point3d RightRailPoint { get; set; }

        #endregion
    }
}