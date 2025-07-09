using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Infrabel.AutodeskPlatform.TopoHelper.Model.Naming;

namespace Infrabel.AutodeskPlatform.TopoHelper.UserControls.ViewModels
{
    public class CogoPointNamingSettingsViewModel : INotifyPropertyChanged
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

        public CogoPointNamingSettingsViewModel()
        {
            PrefixPatterns = new ObservableCollection<PrefixPattern>();
            DescriptionLookupTable = new ObservableCollection<DescriptionMapping>();
            DefaultPattern = "{Description}-{Counter:4}";
        }

        public void CopyFrom(CogoPointNamingSettings settings)
        {
            DefaultPattern = settings.DefaultPattern;
            
            PrefixPatterns.Clear();
            foreach (var pattern in settings.PrefixPatterns)
            {
                PrefixPatterns.Add(new PrefixPattern 
                { 
                    Prefix = pattern.Prefix, 
                    Pattern = pattern.Pattern 
                });
            }
            
            DescriptionLookupTable.Clear();
            foreach (var mapping in settings.DescriptionLookupTable)
            {
                DescriptionLookupTable.Add(new DescriptionMapping 
                { 
                    Description = mapping.Description, 
                    Pattern = mapping.Pattern 
                });
            }
        }

        public CogoPointNamingSettings ToModel()
        {
            var settings = new CogoPointNamingSettings();
            settings.DefaultPattern = DefaultPattern;
            
            settings.PrefixPatterns.Clear();
            foreach (var pattern in PrefixPatterns)
            {
                settings.PrefixPatterns.Add(new PrefixPattern 
                { 
                    Prefix = pattern.Prefix, 
                    Pattern = pattern.Pattern 
                });
            }
            
            settings.DescriptionLookupTable.Clear();
            foreach (var mapping in DescriptionLookupTable)
            {
                settings.DescriptionLookupTable.Add(new DescriptionMapping 
                { 
                    Description = mapping.Description, 
                    Pattern = mapping.Pattern 
                });
            }
            
            return settings;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
