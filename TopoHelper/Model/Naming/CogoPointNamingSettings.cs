using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Infrabel.AutodeskPlatform.TopoHelper.Model.Naming
{
    #region COGO Naming Model Classes

    public class CogoPointNamingSettings
    {
        public ObservableCollection<PrefixPattern> PrefixPatterns { get; set; } = new ObservableCollection<PrefixPattern>();
        public ObservableCollection<DescriptionMapping> DescriptionLookupTable { get; set; } = new ObservableCollection<DescriptionMapping>();
        public string DefaultPattern { get; set; } = "{Description}-{Counter:4}";
    }

    [Serializable]
    public class CogoPointNamingSettings_Serializable
    {
        public List<PrefixPattern> PrefixPatterns { get; set; } = new List<PrefixPattern>();
        public List<DescriptionMapping> DescriptionLookupTable { get; set; } = new List<DescriptionMapping>();
        public string DefaultPattern { get; set; }
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

}
