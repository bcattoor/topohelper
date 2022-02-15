using Autodesk.AutoCAD.Geometry;
using CsvHelper.Configuration;
using TopoHelper.Csv.Converters;
using TopoHelper.Model.Results;

// ReSharper disable ClassNeverInstantiated.Global (CSV HELPER asks for sealed class)

namespace TopoHelper.Csv.Mapping
{
    internal sealed class CalculateDisplacementResultMap : ClassMap<CalculateDisplacementSectionResult>
    {
        #region Public Constructors

        public CalculateDisplacementResultMap()
        {
            Map(m => m.OriginalLeftRailPoint).Name("Original Left Rail Point").TypeConverter<Point3dConverter<Point3d>>();
            Map(m => m.OriginalRightRailPoint).Name("Original Right Rail Point").TypeConverter<Point3dConverter<Point3d>>();
            Map(m => m.LeftRailPoint).Name("New Left Rail Point").TypeConverter<Point3dConverter<Point3d>>();
            Map(m => m.RightRailPoint).Name("New Right Rail Point").TypeConverter<Point3dConverter<Point3d>>();
            Map(m => m.DisplacementSectionXy).Name("DisplacementSection XY");
            Map(m => m.DisplacementSectionZ).Name("DisplacementSection Z");
            Map(m => m.Cant).Name("Cant");
            Map(m => m.Gauge).Name("Gauge");
            Map(m => m.Chainage).Name("Chainage");
        }

        #endregion
    }

    internal sealed class DistanceBetween2PolylinesResultMap : ClassMap<DistanceBetween2PolylinesSectionResult>
    {
        #region Public Constructors

        public DistanceBetween2PolylinesResultMap()
        {
            Map(m => m.Chainage).Name("Chainage");
            Map(m => m.DeltaXy2d).Name("Delta XY 2d");
            Map(m => m.DeltaXy3d).Name("Delta XY 3d");
            Map(m => m.DeltaZ).Name("Delta Z");
            Map(m => m.FromPoint).Name("Vertex point on polyline").TypeConverter<Point3dConverter<Point3d>>();
            Map(m => m.ToPoint).Name("Projected point on polyline").TypeConverter<Point3dConverter<Point3d>>();
        }

        #endregion
    }

    internal sealed class MeasuredSectionMap : ClassMap<MeasuredSectionResult>
    {
        #region Public Constructors

        public MeasuredSectionMap()
        {
            Map(m => m.Chainage).Name("Chainage");
            Map(m => m.Cant).Name("Cant");
            Map(m => m.Gauge).Name("Gauge");
            Map(m => m.CantDirection).Name("Cant Direction", "CantDirection");
            Map(m => m.TrackAxisPoint).Name("Track Axis Point", "TrackAxisPoint");
            Map(m => m.TrackAxisPoint).TypeConverter<Point3dConverter<Point3d>>();
            Map(m => m.LeftRailMeasuredPoint).Name("Left Rail Point", "LeftRailPoint").TypeConverter<Point3dConverter<Point3d>>();
            Map(m => m.RightRailMeasuredPoint).Name("Right Rail Point", "RightRailPoint").TypeConverter<Point3dConverter<Point3d>>();
        }

        #endregion
    }
}