using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TopoHelper.AutoCAD;
using TopoHelper.Properties;

// ReSharper disable MemberCanBePrivate.Global

namespace TopoHelper.UserControls.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        #region Private Fields

        private static readonly Settings SettingsDefault = Settings.Default;
        private RelayCommand _cancel, _clearSearch, _reloadSettings, _save;
        private CollectionViewSource _dataGridView;
        private string _searchString;

        #endregion

        #region Public Constructors

        public SettingsViewModel()
        {
            RefreshView();

            MenuItems = new ObservableCollection<AutoCadCommandViewModel>();
            var commandStrings = Assembly.GetExecutingAssembly().GetCommands(true);
            var menuItemsList = new List<AutoCadCommandViewModel>();

            foreach (var s in commandStrings)
                menuItemsList.Add(new AutoCadCommandViewModel { CommandName = s });

            // edit 02/02/22 BJORN CATTOOR 
            // don't add the command "settings" to the list
            foreach (var command in _ = menuItemsList.Where(x => !x.FriendlyName.Equals("Settings")).OrderBy(x => x.FriendlyName))
                MenuItems.Add(command);

            RaisePropertyChanged(nameof(MenuItems));
        }

        #endregion

        #region Public Properties

        public ICommand CancelCommand =>
            _cancel ?? (_cancel = new RelayCommand(Cancel,
                CanCancel));

        public ICommand ClearSearchCommand =>
            _clearSearch ?? (_clearSearch = new RelayCommand(ClearSearch, CanClearSearch));

        public bool ClearSearchIsVisible { get => !string.IsNullOrEmpty(SearchString); }

        public CollectionViewSource DataGridView
        {
            get => _dataGridView;
            set { _dataGridView = value; RaisePropertyChanged(nameof(DataGridView)); }
        }

        public ObservableCollection<AutoCadCommandViewModel> MenuItems { get; set; }
        public Action OnCancel { get; set; }

        public ICommand ReloadSettingsCommand =>
            _reloadSettings ?? (_reloadSettings = new RelayCommand(ReloadSettings,
                CanReloadSettings));

        public ICommand SaveCommand =>
            _save ?? (_save = new RelayCommand(Save,
                CanSave));

        public string SearchString
        {
            get => _searchString;
            set
            {
                if (!value.Equals(_searchString))
                {
                    FilterView(value);
                }
                _searchString = value;
                RaisePropertyChanged(nameof(SearchString));
                RaisePropertyChanged(nameof(ClearSearchIsVisible));
            }
        }

        #endregion

        #region Public Methods

        public static bool CanCancel(object parameter)
        {
            return true;
        }

        public void Cancel(object parameter)
        {
            /*Cancel Stuff*/
            if (((DataGridView.Source as ObservableCollection<SettingsEntryViewModel>) ?? throw new InvalidOperationException()).Any(p => p.IsDirty))
            {
                // Warn user there are still changes pending to be saved.
                var res = MessageBox.Show("Some changes have not yet been saved, do you really want to cancel.", "Cancel changes?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.No)
                    return;
            }

            SettingsDefault.Reload();
            OnCancel?.Invoke();
        }

        public bool CanReloadSettings(object parameters)
        {
            if (DataGridView.Source == null)
                return false;
            return true;
        }

        public bool CanSave(object parameter)
        {
            if ((DataGridView.Source) == null)
                return false;
            var collection = DataGridView.Source as ObservableCollection<SettingsEntryViewModel>;
            Debug.Assert(collection != null, nameof(collection) + " != null");
            return !collection.Any(p => p.HasErrors) && collection.Any(p => p.IsDirty);
        }

        public void RefreshView()
        {
            var collection = SettingsDefault.Properties;

            var type = typeof(Settings);

            if (DataGridView == null)
            {
                DataGridView = new CollectionViewSource
                {
                    Source = new ObservableCollection<SettingsEntryViewModel>()
                };
                DataGridView.SortDescriptions.Add(new SortDescription(nameof(SettingsEntryViewModel.Name), ListSortDirection.Ascending));
            }

            // Remove items from collection.
            if (((ObservableCollection<SettingsEntryViewModel>)DataGridView.Source).Count != 0)
                ((ObservableCollection<SettingsEntryViewModel>)DataGridView.Source)?.Clear();

            foreach (SettingsProperty item in collection)
            {
                var property = type.GetProperty(item.Name);
                // Don't add read-only Properties
                Debug.Assert(property != null, nameof(property) + " != null");
                if (!property.CanWrite) continue;
                var val = type.GetProperty(item.Name)?.GetValue(SettingsDefault);
                var t = type.GetProperty(item.Name)?.PropertyType;

                ((ObservableCollection<SettingsEntryViewModel>)DataGridView.Source)?.Add(new SettingsEntryViewModel(t, val) { Name = item.Name });
            }
        }

        public void ReloadSettings(object paramers)
        {
            RefreshView();
        }

        public void Save(object parameter)
        {
            if (!(DataGridView.Source is ObservableCollection<SettingsEntryViewModel> collection)) return;

            var saved = false;
            /*Save Stuff*/
            foreach (var item in collection)
            {
                if (!item.IsDirty || item.HasErrors) continue;
                item.SaveValueToObject(SettingsDefault);
                saved = true;
            }

            if (!saved) return;
            SettingsDefault.Save(); SettingsDefault.Reload(); RefreshView();
        }

        #endregion

        #region Private Methods

        private bool CanClearSearch(object obj)
        {
            return !string.IsNullOrEmpty(SearchString);
        }

        private void ClearSearch(object obj)
        {
            SearchString = "";
        }

        private void FilterView(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                DataGridView.View.Filter = null;
            else
                DataGridView.View.Filter = item =>
                {
                    if (item == null) return false;
                    if (!(item is SettingsEntryViewModel)) return false;
                    if (((SettingsEntryViewModel)item).Name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    return ((SettingsEntryViewModel)item).ValueString.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
                };
        }

        #endregion
    }
}