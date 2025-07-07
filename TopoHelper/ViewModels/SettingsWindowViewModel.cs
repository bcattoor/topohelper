using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Infrabel.AutodeskPlatform.TopoHelper.Naming;

namespace Infrabel.AutodeskPlatform.TopoHelper.ViewModels
{
    public class SettingsWindowViewModel : INotifyPropertyChanged
    {
        private CogoPointNamingSettings _namingSettings;
        public CogoPointNamingSettings NamingSettings
        {
            get => _namingSettings;
            set { _namingSettings = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand ReloadSettingsCommand { get; }
        public ICommand CancelCommand { get; }


        public SettingsWindowViewModel()
        {
            LoadCogoPointNamingSettings();
            SaveCommand = new RelayCommand(SaveSettings);
            ReloadSettingsCommand = new RelayCommand(LoadCogoPointNamingSettings);
            CancelCommand = new RelayCommand(CloseWindow);
        }

        public void LoadCogoPointNamingSettings()
        {
            NamingSettings = CogoPointNamingEngine.LoadSettings();
        }

        private void SaveSettings(object obj)
        {
            CogoPointNamingEngine.SaveSettings(NamingSettings);
            // Optionally, provide feedback to the user
            MessageBox.Show("Settings saved successfully.", "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void CloseWindow(object obj)
        {
             if(obj is Window window)
             {
                 window.Close();
             }
        }


        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }

    // Basic implementation of ICommand
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = p => execute();
            if (canExecute != null)
                _canExecute = p => canExecute();
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}