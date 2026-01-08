using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;

namespace SEToolbox.Support
{
    public static class ToolboxUpdater
    {
        internal static readonly string[] RequiredAssemblies = [
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

        /// <summary>
        /// Required dependancies which must be copied for SEToolbox to work.
        /// </summary>
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

        #region GetApplicationFilePath

        public static string GetApplicationFilePath()
        {
            var gamePath = GlobalSettings.Default.SEBinPath;

            if (string.IsNullOrEmpty(gamePath))
            {
                // We use the Bin64 Path, as these assemblies are marked "AllCPU", and will work regardless of processor architecture.
                gamePath = GetGameRegistryFilePath();
                if (!string.IsNullOrEmpty(gamePath))
                    gamePath = Path.Combine(gamePath, "Bin64");
            }

            return gamePath;
        }

        public static string GetApplicationContentPath()
        {
            return Path.GetFullPath(Path.Combine(GetApplicationFilePath(), @"..\Content"));
        }

        /// <summary>
        /// Looks for the Space Engineers install location in the Registry, which should return the form:
        /// "C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers"
        /// </summary>
        /// <returns></returns>
        public static string GetGameRegistryFilePath()
        {
            string keypath = @"SOFTWARE\" + (@"Wow6432Node\" ?? null) + @"Microsoft\Windows\CurrentVersion\Uninstall\Steam App 244850";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(keypath, false);
            if (Environment.Is64BitProcess)
                key = Registry.LocalMachine.OpenSubKey(keypath, false);

            if (key != null)
                return key.GetValue("InstallLocation") as string;

            // Backup check, but no choice if the above goes to pot.
            // Using the [Software\Valve\Steam\SteamPath] as a base for "\steamapps\common\SpaceEngineers", is unreliable, as the Steam Library is customizable and could be on another drive and directory.
            string steamPath = GetSteamFilePath();
            if (!string.IsNullOrEmpty(steamPath))
            {
                return Path.Combine(steamPath, @"SteamApps\common\SpaceEngineers" ?? @"steamapps\common\SpaceEngineers");
            }

            return null;
        }

        #endregion

        #region GetSteamFilePath

        /// <summary>
        /// Looks for the Steam install location in the Registry, which should return the form:
        /// "C:\Program Files (x86)\Steam"
        /// </summary>
        /// <returns></returns>
        public static string GetSteamFilePath()
        {
            RegistryKey key;
            string keypath = @"SOFTWARE\" + (@"Wow6432Node\" ?? null) + @"\Valve\Steam";

            if (Environment.Is64BitProcess)
            {
                key = Registry.LocalMachine.OpenSubKey(keypath, false);

                if (key != null)
                {
                    return (string)key.GetValue("InstallPath");
                }

            }
            return null;
        }
        #endregion

        #region IsSpaceEngineersInstalled

        /// <summary>
        /// Checks for key directory names from the game bin folder.
        /// </summary>
        /// <param name="installationPath"></param>
        /// <returns></returns>

        public static bool ValidateSpaceEngineersInstall(string installationPath)
        {
            const string executableName = "SpaceEngineers.exe";
            const string contentPath = @"..\Content";

            if (string.IsNullOrWhiteSpace(installationPath) ||
                string.IsNullOrEmpty(installationPath) &&
                !Directory.Exists(installationPath) &&
                !Directory.Exists(Path.Combine(installationPath, contentPath))

                && !File.Exists(Path.Combine(executableName)))
            {
                return false;
            }
            // Skip checking for the .exe. Not required for the Toolbox currently.
            return true;
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
                    return true;
            }

            return false;
        }

        public static bool UpdateBaseFiles()
        {
            string baseFilePath = GetApplicationFilePath();
            string appFilePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            var requiredFiles = new HashSet<string>(CoreSpaceEngineersFiles.Concat(OptionalSpaceEngineersFiles), StringComparer.OrdinalIgnoreCase);
            var existingFiles = new HashSet<string>(Directory.EnumerateFiles(baseFilePath, "*.dll", SearchOption.TopDirectoryOnly)
                                                             .Select(f => Path.GetFileName(f)));

            var filesToCopy = requiredFiles.Where(f => !existingFiles.Contains(f))
                                           .Select(f => new { Source = Path.Combine(appFilePath, f), Destination = Path.Combine(baseFilePath, f) })
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

        public static bool DoFilesDiffer(string fileAPath, string fileBPath)
        {
            using var stream1 = File.Exists(fileAPath) ? new FileStream(fileAPath, FileMode.Open, FileAccess.Read) : null;
            using var stream2 = File.Exists(fileBPath) ? new FileStream(fileBPath, FileMode.Open, FileAccess.Read) : null;
            if (Conditional.NotNull(stream1, stream2))
            {
                var bufferA = new byte[stream1.Length];
                var bufferB = new byte[stream2.Length];

                var readStreamA = stream1.Read(bufferA, 0, bufferA.Length);
                var readStreamB = stream2.Read(bufferB, 0, bufferB.Length);

                if (readStreamA != readStreamB)
                    return true;


                return !Enumerable.SequenceEqual(bufferA, bufferB);

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
                return false;

            var pricipal = new WindowsPrincipal(identity);
            _isRunningElevated = pricipal.IsInRole(WindowsBuiltInRole.Administrator);

            return _isRunningElevated.Value;
        }

        #endregion

        #region RunElevated

        internal static int? RunElevated(string fileName, string arguments, bool elevate, bool waitForExit)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments
            };

            if (elevate)
                processInfo.Verb = "runas";

            try
            {
                var process = Process.Start(processInfo);

                if (!ReferenceEquals(null, elevate) && waitForExit)
                {
                    process.WaitForExit();
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
