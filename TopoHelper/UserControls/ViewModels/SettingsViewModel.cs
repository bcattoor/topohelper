using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Infrabel.AutodeskPlatform.TopoHelper.Model;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Infrabel.AutodeskPlatform.TopoHelper.Properties;
using Infrabel.AutodeskPlatform.AutoCADCommon.Extensions;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Threading;


namespace Infrabel.AutodeskPlatform.TopoHelper.UserControls.ViewModels
{
    /// <summary>
    /// ViewModel for the Settings user control.
    /// Note: Ensure that the corresponding DataGrid in the XAML file has virtualization enabled
    /// for optimal performance with large datasets. Example:
    /// <DataGrid VirtualizingPanel.IsVirtualizing="True"
    ///           VirtualizingPanel.VirtualizationMode="Recycling"
    ///           EnableRowVirtualization="True"
    ///           EnableColumnVirtualization="True">
    /// </summary>
    public class SettingsViewModel : BaseViewModel, IDisposable
    {
        #region Private Fields

        private static readonly Settings SettingsDefault = Settings.Default;
        private RelayCommand _cancel, _clearSearch, _reloadSettings, _save;
        private RelayCommand _addPrefixPattern, _addDescriptionMapping;
        private CollectionViewSource _dataGridView;
        private string _searchString;
        private CogoPointNamingSettings _namingSettings;
        private bool _isCogoNamingValid = true;
        private string _cogoNamingValidationError = "";
        private bool _isLoading;

        #endregion

        #region Public Properties

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    RaisePropertyChanged(nameof(IsLoading));
                }
            }
        }

        private RelayCommand _processSelectedCogoPointsCommand;
        public ICommand ProcessSelectedCogoPointsCommand => _processSelectedCogoPointsCommand ?? 
            (_processSelectedCogoPointsCommand = new RelayCommand(ProcessSelectedCogoPoints));

        private RelayCommand _processSelectedBlocksCommand;
        public ICommand ProcessSelectedBlocksCommand => _processSelectedBlocksCommand ?? 
            (_processSelectedBlocksCommand = new RelayCommand(ProcessSelectedBlocks));

        #endregion

        #region IDisposable Implementation

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _cts?.Dispose();
                    SafeCogoPoints.Clear();
                    SafeBlocks.Clear();
                    MenuItems?.Clear();
                }

                // Free unmanaged resources (if any)

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SettingsViewModel()
        {
            Dispose(false);
        }

        #endregion

        #region Constructor

        public SettingsViewModel()
        {
            try
            {
                RefreshView();
                LoadCogoPointNamingSettings();

                CogoPoints = new ObservableCollection<CogoPointDisplay>();
                RefreshCogoPointsCommand = new RelayCommand(async _ => await LoadCogoPointsAsync());

                Blocks = new RangeObservableCollection<BlockDisplay>();
                RefreshBlocksCommand = new RelayCommand(async _ => await LoadBlocksAsync());

                MenuItems = new ObservableCollection<AutoCadCommandViewModel>();
                var commandStrings = System.Reflection.Assembly.GetExecutingAssembly().GetCommands(true);
                var menuItemsList = new List<AutoCadCommandViewModel>();
                foreach (var s in commandStrings)
                {
                    menuItemsList.Add(new AutoCadCommandViewModel { CommandName = s });
                }
                foreach (var command in menuItemsList.Where(x => x.FriendlyName != null && !x.FriendlyName.Equals("Settings")).OrderBy(x => x.FriendlyName))
                {
                    MenuItems.Add(command);
                }
                RaisePropertyChanged(nameof(MenuItems));
                ValidateCogoNaming();

                // Initialize CancellationTokenSource and CancelOperationCommand
                _cts = new CancellationTokenSource();
                CancelOperationCommand = new RelayCommand(_ => CancelLoading());

                // Asynchronously load COGO points and blocks
                _ = LoadCogoPointsAsync().ConfigureAwait(false);
                _ = LoadBlocksAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SettingsViewModel constructor: {ex.Message}");
                StatusMessage = "Error initializing SettingsViewModel. Please try restarting the application.";
            }
        }
        #endregion

        #region Public Properties

        private ObservableCollection<CogoPointDisplay> _cogoPoints;
        public ObservableCollection<CogoPointDisplay> CogoPoints
        {
            get => _cogoPoints;
            set
            {
                _cogoPoints = value;
                RaisePropertyChanged(nameof(CogoPoints));
            }
        }
        public ICommand RefreshCogoPointsCommand { get; }
        public RangeObservableCollection<BlockDisplay> Blocks { get; set; } = new RangeObservableCollection<BlockDisplay>();
        public ICommand RefreshBlocksCommand { get; }

        // Ensure CogoPoints is never null
        public ObservableCollection<CogoPointDisplay> SafeCogoPoints
        {
            get => CogoPoints ?? (CogoPoints = new ObservableCollection<CogoPointDisplay>());
        }


        // Ensure Blocks is never null
        public RangeObservableCollection<BlockDisplay> SafeBlocks
        {
            get => Blocks ?? (Blocks = new RangeObservableCollection<BlockDisplay>());
        }
        public ICommand CancelCommand => _cancel ?? (_cancel = new RelayCommand(Cancel));
        public ICommand ClearSearchCommand => _clearSearch ?? (_clearSearch = new RelayCommand(ClearSearch));
        public bool ClearSearchIsVisible => !string.IsNullOrEmpty(SearchString);
        public CollectionViewSource DataGridView
        {
            get => _dataGridView ?? (_dataGridView = new CollectionViewSource());
            set
            {
                if (_dataGridView != value)
                {
                    _dataGridView = value;
                    RaisePropertyChanged(nameof(DataGridView));
                }
            }
        }
        public ObservableCollection<AutoCadCommandViewModel> MenuItems { get; set; }
        public Action OnCancel { get; set; }
        public ICommand ReloadSettingsCommand => _reloadSettings ?? (_reloadSettings = new RelayCommand(ReloadSettings));
        public ICommand SaveCommand => _save ?? (_save = new RelayCommand(Save, CanSave));
        public ICommand CancelOperationCommand { get; private set; }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; RaisePropertyChanged(nameof(StatusMessage)); }
        }

        public string SearchString
        {
            get => _searchString;
            set { _searchString = value; FilterView(value); RaisePropertyChanged(nameof(SearchString)); RaisePropertyChanged(nameof(ClearSearchIsVisible)); }
        }

        public CogoPointNamingSettings NamingSettings
        {
            get => _namingSettings;
            set { _namingSettings = value; RaisePropertyChanged(nameof(NamingSettings)); ValidateCogoNaming(); }
        }

        public bool IsCogoNamingValid
        {
            get => _isCogoNamingValid;
            private set { _isCogoNamingValid = value; RaisePropertyChanged(nameof(IsCogoNamingValid)); }
        }

        public string CogoNamingValidationError
        {
            get => _cogoNamingValidationError;
            private set { _cogoNamingValidationError = value; RaisePropertyChanged(nameof(CogoNamingValidationError)); }
        }

        public ICommand AddPrefixPatternCommand => _addPrefixPattern ?? (_addPrefixPattern = new RelayCommand(param =>
        {
            var newPattern = new PrefixPattern { Prefix = "New Prefix", Pattern = "New-Pattern-{Counter:3}" };
            NamingSettings.PrefixPatterns.Add(newPattern);
            HookPropertyChanged(newPattern);
        }));

        public ICommand AddDescriptionMappingCommand => _addDescriptionMapping ?? (_addDescriptionMapping = new RelayCommand(param =>
        {
            var newMapping = new DescriptionMapping { Description = "New Description", Pattern = "New-Pattern-{Counter:3}" };
            NamingSettings.DescriptionLookupTable.Add(newMapping);
            HookPropertyChanged(newMapping);
        }));

        #region Classificatie Instellingen Properties en Commands

        private ObservableCollection<string> _knownBlockNames;
        public ObservableCollection<string> KnownBlockNames
        {
            get
            {
                if (_knownBlockNames == null)
                {
                    _knownBlockNames = new ObservableCollection<string>();
                    foreach (string item in Settings.Default.FromBlockToCogo_KnownRealBlockNames_ToSetCogoProperties)
                    {
                        _knownBlockNames.Add(item);
                    }
                    _knownBlockNames.CollectionChanged += KnownBlockNames_CollectionChanged;
                }
                return _knownBlockNames;
            }
        }

        private ObservableCollection<string> _knownLayers;
        public ObservableCollection<string> KnownLayers
        {
            get
            {
                if (_knownLayers == null)
                {
                    _knownLayers = new ObservableCollection<string>();
                    foreach (string item in Settings.Default.FromBlockToCogo_Known_Layer_Names_ToSetCogoProperties)
                    {
                        _knownLayers.Add(item);
                    }
                    _knownLayers.CollectionChanged += KnownLayers_CollectionChanged;
                }
                return _knownLayers;
            }
        }

        private ObservableCollection<string> _knownAttributes;
        public ObservableCollection<string> KnownAttributes
        {
            get
            {
                if (_knownAttributes == null)
                {
                    _knownAttributes = new ObservableCollection<string>();
                    foreach (string item in Settings.Default.FromBlockToCogo_Known_Block_Attributes_ToSetCogoProperties)
                    {
                        _knownAttributes.Add(item);
                    }
                    _knownAttributes.CollectionChanged += KnownAttributes_CollectionChanged;
                }
                return _knownAttributes;
            }
        }

        private string _selectedKnownBlockName;
        public string SelectedKnownBlockName
        {
            get => _selectedKnownBlockName;
            set { _selectedKnownBlockName = value; RaisePropertyChanged(nameof(SelectedKnownBlockName)); }
        }

        private string _selectedKnownLayer;
        public string SelectedKnownLayer
        {
            get => _selectedKnownLayer;
            set { _selectedKnownLayer = value; RaisePropertyChanged(nameof(SelectedKnownLayer)); }
        }

        private string _selectedKnownAttribute;
        public string SelectedKnownAttribute
        {
            get => _selectedKnownAttribute;
            set { _selectedKnownAttribute = value; RaisePropertyChanged(nameof(SelectedKnownAttribute)); }
        }

        private RelayCommand _addKnownBlockNameCommand;
        public ICommand AddKnownBlockNameCommand => _addKnownBlockNameCommand ?? (_addKnownBlockNameCommand = new RelayCommand(param =>
        {
            KnownBlockNames.Add("Nieuw Block");
            SetUnsavedChangesStatus();
        }));

        private RelayCommand _removeKnownBlockNameCommand;
        public ICommand RemoveKnownBlockNameCommand => _removeKnownBlockNameCommand ?? (_removeKnownBlockNameCommand = new RelayCommand(param =>
        {
            if (SelectedKnownBlockName != null)
            {
                KnownBlockNames.Remove(SelectedKnownBlockName);
                SetUnsavedChangesStatus();
            }
        }));

        private RelayCommand _addKnownLayerCommand;
        public ICommand AddKnownLayerCommand => _addKnownLayerCommand ?? (_addKnownLayerCommand = new RelayCommand(param =>
        {
            KnownLayers.Add("Nieuwe Laag");
            SetUnsavedChangesStatus();
        }));

        private RelayCommand _removeKnownLayerCommand;
        public ICommand RemoveKnownLayerCommand => _removeKnownLayerCommand ?? (_removeKnownLayerCommand = new RelayCommand(param =>
        {
            if (SelectedKnownLayer != null)
            {
                KnownLayers.Remove(SelectedKnownLayer);
                SetUnsavedChangesStatus();
            }
        }));

        private RelayCommand _addKnownAttributeCommand;
        public ICommand AddKnownAttributeCommand => _addKnownAttributeCommand ?? (_addKnownAttributeCommand = new RelayCommand(param =>
        {
            KnownAttributes.Add("Nieuw Attribuut");
            SetUnsavedChangesStatus();
        }));

        private RelayCommand _removeKnownAttributeCommand;
        public ICommand RemoveKnownAttributeCommand => _removeKnownAttributeCommand ?? (_removeKnownAttributeCommand = new RelayCommand(param =>
        {
            if (SelectedKnownAttribute != null)
            {
                KnownAttributes.Remove(SelectedKnownAttribute);
                SetUnsavedChangesStatus();
            }
        }));

        private void KnownBlockNames_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SetUnsavedChangesStatus();
        }

        private void KnownLayers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SetUnsavedChangesStatus();
        }

        private void KnownAttributes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SetUnsavedChangesStatus();
        }

        #endregion

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the status message to indicate unsaved changes.
        /// </summary>
        public void SetUnsavedChangesStatus()
        {
            StatusMessage = "Unsaved changes pending. Remember to save.";
        }

        /// <summary>
        /// Cancels the current operation and reloads the default settings.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        public void Cancel(object parameter)
        {
            if ((DataGridView.Source as ObservableCollection<SettingsEntryViewModel>)?.Any(p => p.IsDirty) == true)
            {
                var res = MessageBox.Show("Changes pending. Cancel anyway?", "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.No) return;
            }
            SettingsDefault.Reload();
            OnCancel?.Invoke();
        }

        /// <summary>
        /// Saves the current settings.
        /// </summary>
        /// <param name="parameter">The command parameter.</param>
        public void Save(object parameter)
        {
            if (DataGridView.Source is ObservableCollection<SettingsEntryViewModel> collection)
            {
                foreach (var item in collection.Where(i => i.IsDirty && !i.HasErrors))
                {
                    item.SaveValueToObject(SettingsDefault);
                    item.ResetDirtyState();
                }
            }
            SaveCogoPointNamingSettings();
            SaveClassificationSettings();
            SettingsDefault.Save();
            SettingsDefault.Reload();
            RefreshView();
            StatusMessage = "Settings saved successfully.";
        }

        private void SaveClassificationSettings()
        {
            // Opslaan van KnownBlockNames
            Settings.Default.FromBlockToCogo_KnownRealBlockNames_ToSetCogoProperties.Clear();
            foreach (var item in KnownBlockNames)
            {
                Settings.Default.FromBlockToCogo_KnownRealBlockNames_ToSetCogoProperties.Add(item);
            }

            // Opslaan van KnownLayers
            Settings.Default.FromBlockToCogo_Known_Layer_Names_ToSetCogoProperties.Clear();
            foreach (var item in KnownLayers)
            {
                Settings.Default.FromBlockToCogo_Known_Layer_Names_ToSetCogoProperties.Add(item);
            }

            // Opslaan van KnownAttributes
            Settings.Default.FromBlockToCogo_Known_Block_Attributes_ToSetCogoProperties.Clear();
            foreach (var item in KnownAttributes)
            {
                Settings.Default.FromBlockToCogo_Known_Block_Attributes_ToSetCogoProperties.Add(item);
            }
        }

        public void ReloadSettings(object parameter)
        {
            RefreshView();
            LoadCogoPointNamingSettings();
        }

        public void RefreshView()
        {
            var settingsEntries = new List<SettingsEntryViewModel>();
            var type = typeof(Settings);

            foreach (SettingsProperty item in SettingsDefault.Properties)
            {
                var property = type.GetProperty(item.Name);
                if (property == null || !property.CanWrite) continue;
                settingsEntries.Add(new SettingsEntryViewModel(property.PropertyType, property.GetValue(SettingsDefault), this) { Name = item.Name });
            }

            var newSource = new RangeObservableCollection<SettingsEntryViewModel>();
            newSource.AddRange(settingsEntries);

            if (DataGridView == null)
            {
                DataGridView = new CollectionViewSource { Source = newSource };
                DataGridView.SortDescriptions.Add(new SortDescription(nameof(SettingsEntryViewModel.Name), ListSortDirection.Ascending));
            }
            else
            {
                DataGridView.Source = newSource;
            }

            RaisePropertyChanged(nameof(DataGridView));
        }
            #endregion

            #region Private Methods

            private bool CanSave(object parameter)
        {
            var source = DataGridView?.Source as ObservableCollection<SettingsEntryViewModel>;
            bool hasValidChanges = source?.Any(p => p.IsDirty && !p.HasErrors) ?? false;
            return IsCogoNamingValid && (hasValidChanges || true); // Allow saving COGO settings anytime they're valid
        }

        private void FilterView(string value)
        {
            if (DataGridView?.View == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                DataGridView.View.Filter = null;
            }
            else
            {
                DataGridView.View.Filter = item =>
                    (item as SettingsEntryViewModel)?.Name?.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        private void ClearSearch(object p) => SearchString = "";

        private void ValidateCogoNaming()
        {
            if (NamingSettings == null) return;
            if (NamingSettings.PrefixPatterns.Any(p => string.IsNullOrWhiteSpace(p.Prefix) || string.IsNullOrWhiteSpace(p.Pattern)) ||
                NamingSettings.DescriptionLookupTable.Any(d => string.IsNullOrWhiteSpace(d.Description) || string.IsNullOrWhiteSpace(d.Pattern)) ||
                string.IsNullOrWhiteSpace(NamingSettings.DefaultPattern))
            {
                IsCogoNamingValid = false;
                CogoNamingValidationError = "All Prefix, Description, and Pattern fields are required.";
            }
            else
            {
                IsCogoNamingValid = true;
                CogoNamingValidationError = "";
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private void CogoSetting_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ValidateCogoNaming();
            StatusMessage = "Unsaved changes in COGO Naming.";
            CommandManager.InvalidateRequerySuggested();
        }
        private void HookPropertyChanged(INotifyPropertyChanged item) => item.PropertyChanged += CogoSetting_PropertyChanged;

        private void LoadCogoPointNamingSettings()
        {
            NamingSettings = CogoPointNamingEngine.LoadSettings();
            NamingSettings.PropertyChanged += (s, e) => ValidateCogoNaming();
            foreach (var item in NamingSettings.PrefixPatterns) HookPropertyChanged(item);
            foreach (var item in NamingSettings.DescriptionLookupTable) HookPropertyChanged(item);
            NamingSettings.PrefixPatterns.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null) foreach (INotifyPropertyChanged item in e.NewItems) HookPropertyChanged(item);
                ValidateCogoNaming();
                StatusMessage = "Unsaved changes in COGO Naming.";
                CommandManager.InvalidateRequerySuggested();
            };
            NamingSettings.DescriptionLookupTable.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null) foreach (INotifyPropertyChanged item in e.NewItems) HookPropertyChanged(item);
                ValidateCogoNaming();
                StatusMessage = "Unsaved changes in COGO Naming.";
                CommandManager.InvalidateRequerySuggested();
            };
        }

        private void SaveCogoPointNamingSettings() => CogoPointNamingEngine.SaveSettings(NamingSettings);
        private CancellationTokenSource _cts;

        private async Task LoadCogoPointsAsync(int retryCount = 3)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    IsLoading = true;
                    var cogoPointsList = await Task.Run(() =>
                    {
                        var civilDoc = CivilApplication.ActiveDocument;
                        var acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                        if (civilDoc == null || acDoc == null)
                        {
                            throw new InvalidOperationException("Civil document or AutoCAD document is null");
                        }

                        var db = acDoc.Database;
                        if (db == null)
                        {
                            throw new InvalidOperationException("Database is null");
                        }

                        var points = new List<CogoPointDisplay>();
                        using (var tr = db.TransactionManager.StartTransaction())
                        {
                            foreach (ObjectId cogoPointId in civilDoc.CogoPoints)
                            {
                                _cts.Token.ThrowIfCancellationRequested();
                                var cogoPoint = tr.GetObject(cogoPointId, OpenMode.ForRead) as CogoPoint;
                                if (cogoPoint != null)
                                {
                                    try
                                    {
                                        points.Add(new CogoPointDisplay
                                        {
                                            PointNumber = cogoPoint.PointNumber,
                                            Name = cogoPoint.PointName,
                                            Easting = cogoPoint.Easting,
                                            Northing = cogoPoint.Northing,
                                            Elevation = cogoPoint.Elevation,
                                            RawDescription = cogoPoint.RawDescription,
                                            LabelStyleName = GetStyleName(tr, cogoPoint.LabelStyleId),
                                            PointStyleName = GetStyleName(tr, cogoPoint.StyleId),
                                            LayerName = GetLayerName(tr, cogoPoint.LayerId)
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error loading COGO point {cogoPoint.PointNumber}: {ex.Message}");
                                    }
                                }
                            }
                            tr.Commit();
                        }
                        return points;
                    }, _cts.Token);

                    // Update UI on the main thread
                    if (cogoPointsList.Count > 0)
                    {
                        CogoPoints = new ObservableCollection<CogoPointDisplay>(cogoPointsList);
                        RaisePropertyChanged(nameof(CogoPoints));
                        StatusMessage = $"Loaded {cogoPointsList.Count} COGO points.";
                    }
                    else
                    {
                        StatusMessage = "No COGO points found in the current document.";
                    }

                    return; // Success, exit the retry loop
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("LoadCogoPointsAsync was cancelled.");
                    StatusMessage = "Loading COGO points was cancelled.";
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in LoadCogoPointsAsync (Attempt {i + 1}): {ex.Message}");
                    if (i == retryCount - 1) // If this was the last attempt
                    {
                        StatusMessage = $"Failed to load COGO points: {ex.Message}";
                        System.Windows.MessageBox.Show($"Failed to load COGO points after {retryCount} attempts. Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                    await Task.Delay(1000, _cts.Token); // Wait for 1 second before retrying
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task LoadBlocksAsync(int retryCount = 3)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    IsLoading = true;
                    var blocksList = await Task.Run(() =>
                    {
                        var acDoc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                        if (acDoc == null)
                        {
                            throw new InvalidOperationException("AutoCAD document is null");
                        }

                        var db = acDoc.Database;
                        if (db == null)
                        {
                            throw new InvalidOperationException("Database is null");
                        }

                        var blocks = new List<BlockDisplay>();
                        using (var tr = db.TransactionManager.StartOpenCloseTransaction())
                        {
                            var ms = tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord;
                            if (ms == null)
                            {
                                throw new InvalidOperationException("Current space is null");
                            }

                            foreach (ObjectId entId in ms)
                            {
                                _cts.Token.ThrowIfCancellationRequested();
                                var blkRef = tr.GetObject(entId, OpenMode.ForRead) as BlockReference;
                                if (blkRef != null)
                                {
                                    blocks.Add(new BlockDisplay
                                    {
                                        Name = blkRef.Name,
                                        Handle = blkRef.Handle.ToString(),
                                        Layer = blkRef.Layer,
                                        PositionX = blkRef.Position.X,
                                        PositionY = blkRef.Position.Y,
                                        PositionZ = blkRef.Position.Z
                                    });
                                }
                            }
                            tr.Commit();
                        }
                        return blocks;
                    }, _cts.Token);

                    // Update UI on the main thread
                    if (blocksList.Count > 0)
                    {
                        Blocks.Clear();
                        Blocks.AddRange(blocksList);
                        RaisePropertyChanged(nameof(Blocks));
                        StatusMessage = $"Loaded {blocksList.Count} blocks.";
                    }
                    else
                    {
                        Blocks.Clear();
                        StatusMessage = "No blocks found in the current document.";
                    }

                    return; // Success, exit the retry loop
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("LoadBlocksAsync was cancelled.");
                    StatusMessage = "Loading blocks was cancelled.";
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in LoadBlocksAsync (Attempt {i + 1}): {ex.Message}");
                    if (i == retryCount - 1) // If this was the last attempt
                    {
                        StatusMessage = $"Failed to load blocks: {ex.Message}";
                        System.Windows.MessageBox.Show($"Failed to load blocks after {retryCount} attempts. Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                    await Task.Delay(1000, _cts.Token); // Wait for 1 second before retrying
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        public void CancelLoading()
        {
            Debug.WriteLine("Cancelling ongoing operation...");
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            IsLoading = false;
            StatusMessage = "Operation cancelled by user.";
            Debug.WriteLine("Operation cancelled.");
        }

        private async void LoadCogoPoints(object parameter)
        {
            await LoadCogoPointsAsync();
        }

        private async void LoadBlocks(object parameter)
        {
            await LoadBlocksAsync();
        }

        private void ProcessSelectedCogoPoints(object parameter)
        {
            var selectedPoints = CogoPoints.Where(p => p.IsSelected).ToList();
            if (selectedPoints.Count > 0)
            {
                StatusMessage = $"{selectedPoints.Count} COGO punten geselecteerd voor verwerking.";
                // Hier kun je de geselecteerde punten verwerken
                // Bijvoorbeeld:
                // - Exporteren naar CSV
                // - Eigenschappen wijzigen
                // - Verwijderen
                // - etc.
            }
            else
            {
                StatusMessage = "Geen COGO punten geselecteerd.";
            }
        }

        private void ProcessSelectedBlocks(object parameter)
        {
            var selectedBlocks = Blocks.Where(b => b.IsSelected).ToList();
            if (selectedBlocks.Count > 0)
            {
                StatusMessage = $"{selectedBlocks.Count} blokken geselecteerd voor verwerking.";
                // Hier kun je de geselecteerde blokken verwerken
            }
            else
            {
                StatusMessage = "Geen blokken geselecteerd.";
            }
        }
        #endregion

        #region Helper Methods

        private string GetStyleName(Transaction tr, ObjectId styleId)
        {
            if (tr == null)
            {
                Debug.WriteLine("Transaction is null in GetStyleName");
                return "Unknown";
            }

            if (!styleId.IsValid)
            {
                return "None";
            }

            try
            {
                var style = tr.GetObject(styleId, OpenMode.ForRead);
                return style?.GetType().Name ?? "Unknown";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting style name for ID {styleId}: {ex.Message}");
                return "Error";
            }
        }

        private string GetLayerName(Transaction tr, ObjectId layerId)
        {
            if (tr == null)
            {
                Debug.WriteLine("Transaction is null in GetLayerName");
                return "Unknown";
            }

            if (!layerId.IsValid)
            {
                return "0";
            }

            try
            {
                var layer = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
                return layer?.Name ?? "Unknown";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting layer name for ID {layerId}: {ex.Message}");
                return "Error";
            }
        }

        #endregion
    }

    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification = false;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
                base.OnCollectionChanged(e);
        }

        public void AddRange(IEnumerable<T> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            _suppressNotification = true;

            foreach (T item in list)
            {
                Add(item);
            }
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    // The RelayCommand class is defined in its own file: TopoHelper/UserControls/ViewModels/RelayCommand.cs
    // This duplicate definition is removed to resolve ambiguity errors.
}
