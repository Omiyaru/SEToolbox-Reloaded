using Microsoft.VisualBasic.FileIO;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
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
            set => SetProperty(ref _checkpoint, value, () =>
            {
                if (_checkpoint == null)
                {
                    _version = new Version();
                    _sessionName = Res.ErrorInvalidSaveLabel;
                    _lastSaveTime = DateTime.MinValue;
                    _isValid = false;
                }
                else
                {
                    _version = Version.TryParse(_checkpoint.AppVersion.ToString(CultureInfo.InvariantCulture), out var v) ? v : new Version();
                    _sessionName = _checkpoint.SessionName ?? Res.ErrorInvalidSaveLabel;
                    _lastSaveTime = _checkpoint.LastSaveTime;
                    _isValid = true;
                    WorkshopId = _checkpoint?.WorkshopId;
                }
            }, nameof(Checkpoint), nameof(SessionName), nameof(LastSaveTime), nameof(IsValid));
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

        public string ThumbnailImageFileName => Path.Combine(_savePath, SpaceEngineersConsts.ThumbnailImageFileName);

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

            bool result = SpaceEngineersApi.TryReadSpaceEngineersFile(fileName, out MyObjectBuilder_Checkpoint checkpoint, out _compressedCheckpointFormat, out errorInformation, snapshot);
            Checkpoint = checkpoint;
            return result;
        }

        public void LoadDefinitionsAndMods()
        {
            if (Conditional.Null(_resources, Checkpoint, Checkpoint.Mods))
            {
                return;
            }

            List<MyObjectBuilder_Checkpoint.ModItem> mods = [.. Checkpoint.Mods];
            var result = SpaceEngineersWorkshop.DownloadWorldModsBlocking(mods, cancelToken: null);

            if (result.Result == 0)
            {
                mods.Clear();
            }

            _resources.LoadDefinitionsAndMods(DataPath.ModsPath, mods);
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
            string checkpointBackupFileName = checkpointFileName + ".bak";
            if (backupFile)
            { 
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
                string sectorBackupFileName = $"{sectorFileName}{SpaceEngineersConsts.ProtobuffersExtension ?? null}.bak";
                
                if (File.Exists(sectorBackupFileName))
                {
                    FileSystem.DeleteFile(sectorBackupFileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }

                File.Move(sectorFileName, sectorBackupFileName);

                // The protoBuf (.sbsPB, .sbsB1) may not exist in older save games.
                if (File.Exists(sectorFileName + SpaceEngineersConsts.ProtobuffersExtension))
                {
                    File.Move(sectorFileName + SpaceEngineersConsts.ProtobuffersExtension, sectorBackupFileName);
                }
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
            string tempFileName = TempFileUtil.NewFileName();
            var file = fileName ?? tempFileName;

            xDoc = new XmlDocument();

            var fileInfo = new FileInfo(file);
            try
            {
                if (ZipTools.IsGzipedFile(fileName))
                {
                    // New file format is compressed.
                    // Using a temporary file in this situation has less performance issues as it's moved straight to disk.

                    xDoc.Load(fileInfo.FullName);
                    ZipTools.GZipUncompress(fileName, tempFileName);
                    if (fileInfo.FullName == fileName)
                    {
                        _compressedCheckpointFormat = false;
                    }
                    else if (fileInfo.FullName == tempFileName)
                    {
                        _compressedCheckpointFormat = true;
                    }
                }
            }
            catch (Exception ex) when (ex is ArgumentNullException || ex is NullReferenceException || ex is UnauthorizedAccessException)
            {
                Log.WriteLine($"Error loading WorldResource.LoadSectorXml: {ex.Message}");
                return null;
            }
            return xDoc;
        }

        public void SaveSectorXml(bool backupFile, XmlDocument xDoc)
        {
            string sectorFileName = Path.Combine(SavePath, SpaceEngineersConsts.SandBoxSectorFileName);
            string sectorBackupFileName = sectorFileName + ".bak";
            if (backupFile)
            {
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
                using Stream stream = MyFileSystem.OpenRead(fileName).UnwrapGZip();
                doc = XDocument.Load(stream);
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

                SessionName = MyStatControlText.SubstituteTexts(session?.Value);
                LastSaveTime = DateTime.TryParse(lastSaveTime?.Value, out DateTime tempDateTime) ? tempDateTime : DateTime.MinValue;
                if (ulong.TryParse(workshopId?.Value, out ulong tmp))
                {
                    WorkshopId = tmp;
                }

                if (appVersion == null || appVersion.Value == "0")
                {
                    Version = new Version();
                }
                if (Version.TryParse(appVersion.Value, out Version version))
                {
                        Version = version;
                        //string subString = appVersion.Value.Substring(0, appVersion.Value.Length - 6) + "." + appVersion.Value.Substring(appVersion.Value.Length - 6, 3) + "." + appVersion.Value.Substring(appVersion.Value.Length - 3);
                        //Version = new Version(subString);
                }
                else
                {
                    Version = new Version();
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
        public List<MyObjectBuilder_Cockpit> GetCockpits() => [.. SectorData.SectorObjects.OfType<MyObjectBuilder_CubeGrid>().OfType<MyObjectBuilder_Cockpit>()];
        public List<MyObjectBuilder_Character> GetPilots() => [.. GetCockpits().Where(c => c.GetHierarchyCharacters().Count > 0) as IEnumerable<MyObjectBuilder_Character>];

        public List<MyObjectBuilder_Character> GetCharacters() => [.. SectorData.SectorObjects.OfType<MyObjectBuilder_Character>()];
        
        public MyObjectBuilder_Character FindPlayerCharacter()
        {
            SectorData ??= null;
            Checkpoint ??= null;

            foreach (var character in GetCharacters().Where(c => c.EntityId == Checkpoint.ControlledObject))
            {
              
                return character;
            }

            foreach (var pilot in GetPilots().Where(c => c.EntityId == Checkpoint.ControlledObject))


            {
                return pilot;
            }

            return null;
        }

        public MyObjectBuilder_Character FindAstronautCharacter()
        {
            return GetCharacters()?.FirstOrDefault() ?? null;
        }

        public MyObjectBuilder_Cockpit FindPilotCharacter()
        {

            foreach (var cockpit in GetCockpits())
            {
                return cockpit;
            }

            return null;
        }

        #endregion

        #endregion
    }
}
