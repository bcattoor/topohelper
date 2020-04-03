using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TopoHelper.Csv.Mapping;
using TopoHelper.Properties;

namespace TopoHelper.Csv
{
    internal class ReadWrite
    {
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;

        private ReadWrite()
        {
            _culture.NumberFormat.NumberDecimalSeparator = Settings.Default.IO_CSV_NUMBER_DECIMAL_SEPERATOR;
            _culture.NumberFormat.NumberGroupSeparator = "";
        }

        public static ReadWrite Instance { get; } = new ReadWrite();

        public string FilePath { get; set; }
        public string Delimiter { get; set; }

        internal void WriteMeasuredSections<T>(IEnumerable<T> records)
        {
            using (var writer = new StreamWriter(FilePath))

            {
                using (var csv = new CsvWriter(writer, _culture))
                {
                    csv.Configuration.Delimiter = Delimiter;

                    csv.Configuration.RegisterClassMap<MeasuredSectionMap>();
                    csv.WriteRecords(records);
                }
            }
        }

        internal void WriteCalculateDisplacementResult<T>(IEnumerable<T> records)
        {
            using (var writer = new StreamWriter(FilePath))

            {
                using (var csv = new CsvWriter(writer, _culture))
                {
                    csv.Configuration.Delimiter = Delimiter;
                    csv.Configuration.CultureInfo.NumberFormat.NumberDecimalSeparator = Settings.Default.IO_CSV_NUMBER_DECIMAL_SEPERATOR;
                    csv.Configuration.CultureInfo.NumberFormat.NumberGroupSeparator = "";
                    csv.Configuration.RegisterClassMap<CalculateDisplacementResultMap>();
                    csv.WriteRecords(records);
                }
            }
        }

        internal void WriteDistanceBetween2PolylinesResult<T>(IEnumerable<T> records)
        {
            using (var writer = new StreamWriter(FilePath))

            {
                using (var csv = new CsvWriter(writer, _culture))
                {
                    csv.Configuration.Delimiter = Delimiter;
                    csv.Configuration.CultureInfo.NumberFormat.NumberDecimalSeparator = Settings.Default.IO_CSV_NUMBER_DECIMAL_SEPERATOR;
                    csv.Configuration.CultureInfo.NumberFormat.NumberGroupSeparator = "";
                    csv.Configuration.RegisterClassMap<DistanceBetween2PolylinesResultMap>();
                    csv.WriteRecords(records);
                }
            }
        }
    }
}