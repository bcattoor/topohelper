using System.ComponentModel;

namespace TopoHelper.UserControls.ViewModels
{
    /// <summary>
    /// Basic view model functions.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        #region Public Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Public Properties

        public virtual bool IsDirty { get; protected set; }
        public virtual bool IsNew { get; protected set; }

        #endregion

        #region Protected Methods

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}