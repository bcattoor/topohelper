using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Diagnostics;
using System.Text;
using WixSharp;
using WixSharp.CommonTasks;

// DON'T FORGET to update NuGet package "WixSharp". NuGet console:
// Update-Package WixSharp NuGet Manager UI: updates tab

namespace TopoHelper.WixSharpSetup
{
    internal partial class Program
    {
        #region Private Fields

        //? --> More info about AutoCAD registry logic:
        //- https://jtbworld.com/autocad-information#AutoCAD-registry-details
        private const string KeyACAD19 = @"Software\Autodesk\AutoCAD\R23.0\ACAD-2001:409\Applications\Infrabel.TopoHelper";

        private const string KeyC3D19 = @"Software\Autodesk\AutoCAD\R23.0\ACAD-2000:409\Applications\Infrabel.TopoHelper";
        private const string productDescription = "This application is for internal Infrabel usage only. It is a proof of concept, and should not be used in any production environment.";
        private const string productEmailContact = "Bjorn.Cattoor@Infrabel.be";
        private const string productGUID = "1EB1D6F2-8704-4D6F-9BDE-83D07308D140";
        private const string productHelpDeskTelephoneNumber = "helpdesk";
        private const string productManufacturer = "By Bjorn Cattoor, for Infrabel.";
        private const string productName = "Infrabel IAM:Topo Helper, AutoCAD-plugin.";
        private const string productTargetInstallPath = @"%LocalAppData%\Infrabel\TopoHelper";
        private const string sourcePathOfFiles = @"C:\Users\cwn8400\Documents\Source\Repos\concepts\TopoHelper\TopoHelper.WixSharpSetup\files";
        private const string installerFolderPath = @"..\installer\";
        private const string dllFinalPath = "[INSTALLDIR]TopoHelper.dll";
        private const string typeInt = "Type=integer";
        private const string acadTrustedLoactionsExe = "AutocadTrustedLocations.exe";
        private const string ErrorMessageDotNet = "Please install .NET 4.7.2 first. Contact your system administrator to install.";
        public static readonly System.Reflection.Assembly Reference = typeof(Program).Assembly;

        public static Version Version = Reference.GetName().Version;

        #endregion

        #region Public Methods

        public static void Main()
        {
            var project = CreateProject();

            project.BuildMsi();
        }

        #endregion

        #region Private Methods

        private static ProductInfo CreateProductInfo()
        {
            return new ProductInfo()
            {
                Name = productName,
                Manufacturer = productManufacturer,
                Contact = productEmailContact,
                HelpTelephone = productHelpDeskTelephoneNumber,
                Comments = productDescription,
                InstallLocation = productTargetInstallPath,
                Readme = productTargetInstallPath + "\\readme.txt"
            };
        }

        private static ManagedProject CreateProject()
        {
            var binaryCode = new Feature("binary's", "My application binary's. This is mandatory of-course.", true, false);
            var registerDllInAutoCAD2019 = new Feature("Register AutoCAD 2019 dll.", "Check this if you want to register the dll to automatically load upon the launching of AutoCAD.", true, true);
            var registerDllInC3D2019 = new Feature("Register dll in C3D 2019.", "Check this if you want to register the dll to automatically load upon the launching of AutoCAD C3D.", true, true);

            // <Guid("1EB1D6F2-8704-4D6F-9BDE-83D07308D14F")>
            var project =
                new ManagedProject(productName,
                    new InstallDir(binaryCode, productTargetInstallPath,
                            new WixSharp.File(@"readme.txt"),
                            new WixSharp.File(@"license.rtf"),
                            new Files(@"release\*.*"),
                            new File(new Id("ATL"), @"trustedlocations\AutocadTrustedLocations.exe"),
                            new File(@"trustedlocations\CommandLineArgumentsParser.dll"),
                            new File(@"trustedlocations\AutocadTrustedLocations.exe.config")
                    )
                )

                // Object initializer:
                {
                    Name = productName,
                    OutFileName = $"infrabel.topohelper.{Version}",
                    Description = productDescription,
                    GUID = new Guid(productGUID),
                    Version = Version,
                    ControlPanelInfo = CreateProductInfo(),
                    InstallPrivileges = InstallPrivileges.limited,
                    InstallScope = InstallScope.perUser,
                    SourceBaseDir = sourcePathOfFiles,
                    ReinstallMode = "amus",
                    LicenceFile = @"license.rtf"
                };

            project.MajorUpgradeStrategy = MajorUpgradeStrategy.Default;
            project.MajorUpgradeStrategy.RemoveExistingProductAfter = Step.InstallInitialize;
            project.PreserveTempFiles = false;
            project.UI = WUI.WixUI_Common;
            project.SetNetFxPrerequisite(Condition.Net46_Installed, ErrorMessageDotNet);
            project.AfterInstall += Project_AfterInstall;
            project.BeforeInstall += Project_BeforeInstall;
            project.OutDir = installerFolderPath;

            // Register application with autoCAD
            project.AddRegValues(
                //? AutoCAD 2019
                new RegValue(registerDllInAutoCAD2019, RegistryHive.LocalMachineOrUsers, KeyACAD19, "LOADCTRLS", 14)
                { AttributesDefinition = typeInt },
                new RegValue(registerDllInAutoCAD2019, RegistryHive.LocalMachineOrUsers, KeyACAD19, "LOADER", dllFinalPath),
                new RegValue(registerDllInAutoCAD2019, RegistryHive.LocalMachineOrUsers, KeyACAD19, "MANAGED", 1)
                { AttributesDefinition = typeInt },

                new RegValue(registerDllInAutoCAD2019, RegistryHive.LocalMachineOrUsers, KeyACAD19, "DESCRIPTION", productDescription),

                //? AutoCAD C3D 2019
                new RegValue(registerDllInC3D2019, RegistryHive.LocalMachineOrUsers, KeyC3D19, "LOADCTRLS", 14)
                { AttributesDefinition = typeInt },

                new RegValue(registerDllInC3D2019, RegistryHive.LocalMachineOrUsers, KeyC3D19, "LOADER", dllFinalPath),

                new RegValue(registerDllInC3D2019, RegistryHive.LocalMachineOrUsers, KeyC3D19, "MANAGED", 1)
                { AttributesDefinition = typeInt },

                new RegValue(registerDllInC3D2019, RegistryHive.LocalMachineOrUsers, KeyC3D19, "DESCRIPTION", productDescription));

            return project;
        }

        private static void Project_AfterInstall(SetupEventArgs e)
        {
            if (e.IsInstalling)
            {
                try
                {
                    // ADD my software folder to the trusted locations in C3D
                    var exeString = e.InstallDir + acadTrustedLoactionsExe;
                    var arg = $"-t add -p \"{e.InstallDir + "..."}\"";
                    var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = exeString,
                            Arguments = arg,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };

                    var sb = new StringBuilder(Environment.NewLine);
                    proc.Start();
                    while (!proc.StandardOutput.EndOfStream)
                    {
                        sb.AppendLine(proc.StandardOutput.ReadLine());
                    }

                    if (proc.ExitCode == 0)
                        NotepadHelper.ShowMessage(
                            title: "Installed",
                            message: "Application has been installed, you can now use it in AutoCAD."
                            + sb.ToString());
                    else
                        NotepadHelper.ShowMessage(
                            title: "Installed",
                            message: "Application has been installed, though we had some problems adding trusted locations to the registry."
                            + sb.ToString());
                }
                catch (Exception ex)
                {
                    // We return fail here because we want the uninstall process
                    // to stop and rollback Something went wrong, it needs to be fixed!
                    Debug.WriteLine(ex.Message);
                    System.Windows.MessageBox.Show("Installation failed!");
                    e.Result = ActionResult.Failure;
                }
            }
        }

        private static void Project_BeforeInstall(SetupEventArgs e)
        {
            if (!e.IsInstalling)
            {
                try
                {
                    var exeString = e.InstallDir + acadTrustedLoactionsExe;
                    var arg = $"-t remove -p \"{e.InstallDir + "..."}\"";

                    var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = exeString,
                            Arguments = arg,
                            UseShellExecute = false,
                            RedirectStandardOutput = false,
                            CreateNoWindow = true
                        }
                    };

                    proc.Start();
                }
                catch (Exception ex)
                {
                    // We return success here because we want the uninstall
                    // process to continue If we stop here the application will
                    // become irremovable.
                    Debug.WriteLine(ex.Message);
                    System.Windows.MessageBox.Show("Removal failed!");
                    e.Result = ActionResult.Success;
                }
            }
        }

        #endregion
    }
}