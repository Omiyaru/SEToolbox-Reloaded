
using SEToolbox.Interop;
using SEToolbox.Models;
using SEToolbox.Support;
using SEToolbox.ViewModels;
using SEToolbox.Views;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

using static Sandbox.Game.World.MyWorldGenerator;
using static SEToolbox.Support.GlobalSettings;

using Consts = SEToolbox.Interop.SpaceEngineersConsts;
using Res = SEToolbox.Properties.Resources;
using Screen = System.Windows.Forms.Screen;

namespace SEToolbox
{

    public class CoreToolbox
    {
        private string _tempBinPath;
        private readonly SpaceEngineersCore core = new();
        private string gameBinDir = ToolboxUpdater.GetApplicationFilePath();

        #region  Methods

        public bool Init(string[] args)
        {


            // Detection and correction of local settings of SE install location.
            DetectInstall();
            LoadAssemblies(args);

            //AltLoad(filePath, true);

            return true;
        }
        public string[] validApplications = [
               "SpaceEngineers.exe",
                "SpaceEngineersDedicated.exe",
                //"MedievalEngineers.exe",
                //"MedievalEngineersDedicated.exe"
            ];


        public bool DetectInstall()
        {
            string filePath = ToolboxUpdater.GetApplicationFilePath();

            if (Default.PromptUser || !ToolboxUpdater.ValidateSpaceEngineersInstall(filePath))
            {
                var files = Directory.EnumerateFiles(gameBinDir).Select(Path.GetFileName).ToList();
                bool isValid = false;
                var validApplication = validApplications.FirstOrDefault(files.Contains);
                if (isValid = !string.IsNullOrEmpty(validApplication))
                {
                    gameBinDir = Path.Combine(filePath, validApplication);
                }

                var faModel = new FindApplicationModel()
                {
                    GameApplicationPath = gameBinDir
                };
                var faViewModel = new FindApplicationViewModel(faModel);
                var faWindow = new WindowFindApplication(faViewModel);

                if (faWindow?.ShowDialog() == true)
                {
                    filePath = faModel.GameBinPath;
                }
                else
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Default.SEBinPath;
            }

            // Update and save user path.
            Default.SEBinPath = filePath;
            Default.Save();

            return true;
        }

        public Task<bool> LoadAssemblies(string[] args)
        {
            string delimiter = "/" ?? "-";
            bool ignoreUpdates = args.Any(arg => arg.Equals(delimiter + "X", StringComparison.OrdinalIgnoreCase));
            bool oldDlls = true; // argsContains($"{("/"||"-")}" + ("OLDDLL", StringComparison.CurrentCultureIgnoreCase));
            bool altDlls = !oldDlls;

            // Go looking for any changes in the Dependant Space Engineers assemblies and immediately attempt to update.
            if (!ignoreUpdates && !altDlls && ToolboxUpdater.IsBaseAssembliesChanged() && !Debugger.IsAttached)
            {
                ToolboxUpdater.RunElevated(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SEToolboxUpdate"), $"{delimiter}/B " + String.Join(" ", args), false, false);
                return Task.FromResult(false);
            }

            Process proc = Process.GetCurrentProcess();
            if (Process.GetProcessesByName(proc.ProcessName).Length == 1)
            {
                // Clean up Temp files if this is the only instance running.
                TempFileUtil.DestroyTempFiles();
            }
           return Task.FromResult(true);
        }
        // Do not load any of the Space Engineers assemblies or dependent classes before this point.
        // ============================================


        public bool Load(string[] args)
        {
            VerifyGameVersion(args);
            InitializeWorld(args);
            core.SpaceEngineersCoreLoader();
            InitializeExplorerModel(args);
            RestoreExplorerWindow(false);
            InitializeWindow(eWindow);
            ValidateLoadState(eWindow);
            return true;
        }

        public static Task<bool> VerifyGameVersion(string[] args)
        {
            // Fetch the game version and store, so it can be retrieved during crash if the toolbox makes it this far.
            Version currentGameVersion = Consts.GetSEVersion();
            bool isNewVersion = Default.SEVersion != currentGameVersion;
            Default.SEVersion = currentGameVersion;

            // Test the Space Engineers version to make sure users are using an version that is new enough for SEToolbox to run with!
            // This is usually because a user has not updated a manual install of a Dedicated Server, or their Steam did not update properly.
            if (Default.SEVersion < GetAppVersion(true))
            {
                MessageBox.Show(string.Format(Res.DialogOldSEVersionMessage, Consts.GetSEVersion(), Default.SEBinPath, GetAppVersion()), Res.DialogOldSEVersionTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Application.Current.Shutdown();
                return Task.FromResult(false);
            }
            string delimiter = "/" ?? "-";
            // the /B argument indicates the SEToolboxUpdate had started SEToolbox after fetching updated game binaries.
            if (isNewVersion && args.Any(arg => arg.Equals($"{delimiter}B", StringComparison.OrdinalIgnoreCase)))
            {
                // Reset the counter used to indicate if the game binaries have updated.
                Default.TimesStartedLastGameUpdate = null;
            }
            return Task.FromResult(true);
        }

        private static Task<bool> InitializeWorld(string[] args)
        {
            string worldDirectory = null;
            {
                if (args.Length == 0)
                {
                    return Task.FromResult(false);
                }
                worldDirectory = args
                    .Where(arg => IsValidGameFilePath(arg) && IsSandboxFile(Path.GetFileName(arg)))
                    .Select(GetWorldDirectory)
                    .FirstOrDefault();


                return Task.FromResult(!string.IsNullOrEmpty(worldDirectory));

            }
        }

        private static bool IsValidGameFilePath(string path)
        {
            return path.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && File.Exists(path);
        }

        private static bool IsSandboxFile(string fileName)
        {
            return fileName.Equals(Consts.SandBoxCheckpointFileName, StringComparison.InvariantCultureIgnoreCase)
                || fileName.Equals(Consts.SandBoxSectorFileName, StringComparison.InvariantCultureIgnoreCase);
        }



        private static string GetWorldDirectory(string path)
        {
            return Path.GetFileName(path).Equals(Consts.SandBoxCheckpointFileName, StringComparison.InvariantCultureIgnoreCase)
                ? Path.GetDirectoryName(path)
                : Path.GetDirectoryName(Path.GetDirectoryName(path));
        }

        public bool InitializeExplorerModel(string[] args)
        {
            return Task.FromResult(true);
        }

        private static Task<bool> InitializeWorld(string[] args)
        {
            string worldDirectory = null;
            {
                if (args.Length == 0)
                {
                    return Task.FromResult(false);
                }
                worldDirectory = args
                    .Where(arg => IsValidGameFilePath(arg) && IsSandboxFile(Path.GetFileName(arg)))
                    .Select(GetWorldDirectory)
                    .FirstOrDefault();


                return Task.FromResult(!string.IsNullOrEmpty(worldDirectory));

            }
        }

        private static bool IsValidGameFilePath(string path)
        {
            return path.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && File.Exists(path);
        }

        private static bool IsSandboxFile(string fileName)
        {
            return fileName.Equals(Consts.SandBoxCheckpointFileName, StringComparison.InvariantCultureIgnoreCase)
                || fileName.Equals(Consts.SandBoxSectorFileName, StringComparison.InvariantCultureIgnoreCase);
        }



        private static string GetWorldDirectory(string path)
        {
            return Path.GetFileName(path).Equals(Consts.SandBoxCheckpointFileName, StringComparison.InvariantCultureIgnoreCase)
                ? Path.GetDirectoryName(path)
                : Path.GetDirectoryName(Path.GetDirectoryName(path));
        }

        public bool InitializeExplorerModel(string[] args)
        {
            // Force pre-loading of any Space Engineers resources.
            SpaceEngineersCore.LoadDefinitions();

            // Load the Space Engineers assemblies, or dependant classes after this point.
            ExplorerModel explorerModel = new();
            var delimiter = "/" ?? "-";
            if (args.Any(a => a.Equals($"{delimiter}WR", StringComparison.OrdinalIgnoreCase)))
                ExplorerModel explorerModel = new();
            {
                ResourceReportModel.GenerateOfflineReport(explorerModel, args);
                Application.Current.Shutdown();
                return false;
            }

            return true;
        }
        private static WindowExplorer eWindow = new();
        private static readonly ExplorerModel explorerModel = new();

        public static void RestoreExplorerWindow(bool allowClose = true)
        {
            var eViewModel = new ExplorerViewModel(explorerModel);

            if (allowClose)
            {
                eWindow = new WindowExplorer(eViewModel);
                if (!(bool)Conditional.ConditionPairs(null, eWindow, null, eViewModel, false, !allowClose))
                {
                    eViewModel.CloseRequested += (sender, e) =>
                    {
                        SaveWindowSettings(eWindow);
                        Application.Current.Shutdown();
                    };
                }
            }
        }

        public static void InitializeWindow(WindowExplorer eWindow)
        {
            if (eWindow != null)
            {
                eWindow.Loaded += (sender, e) =>
                {
                    Log.Debug("Main window loaded.");
                    Splasher.CloseSplash();
                    var windowDimensions = Default?.WindowDimensions?.Values?.FirstOrDefault();
                    var workingAreaRect = Screen.PrimaryScreen.WorkingArea;
                    var windowRect = new Rectangle(
                        (int)(windowDimensions.Value.Left ?? eWindow.Left),
                        (int)(windowDimensions.Value.Top ?? eWindow.Top),
                        (int)(windowDimensions.Value.Width ?? eWindow.Width),
                        (int)(windowDimensions.Value.Height ?? eWindow.Height)
                    );

                    bool isInsideDesktop = workingAreaRect.Contains(windowRect);
                    bool hasWindowDimensions = Default?.WindowDimensions?.Count > 0;
                    foreach (Screen screen in Screen.AllScreens)
                    {
                        try
                        {
                            isInsideDesktop |= screen.WorkingArea.IntersectsWith(windowRect);
                        }
                        catch
                        {
                            // some virtual screens have been know to cause issues.
                        }
                    }

                    if (!isInsideDesktop && hasWindowDimensions)
                    {
                        eWindow.Dispatcher.Invoke(() =>
                        {
                            Default.WindowDimensions?.Keys
                                .Where(key => key.HasValue)
                                .ForEach(key => Default.SetWindowDimension(key.Value));
                            if (Default.WindowState.HasValue)
                            {
                                eWindow.WindowState = Default.WindowState.GetValueOrDefault();
                            }
                        });
                    }
                };
            }
        }

        public static bool ValidateLoadState(WindowExplorer eWindow)
        {
            if (!Default.TimesStartedTotal.HasValue)
                Default.TimesStartedTotal = Default.TimesStartedTotal.GetValueOrDefault() + 1;
            Default.TimesStartedLastReset = Default.TimesStartedLastReset.GetValueOrDefault() + 1;
            Default.TimesStartedLastGameUpdate = Default.TimesStartedLastGameUpdate.GetValueOrDefault() + 1;
            Default.Save();
            eWindow?.ShowDialog();
            bool isInsideDesktop = workingAreaRect.Contains(windowRect);
            bool hasWindowDimensions = Default?.WindowDimensions?.Count > 0;
            foreach (Screen screen in Screen.AllScreens)
            {
                try
                {
                    isInsideDesktop |= screen.WorkingArea.IntersectsWith(windowRect);
                }
                catch
                {
                    // some virtual screens have been know to cause issues.
                }
            }

            if (!isInsideDesktop && hasWindowDimensions)
            {
                eWindow.Dispatcher.Invoke(() =>
                {
                    Default.WindowDimensions?.Keys
                        .Where(key => key.HasValue)
                        .ForEach(key => Default.SetWindowDimension(key.Value));
                    if (Default.WindowState.HasValue)
                    {
                        eWindow.WindowState = Default.WindowState.GetValueOrDefault();
                    }
                });
            }
        }


        public static bool ValidateLoadState(WindowExplorer eWindow)
        {
            if (!Default.TimesStartedTotal.HasValue)
                Default.TimesStartedTotal = Default.TimesStartedTotal.GetValueOrDefault() + 1;
            Default.TimesStartedLastReset = Default.TimesStartedLastReset.GetValueOrDefault() + 1;
            Default.TimesStartedLastGameUpdate = Default.TimesStartedLastGameUpdate.GetValueOrDefault() + 1;
            Default.Save();
            eWindow?.ShowDialog();

            return true;
        }

        public static void ExitApplication()
        {
            if (VRage.Plugins.MyPlugins.Loaded)
            {
                VRage.Plugins.MyPlugins.Unload();
            }
            TempFileUtil.Dispose();
        }

        private static readonly List<WindowDimension> _windowDimensions = [];

        private static void SaveWindowSettings(WindowExplorer eWindow)
        {
            Default.WindowState = eWindow.WindowState;
            eWindow.WindowState = WindowState.Normal; // Reset the State before getting the window size.
            foreach (var item in _windowDimensions)
            {
                _windowDimensions.Add(item);
            }

            Default.Save();
            foreach (var item in _windowDimensions)
            {
                _windowDimensions.Add(item);
            }

            Default.Save();
        }

        Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {

            // Retrieve the list of referenced assemblies in an array of AssemblyName.
            string fileName = $"{args.Name.Substring(0, args.Name.IndexOf(",", StringComparison.Ordinal))}.dll";
            Assembly ResolveAssembly(object sender, ResolveEventArgs args)
            {

                // Retrieve the list of referenced assemblies in an array of AssemblyName.
                string fileName = $"{args.Name.Substring(0, args.Name.IndexOf(",", StringComparison.Ordinal))}.dll";

                const string filter = @"^(?<assembly>(?:\w+(?:\.?\w+)+))\s*(?:,\s?Version=(?<version>\d+\.\d+\.\d+\.\d+))?(?:,\s?Culture=(?<culture>[\w-]+))?(?:,\s?PublicKeyToken=(?<token>\w+))?$";
                Match match = Regex.Match(args.Name, filter);
                if (match.Success)
                {
                    fileName = match.Groups["assembly"].Value + ".dll";
                }

                if (ToolboxUpdater.CoreSpaceEngineersFiles.Any(f => string.Equals(f, fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    string assemblyPath = Path.Combine(_tempBinPath, fileName);

                    // Load the assembly from the specified path and then   Return the loaded assembly.
                    return Assembly.LoadFrom(assemblyPath);
                }

                return null;
            }
           
        }
 		#endregion
    }
}


