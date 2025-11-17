using Microsoft.VisualBasic.FileIO;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Networking;
using Sandbox.Game.GUI;
using SEToolbox.Interop;
using SEToolbox.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using VRage.FileSystem;
using VRage.Game;
using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.Models
{
    public class WorldResource : BaseModel
    {
        #region Fields

        private string _groupDescription;
        private SaveWorldType _saveType;
        private string _userName;
        private string _saveName;
        private string _savePath;
        private MyObjectBuilder_Checkpoint _checkpoint;
        private bool _compressedCheckpointFormat;
        private MyObjectBuilder_Sector _sectorData;
        private bool _compressedSectorFormat;
        private readonly SpaceEngineersResources _resources;
        private bool _isValid;
        private Version _version;
        private ulong? _workshopId;
        private string _sessionName;
        private DateTime _lastSaveTime;

        #endregion

        public WorldResource()
        {
            _resources = new SpaceEngineersResources();
        }
        #region Properties

        public string GroupDescription
        {
            get => _groupDescription;

            set => SetProperty(ref _groupDescription, value, nameof(GroupDescription));
        }

        public SaveWorldType SaveType
        {
            get => _saveType;

            set => SetProperty(ref _saveType, value, nameof(SaveType));
        }

        /// <summary>
        /// This will be the SteamId of the local user, or the Instance name of the Server.
        /// </summary>
        public string UserName
        {
            get => _userName;

            set => SetProperty(ref _userName, value, nameof(UserName));
        }

        public string SaveName
        {
            get => _saveName;

            set => SetProperty(ref _saveName, value, nameof(SaveName));
        }

        public string SavePath
        {
            get => _savePath;

            set => SetProperty(ref _savePath, value, nameof(SavePath));
        }

        public UserDataPath DataPath { get; set; }

        public MyObjectBuilder_Checkpoint Checkpoint
        {
            get => _checkpoint;

            set
            {
                SetProperty(ref _checkpoint, value,
                () =>
                    {
                        IsValid = _checkpoint != null;
                        if (_checkpoint == null)
                        {
                            _version = new Version();
                            _sessionName = Res.ErrorInvalidSaveLabel;
                            _lastSaveTime = DateTime.MinValue;
                        }
                        else
                        {
                            try
                            {
                                var str = _checkpoint.AppVersion.ToString(CultureInfo.InvariantCulture);
                                str = str.Substring(0, str.Length - 6) + "." + str.Substring(str.Length - 6, 3) + "." + str.Substring(str.Length - 3);
                                _version = new Version(str);
                            }
                            catch
                            {
                                _version = new Version();
                            }

                            _sessionName = _checkpoint?.SessionName;
                            _lastSaveTime = _checkpoint.LastSaveTime;
                        }

                        WorkshopId = _checkpoint?.WorkshopId;
                    },

                        nameof(Checkpoint), nameof(SessionName), nameof(LastSaveTime), nameof(IsValid),

                        _isValid = _checkpoint != null); }
             
            }

        
        
        public string SessionName
        {
            get => _sessionName;

            set => SetProperty(ref _sessionName, value, nameof(SessionName));
        }

        public DateTime LastSaveTime
        {
            get => _lastSaveTime;

            set => SetProperty(ref _lastSaveTime, value, nameof(LastSaveTime));

        }

        public Version Version
        {
            get => _version;

            set => SetProperty(ref _version, value, nameof(Version));
        }

        public bool IsWorkshopItem => _workshopId.HasValue;

        public ulong? WorkshopId
        {
            get => _workshopId;

            set => SetProperty(ref _workshopId, value, nameof(WorkshopId));
  
        }

        public bool IsValid
        {
            get => _isValid;

            set => SetProperty(ref _isValid, value, nameof(IsValid));
        }

        public string ThumbnailImageFileName => Path.Combine(SavePath, SpaceEngineersConsts.ThumbnailImageFileName);

        public override string ToString()
        {
            return SessionName;
        }

        public MyObjectBuilder_Sector SectorData
        {
            get => _sectorData;

            set => SetProperty(ref _sectorData, value, nameof(SectorData));
        }

        public SpaceEngineersResources Resources
        {
            get => _resources; 
        }

        #endregion

        #region Methods

        #region Load and Save

        /// <summary>
        /// Loads checkpoint file.
        /// </summary>
        public bool LoadCheckpoint(out string errorInformation, bool snapshot = false)
        {
            string fileName = Path.Combine(SavePath, SpaceEngineersConsts.SandBoxCheckpointFileName);

            bool retVal = SpaceEngineersApi.TryReadSpaceEngineersFile(fileName, out MyObjectBuilder_Checkpoint checkpoint, out _compressedCheckpointFormat, out errorInformation, snapshot);
            Checkpoint = checkpoint;
            return retVal;
        }

        public void LoadDefinitionsAndMods()
        {
            if (_resources == null || Checkpoint == null || Checkpoint.Mods == null)
                return;

            SpaceEngineersWorkshop.DownloadWorldModsBlocking(Checkpoint.Mods, cancelToken: null);

            _resources.LoadDefinitionsAndMods(DataPath.ModsPath, Checkpoint.Mods);
        }

        public bool LoadSector(out string errorInformation, bool snapshot = false)
        {
            string fileName = Path.Combine(SavePath, SpaceEngineersConsts.SandBoxSectorFileName);

            bool retVal = SpaceEngineersApi.TryReadSpaceEngineersFile(fileName, out MyObjectBuilder_Sector sectorData, out _compressedSectorFormat, out errorInformation, snapshot);
            SectorData = sectorData;
            return retVal;
        }

        public void SaveCheckPoint(bool backupFile)
        {
            string checkpointFileName = Path.Combine(SavePath, SpaceEngineersConsts.SandBoxCheckpointFileName);

            if (backupFile)
            {
                string checkpointBackupFileName = checkpointFileName + ".bak";

                if (File.Exists(checkpointBackupFileName))
                {
                    FileSystem.DeleteFile(checkpointBackupFileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }

                File.Move(checkpointFileName, checkpointBackupFileName);
            }

            if (_compressedCheckpointFormat)
            {
                string tempFileName = TempFileUtil.NewFileName();
                SpaceEngineersApi.WriteSpaceEngineersFile(Checkpoint, tempFileName);
                ZipTools.GZipCompress(tempFileName, checkpointFileName);
            }
            else
            {
                SpaceEngineersApi.WriteSpaceEngineersFile(Checkpoint, checkpointFileName);
            }
        }

        public void SaveSector(bool backupFile)
        {
            string sectorFileName = Path.Combine(SavePath, SpaceEngineersConsts.SandBoxSectorFileName);

            if (backupFile)
            {
                // xml sector file.  (it may or may not be compressed)
                string sectorBackupFileName = sectorFileName + ".bak";

                if (File.Exists(sectorBackupFileName))
                    FileSystem.DeleteFile(sectorBackupFileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    File.Move(sectorFileName, sectorBackupFileName);

                // binary sector file. (it may or may not be compressed)
                sectorBackupFileName = sectorFileName + SpaceEngineersConsts.ProtobuffersExtension + ".bak";

                if (File.Exists(sectorBackupFileName))
                    FileSystem.DeleteFile(sectorBackupFileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

                // The protoBuf (.sbsPB, .sbsB1) may not exist in older save games.
                if (File.Exists(sectorFileName + SpaceEngineersConsts.ProtobuffersExtension))
                    File.Move(sectorFileName + SpaceEngineersConsts.ProtobuffersExtension, sectorBackupFileName);
            }

            if (_compressedSectorFormat)
            {
                string tempFileName = TempFileUtil.NewFileName();
                SpaceEngineersApi.WriteSpaceEngineersFile(SectorData, tempFileName);
                ZipTools.GZipCompress(tempFileName, sectorFileName);
            }
            else
            {
                SpaceEngineersApi.WriteSpaceEngineersFile(SectorData, sectorFileName);
            }
            SpaceEngineersApi.WriteSpaceEngineersFilePB(SectorData, sectorFileName + SpaceEngineersConsts.ProtobuffersExtension, _compressedSectorFormat);
        }

        public XmlDocument LoadSectorXml()
        {
            string fileName = Path.Combine(SavePath, SpaceEngineersConsts.SandBoxCheckpointFileName);
            XmlDocument xDoc;
            try
            {
                if (ZipTools.IsGzipedFile(fileName))
                {
                    // New file format is compressed.
                    // These steps could probably be combined, but would have to use a MemoryStream, which has memory limits before it causes performance issues when chunking memory.
                    // Using a temporary file in this situation has less performance issues as it's moved straight to disk.
                    string tempFileName = TempFileUtil.NewFileName();
                    ZipTools.GZipUncompress(fileName, tempFileName);
                    xDoc = new XmlDocument();
                    xDoc.Load(tempFileName);
                    _compressedCheckpointFormat = true;
                }
                else
                {
                    // Old file format is raw XML.
                    xDoc = new XmlDocument();
                    xDoc.Load(fileName);
                    _compressedCheckpointFormat = false;
                }
            }
            catch (Exception ex) when (ex is ArgumentNullException || ex is NullReferenceException || ex is UnauthorizedAccessException)
            {
                SConsole.WriteLine($"Error loading WorldResource.LoadSectorXml: {ex.Message}");
                return null;
            }

            return xDoc;
        }

        public void SaveSectorXml(bool backupFile, XmlDocument xDoc)
        {
            string sectorFileName = Path.Combine(SavePath, SpaceEngineersConsts.SandBoxSectorFileName);

            if (backupFile)
            {
                string sectorBackupFileName = sectorFileName + ".bak";

                if (File.Exists(sectorBackupFileName))
                {
                    FileSystem.DeleteFile(sectorBackupFileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }

                File.Move(sectorFileName, sectorBackupFileName);
            }

            if (_compressedSectorFormat)
            {
                string tempFileName = TempFileUtil.NewFileName();
                xDoc.Save(tempFileName);
                ZipTools.GZipCompress(tempFileName, sectorFileName);
            }
            else
            {
                xDoc.Save(sectorFileName);
            }
        }

        public void SaveCheckPointAndSector(bool backupFile)
        {
            LastSaveTime = DateTime.Now;
            Checkpoint.AppVersion = SpaceEngineersConsts.GetSEVersionInt();
            SectorData.AppVersion = SpaceEngineersConsts.GetSEVersionInt();
            SaveCheckPoint(backupFile);
            SaveSector(backupFile);
        }

        public void LoadWorldInfo()
        {
            string fileName = Path.Combine(SavePath, SpaceEngineersConsts.SandBoxCheckpointFileName);

            if (!File.Exists(fileName))
            {
                IsValid = false;
                SessionName = Res.ErrorInvalidSaveLabel;
                return;
            }

            try
            {
                XDocument doc;
                using (Stream stream = MyFileSystem.OpenRead(fileName).UnwrapGZip())
                {
                    doc = XDocument.Load(stream);
                }

                XElement root = doc.Root;
                if (root == null)
                {
                    IsValid = true;
                    SessionName = Res.ErrorInvalidSaveLabel;
                    return;
                }

                XElement session = root.Element("SessionName");
                XElement lastSaveTime = root.Element("LastSaveTime");
                XElement workshopId = root.Element("WorkshopId");
                XElement appVersion = root.Element("AppVersion");

                if (session != null) 
                SessionName = MyStatControlText.SubstituteTexts(session.Value);
                if ( lastSaveTime != null && DateTime.TryParse(lastSaveTime?.Value, out DateTime tempDateTime))
                    LastSaveTime = tempDateTime;
                else
                    LastSaveTime = DateTime.MinValue;

                if (workshopId != null && ulong.TryParse(workshopId.Value, out ulong tmp))
                    WorkshopId = tmp;

                if (appVersion == null || appVersion?.Value == "0")
                    Version = new Version();
                else
                {
                    try
                    {
                        string str = appVersion.Value.Substring(0, appVersion.Value.Length - 6) + "." + appVersion.Value.Substring(appVersion.Value.Length - 6, 3) + "." + appVersion.Value.Substring(appVersion.Value.Length - 3);
                        Version = new Version(str);
                    }
                    catch
                    {
                        Version = new Version();
                    }
                }

                IsValid = true;
            }
            catch
            {
                IsValid = false;
                SessionName = Res.ErrorInvalidSaveLabel;
            }
        }

        #endregion

        #region Miscellaneous

        public MyObjectBuilder_Character FindPlayerCharacter()
        {
            if (SectorData == null || Checkpoint == null)
                return null;

            foreach (var entityBase in SectorData.SectorObjects)
            {
                if (entityBase is MyObjectBuilder_Character character && character.EntityId == Checkpoint.ControlledObject)
                {
                    return character;
                }

                if (entityBase is MyObjectBuilder_CubeGrid cubeGrid)
                {
                    foreach (var cube in cubeGrid.CubeBlocks.Where(e => e.EntityId == Checkpoint.ControlledObject && e is MyObjectBuilder_Cockpit))
                    {
                        List<MyObjectBuilder_Character> pilots = cube.GetHierarchyCharacters();
                        if (pilots.Count > 0)
                            return pilots[0];
                    }
                }
            }

            return null;
        }

        public MyObjectBuilder_Character FindAstronautCharacter()
        {
            return SectorData?.SectorObjects.OfType<MyObjectBuilder_Character>().FirstOrDefault();
        }

        public MyObjectBuilder_Cockpit FindPilotCharacter()
        {
            if (SectorData != null)
            {
                foreach (var entityBase in SectorData.SectorObjects)
                {
                    if (entityBase is MyObjectBuilder_CubeGrid grid)
                    {
                        foreach (var cube in grid.CubeBlocks.Where(e => e is MyObjectBuilder_Cockpit))
                        {
                            List<MyObjectBuilder_Character> pilots = cube.GetHierarchyCharacters();
                            if (pilots.Count > 0)
                                return (MyObjectBuilder_Cockpit)cube;
                        }

                    }
                }
            }

            return null;
        }

        #endregion

        #endregion
    }
}
