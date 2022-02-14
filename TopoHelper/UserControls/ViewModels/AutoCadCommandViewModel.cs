using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Specialized;
using System.Reflection;
using System.Windows.Input;
using TopoHelper.Model.String;
using TopoHelper.AutoCAD;

namespace TopoHelper.UserControls.ViewModels
{
    public class AutoCadCommandViewModel : BaseViewModel
    {
        #region Private Fields

        private RelayCommand executeCommand;
        private string friendlyName;

        #endregion

        #region Public Properties

        public ICommand AutoCADCommand =>
            executeCommand ?? (executeCommand = new RelayCommand(ExecuteCommand, CanExecuteCommand));

        public string CommandName { get; set; }

        public string FriendlyName
        {
            get
            {
                if (friendlyName == null) friendlyName = CommandName.ToFriendlyCommandName();
                return friendlyName;
            }
        }

        #endregion

        #region Public Methods

        public bool CanExecuteCommand(object obj)
        {
            if (Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager == null) return false;
            if (Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument == null) return false;
            if (Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor.IsQuiescent)
                return true;
            return false;
        }

        public void ExecuteCommand(object obj)
        {
            Document document = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            try
            {
                document.SendCommandSynchronously($"{CommandName} ");
            }
            catch (System.Exception exception)
            {
                System.Windows.MessageBox.Show(exception.Message);
            }
        }

        #endregion
    }
}