using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Collections.Generic;
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
        private const string SourcePathOfFiles = @"C:\Users\cwn8400\Documents\GitHub\topohelper2022\TopoHelper.WixSharpSetup\files";

        //? --> More info about AutoCAD registry logic:
        //- https://jtbworld.com/autocad-information#AutoCAD-registry-details
        /// <summary>
        /// registryKey AutoCAD_20xx https://www.cadforum.cz/en/products.asp?prod=AutoCAD
        /// 2019: 001K1; reg: Autodesk\AutoCAD\R23.0\ACAD-2001
        /// 2020: 001L1; reg: Autodesk\AutoCAD\R23.1\ACAD-3001
        /// 2021: 001M1; reg: Autodesk\AutoCAD\R24.0\ACAD-4101
        /// 2022: 001N1; reg: Autodesk\AutoCAD\R24.1\ACAD-5101
        /// </summary>
        private static readonly string[] KeysAutocad = {
            @"Software\Autodesk\AutoCAD\R23.0\ACAD-2001:409\Applications\Infrabel.TopoHelper",//2019
            @"Software\Autodesk\AutoCAD\R23.0\ACAD-3001:409\Applications\Infrabel.TopoHelper",//2020
            @"Software\Autodesk\AutoCAD\R23.0\ACAD-4101:409\Applications\Infrabel.TopoHelper",//2021
};

        /// <summary>
        /// registryKey C3D_20xx
        /// https://www.cadforum.cz/en/products.asp?prod=Civil+3D
        /// 2019: 237K1; reg: Autodesk\AutoCAD\R23.0\ACAD-2000
        /// 2020: 237L1; reg: Autodesk\AutoCAD\R23.1\ACAD-3000
        /// 2021: 237M1; reg: Autodesk\AutoCAD\R24.0\ACAD-3000
        ///! 2022: 237N1; reg: Autodesk\AutoCAD\R24.1\ACAD todo: set value
        /// </summary>
        private static readonly string[] KeysC3D = {
            @"Software\Autodesk\AutoCAD\R23.0\ACAD-2000:409\Applications\Infrabel.TopoHelper",//2020
            // @"Computer\HKEY_CURRENT_USER\Software\Autodesk\AutoCAD\R24.0\ACAD-4100:409\Applications"
            @"Software\Autodesk\AutoCAD\R24.0\ACAD-4100:409\Applications\Infrabel.TopoHelper"//2021
};

        private const string ProductDescription = "This application is for internal Infrabel usage only. It is a proof of concept, and should only be used as such. No warrenty is given!";
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

#if DEBUG
            project.BuildMsiCmd();
#else
            project.BuildMsi();
#endif
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
            var binaryCode = new Feature("The binary's", "The application binary's. This is mandatory of-course.", true, false);
            var registerDllInAutoCad = new Feature("Autoload into AutoCAD on startup.", "Check this if you want to register the dll to automatically load upon the launching of AutoCAD.", true, true);
            var registerDllInC3D = new Feature("Autoload into Civil 3D on startup.", "Check this if you want to register the dll to automatically load upon the launching of AutoCAD Civil 3D.", true, true);

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
            project.UI = WUI.WixUI_Mondo;
            project.SetNetFxPrerequisite(Condition.Net46_Installed, ErrorMessageDotNet);
            project.AfterInstall += Project_AfterInstall;
            project.BeforeInstall += Project_BeforeInstall;
            project.OutDir = InstallerFolderPath;

            //? Add registy values for autocad
            var values = new List<RegValue>();
            foreach (var item in KeysAutocad)
            {
                CreateLoaderRegistryKeys(registerDllInAutoCad, values, item);
            }

            //? Add registy values for C3D

            foreach (var item in KeysC3D)
            {
                CreateLoaderRegistryKeys(registerDllInC3D, values, item);
            }

            // Register application with autoCAD
            project.AddRegValues(values.ToArray());

            return project;

            void CreateLoaderRegistryKeys(Feature feature, List<RegValue> listToAddTo, string item)
            {
                listToAddTo.Add(new RegValue(feature, RegistryHive.LocalMachineOrUsers, item, "LOADCTRLS", 14) { AttributesDefinition = TypeInt });
                listToAddTo.Add(new RegValue(feature, RegistryHive.LocalMachineOrUsers, item, "LOADER", DllFinalPath));
                listToAddTo.Add(new RegValue(feature, RegistryHive.LocalMachineOrUsers, item, "MANAGED", 1) { AttributesDefinition = TypeInt });
                listToAddTo.Add(new RegValue(feature, RegistryHive.LocalMachineOrUsers, item, "DESCRIPTION", ProductDescription));
            }
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
                    //++ Return SUCCES!
                    //! We return success here because we want the uninstall
                    //! process to continue If we stop here the application will
                    //! become irremovable.
                    Debug.WriteLine(ex.Message);
                    MessageBox.Show($"Removal failed:\r\n {ex.Message}");
                    e.Result = ActionResult.Success; //+ Careful
                                                     //!we Return SUCCES!
                }
            }
        }

        #endregion
    }
}