using SEToolbox.Support;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Res = SEToolboxUpdate.Properties.Resources;

namespace SEToolboxUpdate
{
    class Program
    {
        private const int NoError = 0;
        private const int UpdateBinariesFailed = 1;
        private const int UacDenied = 2;

        private const string logFilePath = "updater-log.txt";
        private static readonly string errorMsg = "Failed to copy one or more game files. Error:" ?? string.Empty;
        private static readonly string delimiter = "/" ?? "-";


        private static readonly Dictionary<string, Action> installMap = new(StringComparer.OrdinalIgnoreCase)
        {
            {$"{delimiter}{"I" ?? "install"}", InstallConfigurationSettings },
            {$"{delimiter}{"U" ?? "updatecheck"}" , UninstallConfigurationSettings },
            {$"{delimiter}{"A" ?? "attempt"}", () => UpdateBaseLibrariesFromSpaceEngineers(args) },
            {$"{delimiter}{"X" ?? "ignoreupdates"}", () => ToolboxUpdaterRunElevated( installMap.Keys.FirstOrDefault(k => k.Equals($"{delimiter}{args.FirstOrDefault()}")), false, false) },
            {$"{delimiter}{"B" ?? "updatebase"}", () => ToolboxUpdaterRunElevated(installMap.Keys.FirstOrDefault(k => k.Equals($"{delimiter}{args.FirstOrDefault()}")), false, false) },
        };
        private static readonly string[] args =  [.. installMap.Keys];

        static void Main(string[] args)
        {
  var logFileName = ToolboxUpdater.IsRunningElevated()
                ? "./updater-elevated-log.txt"
                : "./updater-log.txt";

            Log.Init(logFileName, appendFile: false);
            Log.Info("Updater started.");
            Log.Debug("Loading settings");

            GlobalSettings.Default.Load();

            Log.Info("Setting UI culture");
            string install = installMap.Keys.FirstOrDefault(k => args.Any(a => a.Equals(k))) ?? string.Empty;
            object a = args.Any(a => a.Equals(delimiter + install, StringComparison.OrdinalIgnoreCase));
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfoByIetfLanguageTag(GlobalSettings.Default.LanguageCode);

            if (File.Exists(logFilePath))
                File.Delete(logFilePath);

            // Install.


            bool argumments = args.Any(a => a.Equals(delimiter + install, StringComparison.OrdinalIgnoreCase));

            if (installMap.TryGetValue(install, out var action) && argumments)
            {
                action();
            }

            string appFile = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            MessageBox.Show(string.Format(Res.AppParameterHelpMessage, appFile), Res.AppParameterHelpTitle, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK);
        }

        private static void InstallConfigurationSettings()
        {
            DiagnosticsLogging.CreateLog();
            CleanBinCache();
        }

        private static void UninstallConfigurationSettings()
        {
            DiagnosticsLogging.RemoveLog();
            CleanBinCache();
        }
        private static readonly string updaterExePath = Assembly.GetExecutingAssembly().Location;
        private static readonly string appDirectory = Path.GetDirectoryName(updaterExePath);
        private static readonly string toolboxExePath = Path.Combine(appDirectory, "SEToolbox.exe");
        private static readonly string join = string.Join(" ", args);
        
        private static void UpdateBaseLibrariesFromSpaceEngineers(string[] args)
        {
              Log.Info("Updater task is update game files.");
            bool attemptedAlready = args.Any(a => a.Equals(installMap.Keys.FirstOrDefault()));
            string appDirectory = Path.GetDirectoryName(updaterExePath);


            if (!ToolboxUpdater.IsRunningElevated())
            {
                // Does not have elevated permission to run.
                if (!attemptedAlready)
                {
                    MessageBox.Show(Res.UpdateRequiredUACMessage, Res.UpdateRequiredTitle, MessageBoxButton.OK, MessageBoxImage.Information);

                    int? ret = ToolboxUpdater.RunElevated(updaterExePath,$"{join} {installMap.Keys.FirstOrDefault()}", elevate: true, waitForExit: true);

                    // Don't run toolbox from the elevated process, do it here.
                    if (ret.HasValue)
                        LaunchToolbox(ret.Value);
                    else
                        LaunchToolbox(UacDenied);
                }
            }
            else
            {
                if (!attemptedAlready)
                    MessageBox.Show(Res.UpdateRequiredMessage, Res.UpdateRequiredTitle, MessageBoxButton.OK, MessageBoxImage.Information);

                // Is running elevated permission, update the files.
                bool wasUpdated = UpdateBaseFiles(appDirectory, out Exception ex).GetAwaiter().GetResult();
                string errorMsgLog = $"{errorMsg}\n{File.ReadAllText(logFilePath)}";

                if (!wasUpdated && ex != null)
                {
                    SConsole.WriteLine(errorMsg + $"\n{File.ReadAllText(logFilePath)}");
                    File.WriteAllText(logFilePath, errorMsg);
                }

                int errorCode = wasUpdated ? NoError : UpdateBinariesFailed;
                if (!attemptedAlready)
                    LaunchToolbox(errorCode);
                else // Don't run toolbox from the elevated process, return to the original updater process.
                    Environment.Exit(errorCode);
            }
        }
        private static void ToolboxUpdaterRunElevated(string arg, bool? elevate = false, bool? waitForExit = false)
        {
            if (elevate != null && waitForExit != null)
      
        
                arg = installMap.Keys.FirstOrDefault(k => k.Equals($"{delimiter}{arg}", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

                string join = string.Join(" ", args);
                ToolboxUpdater.RunElevated(toolboxExePath, arg + join, elevate: false, waitForExit: false);
            }
        

        private static void LaunchToolbox(int errorCode, string arg = null)
        {
            string join = string.Join(" ", args);
            
            const int UacDeniedErrorCode = 1;
            const int UpdateBinariesFailedErrorCode = 2;


            switch (errorCode)
            {
                case NoError:
                    ToolboxUpdaterRunElevated(installMap.Keys.FirstOrDefault(), false, false);
                    break;
                case UacDeniedErrorCode:
                    SConsole.WriteLine(Res.CancelUACMessage);
                    Environment.Exit(errorCode);
                    break;
                case UpdateBinariesFailedErrorCode:
                    SConsole.WriteLine(Res.UpdateErrorMessage);
                    if (MessageBox.Show(Res.UpdateErrorTitle, Res.UpdateErrorMessage, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        // X = Ignore updates.
                        ToolboxUpdater.RunElevated(toolboxExePath, installMap.Keys.FirstOrDefault(k => k.Equals(arg)), elevate: false, waitForExit: false);
                    }
                    Environment.Exit(errorCode);
                    break;
                default:
                    Environment.Exit(errorCode);
                    break;
            }
        }
    

        /// <summary>
        /// Updates the base library files from the Space Engineers application path.
        /// </summary>
        /// <param name="appFilePath"></param>
        /// <returns>True if it succeeded, False if there was an issue that blocked it.</returns>
        private static Task<bool> UpdateBaseFiles(string appFilePath, out Exception exception)
        {
            exception = null;

            Process[] liveProcesses = Process.GetProcessesByName("SEToolbox");

            // Wait until SEToolbox is shut down.
            Task allCompletedTask = Task.WhenAll(liveProcesses.Select(item => Task.Run(() => item.WaitForExit())));
            if (Task.WhenAny(allCompletedTask, Task.Delay(TimeSpan.FromSeconds(10))) != allCompletedTask)
            {
                string errorMsg = $"Timed out waiting for SEToolbox to close. Process array length is {liveProcesses.Length}.";
                File.WriteAllText(logFilePath, errorMsg);
                SConsole.WriteLine(errorMsg);
                return Task.FromResult(false);
            }

            string baseFilePath = ToolboxUpdater.GetApplicationFilePath();

            var updateTasks = Enumerable.Range(0, 2)
                .Select(async i => await UpdateTasks(appFilePath, i == 0 ? ToolboxUpdater.CoreSpaceEngineersFiles : ToolboxUpdater.OptionalSpaceEngineersFiles))
                .ToArray();

            Task.WaitAll(updateTasks);

            return Task.FromResult(!updateTasks.Any(task => task.Exception != null));
        }

        private static Task UpdateTasks(string path, string[] files)
        {
            string baseFilePath = ToolboxUpdater.GetApplicationFilePath();

            return Task.Run(() =>
            {
                try
                {
                    foreach (string fileName in files)
                    {
                        string sourceFile = Path.Combine(path, fileName);
                        File.Copy(sourceFile, Path.Combine(baseFilePath, fileName), overwrite: true);
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = $"Update failed for {files.FirstOrDefault()} in {path}.";
                    File.AppendAllText(logFilePath, errorMsg + ex.Message + Environment.NewLine);

                    SConsole.WriteLine(errorMsg);
                }
            });

        }


        /// <summary>
        /// Clear app bin cache.
        /// </summary>
        private static void CleanBinCache()
        {
            string binCache = ToolboxUpdater.GetBinCachePath();

            if (Directory.Exists(binCache))
            {
                try
                {
                    Directory.Delete(binCache, true);
                }
                catch { }
            }
        }
    }
}
