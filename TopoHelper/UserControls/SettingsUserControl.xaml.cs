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
            txtSearch.Text = "Type here to search: ";
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

        private void TxtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataGrid == null || sender == null) return;
            if (sender is TextBox)
                if (!string.IsNullOrWhiteSpace((sender as TextBox).Text))
                {
                    txtSearch.Text = $"{DataGrid.Items.Count} items";
                }
                else
                {
                    txtSearch.Text = "Type here to search: ";
                }
        }
    }
}