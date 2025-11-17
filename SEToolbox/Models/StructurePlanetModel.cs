using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using Sandbox.Engine.Voxels.Planet;
using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Support;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace SEToolbox.Models
{
    [Serializable]
    public class StructurePlanetModel : StructureBaseModel
    {
        #region Fields
        private string _sourceVoxelFilePath;

        private string _voxelFilePath;
        private Vector3I _size;
        private Vector3D _contentCenter;

        [NonSerialized]
        private BackgroundWorker _asyncWorker;

        [NonSerialized]
        private  MyVoxelMapBase _voxelMap;

        [NonSerialized]
        private bool _isLoadingAsync;
    
        public BoundingBoxI ContentBounds { get; private set; }
        #endregion

        #region Ctor

        public StructurePlanetModel(MyObjectBuilder_EntityBase entityBase, string voxelPath)
            : base(entityBase)
        {
            if (voxelPath != null)
            {
                VoxelFilePath = Path.Combine(voxelPath, Name +  MyVoxelMapBase.FileExtension.V2);
                string previewFile = VoxelFilePath;

                if (!File.Exists(VoxelFilePath))
                {
                    string oldFilePath = Path.Combine(voxelPath, Name +  MyVoxelMapBase.FileExtension.V1);
                    if (File.Exists(oldFilePath))
                    {
                        SourceVoxelFilePath = oldFilePath;
                        previewFile = oldFilePath;
                        SpaceEngineersCore.ManageDeleteVoxelList.Add(oldFilePath);
                    }
                }

                ReadVoxelDetails(previewFile);
            }
        }

        #endregion

        #region Properties

        [XmlIgnore]
        public MyObjectBuilder_Planet Planet
        {
            get => EntityBase as MyObjectBuilder_Planet; 
        }

        [XmlIgnore]
        public string Name
        {
            get => Planet.StorageName;

            set => SetProperty(Planet.StorageName, value, nameof(Name));
        }

        /// <summary>
        /// This is the location of the temporary source file for importing/generating a Voxel file.
        /// </summary>
        public string SourceVoxelFilepath
        {
            get => _sourceVoxelFilePath;

            set
            {
                SetProperty(ref _sourceVoxelFilePath, value, nameof(SourceVoxelFilePath));
                    ReadVoxelDetails(SourceVoxelFilePath);
            }
        }

        /// <summary>
        /// This is the actual file/path for the Voxel file. It may not exist yet.
        /// </summary>
        public string VoxelFilePath
        {
            get => _voxelFilePath;

            set => SetProperty(ref _voxelFilePath, value, nameof(VoxelFilePath));
            
        }

        [XmlIgnore]
        public Vector3I Size
        {
            get => _size;

            set => SetProperty(ref _size, value, nameof(Size));
        }

        [XmlIgnore]
        public int Seed
        {
            get => Planet.Seed;

            set => SetProperty(Planet.Seed, value, nameof(Seed));
        }


        [XmlIgnore]
        public float Radius
        {
            get => Planet.Radius;

            set => SetProperty(Planet.Radius, value, nameof(Radius));
        }

        public bool HasAtmosphere
        {
            get => Planet.HasAtmosphere;

            set => SetProperty(Planet.HasAtmosphere, value, nameof(HasAtmosphere));
        }

        [XmlIgnore]
        public float AtmosphereRadius
        {
            get => Planet.AtmosphereRadius;

            set => SetProperty(Planet.AtmosphereRadius, value, nameof(AtmosphereRadius));
        }

        [XmlIgnore]
        public float MinimumSurfaceRadius
        {
            get => Planet.MinimumSurfaceRadius;

            set => SetProperty(Planet.MinimumSurfaceRadius, value, nameof(MinimumSurfaceRadius));
        }

        [XmlIgnore]
        public float MaximumHillRadius
        {
            get => Planet.MaximumHillRadius;

            set => SetProperty(Planet.MaximumHillRadius, value, nameof(MaximumHillRadius));
        }

        [XmlIgnore]
        public float GravityFalloff
        {
            get => Planet.GravityFalloff;

            set => SetProperty(Planet.GravityFalloff, value, nameof(GravityFalloff));
        }

        [XmlIgnore]
        public float SurfaceGravity
        {
            get => Planet.SurfaceGravity;

            set => SetProperty(Planet.SurfaceGravity, value, nameof(SurfaceGravity));
        }

        [XmlIgnore]
        public bool SpawnsFlora
        {
            get => Planet.SpawnsFlora;

            set => SetProperty(Planet.SpawnsFlora, value, nameof(SpawnsFlora));
        }

        [XmlIgnore]
        public bool ShowGPS
        {
            get => Planet.ShowGPS;

            set => SetProperty(Planet.ShowGPS, value, nameof(ShowGPS));
        }

        [XmlIgnore]
        public string PlanetGenerator
        {
            get => Planet.PlanetGenerator;

            set => SetProperty(Planet.PlanetGenerator, value, nameof(PlanetGenerator));
        }

        private object ContentSize()
        {
            return _voxelMap.BoundingContent;
        }

        #endregion

        #region Methods

        [OnSerializing]
        private void OnSerializingMethod(StreamingContext context)
        {
            SerializedEntity = SpaceEngineersApi.Serialize<MyObjectBuilder_Planet>(Planet);
        }

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            EntityBase = SpaceEngineersApi.Deserialize<MyObjectBuilder_Planet>(SerializedEntity);
        }

        public override void UpdateGeneralFromEntityBase()
        {
            ClassType = ClassType.Planet;
            DisplayName = Name;
        }

        public override void InitializeAsync()
        {
            if (_asyncWorker == null)
            {
                _asyncWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
                _asyncWorker.DoWork += (sender, e) =>
                {
                    if (_isLoadingAsync) return;

                        _isLoadingAsync = true;
                        IsBusy = true;

                        if (Planet != null)
                        {
                            _voxelMap.RefreshAssets();
                            _contentCenter = _voxelMap.ContentCenter;
                            Center = new Vector3D(
                                _contentCenter.X + 0.5f + PositionX,
                                _contentCenter.Y + 0.5f + PositionY,
                                _contentCenter.Z + 0.5f + PositionZ
                            );

                            Name = Planet.StorageName ?? Name;
                            Seed = Planet.Seed;
                            Radius = Planet.Radius;
                            HasAtmosphere = Planet.HasAtmosphere;
                            MinimumSurfaceRadius = Planet.MinimumSurfaceRadius;
                            MaximumHillRadius = Planet.MaximumHillRadius;
                            AtmosphereRadius = Planet.AtmosphereRadius;
                            GravityFalloff = Planet.GravityFalloff;
                            SurfaceGravity = Planet.SurfaceGravity;
                            SpawnsFlora = Planet.SpawnsFlora;
                            ShowGPS = Planet.ShowGPS;
                            PlanetGenerator = Planet.PlanetGenerator;

                            ReadVoxelDetails(SourceVoxelFilePath);
                        }
                        else
                        {
                            throw new NullReferenceException("Planet is null");
                        }

                        IsBusy = false;
                        _isLoadingAsync = false;


                    if (!_asyncWorker.IsBusy)
                    {
                        _asyncWorker.RunWorkerAsync();
                    }
                };
            }
        }

        public override void CancelAsync()
        {
            if (_asyncWorker?.IsBusy == true && _asyncWorker.WorkerSupportsCancellation)
            {
                _asyncWorker.CancelAsync();

                // Attempt to abort the file access to the ZipTools zip reader if necessary.
                FieldInfo field = ReflectionUtil.GetField<ZipArchive>("_reader", BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null)
                {
                    ZipArchive zipReader = (ZipArchive)field.GetValue(null);
                    zipReader?.Dispose();
                }
            }

        }
        private void ReadVoxelDetails(string fileName)
        {
            if (fileName != null && File.Exists(fileName) && _voxelMap == null)
            {
                _voxelMap = new  MyVoxelMapBase();
                _voxelMap.Load(fileName);

                Size = _voxelMap.Size;
                _contentCenter = _voxelMap.ContentCenter;
                IsValid = _voxelMap.IsValid;
                OnPropertyChanged(nameof(Size), nameof(IsValid));
                Center = new Vector3D(_contentCenter.X + 0.5f + PositionX, _contentCenter.Y + 0.5f + PositionY, _contentCenter.Z + 0.5f + PositionZ);
                WorldAabb = new BoundingBoxD(PositionAndOrientation.Value.Position, PositionAndOrientation.Value.Position + new Vector3D(Size));
            }
        }

        public override void RecalcPosition(Vector3D playerPosition)
        {
            base.RecalcPosition(playerPosition);
            Center = new Vector3D(_contentCenter.X + 0.5f + PositionX, _contentCenter.Y + 0.5f + PositionY, _contentCenter.Z + 0.5f + PositionZ);
            WorldAabb = new BoundingBoxD(PositionAndOrientation.Value.Position, PositionAndOrientation.Value.Position + new Vector3D(Size));
        }

        /// <summary>
        /// Regenerate the Planet voxel.
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="radius"></param>
        public void RegeneratePlanet(int seed, float radius)
        {
            MyPlanetStorageProvider provider = new();
            MyPlanetGeneratorDefinition planetDefinition = MyDefinitionManager.Static.GetDefinition<MyPlanetGeneratorDefinition>(MyStringHash.GetOrCompute(Planet.PlanetGenerator));
            provider.Init(seed, planetDefinition, radius, true);

            float minHillSize = provider.Radius * planetDefinition.HillParams.Min;
            float maxHillSize = provider.Radius * planetDefinition.HillParams.Max;

            float atmosphereRadius = planetDefinition.AtmosphereSettings.HasValue && planetDefinition.AtmosphereSettings.Value.Scale > 1f ? 1 + planetDefinition.AtmosphereSettings.Value.Scale : 1.75f;
            atmosphereRadius *= provider.Radius;

            Planet.Seed = seed;
            Planet.Radius = radius;
            Planet.AtmosphereRadius = atmosphereRadius;
            Planet.MinimumSurfaceRadius = radius + minHillSize;
            Planet.MaximumHillRadius = radius + maxHillSize;

            provider.Init(Planet.Seed, planetDefinition, radius, true);

             MyVoxelMapBase asteroid = new()
            {
                Storage = new MyOctreeStorage(provider, provider.StorageSize)
            };

            string tempFileName = TempFileUtil.NewFileName( MyVoxelMapBase.FileExtension.V2);
            asteroid.Save(tempFileName);
            SourceVoxelFilepath = tempFileName;
            UpdateNewSource(asteroid, tempFileName);
            
         OnPropertyChanged(nameof(Seed), nameof(Radius), nameof(AtmosphereRadius), nameof(MinimumSurfaceRadius), nameof(MaximumHillRadius));
            // Update properties after regeneration
            Size = _voxelMap.Size;
            _contentCenter = _voxelMap.ContentCenter;
            IsValid = _voxelMap.IsValid;
            OnPropertyChanged(nameof(Size), nameof(IsValid));
            Center = new Vector3D(_contentCenter.X + 0.5f + PositionX, _contentCenter.Y + 0.5f + PositionY, _contentCenter.Z + 0.5f + PositionZ);
            WorldAabb = new BoundingBoxD(PositionAndOrientation.Value.Position, PositionAndOrientation.Value.Position + new Vector3D(Size));
        }

        public void UpdateNewSource( MyVoxelMapBase newMap, string fileName)
        {
            _voxelMap?.Dispose();
            _voxelMap = newMap;
            SourceVoxelFilePath = fileName;

            Size = _voxelMap.Size;
            ContentBounds = _voxelMap.BoundingContent;//
            IsValid = _voxelMap.IsValid;

            OnPropertyChanged(nameof(Size));
            OnPropertyChanged(nameof(ContentSize));
            OnPropertyChanged(nameof(IsValid));
            Center = new Vector3D(_voxelMap.ContentCenter.X + 0.5f + PositionX, _voxelMap.ContentCenter.Y + 0.5f + PositionY, _voxelMap.ContentCenter.Z + 0.5f + PositionZ);
            WorldAabb = new BoundingBoxD(PositionAndOrientation.Value.Position, PositionAndOrientation.Value.Position + new Vector3D(Size));
        }

        #endregion
    }
}
