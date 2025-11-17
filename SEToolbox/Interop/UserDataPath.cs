using System;
using System.Collections.Generic;
using System.IO;
using SEConsts = SEToolbox.Interop.SpaceEngineersConsts;
namespace SEToolbox.Interop
{


    public class UserDataPath(string basePath, string savesPathPart, string modsPathPart, string blueprintsPathPart,string shaderPathPart = null, string modsCachePathPart = null )
    {
        #region Ctor
        #region Properties
        public string DataPath { get; set; } = basePath;
        public string SavesPath { get; set; } = Path.Combine(basePath, savesPathPart);
        public string ModsPath { get; set; } = Path.Combine(basePath, modsPathPart);
        public string BlueprintsPath { get; set; } = blueprintsPathPart != null ? Path.Combine(basePath, blueprintsPathPart) : null;
        //public string BackupsPath { get; set; } = Path.Combine(basePath, backupsPathPart);
        public string ShaderPath { get; set; } = shaderPathPart != null ? Path.Combine(basePath, shaderPathPart) : null;
        //or is it shaders2
        public string ModsCache { get; set; } = modsCachePathPart != null ? Path.Combine(basePath, modsCachePathPart) : null;
    
        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Determine the correct UserDataPath for this save game if at all possible to allow finding the mods folder.
        /// </summary>
        /// <param name="savePath"></param>
        /// <returns></returns>
        public static UserDataPath FindFromSavePath(string savePath)
        {
            UserDataPath dataPath = SEConsts.BaseLocalPath;
            string basePath = GetPathBase(savePath, SEConsts.Folders.SavesFolder);
            if (basePath != null)
            {
                dataPath = new UserDataPath(basePath,
                               SEConsts.Folders.SavesFolder,
                               SEConsts.Folders.ModsFolder,
                               SEConsts.Folders.BlueprintsFolder
                               );
            }

            return dataPath;
        }

        #endregion

        #region Helpers

        private static string GetPathBase(string path, string baseName)
        {
            string currentPath = path;
            while (true)
            {
                string currentName = Path.GetFileName(currentPath);
                if (currentName.Equals(baseName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return currentPath;
                }
                string parentPath = Path.GetDirectoryName(currentPath);
                if (parentPath == null || parentPath == currentPath)
                {
                    return null;
                }
                currentPath = parentPath;
            }
        }

        public string GetDataPathOrDefault(string key, string defaultValue)
        {

            // TODO: this code is obsolete and needs to be cleaned up.
            // #31 https://github.com/midspace/SEToolbox/commit/354fd4cba31d1d8accac4c8188189dd1b114209b#diff-816c9c8868fbb3625db0cc45485797ef
            //if deleted this breaks things, something else needs to be done.

            var userDataPath = new UserDataPath(SEConsts.BaseLocalPath.DataPath, SEConsts.Folders.SavesFolder, SEConsts.Folders.ModsFolder, SEConsts.Folders.BlueprintsFolder);
            string path = userDataPath.GetPathOrDefault(key);

            if (string.IsNullOrWhiteSpace(path))
                return defaultValue;

            return path;
        }
        internal static readonly Dictionary<string, string> PathMap = new()
        {
            {SEConsts.Folders.ModsFolder, nameof(ModsPath)},
            {SEConsts.Folders.BlueprintsFolder, nameof(BlueprintsPath)},
            {SEConsts.Folders.ModsCacheFolder, nameof(ModsCache)},
            {SEConsts.Folders.ShadersFolder, nameof(ShaderPath)},
        };

        internal string GetPathOrDefault(string key)
        {
            return PathMap.TryGetValue(key, out var value) ? value : null;
        }
        
        #endregion
    }
} 