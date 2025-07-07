using Autodesk.Civil.DatabaseServices;

namespace Infrabel.AutodeskPlatform.TopoHelper.Model
{
    public class CogoPointDisplay
    {
        public uint PointNumber { get; set; }
        public string Name { get; set; }
        public double Easting { get; set; }
        public double Northing { get; set; }
        public double Elevation { get; set; }
        public string RawDescription { get; set; }
        public string LabelStyleName { get; set; }
        public string PointStyleName { get; set; }
        public string LayerName { get; set; }
    }
}