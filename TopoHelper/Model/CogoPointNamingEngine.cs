using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Infrabel.AutodeskPlatform.TopoHelper.Properties;

namespace Infrabel.AutodeskPlatform.TopoHelper.Model
{

    #region COGO Naming Engine

    public static class CogoPointNamingEngine
    {
        public static CogoPointNamingSettings LoadSettings()
        {
            var xmlConfig = Settings.Default.CogoPointNamingConfiguration;
            if (string.IsNullOrWhiteSpace(xmlConfig))
            {
                var defaultSettings = new CogoPointNamingSettings();
                defaultSettings.PrefixPatterns.Add(new PrefixPattern { Prefix = "CATA", Pattern = "CATA-{Counter:3}" });
                defaultSettings.PrefixPatterns.Add(new PrefixPattern { Prefix = "CATB", Pattern = "CATB-{Counter:3}" });
                SaveSettings(defaultSettings);
                return defaultSettings;
            }

            try
            {
                var serializer = new XmlSerializer(typeof(CogoPointNamingSettings_Serializable));
                using (var reader = new StringReader(xmlConfig))
                {
                    var loaded = (CogoPointNamingSettings_Serializable)serializer.Deserialize(reader);
                    return new CogoPointNamingSettings
                    {
                        PrefixPatterns = new ObservableCollection<PrefixPattern>(loaded.PrefixPatterns),
                        DescriptionLookupTable = new ObservableCollection<DescriptionMapping>(loaded.DescriptionLookupTable),
                        DefaultPattern = loaded.DefaultPattern
                    };
                }
            }
            catch 
            {
                return new CogoPointNamingSettings(); 
            }
        }

        public static void SaveSettings(CogoPointNamingSettings settings)
        {
            var settingsToSave = new CogoPointNamingSettings_Serializable
            {
                PrefixPatterns = settings.PrefixPatterns.ToList(),
                DescriptionLookupTable = settings.DescriptionLookupTable.ToList(),
                DefaultPattern = settings.DefaultPattern
            };

            var serializer = new XmlSerializer(typeof(CogoPointNamingSettings_Serializable));
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, settingsToSave);
                Settings.Default.CogoPointNamingConfiguration = writer.ToString();
                Settings.Default.Save();
            }
        }
    }
    
    #endregion

}
