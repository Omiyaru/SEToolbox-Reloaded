using SEToolbox.Support;

using System;
    using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SEToolbox.Interop
{

    class SymbolicLink
    {
        #region Constants
        public static string SavesFolder => "Saves";
        public static string ModsFolder => "Mods";
        public static string BlueprintsFolder => "Blueprints";
        public static string LocalBlueprintsSubFolder => "local";

        #endregion
        #region Symbolic Link
        /// <summary>
        /// Creates a symbolic link from the original save folder to a user-specified location.
        /// </summary>
        /// <param name="targetPath">The directory to which the save folder should be linked.</param>
        /// <param name="sourcePath">The original base path, which is the path to the save folder that will be linked to the target directory.</param>
        /// <param name="folderName = "/Saves, /Mods, /Blueprints" >The folder names that will be to the target directory.</param>
        public static void CreateSaveFolderSymbolicLink(string targetPath, string folderName, IProgress<int> progress = null)
        {

            if (string.IsNullOrWhiteSpace(targetPath) || !Directory.Exists(targetPath))
            {
                return;
            }

            var sourcePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpaceEngineers" + folderName);


            folderName = folderName switch
            {
                "Saves" => SavesFolder,
                "Mods" => ModsFolder,
                "Blueprints" => BlueprintsFolder,
                _ => throw new ArgumentException($"Invalid folder name specified: {folderName}", nameof(folderName))
            };

            try
            {
                CreateTempFolder(targetPath, sourcePath, folderName, progress);
            }
            catch (Exception ex)
            {
                SConsole.WriteLine($"An error occurred: {ex.Message}, Reverting changes.");
                RevertChanges(sourcePath, targetPath);
            }
        }


        private static void CreateTempFolder(string targetDirectory,
                                             string sourcePath,
                                             string folderName,
                                             IProgress<int> progress)
        {
            string tempFolder = Path.Combine(
                Path.GetTempPath(),
                $"SEToolbox_{Guid.NewGuid():N}");

            Directory.Move(sourcePath, tempFolder);
            Directory.CreateDirectory(targetDirectory);
            CopyFilesAndDirectories(tempFolder, targetDirectory, progress);
            CreateSymbolicLink(sourcePath, targetDirectory + folderName);
        }


        public static void  CreateSymbolicLink(
                    string sourcePath,
                    string targetPath)
        {
            var command = $"/C mklink /J \"{sourcePath}\" \"{targetPath}\"";
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo("cmd", command)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                Directory.Delete(sourcePath, true);
            }
        }
        private static void CopyFilesAndDirectories(string sourcePath,
                                                    string targetPath,
                                                    IProgress<int> progress = null)
        {
            var sourceDirectoryInfo = new DirectoryInfo(sourcePath);
            var targetDirectoryInfo = new DirectoryInfo(targetPath);

            CopyFilesAndDirectories(sourceDirectoryInfo, targetDirectoryInfo, progress);
        }

        private static void CopyFilesAndDirectories(DirectoryInfo sourceDirectoryInfo,
                                                    DirectoryInfo targetDirectoryInfo,
                                                    IProgress<int> progress = null)
        {
            if (Conditional.Null(sourceDirectoryInfo , targetDirectoryInfo))
            {
                return;
            }

            var directories = sourceDirectoryInfo.GetDirectories("*", SearchOption.AllDirectories);
            var files = sourceDirectoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
            var totalItems = files.Length + directories.Length;
            int processedItems = 0;

            foreach (var sourceDirectory in directories)
            {
                var targetDirectory = Path.Combine(targetDirectoryInfo.FullName,
                                    sourceDirectory.FullName.Substring(sourceDirectoryInfo.FullName.Length + 1));
                Directory.CreateDirectory(targetDirectory);
                processedItems++;
                progress?.Report(processedItems * 100 / totalItems);
            }

            Parallel.ForEach(files, sourceFile =>
            {
                var targetFile = Path.Combine(targetDirectoryInfo.FullName,
                                sourceFile.FullName.Substring(sourceDirectoryInfo.FullName.Length + 1));
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                File.Copy(sourceFile.FullName, targetFile, true);
                Interlocked.Increment(ref processedItems);
                progress?.Report(processedItems * 100 / totalItems);
            });
        }

        private static void RevertChanges(string sourcePath,
                                          string targetDirectory,
                                          IProgress<int> progress = null)
        {
            string tempFolder = Path.Combine(
                Path.GetTempPath(),
                $"SEToolbox_{Guid.NewGuid():N}");

            if (Directory.Exists(tempFolder))
            {
                CopyFilesAndDirectories(tempFolder, sourcePath, progress);
                Directory.Delete(targetDirectory, true);
            }
        }
        #endregion
    }
}