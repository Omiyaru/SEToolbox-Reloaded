using SEToolbox.Interfaces;
using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Support;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Voxels;
using VRageMath;
using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.Models
{
    [Serializable]
    public class StructureVoxelModel : StructureBaseModel
    {
        #region Fields

        private string _sourceVoxelFilePath;
        private string _voxelFilePath;
        private Vector3I _size;
        private BoundingBoxI _contentBounds;
        private BoundingBoxI _inflatedContentBounds;
        private long _voxCells;

        [NonSerialized]
        private BackgroundWorker _asyncWorker;

        [NonSerialized]
        private MyVoxelMapBase _voxelMap;

        [NonSerialized]
        private VoxelMaterialAssetModel _selectedMaterialAsset;

        [NonSerialized]
        private List<VoxelMaterialAssetModel> _materialAssets;

        [NonSerialized]
        private List<VoxelMaterialAssetModel> _gameMaterialList;

        [NonSerialized]
        private List<VoxelMaterialAssetModel> _editMaterialList;

        [NonSerialized]
        private bool _isLoadingAsync;
        // private readonly object _materialVolume;
        // private readonly ContainmentType _containsMaterial;

        #endregion

        #region Ctor

        public StructureVoxelModel(MyObjectBuilder_EntityBase entityBase, string voxelPath)
            : base(entityBase)
        {
            string contentPath = ToolboxUpdater.GetApplicationContentPath();

            if (voxelPath != null)
            {
                VoxelFilePath = Path.Combine(voxelPath, entityBase.Name + MyVoxelMapBase.FileExtension.V2);
                string previewFile = VoxelFilePath;

                if (!File.Exists(VoxelFilePath))
                {
                    string oldFilePath = Path.Combine(voxelPath, entityBase.Name + MyVoxelMapBase.FileExtension.V1);
                    if (File.Exists(oldFilePath))
                    {
                        SourceVoxelFilePath = oldFilePath;
                        previewFile = oldFilePath;
                        SpaceEngineersCore.ManageDeleteVoxelList.Add(oldFilePath);
                    }
                }

                // Has a huge upfront loading cost
                //ReadVoxelDetails(previewFile);
            }

            Dictionary<string, string> materialList = [];
            foreach (MyVoxelMaterialDefinition item in SpaceEngineersResources.VoxelMaterialDefinitions.OrderBy(m => m.Id.SubtypeName))
            {
                string texture = item.GetVoxelDisplayTexture();
                materialList.Add(item.Id.SubtypeName, texture != null ? SpaceEngineersCore.GetDataPathOrDefault(texture, Path.Combine(contentPath, texture)) : string.Empty);
            }

            GameMaterialList = [.. materialList.Select(m => new VoxelMaterialAssetModel { MaterialName = m.Key, DisplayName = m.Key, TextureFile = m.Value })];
            EditMaterialList =
            [
                new VoxelMaterialAssetModel { MaterialName = null, DisplayName = Res.CtlVoxelMnuRemoveMaterial },
                .. materialList.Select(m => new VoxelMaterialAssetModel { MaterialName = m.Key, DisplayName = m.Key, TextureFile = m.Value }),
            ];
        }

        #endregion

        #region Properties

        [XmlIgnore]
        public MyObjectBuilder_VoxelMap VoxelMap
        {
            get => EntityBase as MyObjectBuilder_VoxelMap;
        }

        [XmlIgnore]
        public string Name
        {
            get => VoxelMap.StorageName ?? string.Empty;
            set => SetProperty(VoxelMap.StorageName, value, nameof(Name));
        }

        /// <summary>
        /// This is the location of the temporary source file for importing/generating a Voxel file.
        /// </summary>
        public new string SourceVoxelFilePath
        {
            get => _sourceVoxelFilePath;
            set => SetProperty(ref _sourceVoxelFilePath, value, nameof(SourceVoxelFilePath), () =>
                   ReadVoxelDetails(SourceVoxelFilePath));
        }

        /// <summary>
        /// This is the actual file/path for the Voxel file. It may not exist yet.
        /// </summary>
        public string VoxelFilePath
        {
            get => _voxelFilePath ?? string.Empty;
            set => SetProperty(ref _voxelFilePath, value, nameof(VoxelFilePath));
        }

        [XmlIgnore]
        public Vector3I Size
        {
            get => _size;
            set => SetProperty(ref _size, value, nameof(Size));
        }

        [XmlIgnore]
        public Vector3I ContentSize
        {
            get => _contentBounds.Size + 1;  // Content size
        }

        /// <summary>
        /// Represents the Cell content, not the Cell boundary.
        /// So Min and Max values are both inclusive.
        /// </summary>
        [XmlIgnore]
        public BoundingBoxI ContentBounds
        {
            get => _contentBounds;
            set => SetProperty(ref _contentBounds, value, nameof(ContentBounds));
        }

        [XmlIgnore]
        public BoundingBoxI InflatedContentBounds => _inflatedContentBounds;

        [XmlIgnore]
        public long VoxCells
        {
            get => _voxCells;
            set => SetProperty(ref _voxCells, value, nameof(VoxCells));
        }

        [XmlIgnore]
        public double Volume
        {
            get => (double)_voxCells / 255;
        }

        /// <summary>
        /// This is detail of the breakdown of ores in the asteroid.
        /// </summary>
        [XmlIgnore]
        public List<VoxelMaterialAssetModel> MaterialAssets
        {
            get => _materialAssets;
            set => SetProperty(ref _materialAssets, value, nameof(MaterialAssets));
        }

        [XmlIgnore]
        public VoxelMaterialAssetModel SelectedMaterialAsset
        {
            get => _selectedMaterialAsset;
            set => SetProperty(ref _selectedMaterialAsset, value, nameof(SelectedMaterialAsset));
        }

        [XmlIgnore]
        public List<VoxelMaterialAssetModel> GameMaterialList
        {
            get => _gameMaterialList;
            set => SetProperty(ref _gameMaterialList, value, nameof(GameMaterialList));
        }

        [XmlIgnore]
        public List<VoxelMaterialAssetModel> EditMaterialList
        {
            get => _editMaterialList;
            set => SetProperty(ref _editMaterialList, value, nameof(EditMaterialList));
        }

        #endregion

        #region Methods

        [OnSerializing]
        private void OnSerializingMethod(StreamingContext context)
        {
            SerializedEntity = SpaceEngineersApi.Serialize<MyObjectBuilder_VoxelMap>(VoxelMap);
        }

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            EntityBase = SpaceEngineersApi.Deserialize<MyObjectBuilder_VoxelMap>(SerializedEntity);
        }

        public override void UpdateGeneralFromEntityBase()
        {
            ClassType = ClassType.Voxel;
            DisplayName = Name;
        }

        public override void InitializeAsync()
        {
            _asyncWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            _asyncWorker.DoWork += delegate
            {
                if (!_isLoadingAsync)
                {
                    _isLoadingAsync = true;
                    IsBusy = true;
                    LoadDetailsSync();
                    IsBusy = false;
                    _isLoadingAsync = false;
                }
                else
                {
                    _asyncWorker.CancelAsync();
                    _asyncWorker.RunWorkerAsync();
                }
            };
            _asyncWorker.RunWorkerCompleted += delegate
            {
                OnPropertyChanged(nameof(Size), nameof(ContentSize), nameof(ContentBounds), nameof(Center), nameof(VoxCells), nameof(Volume));
            };

            _asyncWorker.RunWorkerAsync();
        }

        public void LoadDetailsSync()
        {
            ReadVoxelDetails(SourceVoxelFilePath ?? VoxelFilePath);

            if (_voxelMap != null && (MaterialAssets == null || MaterialAssets.Count == 0))
            {
                Dictionary<string, long> details = _voxelMap.RefreshAssets();
                _contentBounds = _voxelMap.BoundingContent;
                _inflatedContentBounds = _voxelMap.InflatedBoundingContent;
                _voxCells = _voxelMap.VoxCells;
                Center = new Vector3D(_voxelMap.ContentCenter.X + 0.5f + PositionX, _voxelMap.ContentCenter.Y + 0.5f + PositionY, _voxelMap.ContentCenter.Z + 0.5f + PositionZ);

                var sum = details.Values.ToList().Sum();
                var list = new VoxelMaterialAssetModel[details.Count].ToList();

                foreach (KeyValuePair<string, long> kvp in details)
                {
                    list.Add(new VoxelMaterialAssetModel { MaterialName = kvp.Key, Volume = (double)kvp.Value/255, Percent = (double)kvp.Value/sum });
                }

                MaterialAssets = list;
            }
        }

        public override void CancelAsync()
        {
            if (_asyncWorker != null && _asyncWorker.IsBusy && _asyncWorker.WorkerSupportsCancellation)
            {
                _asyncWorker?.CancelAsync();
            }
        }

        private void ReadVoxelDetails(string fileName)
        {
            if (fileName != null && File.Exists(fileName))
            {
                _voxelMap = new MyVoxelMapBase();
                _voxelMap.Load(fileName);

                Size = _voxelMap.Size;
                ContentBounds = _voxelMap.BoundingContent;
                IsValid = _voxelMap.IsValid;

                OnPropertyChanged(nameof(Size), nameof(ContentSize), nameof(IsValid));
                Center = new Vector3D(_voxelMap.ContentCenter.X + 0.5f + PositionX, _voxelMap.ContentCenter.Y + 0.5f + PositionY, _voxelMap.ContentCenter.Z + 0.5f + PositionZ);
                WorldAabb = new BoundingBoxD(PositionAndOrientation.Value.Position, PositionAndOrientation.Value.Position + new Vector3D(Size));
            }
        }

        public override void RecalcPosition(Vector3D playerPosition)
        {
            base.RecalcPosition(playerPosition);
            if (IsValid)
            {
                Center = new Vector3D(_voxelMap.ContentCenter.X + 0.5f + PositionX, _voxelMap.ContentCenter.Y + 0.5f + PositionY, _voxelMap.ContentCenter.Z + 0.5f + PositionZ);
                WorldAabb = new BoundingBoxD(PositionAndOrientation.Value.Position, PositionAndOrientation.Value.Position + new Vector3D(Size));
            }
        }

        public void UpdateNewSource(MyVoxelMapBase newMap, string fileName)
        {
          
                _voxelMap?.Dispose();
                _voxelMap = newMap;
                SourceVoxelFilePath = fileName;

                Size = _voxelMap.Size;
                ContentBounds = _voxelMap.BoundingContent;
                IsValid = _voxelMap.IsValid;

                OnPropertyChanged(nameof(Size), nameof(ContentSize), nameof(IsValid));
                Center = new Vector3D(_voxelMap.ContentCenter.X + 0.5f + PositionX, _voxelMap.ContentCenter.Y + 0.5f + PositionY, _voxelMap.ContentCenter.Z + 0.5f + PositionZ);
                WorldAabb = new BoundingBoxD(PositionAndOrientation.Value.Position, PositionAndOrientation.Value.Position + new Vector3D(Size));
        }

        public void RotateAsteroid(Quaternion quaternion)
        {
            string sourceFile = SourceVoxelFilePath ?? VoxelFilePath;
            bool changed;
            MyVoxelMapBase asteroid = new();
            asteroid.Load(sourceFile);

            MyVoxelMapBase newAsteroid = new();
            Vector3I newSize = asteroid.Size;
            newAsteroid.Create(newSize, SpaceEngineersResources.GetDefaultMaterialIndex());

            Vector3I halfSize = asteroid.Storage.Size / 2;
            // Don't use anything smaller than 64 for smaller voxels, as it trashes the cache.
            Vector3I cacheSize = new(64);
            Vector3I halfCacheSize = new(32); // This should only be used for the Transform, not the cache.

            // read the asteroid in chunks of 64 to avoid the Arithmetic overflow issue.
            Vector3I block = Vector3I.Zero;
            PRange.ProcessRange(block, asteroid.Storage.Size + 1 / 64);

            #region Source Voxel

            MyStorageData cache = new();
            cache.Resize(cacheSize);
            // LOD1 is not detailed enough for content information on asteroids.
            asteroid.Storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, block, block + cacheSize - 1);

            #endregion

            #region Target Voxel

            // the block is a cubiod. The entire space needs to rotate, to be able to gauge where the new block position starts from.
            var newBlockMin = Vector3I.Transform(block - halfSize, quaternion) + halfSize;
            var newBlockMax = Vector3I.Transform(block + 64 - halfSize, quaternion) + halfSize;
            var newBlock = Vector3I.Min(newBlockMin, newBlockMax);

            MyStorageData newCache = new();
            newCache.Resize(cacheSize);
            newAsteroid.Storage.ReadRange(newCache, MyStorageDataTypeFlags.ContentAndMaterial, 0, newBlock, newBlock + cacheSize - 1);

            #endregion

            Vector3I p = Vector3I.Zero;
            PRange.ProcessRange(p, cacheSize);

            byte volume = cache.Content(ref p);
            byte cellMaterial = cache.Material(ref p);

            Vector3I newP1 = Vector3I.Transform(p - halfCacheSize, quaternion) + halfCacheSize;
            Vector3I newP2 = Vector3I.Transform(p + 1 - halfCacheSize, quaternion) + halfCacheSize;
            Vector3I newP = Vector3I.Min(newP1, newP2);

            newCache.Content(ref newP, volume);
            newCache.Material(ref newP, cellMaterial);
            changed = true;
            if (changed)
            {
                newAsteroid.Storage.WriteRange(newCache, MyStorageDataTypeFlags.ContentAndMaterial, newBlock, newBlock + cacheSize - 1);
            }

            SaveToFile(newAsteroid);
        }

        public void SaveToFile(MyVoxelMapBase newAsteroid)
        {
            string tempFileName = TempFileUtil.NewFileName(MyVoxelMapBase.FileExtension.V2);
            newAsteroid.Save(tempFileName);
            SourceVoxelFilePath = tempFileName;
        }

        public bool ExtractStationIntersect(IMainView mainViewModel, bool tightIntersection)
        {
            if (mainViewModel == null)
            {
                throw new ArgumentNullException(nameof(mainViewModel));
            }

            // Make a shortlist of station Entities in the bounding box of the asteroid.
            BoundingBoxD asteroidWorldAabb = new((Vector3D)ContentBounds.Min + PositionAndOrientation.Value.Position, (Vector3D)ContentBounds.Max + PositionAndOrientation.Value.Position);
            List<StructureCubeGridModel> stations = [.. mainViewModel.GetIntersectingEntities(asteroidWorldAabb).Where(e => e?.ClassType == ClassType.LargeStation).Cast<StructureCubeGridModel>()];


            if (stations.Count == 0)
            {
                return false;
            }

            bool modified = false;
            string sourceFile = SourceVoxelFilePath ?? VoxelFilePath;
            if (string.IsNullOrEmpty(sourceFile))
            {
                throw new ArgumentException("Source voxel file path is null or empty.");
            }

            MyVoxelMapBase asteroid = new();
            asteroid.Load(sourceFile);


            int totalBlocks = stations.Sum(s => s.CubeGrid.CubeBlocks.Count);
            mainViewModel.ResetProgress(0, totalBlocks);

            // Search through station entities cubes for intersection with this voxel.
            foreach (StructureCubeGridModel station in stations)
            {
                asteroid = new MyVoxelMapBase();
                asteroid.Load(sourceFile);
                Quaternion quaternion = station.PositionAndOrientation.Value.ToQuaternion(); ;

                foreach (MyObjectBuilder_CubeBlock cube in station.CubeGrid.CubeBlocks)
                {
                    mainViewModel.IncrementProgress();

                    Sandbox.Definitions.MyCubeBlockDefinition definition = SpaceEngineersApi.GetCubeDefinition(cube.TypeId, station.CubeGrid.GridSizeEnum, cube.SubtypeName);
                    Vector3I block = Vector3I.Zero;
                    Vector3I orientSize = definition.Size.Transform(cube.BlockOrientation).Abs();
                    Vector3 min = cube.Min.ToVector3() * station.CubeGrid.GridSizeEnum.ToLength();
                    Vector3 max = (cube.Min + orientSize) * station.CubeGrid.GridSizeEnum.ToLength();
                    Vector3D p1 = Vector3D.Transform(min, quaternion) + station.PositionAndOrientation.Value.Position - (station.CubeGrid.GridSizeEnum.ToLength() / 2);
                    Vector3D p2 = Vector3D.Transform(max, quaternion) + station.PositionAndOrientation.Value.Position - (station.CubeGrid.GridSizeEnum.ToLength() / 2);
                    BoundingBoxD cubeWorldAabb = new(Vector3.Min(p1, p2), Vector3.Max(p1, p2));
                    bool changed = false;

                    // find worldAabb of block.
                    if (asteroidWorldAabb.Intersects(cubeWorldAabb))
                    {
                        Vector3I cacheSize = new(64);
                        Vector3D position = PositionAndOrientation.Value.Position;

                        // read the asteroid in chunks of 64 to avoid the Arithmetic overflow issue.
                        PRange.ProcessRange(block, cacheSize);
                        MyStorageData cache = new();
                        cache.Resize(cacheSize);
                        // LOD1 is not detailed enough for content information on asteroids.
                        Vector3I maxRange = block + cacheSize - 1;
                        asteroid.Storage.ReadRange(cache, MyStorageDataTypeFlags.Content, 0, block, maxRange);

                        changed = false;
                        Vector3I p = Vector3I.Zero;
                        float voxelVolume = 0f;
                        PRange.ProcessRange(p, cacheSize);
                        BoundingBoxD voxelCellBox = new(position + p + block, position + p + block + 1);
                        ContainmentType contains = cubeWorldAabb.Contains(voxelCellBox);
                        if (tightIntersection && contains != ContainmentType.Disjoint)
                        {
                            Vector3D voxelMin = voxelCellBox.Min;
                            Vector3D voxelMax = voxelCellBox.Max;

                            int x = 0, y = 0, z = 0;
                            PRange.ProcessRange(p, x, y, z, cacheSize);
                            Vector3 voxelCenter = new(x + block.X + 0.5f,
                                                      y + block.Y + 0.5f,
                                                      z + block.Z + 0.5f);
                            if (cubeWorldAabb.Contains(voxelCenter) == ContainmentType.Contains)
                            {
                                voxelVolume += 1;
                            }
                        }
                        float voxelContentVolume = voxelVolume / (cacheSize.X * cacheSize.Y * cacheSize.Z);

                        byte content = cache.Content(ref p);
                        byte newContent = (byte)Math.Ceiling(content * voxelContentVolume);
                        if (voxelContentVolume > 0 && content > 0 && newContent < content)
                        {
                            cache.Content(ref p, newContent);
                            changed = true;
                        }
                        else if (contains != ContainmentType.Disjoint && content > 0 || contains == ContainmentType.Contains || contains == ContainmentType.Intersects)
                        {
                            cache.Content(ref p, 0);
                            changed = true;
                        }
                        if (changed)
                        {
                            asteroid.Storage.WriteRange(cache, MyStorageDataTypeFlags.Content, block, maxRange);
                            modified = true;
                        }
                    }
                }
            }

            mainViewModel.ClearProgress();

            if (modified)
            {
                string tempFileName = TempFileUtil.NewFileName(MyVoxelMapBase.FileExtension.V2);
                asteroid.Save(tempFileName);
                // replaces the existing asteroid file, as it is still the same size and dimentions.
                UpdateNewSource(asteroid, tempFileName);
                MaterialAssets = null;
                InitializeAsync();
            }
            return modified;
        }
        #endregion
    }
}
