using System;
using Autodesk.AutoCAD.Geometry;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace TopoHelper.Csv.Converters
{
    internal abstract class Point3dConverter<T> : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            var split = text.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (split.Length == 0 || split.Length > 3)
                throw new InvalidOperationException("To convert from a 3d point string, we need 3 values provided. (ea: 0.0;0.0;0.0)");
            if (double.TryParse(split[0], out var x) && double.TryParse(split[1], out var y) && double.TryParse(split[2], out var z))
            {
                return new Point3d(x, y, z);
            }
            throw new InvalidOperationException($"Failed to convert from a 3d point string. Input-string:{text}");
        }

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            if (!(value is Point3d pt))
                throw new InvalidOperationException("Only 3d-points can be saved as a string using the point Point3dConverter");

            return $"{pt.X};{pt.Y};{pt.Z}";
        }
    }
}