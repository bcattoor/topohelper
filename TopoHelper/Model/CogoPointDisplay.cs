using Autodesk.Civil.DatabaseServices;
using System.ComponentModel;

namespace Infrabel.AutodeskPlatform.TopoHelper.Model
{
    public class CogoPointDisplay : INotifyPropertyChanged
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

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
