using Autodesk.AutoCAD.Geometry;
using CsvHelper.Configuration;
using TopoHelper.Csv.Mapping.Converters;
using TopoHelper.Model.Results;

namespace TopoHelper.Csv.Mapping
{
    internal sealed class CalculateDisplacementResultMap : ClassMap<CalculateDisplacementSectionResult>
    {
        #region Public Constructors

        public CalculateDisplacementResultMap()
        {
            _ = Map(m => m.OriginalLeftRailPoint).Name("Original Left Rail Point").TypeConverter<Point3dConverter<Point3d>>();
            _ = Map(m => m.OriginalRightRailPoint).Name("Original Right Rail Point").TypeConverter<Point3dConverter<Point3d>>();
            _ = Map(m => m.LeftRailPoint).Name("New Left Rail Point").TypeConverter<Point3dConverter<Point3d>>();
            _ = Map(m => m.RightRailPoint).Name("New Right Rail Point").TypeConverter<Point3dConverter<Point3d>>();
            _ = Map(m => m.DisplacementSectionXY).Name("DisplacementSection XY");
            _ = Map(m => m.DisplacementSectionZ).Name("DisplacementSection Z");
            _ = Map(m => m.Cant).Name("Cant");
            _ = Map(m => m.Gauge).Name("Gauge");
            _ = Map(m => m.Chainage).Name("Chainage");
        }

        #endregion
    }

    internal sealed class DistanceBetween2PolylinesResultMap : ClassMap<DistanceBetween2PolylinesSectionResult>
    {
        #region Public Constructors

        public DistanceBetween2PolylinesResultMap()
        {
            _ = Map(m => m.Chainage).Name("Chainage");
            _ = Map(m => m.DeltaXY2d).Name("Delta XY 2d");
            _ = Map(m => m.DeltaXY3d).Name("Delta XY 3d");
            _ = Map(m => m.DeltaZ).Name("Delta Z");
            _ = Map(m => m.FromPoint).Name("Vertex point on polyline").TypeConverter<Point3dConverter<Point3d>>();
            _ = Map(m => m.ToPoint).Name("Projected point on polyline").TypeConverter<Point3dConverter<Point3d>>();
        }

        #endregion
    }

    internal sealed class MeasuredSectionMap : ClassMap<MeasuredSectionResult>
    {
        #region Public Constructors

        public MeasuredSectionMap()
        {
            _ = Map(m => m.Chainage).Name("Chainage");
            _ = Map(m => m.Cant).Name("Cant");
            _ = Map(m => m.Gauge).Name("Gauge");
            _ = Map(m => m.CantDirection).Name("Cant Direction", "CantDirection");
            _ = Map(m => m.TrackAxisPoint).Name("Track Axis Point", "TrackAxisPoint");
            _ = Map(m => m.TrackAxisPoint).TypeConverter<Point3dConverter<Point3d>>();
            _ = Map(m => m.LeftRailMeasuredPoint).Name("Left Rail Point", "LeftRailPoint").TypeConverter<Point3dConverter<Point3d>>();
            _ = Map(m => m.RightRailMeasuredPoint).Name("Right Rail Point", "RightRailPoint").TypeConverter<Point3dConverter<Point3d>>();
        }

        #endregion
    }
}