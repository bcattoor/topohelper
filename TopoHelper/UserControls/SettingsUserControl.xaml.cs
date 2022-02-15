using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace TopoHelper.UserControls
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsUserControl
    {
        #region Public Constructors

        public SettingsUserControl()
        {
            InitializeComponent();
            txtSearch.Text = "Type here to search: ";
        }

        #endregion

        #region Private Methods

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

        private void MenuItem_Click_Navigate_Url(object sender, RoutedEventArgs e)
        {
            if (sender == null) return;

            try
            {
                if (sender is MenuItem)
                {
                    var t = (sender as MenuItem).ToolTip.ToString();
                    if (t != "Over deze applicatie.")
                        Process.Start(t);
                    else
                    {
                        var version = $"Version {MyApplication.GetInformationalVersion()} / {MyApplication.GetAssemblyVersion()} / {MyApplication.GetAssemblyFileVersion()}";
                        // TODO: Show application version info etc ...
                        Clipboard.SetText(version);
                        MessageBox.Show($"{version}\r\nDe bovenstaande info werd gekopieerd naar het klembord.");
                    }
                }
            }
            catch (System.Exception exeption)
            {
                MessageBox.Show(exeption.Message);
            }
        }

        private void TxtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataGrid == null || sender == null) return;
            if (sender is TextBox)
                if (!string.IsNullOrWhiteSpace((sender as TextBox).Text))
                {
                    txtSearch.Text = $"Search resulted in {DataGrid.Items.Count} items";
                }
                else
                {
                    txtSearch.Text = "Type here to search: ";
                }
        }

        #endregion
    }
}