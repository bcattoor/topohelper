using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace TopoHelper.UserControls.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        #region Private Fields

        private static readonly Properties.Settings settings_default = Properties.Settings.Default;
        private RelayCommand _cancel;
        private RelayCommand _reloadSettings;
        private RelayCommand _save;
        private string _filter;
        private CollectionViewSource dataGridView;

        #endregion

        #region Public Constructors

        public SettingsViewModel()
        {
            RefreshView();
        }

        #endregion

        #region Public Properties

        public string Filter {
            get {
                return _filter;
            }
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
                DataGridView.View.Filter = (item) =>
                {
                    if (item == null) return false;
                    if (item is SettingsEntryViewModel)
                    {
                        if ((item as SettingsEntryViewModel).Name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                        if ((item as SettingsEntryViewModel).ValueString.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    }
                    return false;
                };
        }

        public ICommand CancelCommand {
            get {
                if (_cancel == null)
                {
                    _cancel = new RelayCommand(p => Cancel(p),
                        p => CanCancel(p));
                }
                return _cancel;
            }
        }

        public CollectionViewSource DataGridView { get { return dataGridView; } set { dataGridView = value; RaisePropertyChanged(nameof(DataGridView)); } }

        public Action OnCancel { get; set; }

        public ICommand ReloadSettingsCommand {
            get {
                if (_reloadSettings == null)
                {
                    _reloadSettings = new RelayCommand(p => ReloadSettings(p),
                        p => CanReloadSettings(p));
                }
                return _reloadSettings;
            }
        }

        public ICommand SaveCommand {
            get {
                if (_save == null)
                {
                    _save = new RelayCommand(p => Save(p),
                        p => CanSave(p));
                }
                return _save;
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
            if ((DataGridView.Source as ObservableCollection<SettingsEntryViewModel>).Where(p => p.IsDirty).Any())
            {
                // Warn user there are still changes pending to be saved.
                var res = MessageBox.Show("Some changes have not yet been saved, do you really want to cancel.", "Cancel changes?", MessageBoxButton.YesNo, MessageBoxImage.Warning); ;
                if (res == MessageBoxResult.No)
                    return;
            }

            settings_default.Reload();
            OnCancel?.Invoke();
        }

        public bool CanReloadSettings(object parameters)
        {
            if (DataGridView.Source == null)
                return false;
            var collection = DataGridView.Source as ObservableCollection<SettingsEntryViewModel>;
            if (collection.Where(p => p.HasErrors).Any())
                return true;

            if (collection.Where(p => p.IsDirty).Any())
                return true;

            return false;
        }

        public bool CanSave(object parameter)
        {
            if ((DataGridView.Source) == null)
                return false;
            var collection = DataGridView.Source as ObservableCollection<SettingsEntryViewModel>;
            if (collection.Where(p => p.HasErrors).Any())
                return false;

            if (collection.Where(p => p.IsDirty).Any())
                return true;
            return false;
        }

        public void RefreshView()
        {
            var collection = settings_default.Properties;

            var type = typeof(Properties.Settings);

            if (DataGridView == null)
            {
                DataGridView = new CollectionViewSource
                {
                    Source = new ObservableCollection<SettingsEntryViewModel>()
                };
                DataGridView.SortDescriptions.Add(new SortDescription(nameof(SettingsEntryViewModel.Name), ListSortDirection.Ascending));
            }

            // Remove items from collection.
            if ((DataGridView.Source as ObservableCollection<SettingsEntryViewModel>).Count != 0)
                (DataGridView.Source as ObservableCollection<SettingsEntryViewModel>)?.Clear();

            foreach (SettingsProperty item in collection)
            {
                var property = type.GetProperty(item.Name);
                // Don't add read-only Properties
                if (!property.CanWrite) continue;
                var val = type.GetProperty(item.Name).GetValue(settings_default);
                var t = type.GetProperty(item.Name).PropertyType;

                (DataGridView.Source as ObservableCollection<SettingsEntryViewModel>).Add(new SettingsEntryViewModel(t, val) { Name = item.Name });
            }
        }

        public void ReloadSettings(object paramers)
        {
            RefreshView();
        }

        public void Save(object parameter)
        {
            if (!(DataGridView.Source is ObservableCollection<SettingsEntryViewModel> collection)) return;

            bool saved = false;
            /*Save Stuff*/
            foreach (var item in collection)
            {
                if (item.IsDirty && !item.HasErrors)
                {
                    item.SaveValueToObject(settings_default);
                    saved = true;
                }
            }
            if (saved)
            {
                settings_default.Save(); settings_default.Reload(); RefreshView();
            }
        }

        #endregion
    }
}