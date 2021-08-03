using System;
using System.Diagnostics;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using Infrabel.AutodeskPlatform.Core.UserInterface;
using Infrabel.AutodeskPlatform.FrameworkCommon.Log;
using Exception = System.Exception;

namespace TopoHelper
{
    // This class is instantiated by AutoCAD once and kept alive for the
    // duration of the session. If you don't do any one time initialization then
    // you should remove this class.

    public class MyPlugin : IExtensionApplication
    {
        private readonly string applicationName = System.Reflection.Assembly.GetExecutingAssembly().FullName;

        void IExtensionApplication.Initialize()
        {
            Logger.Info(string.Format(@"IAP: {0} LOADED.\r\n", applicationName));
            Debug.WriteLine(string.Format(@"IAP: {0} LOADED.", applicationName));
            // Initialize your plug-in application here

            try
            {
                Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.CurrentDocument.Editor.WriteMessage(string.Format(@"Loaded --> {0}.", applicationName) + System.Environment.NewLine);
                //LanguageSupport.Languages.InitializeLanguage();
                AddRibbonPalette();
            }

            // ReSharper disable once CatchAllClause
            catch (Exception exception)
            {
                Logger.Log(exception);
            }
        }

        void IExtensionApplication.Terminate()
        {
            Logger.Info(string.Format(@"IAP: {0} UNLOADED.", applicationName), false);
        }

        private void AddRibbonPalette()
        {
            // TODO: implementeer dit hier volledig, vervang place holders, en maak
            // het productiewaardig.

            RibbonPanelSource ribbonPanelSource = new RibbonPanelSource
            {
                Title = "TopoHelper Connect",
                Name = "name: TopoHelper Connect",
                Description = "Infrabel Autodesk Platform: TopoHelper."
            };

            RibbonButton dialogRibbonButton = new RibbonButton
            {
                Name = "TopoHelper"
            };
            RibbonToolTip myTool = new RibbonToolTip
            {
                Command = "IAMTopo_Settings",
                Title = "Settings",
                Content = "Open the main panel for TopoHelper plugin."
            };
            dialogRibbonButton.ToolTip = myTool;
            dialogRibbonButton.HelpSource = new Uri("https://professional.bjorncattoor.be/nl/iap/help/track-connect", UriKind.Absolute);
            dialogRibbonButton.ShowText = true;
            dialogRibbonButton.Text = "TopoHelper Settings";
            System.IO.FileInfo imageSmall = Infrabel.AutodeskPlatform.Core.CoreSettings.Dynamic.GetFileFromInstallPath("track_connect_16.png");
            System.IO.FileInfo imageLarge = Infrabel.AutodeskPlatform.Core.CoreSettings.Dynamic.GetFileFromInstallPath("track_connect_32.png");
            if (imageSmall != null)
                dialogRibbonButton.Image = new System.Windows.Media.Imaging.BitmapImage(new Uri(Infrabel.AutodeskPlatform.Core.CoreSettings.Dynamic.GetFileFromInstallPath("track_connect_16.png").FullName));
            dialogRibbonButton.ShowText = true;
            dialogRibbonButton.ShowImage = true;
            if (imageLarge != null)
                dialogRibbonButton.LargeImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(Infrabel.AutodeskPlatform.Core.CoreSettings.Dynamic.GetFileFromInstallPath("track_connect_32.png").FullName));
            dialogRibbonButton.Size = RibbonItemSize.Large;
            dialogRibbonButton.CommandHandler = new MyRibbonCommandHandler();
            dialogRibbonButton.CommandParameter = "IAMTopo_Settings";

            ribbonPanelSource.Items.Add(dialogRibbonButton);

            RibbonPanel ribbonPanel = new RibbonPanel
            {
                Source = ribbonPanelSource
            };
            ribbonPanelSource.Name = "IAMTopo_Settings";

            ApplicationRibbon.AddPanel(ribbonPanel);
        }
    }
}