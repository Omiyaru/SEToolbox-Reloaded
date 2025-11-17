using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Support;
using VRage;
using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.Models
{
    public class ImportVoxelModel : BaseModel
    {
        #region Fields

        private string _fileName;
        private string _sourceFile;
        private bool _isValidVoxelFile;
        private BindablePoint3DModel _position;
        private BindableVector3DModel _forward;
        private BindableVector3DModel _up;
        private MyPositionAndOrientation _characterPosition;
        private bool _isStockVoxel;
        private bool _isFileVoxel;
        private bool _isSphere;
        private GenerateVoxelDetailModel _stockVoxel;
        private MaterialSelectionModel _stockMaterial;
        private List<GenerateVoxelDetailModel> _voxelFileList;
        private readonly ObservableCollection<MaterialSelectionModel> _materialsCollection;
        private int _sphereRadius;
        private int _sphereShellRadius;

        #endregion

        #region Ctor


        public ImportVoxelModel()
        {
            _voxelFileList = [];
            _materialsCollection =
            [
                new() { Value = null, DisplayName = Res.WnImportAsteroidNoChange }
            ];

            foreach (VRage.Game.MyVoxelMaterialDefinition material in SpaceEngineersResources.VoxelMaterialDefinitions.OrderBy(m => m.Id.SubtypeName))
            {
                MaterialsCollection.Add(new MaterialSelectionModel { Value = material.Id.SubtypeName, DisplayName = material.Id.SubtypeName });
            }

            SphereRadius = 150;
            SphereShellRadius = 0;
        }

        #endregion

        #region Properties

        public string FileName
        {
            get => _fileName;

            set => SetProperty(ref _fileName, value, nameof(FileName));
        }

        public string SourceFile
        {
            get => _sourceFile;

            set => SetProperty(ref _sourceFile, value,nameof(SourceFile), ()=>
                {StockMaterial ??= MaterialsCollection[0];});
                
            
        }

        public bool IsValidVoxelFile
        {
            get => _isValidVoxelFile;

            set => SetProperty(ref _isValidVoxelFile, value, nameof(IsValidVoxelFile));
        }

        public BindablePoint3DModel Position
        {
            get => _position;

            set => SetProperty(ref _position, value, nameof(Position));
        }

        public BindableVector3DModel Forward
        {
            get => _forward;

            set => SetProperty(ref _forward, value, nameof(Forward));
        }

        public BindableVector3DModel Up
        {
            get => _up;

            set => SetProperty(ref _up, value, nameof(Up));
        }

        public MyPositionAndOrientation CharacterPosition
        {
            get => _characterPosition;

            set => SetProperty(ref _characterPosition, value, nameof(CharacterPosition));
                //if (value != characterPosition) // Unable to check for equivilence, without long statement. And, mostly uncessary.
           
        }

        public bool IsStockVoxel
        {
            get => _isStockVoxel;

            set => SetProperty(ref _isStockVoxel, value, nameof(IsStockVoxel));
        }

        public bool IsFileVoxel
        {
            get => _isFileVoxel;

            set => SetProperty(ref _isFileVoxel, value, nameof(IsFileVoxel));
        }

        public bool IsSphere
        {
            get => _isSphere;

            set => SetProperty(ref _isSphere,value,nameof(IsSphere));

        }

        public GenerateVoxelDetailModel StockVoxel
        {
            get => _stockVoxel;

            set => SetProperty(ref _stockVoxel, value, nameof(StockVoxel), () =>
                    {
                    IsStockVoxel = true;
                    StockMaterial ??= MaterialsCollection[0];
                    });
        }

        public List<GenerateVoxelDetailModel> VoxelFileList
        {
            get => _voxelFileList;

            set => SetProperty(ref _voxelFileList, value, nameof(VoxelFileList));
        }

        public ObservableCollection<MaterialSelectionModel> MaterialsCollection
        {
            get => _materialsCollection;
         }

        public MaterialSelectionModel StockMaterial
        {
            get => _stockMaterial;

            set => SetProperty(ref _stockMaterial, value, nameof(StockMaterial));
        }

        public int SphereRadius
        {
            get => _sphereRadius;

            set => SetProperty(ref _sphereRadius, value, nameof(SphereRadius));
        }

        public int SphereShellRadius
        {
            get => _sphereShellRadius;

            set => SetProperty(ref _sphereShellRadius, value, nameof(SphereShellRadius));
        }

        #endregion

        #region Methods

        public void Load(MyPositionAndOrientation characterPosition)
        {
            CharacterPosition = characterPosition;

            IList<Sandbox.Definitions.MyVoxelMapStorageDefinition> vms = SpaceEngineersResources.VoxelMapStorageDefinitions;
            string contentPath = ToolboxUpdater.GetApplicationContentPath();

            foreach (Sandbox.Definitions.MyVoxelMapStorageDefinition voxelMap in vms)
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
                VoxelFileList.Add(voxel);
            }

            // Custom voxel files directory.
            List<string> files = [];
            if (!string.IsNullOrEmpty(GlobalSettings.Default.CustomVoxelPath) && Directory.Exists(GlobalSettings.Default.CustomVoxelPath))
            {
                files.AddRange(Directory.GetFiles(GlobalSettings.Default.CustomVoxelPath, "*" + MyVoxelMapBase.FileExtension.V1));
                files.AddRange(Directory.GetFiles(GlobalSettings.Default.CustomVoxelPath, "*" + MyVoxelMapBase.FileExtension.V2));
            }

            VoxelFileList.AddRange(files.Select(file => new GenerateVoxelDetailModel
            {
                Name = Path.GetFileNameWithoutExtension(file),
                SourceFileName = file,
                FileSize = new FileInfo(file).Length,
                Size = MyVoxelMapBase.LoadVoxelSize(file)
            }));


            VoxelFileList = [.. VoxelFileList.OrderBy(s => s.Name)];
        }

        #endregion
    }
}
