using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using WixSharp;
using WixSharp.CommonTasks;
using Assembly = System.Reflection.Assembly;
using Condition = WixSharp.Condition;

// DON'T FORGET to update/fetch NuGet package "WixSharp". NuGet console:
// Update-Package WixSharp NuGet Manager UI: updates tab

// DONT FORGET TO UPDATE HARDCODED PATHS!!

// WHEN BUILDING, and U get ERROR 532459699 or whatever (who invents these
// error-numbers?) make sure to debug the project, its probably a code error, or
// as mentioned above, it could be a hard-coded path problem, or file not found problem.

namespace TopoHelper.WixSharpSetup
{
    internal class Program
    {
        #region Private Fields

        /// <summary>
        /// This is where the source files are located, when rebuilding from new
        /// environment make sure to update this.
        /// </summary>
        private const string SourcePathOfFiles = @"E:\Source\Repos\concepts\TopoHelper\TopoHelper.WixSharpSetup\files";

        //? --> More info about AutoCAD registry logic:
        //- https://jtbworld.com/autocad-information#AutoCAD-registry-details
        /// <summary>
        /// registryKey AutoCAD_2019
        /// </summary>
        private const string KeyAcad19 = @"Software\Autodesk\AutoCAD\R23.0\ACAD-2001:409\Applications\Infrabel.TopoHelper";

        /// <summary>
        /// registryKey C3D_2019
        /// </summary>
        private const string KeyC3D19 = @"Software\Autodesk\AutoCAD\R23.0\ACAD-2000:409\Applications\Infrabel.TopoHelper";

        private const string ProductDescription = "This application is for internal Infrabel usage only. It is a proof of concept, and should only be used as such.";
        private const string ProductEmailContact = "Bjorn.Cattoor@Infrabel.be";
        private const string ProductGuid = "1EB1D6F2-8704-4D6F-9BDE-83D07308D140";
        private const string ProductHelpDeskTelephoneNumber = "helpdesk";
        private const string ProductManufacturer = "By Bjorn Cattoor, for Infrabel.";
        private const string ProductName = "Infrabel IAM:Topo Helper, AutoCAD-plugin.";
        private const string ProductTargetInstallPath = @"%LocalAppData%\Infrabel\TopoHelper";
        private const string InstallerFolderPath = @"..\installer\";
        private const string DllFinalPath = "[INSTALLDIR]TopoHelper.dll";
        private const string TypeInt = "Type=integer";
        private const string AcadTrustedLoactionsExe = "AutocadTrustedLocations.exe";
        private const string ErrorMessageDotNet = "Please install .NET 4.6 first. Contact your system administrator to install.";
        public static readonly Assembly Reference = typeof(Program).Assembly;

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
            return new ProductInfo
            {
                Name = ProductName,
                Manufacturer = ProductManufacturer,
                Contact = ProductEmailContact,
                HelpTelephone = ProductHelpDeskTelephoneNumber,
                Comments = ProductDescription,
                InstallLocation = ProductTargetInstallPath,
                Readme = ProductTargetInstallPath + "\\readme.txt"
            };
        }

        private static ManagedProject CreateProject()
        {
            var binaryCode = new Feature("binary's", "My application binary's. This is mandatory of-course.", true, false);
            var registerDllInAutoCad2019 = new Feature("Register AutoCAD 2019 dll.", "Check this if you want to register the dll to automatically load upon the launching of AutoCAD.", true, true);
            var registerDllInC3D2019 = new Feature("Register dll in C3D 2019.", "Check this if you want to register the dll to automatically load upon the launching of AutoCAD C3D.", true, true);

            // <Guid("1EB1D6F2-8704-4D6F-9BDE-83D07308D14F")>
            var project =
                new ManagedProject(ProductName,
                    new InstallDir(binaryCode, ProductTargetInstallPath,
                            new File(@"readme.txt"),
                            new File(@"license.rtf"),
                            new Files(@"release\*.*"),
                            new File(new Id("ATL"), @"trustedlocations\AutocadTrustedLocations.exe"),
                            new File(@"trustedlocations\CommandLineArgumentsParser.dll"),
                            new File(@"trustedlocations\AutocadTrustedLocations.exe.config")
                    )
                )

                // Object initializer:
                {
                    Name = ProductName,
                    OutFileName = $"infrabel.topohelper.{Version}",
                    Description = ProductDescription,
                    GUID = new Guid(ProductGuid),
                    Version = Version,
                    ControlPanelInfo = CreateProductInfo(),
                    InstallPrivileges = InstallPrivileges.limited,
                    InstallScope = InstallScope.perUser,
                    SourceBaseDir = SourcePathOfFiles,
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
            project.OutDir = InstallerFolderPath;

            // Register application with autoCAD
            project.AddRegValues(
                //? AutoCAD 2019
                new RegValue(registerDllInAutoCad2019, RegistryHive.LocalMachineOrUsers, KeyAcad19, "LOADCTRLS", 14)
                { AttributesDefinition = TypeInt },
                new RegValue(registerDllInAutoCad2019, RegistryHive.LocalMachineOrUsers, KeyAcad19, "LOADER", DllFinalPath),
                new RegValue(registerDllInAutoCad2019, RegistryHive.LocalMachineOrUsers, KeyAcad19, "MANAGED", 1)
                { AttributesDefinition = TypeInt },

                new RegValue(registerDllInAutoCad2019, RegistryHive.LocalMachineOrUsers, KeyAcad19, "DESCRIPTION", ProductDescription),

                //? AutoCAD C3D 2019
                new RegValue(registerDllInC3D2019, RegistryHive.LocalMachineOrUsers, KeyC3D19, "LOADCTRLS", 14)
                { AttributesDefinition = TypeInt },

                new RegValue(registerDllInC3D2019, RegistryHive.LocalMachineOrUsers, KeyC3D19, "LOADER", DllFinalPath),

                new RegValue(registerDllInC3D2019, RegistryHive.LocalMachineOrUsers, KeyC3D19, "MANAGED", 1)
                { AttributesDefinition = TypeInt },

                new RegValue(registerDllInC3D2019, RegistryHive.LocalMachineOrUsers, KeyC3D19, "DESCRIPTION", ProductDescription));

            return project;
        }

        private static void Project_AfterInstall(SetupEventArgs e)
        {
            if (e.IsInstalling)
            {
                try
                {
                    // ADD my software folder to the trusted locations in C3D
                    var exeString = e.InstallDir + AcadTrustedLoactionsExe;
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
                            + sb);
                    else
                        NotepadHelper.ShowMessage(
                            title: "Installed",
                            message: "Application has been installed, though we had some problems adding trusted locations to the registry."
                            + sb);
                }
                catch (Exception ex)
                {
                    // We return fail here because we want the uninstall process
                    // to stop and rollback Something went wrong, it needs to be fixed!
                    Debug.WriteLine(ex.Message);
                    MessageBox.Show("Installation failed!");
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
                    var exeString = e.InstallDir + AcadTrustedLoactionsExe;
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
                    // Wait for the process to be started
                    proc.WaitForExit(15000);
                }
                catch (Exception ex)
                {
                    // We return success here because we want the uninstall
                    // process to continue If we stop here the application will
                    // become irremovable.
                    Debug.WriteLine(ex.Message);
                    MessageBox.Show("Removal failed!");
                    e.Result = ActionResult.Success;
                }
            }
        }

        #endregion
    }
}