using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Infrabel.AutodeskPlatform.TopoHelper.Model;
using Autodesk.Civil.DatabaseServices;

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


}
