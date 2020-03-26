using System.Windows;
using System.Windows.Controls;

namespace TopoHelper.UserControls
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsUserControl
    {
        public SettingsUserControl()
        {
            InitializeComponent();
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column.Header.ToString() == "Errors")
                e.Column.Visibility = Visibility.Collapsed;
            if (e.Column.Header.ToString() == "Type")
                e.Column.Visibility = Visibility.Collapsed;
            if (e.Column.Header.ToString() == "IsDirty")
                e.Column.Visibility = Visibility.Collapsed;
            if (e.Column.Header.ToString() == "IsNew")
                e.Column.Visibility = Visibility.Collapsed;
        }
    }
}