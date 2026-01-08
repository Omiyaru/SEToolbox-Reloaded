using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;


namespace SEToolbox.Support
{
    public static class ToolboxUpdater
    {
        /// <summary>
        /// Required dependancies which must be copied for SEToolbox to work.
        /// </summary>
        internal static readonly string[] RequiredAssemblies =
        [
            "HavokWrapper.dll",                 // x64
            "ProtoBuf.Net.dll",                 // 1.192.x requirement.
            "ProtoBuf.Net.Core.dll",            // 1.192.x requirement.
            "Sandbox.Common.dll",               // AnyCPU
            "Sandbox.Game.dll",                 // x64
            "Sandbox.Game.XmlSerializers.dll",  // 1.191.x requirement.
            "Sandbox.Graphics.dll",             // x64
            "Sandbox.RenderDirect.dll",         // x64      1.187.x requirement.
            "SharpDX.dll",                      // AnyCPU
            "SharpDX.Direct3D11.dll",           // AnyCPU   Required to load Planets.
            "SharpDX.DXGI.dll",                 // AnyCPU   Required to load Planets.
            "SpaceEngineers.Game.dll",          // x64
            "SpaceEngineers.ObjectBuilders.dll",                    // x64
            "SpaceEngineers.ObjectBuilders.XmlSerializers.dll",     // x64
            "steam_api64.dll",                  // x64
            "Steamworks.NET.dll",               // x64      1.187.x requirement.
            "VRage.Ansel.dll",                  // x64      1.181.x requirement.
            "VRage.Audio.dll",                  // MSIL     1.147.x requirement.
            "VRage.dll",                        // AnyCPU
            "VRage.XmlSerializers.dll",
            "VRage.Game.dll",                   // x64
            "VRage.Game.XmlSerializers.dll",    // x64
            "VRage.Input.dll",                  // x64
            "VRage.Library.dll",                // AnyCPU
            "VRage.Math.dll",                   // AnyCPU
            "VRage.Math.XmlSerializers.dll",
            "VRage.Native.dll",                 // x64
            "VRage.NativeWrapper.dll",          // 1.191.x requirement.
            "VRage.Network.dll",
            "VRage.Render.dll",                 // AnyCPU
            "VRage.Render11.dll",               // x64
            "VRage.Scripting.dll",              // x64     1.197.x requirement.
            "VRage.Steam.dll",                  // x64     1.188.x requirement.
            "Steamworks.NET.dll",               // x64     1.188.x requirement.
            "System.Buffers.dll",
            "System.ComponentModel.Annotations.dll",
            "System.Collections.Immutable.dll", // AnyCPU  1.194.x requirement
            "System.Memory.dll",                // MSIL    1.191.x requirement for voxels
            "System.Numerics.Vectors.dll",
            "System.Runtime.CompilerServices.Unsafe.dll",  // MSIL     1.191.x requirement for voxels
            "EmptyKeys.UserInterface.dll",
            "EmptyKeys.UserInterface.Core.dll",
            "SixLabors.Core.dll",
            "SixLabors.ImageSharp.dll"
        ];


        internal static readonly string[] CoreSpaceEngineersFiles = RequiredAssemblies;

        internal static readonly string[] OptionalSpaceEngineersFiles = [
            "msvcp120.dll",                     // VRage.Native dependancy.  // testing dropping it these. Keen may have made a mistake by removing them from DS deployment.
            "msvcr120.dll",                     // VRage.Native dependancy.
        ];

        //internal static readonly string[] CoreMedievalEngineersFiles = {
        //    "Sandbox.Common.dll",
        //    "MedievalEngineers.ObjectBuilders.dll",
        //    "MedievalEngineers.ObjectBuilders.XmlSerializers.dll",
        //    "Sandbox.Game.dll",
        //    "HavokWrapper.dll",
        //    "VRage.dll",
        //    "VRage.Game.dll",
        //    "VRage.Game.XmlSerializers.dll",
        //    "VRage.Library.dll",
        //    "VRage.Math.dll"
        //};

        #region Paths

        private static readonly string _steamRegistryPath = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\{(Environment.Is64BitProcess ? @"Wow6432Node\" : string.Empty)}\Valve\Steam", false)?.GetValue("InstallPath") as string;
        private static readonly string _steamPath =  Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
        public static string GetSteamFilePath() => _steamPath;
        private static readonly string _gameRegistryPath = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\{(Environment.Is64BitProcess ? @"Wow6432Node\" : string.Empty)}Microsoft\Windows\CurrentVersion\Uninstall\Steam App 244850", false)?.GetValue("InstallLocation") as string ?? _steamPath;
        private static readonly string _gamePath = Path.Combine(string.IsNullOrEmpty(GlobalSettings.Default.SEBinPath) ? _steamPath : _gameRegistryPath , @"SteamApps\common\SpaceEngineers")  ;
        private static readonly string _applicationFilePath = Path.Combine(_gamePath, @"Bin64");
        public static string GetApplicationFilePath() => _applicationFilePath;
        public static string GetApplicationContentPath() => Path.GetFullPath(Path.Combine(_applicationFilePath, @"..\Content"));
        
        
        #endregion
        #region IsSpaceEngineersInstalled
        /// <summary>
        /// Checks for key directory names from the game bin folder.
        /// </summary>
        /// <param name="installationPath"></param>
        /// <returns></returns>
        public static bool ValidateSpaceEngineersInstall(string installationPath)
        {

            var contentDirectoryPath = Path.Combine(installationPath, _applicationFilePath);
            const string executableName = "SpaceEngineers.exe";
            var directoryInfo = new DirectoryInfo(installationPath);
            var executablePath = Path.Combine(installationPath, executableName);
            if (directoryInfo.Exists && Directory.Exists(contentDirectoryPath))// && !File.Exists(executablePath))
            {
                return true;
            }
            return false;
        }

        #endregion

        #region IsBaseAssembliesChanged

        public static bool IsBaseAssembliesChanged()
        {
            string baseFilePath = GetApplicationFilePath();
            string appFilePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            foreach (var fileName in CoreSpaceEngineersFiles)
            {
                string baseFile = Path.Combine(baseFilePath, fileName);
                string appFile = Path.Combine(appFilePath, fileName);

                if (DoFilesDiffer(baseFile, appFile))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool UpdateBaseFiles()
        {
            string baseFilePath = GetApplicationFilePath();
            string appFilePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            var requiredFiles = new HashSet<string>(CoreSpaceEngineersFiles.Concat(OptionalSpaceEngineersFiles), StringComparer.OrdinalIgnoreCase);
            var existingFiles = new HashSet<string>(Directory.EnumerateFiles(baseFilePath, "*.dll", SearchOption.TopDirectoryOnly)
                                                             .Select(Path.GetFileName));

            var filesToCopy = requiredFiles.Where(file => !existingFiles.Contains(file))
                                           .Select(file => new { Source = Path.Combine(appFilePath, file), Destination = Path.Combine(baseFilePath, file) })
                                           .ToList();

            if (filesToCopy.Count == 0)
            {
                return false;
            }
            foreach (var file in filesToCopy)
            {
                File.Copy(file.Source, file.Destination, true);
            }
            return true;
        }

        #endregion

        #region DoFilesDiffer

        public static bool DoFilesDiffer(string directoryA, string directoryB, string fileName)
        {
            return DoFilesDiffer(Path.Combine(directoryA, fileName), Path.Combine(directoryB, fileName));
        }

        public static bool DoFilesDiffer(string fileA, string fileB)
        {
            using var stream1 = File.Exists(fileA) ? new FileStream(fileA, FileMode.Open, FileAccess.Read) : null;
            using var stream2 = File.Exists(fileB) ? new FileStream(fileB, FileMode.Open, FileAccess.Read) : null;
            if (Conditional.NotNull(stream1, stream2))
            {
                using var md5A = MD5.Create();
                using var md5B = MD5.Create();

                var bufferA = new byte[stream1.Length];
                var bufferB = new byte[stream2.Length];

                var hashA = md5A.ComputeHash(bufferA);
                var hashB = md5B.ComputeHash(bufferB);

                return hashA != hashB || !Enumerable.SequenceEqual(bufferA, bufferB);
            }
            return false;
        }

        #endregion

        #region IsRunningElevated

        private static bool? _isRunningElevated = null;

        internal static bool IsRunningElevated()
        {
            if (_isRunningElevated.HasValue)
            {
                return _isRunningElevated.Value;
            }

            var identity = WindowsIdentity.GetCurrent();

            if (identity == null)
            {
                return false;
            }

            var pricipal = new WindowsPrincipal(identity);
            _isRunningElevated = pricipal.IsInRole(WindowsBuiltInRole.Administrator);

            return _isRunningElevated.Value;
        }

        #endregion

        #region RunElevated

        internal static int? RunElevated(string fileName, string arguments, bool? elevate, bool waitForExit)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false
            };

            if (elevate == true)
            {
                processInfo.Verb = "runas";
                processInfo.UseShellExecute = true;
            }

            try
            {
                var process = Process.Start(processInfo);

                if (elevate is not null && waitForExit)
                {
                    process?.WaitForExit();
                    return process.ExitCode;
                }

                return 0;
            }
            catch (Win32Exception)
            {
                // Do nothing. Probably the user canceled the UAC window
                return null;
            }
        }

        #endregion

        #region GetBinCachePath

        public static string GetBinCachePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"MidSpace\SEToolbox\__bincache");
        }

        #endregion
    }
}
