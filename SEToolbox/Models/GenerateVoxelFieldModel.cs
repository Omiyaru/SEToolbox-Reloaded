using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Models.Asteroids;
using SEToolbox.Support;
using VRage;
using VRage.Game;
using Sandbox.Definitions;
using VRageMath;

namespace SEToolbox.Models
{
    public class GenerateVoxelFieldModel : BaseModel
    {
        private static readonly List<AsteroidByteFillProperties> VoxelStore;

        #region Fields

        private static int _minimumRange = 400;
        private static int _maximumRange = 800;
        private ObservableCollection<GenerateVoxelDetailModel> _voxelFileList;
        private readonly ObservableCollection<MaterialSelectionModel> _materialsCollection;
        private ObservableCollection<AsteroidByteFillProperties> _voxelCollection;
        private readonly List<int> _percentList;
        private static bool _isInitialValueSet;
        private static double _centerPositionX;
        private static double _centerPositionY;
        private static double _centerPositionZ;
        private static Vector3D _centerPosition;
        private static AsteroidFillType.AsteroidFills _asteroidFillType;

        #endregion

        #region Ctor

        public GenerateVoxelFieldModel()
        {
            _voxelFileList = [];
            _materialsCollection = [];
            _voxelCollection = [];
            _percentList = [];
        }

        static GenerateVoxelFieldModel()
        {
            VoxelStore = [];
        }

        #endregion

        #region Properties

        public ObservableCollection<AsteroidByteFillProperties> VoxelCollection
        {
            get => _voxelCollection;
            set => SetProperty(ref _voxelCollection, value, nameof(VoxelCollection));
        }

        public int MinimumRange
        {
            get => _minimumRange;
            set => SetProperty(ref _minimumRange, value, nameof(MinimumRange));
        }

        public int MaximumRange
        {
            get => _maximumRange;
            set => SetProperty(ref _maximumRange, value, nameof(MaximumRange));
        }

        public ObservableCollection<GenerateVoxelDetailModel> VoxelFileList
        {
            get => _voxelFileList;
            set => SetProperty(ref _voxelFileList, value, nameof(VoxelFileList));
        }

        public ObservableCollection<MaterialSelectionModel> MaterialsCollection
        {
            get => _materialsCollection;
        }

        public List<int> PercentList
        {
            get => _percentList;
        }

        public MaterialSelectionModel BaseMaterial { get; set; }

        public double CenterPositionX
        {
            get => _centerPositionX;
            set => SetProperty(ref _centerPositionX, value, nameof(CenterPositionX));
        }

        public double CenterPositionY
        {
            get => _centerPositionY;
            set => SetProperty(ref _centerPositionY, value, nameof(CenterPositionY));
        }

        public double CenterPositionZ
        {
            get => _centerPositionZ;
            set => SetProperty(ref _centerPositionZ, value, nameof(CenterPositionZ));
        }

        public Vector3D CenterPosition
        {
            get => _centerPosition;
            set => SetProperty(ref _centerPosition, value, nameof(CenterPosition));
        }

        public AsteroidFillType.AsteroidFills AsteroidFillType
        {
            get => _asteroidFillType;
            set => SetProperty(ref _asteroidFillType, value, nameof(AsteroidFillType));
        }

        #endregion

        #region Methods

        public void Load(MyPositionAndOrientation characterPosition)
        {
            if (!_isInitialValueSet)
            {
                // only set the position first time opened and cache.
                CenterPositionX = characterPosition.Position.X;
                CenterPositionY = characterPosition.Position.Y;
                CenterPositionZ = characterPosition.Position.Z;
                AsteroidFillType = Support.AsteroidFillType.AsteroidFills.ByteFiller;
                _isInitialValueSet = true;
            }

            MaterialsCollection.Clear();
            foreach (var material in SpaceEngineersResources.VoxelMaterialDefinitions)
            {
                MaterialsCollection.Add(new MaterialSelectionModel { Value = material.Id.SubtypeName, DisplayName = material.Id.SubtypeName, IsRare = material.IsRare, MinedRatio = material.MinedOreRatio });
            }

            BaseMaterial = MaterialsCollection.FirstOrDefault(m => m.IsRare == false) ?? MaterialsCollection.FirstOrDefault();

            // Voxel Map Storage, includes stock and mod asteroids.
            var voxelMapStorage= SpaceEngineersResources.VoxelMapStorageDefinitions;
            var contentPath = ToolboxUpdater.GetApplicationContentPath();
            var list = new List<GenerateVoxelDetailModel>();

            foreach (MyVoxelMapStorageDefinition voxelMap in voxelMapStorage)
            {
                string fileName = SpaceEngineersCore.GetDataPathOrDefault(voxelMap.StorageFile, Path.Combine(contentPath, voxelMap.StorageFile));

                if (!File.Exists(fileName))
                {
                    continue;
                }

                GenerateVoxelDetailModel voxel = new()
                {
                    Name = Path.GetFileNameWithoutExtension(voxelMap.StorageFile),
                    SourceFileName = fileName,
                    FileSize = new FileInfo(fileName).Length,
                    Size = MyVoxelMapBase.LoadVoxelSize(fileName)
                };
                list.Add(voxel);
            }

            // Custom voxel files directory.
            List<string> files = [];
            if (!string.IsNullOrEmpty(GlobalSettings.Default.CustomVoxelPath) && Directory.Exists(GlobalSettings.Default.CustomVoxelPath))
            {
                files.AddRange(Directory.GetFiles(GlobalSettings.Default.CustomVoxelPath, "*" + MyVoxelMapBase.FileExtension.V1));
                files.AddRange(Directory.GetFiles(GlobalSettings.Default.CustomVoxelPath, "*" + MyVoxelMapBase.FileExtension.V2));
            }

            list.AddRange(files.Select(file => new GenerateVoxelDetailModel
            {
                Name = Path.GetFileNameWithoutExtension(file),
                SourceFileName = file,
                FileSize = new FileInfo(file).Length,
                Size = MyVoxelMapBase.LoadVoxelSize(file)
            }));

            VoxelFileList = new ObservableCollection<GenerateVoxelDetailModel>(list.OrderBy(s => s.Name));

            // Set up a default start.
            if (VoxelStore.Count == 0)
            {
                VoxelCollection.Add(NewDefaultVoxel(1));
            }
            else
            {
                var voxelFileLookup = VoxelFileList.ToDictionary(v => v.Name, v => v);
                var materialLookup = MaterialsCollection.ToDictionary(v => v.DisplayName, v => v);

                foreach (var item in VoxelStore)
                {
                    AsteroidByteFillProperties bfp = (AsteroidByteFillProperties)item.Clone();
                    var materialsList = new List<MaterialSelectionModel>();

                    bfp.VoxelFile = voxelFileLookup.TryGetValue(bfp.VoxelFile.Name, out var voxelFile) ? voxelFile : null;
                    foreach (var material in MaterialsCollection)
                    {   
                        var m = new MaterialSelectionModel { Value = material.Value, DisplayName = material.DisplayName};
                         m = bfp.MaterialsCollection.FirstOrDefault(v => v.DisplayName == material.Value) ?? MaterialsCollection.FirstOrDefault();
                    }
                    VoxelCollection.Add(bfp);
                
                }
                RenumberCollection();

                for (int i = 0; i < 100; i++)
                {
                    PercentList.Add(i);
                }
            }
        }

        public void Unload()
        {
            VoxelStore.Clear();
            VoxelStore.AddRange(_voxelCollection);
        }

        public AsteroidByteFillProperties NewDefaultVoxel(int index)
        {

            return new AsteroidByteFillProperties
            {
                Index = index,
                VoxelFile = VoxelFileList[0],
                MainMaterial = MaterialsCollection[0],
                SecondMaterial = MaterialsCollection[0],
                ThirdMaterial = MaterialsCollection[0],
                FourthMaterial = MaterialsCollection[0],
                FifthMaterial = MaterialsCollection[0],
                SixthMaterial = MaterialsCollection[0],
                SeventhMaterial = MaterialsCollection[0],
            };
        }


        public void RenumberCollection()
        {
            for (int i = 0; i < VoxelCollection.Count; i++)
            {
                VoxelCollection[i].Index = i + 1;
            }
        }

        #endregion
    }
}
