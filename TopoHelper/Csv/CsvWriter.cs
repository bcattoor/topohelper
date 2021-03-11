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
        }

        public static ReadWrite Instance { get; } = new ReadWrite();

        public string FilePath { get; set; }

        public CultureInfo Culture => _culture;

        internal void WriteMeasuredSections<T>(IEnumerable<T> records)
        {
            using (var writer = new StreamWriter(FilePath))
            {
                // We call this function becouse the settings could have been updated
                setCultureFromSettings();
                using (var csv = new CsvWriter(writer, Culture))
                {
                    ;
                    csv.Context.RegisterClassMap<MeasuredSectionMap>();
                    csv.WriteRecords(records);
                }
            }
        }

        internal void WriteCalculateDisplacementResult<T>(IEnumerable<T> records)
        {
            using (var writer = new StreamWriter(FilePath))

            {
                // We call this function becouse the settings could have been updated
                setCultureFromSettings();
                using (var csv = new CsvWriter(writer, Culture))
                {
                    csv.Context.RegisterClassMap<CalculateDisplacementResultMap>();
                    csv.WriteRecords(records);
                }
            }
        }

        internal void WriteDistanceBetween2PolylinesResult<T>(IEnumerable<T> records)
        {
            using (var writer = new StreamWriter(FilePath))

            {
                // We call this function becouse the settings could have been updated
                setCultureFromSettings();
                using (var csv = new CsvWriter(writer, Culture))
                {
                    csv.Context.RegisterClassMap<DistanceBetween2PolylinesResultMap>();
                    csv.WriteRecords(records);
                }
            }
        }

        private void setCultureFromSettings()
        {
            _culture.NumberFormat.NumberDecimalSeparator = Settings.Default.NumberDecimalSeperator_ForAllCSVFiles;
            _culture.NumberFormat.NumberGroupSeparator = Settings.Default.NumberGroupCharacter_ForAllCSVFiles;
            _culture.TextInfo.ListSeparator = Settings.Default.ListSeperator_ForAllCSVFiles;
        }
    }
}