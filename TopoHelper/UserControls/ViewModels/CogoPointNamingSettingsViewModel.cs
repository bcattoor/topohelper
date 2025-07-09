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
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
