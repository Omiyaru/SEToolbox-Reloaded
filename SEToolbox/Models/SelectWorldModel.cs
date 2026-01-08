using SEToolbox.Converters;
using SEToolbox.Interop;
using SEToolbox.Support;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Res = SEToolbox.Properties.Resources;
using SEConsts = SEToolbox.Interop.SpaceEngineersConsts;

namespace SEToolbox.Models
{
    public class SelectWorldModel : BaseModel
    {
        #region Fields

        private WorldResource _selectedWorld;

        private ObservableCollection<WorldResource> _worlds;

        private bool _isBusy;

        #endregion

        #region Ctor

        public SelectWorldModel()
        {
            SelectedWorld = null;
            Worlds = [];
        }

        #endregion

        #region Properties

        /// <summary>
        /// The base path of the save files, minus the userid.
        /// </summary>
        public UserDataPath BaseLocalPath { get; set; }

        public UserDataPath BaseDedicatedServerHostPath { get; set; }

        public UserDataPath BaseDedicatedServerServicePath { get; set; }

        public WorldResource SelectedWorld
        {
            get => _selectedWorld;
            set => SetProperty(ref _selectedWorld, value, nameof(SelectedWorld));
        }

        public ObservableCollection<WorldResource> Worlds
        {
            get => _worlds;
            set => SetProperty(ref _worlds, value, nameof(Worlds));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value, nameof(IsBusy), () =>
            {
                if (_isBusy)
                {
                       System.Windows.Forms.Application.DoEvents();
                }
            });

        }

        #endregion

        #region Methods

        public void Load(UserDataPath baseLocalPath, UserDataPath baseDedicatedServerHostPath, UserDataPath baseDedicatedServerServicePath)
        {
            BaseLocalPath = baseLocalPath;
            BaseDedicatedServerHostPath = baseDedicatedServerHostPath;
            BaseDedicatedServerServicePath = baseDedicatedServerServicePath;
            LoadSaveList();
        }

        public void Refresh()
        {
            LoadSaveList();
        }

        #endregion

        #region Helpers

        private void LoadSaveList()
        {
            Worlds.Clear();
            List<WorldResource> list = [];

            #region Local Saves

            if (Directory.Exists(BaseLocalPath.SavesPath))
            {
                string[] userPaths = Directory.GetDirectories(BaseLocalPath.SavesPath);

                foreach (string userPath in userPaths)
                {
                    string userName = Path.GetFileName(userPath);
                    list.AddRange(FindSaveFiles(userPath, userName, SaveWorldType.Local, BaseLocalPath));
                }
            }

            #endregion

            #region Host Server

            if (Directory.Exists(BaseDedicatedServerHostPath.SavesPath))
            {
                list.AddRange(FindSaveFiles(BaseDedicatedServerHostPath.SavesPath, "Local / Console", SaveWorldType.DedicatedServerHost, BaseDedicatedServerHostPath));
            }

            #endregion

            #region Service Server

            if (Directory.Exists(BaseDedicatedServerServicePath.SavesPath))
            {
                string[] instancePaths = Directory.GetDirectories(BaseDedicatedServerServicePath.SavesPath);

                foreach (string instancePath in instancePaths)
                {
                    string lastLoadedPath = Path.Combine(instancePath, SEConsts.Folders.SavesFolder);

                    if (Directory.Exists(lastLoadedPath))
                    {
                        string instanceName = Path.GetFileName(instancePath);
                        UserDataPath dataPath = new(instancePath, SEConsts.Folders.SavesFolder, SEConsts.Folders.ModsFolder, SEConsts.Folders.BlueprintsFolder);
                        list.AddRange(FindSaveFiles(lastLoadedPath, instanceName, SaveWorldType.DedicatedServerService, dataPath));
                    }
                }
            }

            #endregion

            foreach (WorldResource item in list.OrderByDescending(w => w.LastSaveTime))
            {
                Worlds.Add(item);
            }
        }

        private static IEnumerable<WorldResource> FindSaveFiles(string lastLoadedPath, string userName, SaveWorldType saveType, UserDataPath dataPath)
        {
            List<WorldResource> list = [];

            // Ignore any other base Save paths without the LastLoaded file.
            if (Directory.Exists(lastLoadedPath))
            {
                string[] savePaths = Directory.GetDirectories(lastLoadedPath);

                // Still check every potential game world path.
                foreach (string savePath in savePaths)
                {
                    var saveResource = LoadSaveFromPath(savePath, userName, saveType, dataPath);
                    // This should still allow Games to be copied into the Save path manually.
                    saveResource.LoadWorldInfo();
                    list.Add(saveResource);
                }
            }

            return list;
        }

        internal static WorldResource LoadSaveFromPath(string savePath, string userName, SaveWorldType saveType, UserDataPath dataPath)
        {
            WorldResource saveResource = new()
            {
                GroupDescription = $"{new EnumToResourceConverter().Convert(saveType, typeof(string), null, CultureInfo.CurrentUICulture)}: {userName}",
                SaveType = saveType,
                SaveName = Path.GetFileName(savePath),
                UserName = userName,
                SavePath = savePath,
                DataPath = dataPath,
            };

            return saveResource;
        }

        internal static bool FindSaveSession(string baseSavePath, string findSession, out WorldResource saveResource, out string errorInformation)
        {
            if (Directory.Exists(baseSavePath))
            {
                string[] userPaths = Directory.GetDirectories(baseSavePath);

                foreach (string userPath in userPaths)
                {
                    // Ignore any other base Save paths without the LastLoaded file.
                    if (Directory.Exists(userPath))
                    {
                        string[] savePaths = Directory.GetDirectories(userPath);

                        // Still check every potential game world path.
                        foreach (string savePath in savePaths)
                        {
                            saveResource = new WorldResource
                            {
                                SaveName = Path.GetFileName(savePath),
                                UserName = Path.GetFileName(userPath),
                                SavePath = savePath,
                                DataPath = UserDataPath.FindFromSavePath(savePath)
                            };

                            saveResource.LoadWorldInfo();
                            if (saveResource.IsValid && 
                               (saveResource.SaveName.Equals(findSession, System.StringComparison.CurrentCultureIgnoreCase) || 
                                saveResource.SessionName.Equals(findSession, System.StringComparison.CurrentCultureIgnoreCase)))
                            {
                                return saveResource.LoadCheckpoint(out errorInformation);
                            }
                        }
                    }
                }
            }

            saveResource = null;
            errorInformation = Res.ErrorGameNotFound;
            return false;
        }

        internal static bool LoadSession(string savePath, out WorldResource saveResource, out string errorInformation)
        {
            if (Directory.Exists(savePath))
            {
                string userPath = Path.GetDirectoryName(savePath);

                saveResource = new WorldResource
                {
                    SaveName = Path.GetFileName(savePath),
                    UserName = Path.GetFileName(userPath),
                    SavePath = savePath,
                    DataPath = UserDataPath.FindFromSavePath(savePath)
                };

                return saveResource.LoadCheckpoint(out errorInformation);
            }

            saveResource = null;
            errorInformation = Res.ErrorDirectoryNotFound;
            return false;
        }

        #endregion
    }
}
