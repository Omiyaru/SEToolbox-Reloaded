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
            var vms = SpaceEngineersResources.VoxelMapStorageDefinitions;
            var contentPath = ToolboxUpdater.GetApplicationContentPath();
            var list = new List<GenerateVoxelDetailModel>();

            foreach (MyVoxelMapStorageDefinition voxelMap in vms)
            {
                string fileName = SpaceEngineersCore.GetDataPathOrDefault(voxelMap.StorageFile, Path.Combine(contentPath, voxelMap.StorageFile));

                if (!File.Exists(fileName))
                    continue;

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
                    AsteroidByteFillProperties v1 = (AsteroidByteFillProperties)item.Clone();
                    v1.VoxelFile = voxelFileLookup.TryGetValue(v1.VoxelFile.Name, out var voxelFile) ? voxelFile : null;
                    v1.MainMaterial = materialLookup.TryGetValue(v1.MainMaterial.DisplayName, out var mainMaterial) ? mainMaterial : null;
                    v1.SecondMaterial = materialLookup.TryGetValue(v1.SecondMaterial.DisplayName, out var secondMaterial) ? secondMaterial : null;
                    v1.ThirdMaterial = materialLookup.TryGetValue(v1.ThirdMaterial.DisplayName, out var thirdMaterial) ? thirdMaterial : null;
                    v1.FourthMaterial = materialLookup.TryGetValue(v1.FourthMaterial.DisplayName, out var fourthMaterial) ? fourthMaterial : null;
                    v1.FifthMaterial = materialLookup.TryGetValue(v1.FifthMaterial.DisplayName, out var fifthMaterial) ? fifthMaterial : null;
                    v1.SixthMaterial = materialLookup.TryGetValue(v1.SixthMaterial.DisplayName, out var sixthMaterial) ? sixthMaterial : null;
                    v1.SeventhMaterial = materialLookup.TryGetValue(v1.SeventhMaterial.DisplayName, out var seventhMaterial) ? seventhMaterial : null;
                    VoxelCollection.Add(v1);
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
            var firstMaterial = MaterialsCollection.FirstOrDefault();
            var firstVoxelFile = VoxelFileList.FirstOrDefault();

            if (firstMaterial == null || firstVoxelFile == null)
                return null;

            return new AsteroidByteFillProperties
            {
                Index = index,
                VoxelFile = firstVoxelFile,
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
