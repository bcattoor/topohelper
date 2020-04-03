using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TopoHelper.Properties;

// ReSharper disable MemberCanBePrivate.Global

namespace TopoHelper.UserControls.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        #region Private Fields

        private static readonly Settings SettingsDefault = Settings.Default;
        private RelayCommand _cancel;
        private RelayCommand _reloadSettings;
        private RelayCommand _save;
        private string _filter;
        private CollectionViewSource _dataGridView;

        #endregion

        #region Public Constructors

        public SettingsViewModel()
        {
            RefreshView();
        }

        #endregion

        #region Public Properties

        public string Filter {
            get => _filter;
            set {
                if (!value.Equals(_filter))
                {
                    FilterView(value);
                }
                _filter = value;
            }
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

        public ICommand CancelCommand =>
            _cancel ?? (_cancel = new RelayCommand(Cancel,
                CanCancel));

        public CollectionViewSource DataGridView {
            get => _dataGridView;
            set { _dataGridView = value; RaisePropertyChanged(nameof(DataGridView)); }
        }

        public Action OnCancel { get; set; }

        public ICommand ReloadSettingsCommand =>
            _reloadSettings ?? (_reloadSettings = new RelayCommand(ReloadSettings,
                CanReloadSettings));

        public ICommand SaveCommand =>
            _save ?? (_save = new RelayCommand(Save,
                CanSave));

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
            var collection = DataGridView.Source as ObservableCollection<SettingsEntryViewModel>;
            Debug.Assert(collection != null, nameof(collection) + " != null");
            return collection.Any(p => p.HasErrors) || collection.Any(p => p.IsDirty);
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
    }
}