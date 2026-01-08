using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using SEToolbox.Interop;
using SEToolbox.Models;
using SEToolbox.Support;
using SEToolbox.ViewModels;
using SEToolbox.Views;



using static Sandbox.Game.World.MyWorldGenerator;
using static SEToolbox.Support.GlobalSettings;

using Consts = SEToolbox.Interop.SpaceEngineersConsts;
using Res = SEToolbox.Properties.Resources;
using Screen = System.Windows.Forms.Screen;

namespace SEToolbox
{
    public class CoreToolbox
    {
        #region Fields
        private readonly SpaceEngineersCore core = new();

        private static WindowExplorer eWindow = new();
        private static readonly List<WindowDimension> _windowDimensions = [];
        private static readonly ExplorerModel explorerModel = new();

        #endregion

        #region Methods

        public bool Init(string[] args)
        {
            // Detection and correction of local settings of SE install location.
            FindInstall();
            LoadAssemblies(args);

            return true;
        }

        public string[] validApplications =
        [
            "SpaceEngineers.exe",
            "SpaceEngineersDedicated.exe",
            //"MedievalEngineers.exe",
            //"MedievalEngineersDedicated.exe"
        ];

        public bool FindInstall()
        {
            var gameBinDir = ToolboxUpdater.GetApplicationFilePath();
            SConsole.WriteLine($"Locate SE install path: {gameBinDir}");
            if (Default.PromptUser || !ToolboxUpdater.ValidateSpaceEngineersInstall(gameBinDir))
            {
                if (Default.PromptUser)
                {
                    Log.WriteLine("Prompting user for Space Engineers install location.");
                }

                var validFiles = Directory.EnumerateFiles(gameBinDir).Select(Path.GetFileName).Where(validApplications.Contains);

                var isValid = validFiles.Any();
                if (isValid)
                {
                    gameBinDir = validFiles.FirstOrDefault();
                }
                else
                {
                    Log.WriteLine($"No valid Space Engineers install found in {gameBinDir}");
                    gameBinDir = string.Empty;
                }
            }

            if (string.IsNullOrEmpty(gameBinDir) && !Directory.Exists(gameBinDir))
            {
                var faModel = new FindApplicationModel { GameApplicationPath = gameBinDir };
                var faViewModel = new FindApplicationViewModel(faModel);
                var faWindow = new WindowFindApplication(faViewModel);

                if (faWindow?.ShowDialog() != true)
                {
                    return false;
                }
                gameBinDir = faModel.GameBinPath;
                Log.WriteLine($"SE Install Location: {gameBinDir}");
            }
            else
            {
                gameBinDir = Path.GetDirectoryName(gameBinDir) ?? string.Empty;
            }

            // Update and save user path.
            Default.SEBinPath = gameBinDir;
            Default.Save();
            return true;
        }

        public Task<bool> LoadAssemblies(string[] args)
        {
            string delimiter = "/" ?? "-";
            bool ignoreUpdates = args.Any(arg => arg.Equals($"{delimiter}X", StringComparison.OrdinalIgnoreCase));


            // Go looking for any changes in the Dependant Space Engineers assemblies and immediately attempt to update.
            if (!ignoreUpdates && ToolboxUpdater.IsBaseAssembliesChanged() && !Debugger.IsAttached)
            {
                Log.WriteLine("Running non-elevated update process.");// 
                ToolboxUpdater.RunElevated(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SEToolboxUpdate"), $"{delimiter}/B " + string.Join(" ", args), false, false);
                return Task.FromResult(false);
            }

            var proc = Process.GetCurrentProcess();
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
            InitializeMainWindow();
            return true;
        }

        public Task InitializeMainWindow()
        {
            RestoreExplorerWindow(false);
            InitializeWindow(eWindow);
            UpdateTimesStarted();
            ShowMainWindow(eWindow);

            return Task.FromResult(true);
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
                _ = MessageBox.Show(string.Format(Res.DialogOldSEVersionMessage,
                                                  Consts.GetSEVersion(),
                                                  Default.SEBinPath,
                                                  GetAppVersion()), Res.DialogOldSEVersionTitle,
                                                  MessageBoxButton.OK, MessageBoxImage.Exclamation);
                Application.Current.Shutdown();
                return Task.FromResult(false);
            }
            string delimiter = "/" ?? "-";
            // the /B argument indicates the SEToolboxUpdate had started SEToolbox after fetching updated game binaries.
            if (isNewVersion && args.Any(arg => arg.Equals($"{delimiter}B", StringComparison.OrdinalIgnoreCase)))
            {
                TimesStartedInfo.SinceGameUpdate = null;
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
                worldDirectory = args.Where(arg => IsValidGameFilePath(arg) && IsSandboxFile(Path.GetFileName(arg)))
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
            return Path.GetFileName(path).Equals(Consts.SandBoxCheckpointFileName, StringComparison.InvariantCultureIgnoreCase) ? 
                   Path.GetDirectoryName(path) :
                   Path.GetDirectoryName(Path.GetDirectoryName(path) ?? 
                   throw new InvalidOperationException("Unable to get world directory from path: " + path + Environment.NewLine + Environment.StackTrace));
        }

        public bool InitializeExplorerModel(string[] args)
        {
            // Force pre-loading of any Space Engineers resources.
            SpaceEngineersCore.LoadDefinitions();

            // Load the Space Engineers assemblies, or dependant classes after this point.
            ExplorerModel explorerModel = new();
            var delimiter = "/" ?? "-";
            if (args.Any(a => a.Equals($"{delimiter}WR", StringComparison.OrdinalIgnoreCase)))
            {
                ResourceReportModel.GenerateOfflineReport(explorerModel, args);
                Application.Current.Shutdown();
                return false;
            }

            return true;
        }

        public static void RestoreExplorerWindow(bool allowClose = true)
        {
            ExplorerViewModel eViewModel = new(explorerModel);
            eWindow = new WindowExplorer(eViewModel);
            if (allowClose)
            {
                eViewModel.CloseRequested += (sender, e) =>
                {
                    Log.WriteLine("Saving window settings.");
                    SaveWindowSettings(eWindow);
                    Log.WriteLine("Shutting down.");
                    Application.Current.Shutdown();
                };
            }
        }

        public static void InitializeWindow(WindowExplorer eWindow)
        {
            eWindow?.Loaded += (sender, e) =>
            {
                Log.WriteLine("Main window loading complete.");
                {
                    SConsole.WriteLine($"Main window loading complete.");
                    Splasher.CloseSplash();
                    var windowDimensions = Default.WindowDimensions?.Values?.FirstOrDefault();
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
                        catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
                        {
                            // some virtual screens have been know to cause issues.
                            Log.WriteLine($"Ignoring exception while getting working area for screen {screen.DeviceName}: {ex.Message}");
                        }

                        if (isInsideDesktop && hasWindowDimensions)
                        {
                            foreach (var key in Default.WindowDimensions.Keys.Where(key => key.HasValue))
                            {
                                Default.SetWindowDimension(key.Value);
                            }
                        }

                        eWindow.WindowState = Default.WindowState.HasValue ? Default.WindowState.GetValueOrDefault() : WindowState.Normal;
                    };
                };
            };
        }


        public static void ShowMainWindow(WindowExplorer eWindow)
        {
            Log.WriteLine("Showing main window.");
            if (!eWindow.IsVisible)
            {
                eWindow?.Show();
                eWindow?.Activate();
            }
        }

        public static bool UpdateTimesStarted()
        {
            Log.WriteLine("Updating Start Times .");

            TimesStartedInfo.UpdateTimesStartedInfo();
            Default.Save();
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

        private static void SaveWindowSettings(WindowExplorer eWindow)
        {
            Default.WindowState = eWindow.WindowState;
            eWindow.WindowState = WindowState.Normal; // Reset the State before getting the window size.
            foreach (var item in _windowDimensions)
            {
                _windowDimensions.Add(item);
            }

            Default.Save();
        }
        #endregion
    }
}
