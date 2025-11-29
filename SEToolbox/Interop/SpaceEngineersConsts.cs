using System;
using System.IO;
using System.Reflection;

using MOBSerializerKeen = VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen;
using SEGame = SpaceEngineers.Game.SpaceEngineersGame;
using SF = System.Environment.SpecialFolder;
using VRage.Utils;

namespace SEToolbox.Interop
{
    public class SpaceEngineersConsts
    {
        /// <summary>
        /// Thumbnail image of last position in save.
        /// </summary>
        public const string ThumbnailImageFileName = "thumb.jpg";

        /// <summary>
        /// Contains summary of save content filename.
        /// </summary>
        public const string SandBoxCheckpointFileName = "Sandbox.sbc";

        /// <summary>
        /// Contains Xml serialized main content filename.
        /// </summary>
        public const string SandBoxSectorFileName = "SANDBOX_0_0_0_.sbs";

        /// <summary>
        /// This is the file extension added to the normal filename for Sanbox files, changing the ".sbs" to ".sbsPB"
        /// </summary>
        public readonly static string ProtobuffersExtension = MOBSerializerKeen.ProtobufferExtension ?? "PB";

        public const byte EmptyVoxelMaterial = 0xff;

        // Current set max speed m/s for Ships.
        public const float MaxShipVelocity = 104.375f;

        // Current set max speed m/s for Players - as of update 01.023.
        public const float MaxPlayerVelocity = 111.531f;

        // Estimated max speed m/s for Meteors - as of update 01.024.
        public const float MaxMeteorVelocity = 202.812f;

        public const float PlayerMass = 100f;

        /// <summary>
        /// Converts the internal game value (mL) to the nominal metric (L) for display.
        /// </summary>
        public const float VolumeMultiplier = 1000f;

        /// <summary>
        /// The base path of the save files, minus the userid.
        /// </summary>
        public static readonly UserDataPath BaseLocalPath;
        public static readonly UserDataPath BaseDedicatedServerHostPath;
        public static readonly UserDataPath BaseDedicatedServerServicePath;

        public static class Folders
        {
            public const string SavesFolder = "Saves";
            public const string ModsFolder = "Mods";
            public const string BlueprintsFolder = "Blueprints";
            public const string LocalBlueprintsSubFolder = "local";
            public const string BackupsFolder = "Backups";
            public const string ModsCacheFolder = "ModCache";
            public const string ShadersFolder = "Shaders";//"Shaders2??";
        }

        static SpaceEngineersConsts()
        {
            // Don't access the ObjectBuilders from the static Ctor, as it will cause issues with the Serializer type loader. 

            string basePath = "SpaceEngineers";
            //if (GlobalSettings.Default.SEBinPath.Contains("MedievalEngineers", StringComparison.Ordinal))
            //    basePath = "MedievalEngineers";

            BaseLocalPath = new UserDataPath(Path.Combine(Environment.GetFolderPath(SF.ApplicationData), basePath), Folders.SavesFolder, Folders.ModsFolder, Folders.BlueprintsFolder); // Followed by .\%SteamuserId%\LastLoaded.sbl
            BaseDedicatedServerHostPath = new UserDataPath(Path.Combine(Environment.GetFolderPath(SF.ApplicationData), basePath + "Dedicated"), Folders.SavesFolder, Folders.ModsFolder, null); // Followed by .\LastLoaded.sbl
            BaseDedicatedServerServicePath = new UserDataPath(Path.Combine(Environment.GetFolderPath(SF.CommonApplicationData), basePath + "Dedicated"), savesPathPart: "", "", null); // Followed by .\%instancename%\Saves\LastLoaded.sbl  (.\%instancename%\Mods
        }

        public static string BuildNumberToString(int buildInt, string separator = ".")
        {
            return $"{buildInt / 1000:D2}{separator}{buildInt / 100 % 10:D1}{separator}{buildInt % 100:D2}";
        }
        

        public static Version GetSEVersion()
        {
            try
            {
                return new(BuildNumberToString(new MyVersion(GetSEVersionInt())));//.Replace("_", ".")
            }
            catch
            {
                return new();
            }
        }
        
        public static int GetSEVersionInt() => SE_VERSION;
        private static readonly int SE_VERSION = typeof(SEGame).GetField(nameof(SEGame.SE_VERSION), BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) is int ver ? ver : 0;

        
    }
}
