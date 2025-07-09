using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Infrabel.AutodeskPlatform.TopoHelper.Model.Naming
{
    [Serializable]
    public class PrefixPattern : INotifyPropertyChanged
    {
        private string _prefix;
        private string _pattern;

        public PrefixPattern()
        {
        }

        public PrefixPattern(string prefix, string pattern)
        {
            _prefix = prefix;
            _pattern = pattern;
        }

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
}
