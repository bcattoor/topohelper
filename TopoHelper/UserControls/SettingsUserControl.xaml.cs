using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Infrabel.AutodeskPlatform.TopoHelper.Model;
using System.Xml.Serialization;
using Autodesk.Civil.DatabaseServices;
using Infrabel.AutodeskPlatform.TopoHelper.Properties;

namespace Infrabel.AutodeskPlatform.TopoHelper.UserControls
{
    public partial class SettingsUserControl : UserControl
    {
        public SettingsUserControl()
        {
            InitializeComponent();
            // DataContext is now set by the parent window or DI container.
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            var propName = e.PropertyName;
            if (propName == "Errors" || propName == "Type" || propName == "IsDirty" || propName == "IsNew")
            {
                e.Column.Visibility = Visibility.Collapsed;
            }
        }

        private void MenuItem_Click_Navigate_Url(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                try
                {
                    var tooltip = menuItem.ToolTip.ToString();
                    if (tooltip != "Over deze applicatie.")
                        Process.Start(tooltip);
                    else
                    {
                        var version = $"Version {MyApplication.GetInformationalVersion()} / {MyApplication.GetAssemblyVersion()} / {MyApplication.GetAssemblyFileVersion()}";
                        Clipboard.SetText(version);
                        MessageBox.Show($"{version}\r\nDe bovenstaande info werd gekopieerd naar het klembord.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void TxtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.DataContext is ViewModels.SettingsViewModel vm)
            {
                if (sender is TextBox textBox)
                {
                    vm.SearchString = textBox.Text;
                }
            }
        }

        private void DataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                var dataGrid = sender as DataGrid;
                if (dataGrid != null && dataGrid.SelectedItems.Count > 0)
                {
                    foreach (var item in dataGrid.SelectedItems)
                    {
                        if (item is CogoPointDisplay cogoPoint)
                        {
                            cogoPoint.IsSelected = !cogoPoint.IsSelected;
                        }
                        else if (item is BlockDisplay blockDisplay)
                        {
                            blockDisplay.IsSelected = !blockDisplay.IsSelected;
                        }
                    }
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape)
            {
                var dataGrid = sender as DataGrid;
                if (dataGrid != null)
                {
                    if (this.DataContext is ViewModels.SettingsViewModel viewModel)
                    {
                        if (dataGrid.Name == "CogoPointsDataGrid")
                        {
                            foreach (var item in viewModel.CogoPoints)
                            {
                                item.IsSelected = false;
                            }
                        }
                        else if (dataGrid.Name == "BlocksDataGrid")
                        {
                            foreach (var item in viewModel.Blocks)
                            {
                                item.IsSelected = false;
                            }
                        }
                    }
                    e.Handled = true;
                }
            }
        }
    }

    #region COGO Naming Model Classes

    public class CogoPointNamingSettings : INotifyPropertyChanged
    {
        private ObservableCollection<PrefixPattern> _prefixPatterns;
        private ObservableCollection<DescriptionMapping> _descriptionLookupTable;
        private string _defaultPattern;

        public ObservableCollection<PrefixPattern> PrefixPatterns
        {
            get => _prefixPatterns;
            set { _prefixPatterns = value; OnPropertyChanged(); }
        }
        public ObservableCollection<DescriptionMapping> DescriptionLookupTable
        {
            get => _descriptionLookupTable;
            set { _descriptionLookupTable = value; OnPropertyChanged(); }
        }
        public string DefaultPattern
        {
            get => _defaultPattern;
            set { _defaultPattern = value; OnPropertyChanged(); }
        }

        public CogoPointNamingSettings()
        {
            PrefixPatterns = new ObservableCollection<PrefixPattern>();
            DescriptionLookupTable = new ObservableCollection<DescriptionMapping>();
            DefaultPattern = "{Description}-{Counter:4}";
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    
    [Serializable]
    public class CogoPointNamingSettings_Serializable
    {
        public List<PrefixPattern> PrefixPatterns { get; set; } = new List<PrefixPattern>();
        public List<DescriptionMapping> DescriptionLookupTable { get; set; } = new List<DescriptionMapping>();
        public string DefaultPattern { get; set; }
    }

    [Serializable]
    public class PrefixPattern : INotifyPropertyChanged
    {
        private string _prefix;
        private string _pattern;

        public string Prefix
        {
            get => _prefix;
            set { _prefix = value; OnPropertyChanged(); }
        }
        public string Pattern
        {
            get => _pattern;
            set { _pattern = value; OnPropertyChanged(); }
        }
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    [Serializable]
    public class DescriptionMapping : INotifyPropertyChanged
    {
        private string _description;
        private string _pattern;
        
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }
        public string Pattern
        {
            get => _pattern;
            set { _pattern = value; OnPropertyChanged(); }
        }
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    
    #endregion
    
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
