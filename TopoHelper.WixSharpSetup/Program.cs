using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
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
            @"Software\Autodesk\AutoCAD\R23.0\ACAD-2001:409\Applications\Infrabel.TopoHelper", //?2019
            @"Software\Autodesk\AutoCAD\R23.0\ACAD-3001:409\Applications\Infrabel.TopoHelper", //?2020
            @"Software\Autodesk\AutoCAD\R23.0\ACAD-4101:409\Applications\Infrabel.TopoHelper", //?2021
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
            @"Software\Autodesk\AutoCAD\R23.0\ACAD-2000:409\Applications\Infrabel.TopoHelper", //?2019
            @"Software\Autodesk\AutoCAD\R23.1\ACAD-2000:409\Applications\Infrabel.TopoHelper", //?2020
            @"Software\Autodesk\AutoCAD\R24.0\ACAD-4100:409\Applications\Infrabel.TopoHelper"  //?2021
};

        private const string ProductDescription = "This application is for internal Infrabel usage only. It is a proof of concept, and should only be used as such. No warrenty is given!";
        private const string ProductEmailContact = "Bjorn.Cattoor@Infrabel.be";
        private const string ProductGuid = "1EB1D6F2-8704-4D6F-9BDE-83D07308D140";
        private const string ProductHelpDeskTelephoneNumber = "911/54555";
        private const string ProductManufacturer = "By Bjorn Cattoor, for Infrabel.";
        private const string ProductName = "Infrabel IAM:Topo Helper, AutoCAD-plugin.";
        private const string ProductTargetInstallPath = @"%LocalAppData%\Infrabel\IAP";
        private const string InstallFolderNameTopoHelper = @"TopoHelper";
        private const string InstallFolderNameTrackConnect = @"TrackConnect";
        private const string InstallFolderNameCore = @"Core";
        private const string InstallerFolderPath = @"..\installer\";
        private const string DllFinalPath = "[INSTALLDIR]Core\\Core.dll";
        private const string TypeInt = "Type=integer";
        private const string AcadTrustedLoactionsExe = "AutocadTrustedLocations.exe";
        private const string ErrorMessageDotNet = "Please install .NET 4.7.2 first. Contact your system administrator to install.";
        public static readonly Assembly Reference = typeof(Program).Assembly;

        public static Version Version = Reference.GetName().Version;

        /// <summary>
        /// This is used for the core registry values.
        /// </summary>
        public static string prefixKey = @"Software\Infrabel\IAP";

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
            // define features
            var featBasic = new Feature("The binary's", "The application binary's. This is mandatory of-course.", isEnabled: true, allowChange: false);
            var featRegisterDllInAutoCad = new Feature("Autoload into AutoCAD on startup.", "Check this if you want to register the dll to automatically load upon the launching of all supported AutoCAD (classic) applications.", isEnabled: true, allowChange: true);
            var featRegisterDllInC3D = new Feature("Autoload into Civil 3D on startup.", "Check this if you want to register the dll to automatically load upon the launching of all supported AutoCAD Civil 3D applications.", isEnabled: true, allowChange: true);
            var featTopoHelper = new Feature("Topohelper", "Some helpfull commands for calculating with measured polylines.", isEnabled: true, allowChange: false);
            var featIAPCore = new Feature("IAP: Core", "This is the IAP:Core framework (Core; FrameworkCommon; AutoCad common), used for loading the dll's, adding Ribbon items, and loging.", isEnabled: true, allowChange: false);
            var featTrackConnect = new Feature("IAP: Trackconnect", "This is a plugin used to calculate regression-lines to connect a new design track-layout to.", isEnabled: true, allowChange: true);

            var project =
                new ManagedProject(ProductName,
                    new InstallDir(featBasic, targetPath: ProductTargetInstallPath,
                            new File(sourcePath: @"readme.txt"),
                            new File(sourcePath: @"license.rtf"),
                            //?all files in release folder
                            new File(sourcePath: @"TrustedLocations\AutocadTrustedLocations.exe"),
                            new File(sourcePath: @"TrustedLocations\CommandLineArgumentsParser.dll"),
                            new File(sourcePath: @"TrustedLocations\AutocadTrustedLocations.exe.config"),
                            new File(sourcePath: @"Simplifynet\Simplifynet.pdb"),
                            new File(sourcePath: @"Simplifynet\Simplifynet.dll"),
                            new Dir(featIAPCore, targetPath: InstallFolderNameCore,
                                new Files(sourcePath: $@"{InstallFolderNameCore}\*")),
                            new Dir(featTopoHelper, targetPath: InstallFolderNameTopoHelper,
                                new Files(sourcePath: $@"{InstallFolderNameTopoHelper}\*")),
                            new Dir(featTrackConnect, targetPath: InstallFolderNameTrackConnect,
                                new Files(sourcePath: $@"{InstallFolderNameTrackConnect}\*"))

                    ),
                            //? Close running instances of AutoCAD. They could have
                            //? the DLL's loaded into memory.
                            new CloseApplication("acad.exe", true, false)

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
            project.UI = WUI.WixUI_FeatureTree;
            project.SetNetFxPrerequisite(Condition.Net472_Installed, ErrorMessageDotNet);
            project.AfterInstall += Project_AfterInstall;
            project.BeforeInstall += Project_BeforeInstall;
            project.OutDir = InstallerFolderPath;
            // Optionally enable an ability to repair the installation even when
            // the original MSI is no longer available.
            project.EnableResilientPackage();
            project.EnableUninstallFullUI();

            var values = new List<RegValue>();

            //? Registry values for the IAP
            values.AddRange(CreateRegistryKeysForIAPCore(featIAPCore));

            //? Registry values for the IAP-Trackconnect
            values.AddRange(CreateRegistryKeysForTopoHelperDynamicHook(featTrackConnect, InstallFolderNameTrackConnect, "netLoad", $@"{ProductTargetInstallPath}\{InstallFolderNameTrackConnect}\TrackConnect.dll"));

            //? Registry values for the TopoHelper
            values.AddRange(CreateRegistryKeysForIAPCore(featTopoHelper));
            values.AddRange(CreateRegistryKeysForTopoHelperDynamicHook(featTopoHelper, InstallFolderNameTopoHelper, "netLoad", $@"{ProductTargetInstallPath}\{InstallFolderNameTopoHelper}\TopoHelper.dll"));

            //? Add registy values for autocad

            foreach (var item in KeysAutocad)
            {
                values.AddRange(CreateLoaderRegistryKeys(featRegisterDllInAutoCad, item));
            }

            //? Add registy values for C3D

            foreach (var item in KeysC3D)
            {
                values.AddRange(CreateLoaderRegistryKeys(featRegisterDllInC3D, item));
            }

            // Register application with autoCAD
            project.AddRegValues(values.ToArray());

            return project;
        }

        private static IList<RegValue> CreateLoaderRegistryKeys(Feature feature, string item)
        {
            var listToAdd = new List<RegValue>
            {
                new RegValue(feature, RegistryHive.LocalMachineOrUsers, item, "LOADCTRLS", 14) { AttributesDefinition = TypeInt },
                new RegValue(feature, RegistryHive.LocalMachineOrUsers, item, "LOADER", DllFinalPath),
                new RegValue(feature, RegistryHive.LocalMachineOrUsers, item, "MANAGED", 1) { AttributesDefinition = TypeInt },
                new RegValue(feature, RegistryHive.LocalMachineOrUsers, item, "DESCRIPTION", ProductDescription)
            };
            return listToAdd;
        }

        private static IList<RegValue> CreateRegistryKeysForIAPCore(Feature feature)
        {
            var prefixKeyCore = $@"{prefixKey}";
            var listToAdd = new List<RegValue>
            {
                new RegValue(feature, RegistryHive.LocalMachineOrUsers,prefixKeyCore, name: "InstallDirectory", value: "[INSTALLDIR]"),
                 new RegValue(feature, RegistryHive.LocalMachineOrUsers,prefixKeyCore, name: "Language", value: "nederlands"),
                new RegValue(feature, RegistryHive.LocalMachineOrUsers, prefixKeyCore, name: "LogFilePath", $@"{ProductTargetInstallPath}\core.iaplog")
            };

            return listToAdd;
        }

        private static IList<RegValue> CreateRegistryKeysForTopoHelperDynamicHook(Feature feature, string hookName, string hookType, string hookValue)
        {
            var prefixKeyCore = $@"{prefixKey}\{InstallFolderNameCore}";
            var listToAdd = new List<RegValue>
            {
                new RegValue(feature, RegistryHive.LocalMachineOrUsers, $@"{prefixKeyCore}\Hooks\{hookName}", name: "HookType", value: hookType),
                new RegValue(feature, RegistryHive.LocalMachineOrUsers, $@"{prefixKeyCore}\Hooks\{hookName}", name: "HookValue", value: hookValue)
            };

            return listToAdd;
        }

        internal class DynamicHook
        {
            public string Type { get; set; }
            public string Path { get; set; }
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

                    proc.WaitForExit(15000);

                    if (proc.ExitCode == 0)
                    { //? Disabled this message, on succes it is not usefull
                        /* NotepadHelper.ShowMessage(
                            title: "Installed",
                            message: "Application has been installed, you can now use it in AutoCAD."
                            + sb);*/
                    }
                    else
                        NotepadHelper.ShowMessage(
                            title: "Installed",
                            message: "Application has been installed, though we had some problems adding trusted locations to the registry. Please retry your installation. If the problem persitst, report the issue here: bjorn.cattoor@infrabel.be, or create an issue on GitHub (account required: https://github.com/bcattoor/topohelper/issues/new/choose): "
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
            if (!e.IsInstalling) //? Uninstall
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
            else //? Install
            {
            }
        }

        #endregion
    }
}