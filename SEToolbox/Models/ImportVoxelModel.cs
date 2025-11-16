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
            set
            {
                if (value != _sourceFile)
                {
                    _sourceFile = value;
                    OnPropertyChanged(nameof(SourceFile));
                    StockMaterial ??= MaterialsCollection[0];
                }
            }
        }

        public bool IsValidVoxelFile
        {
            get => _isValidVoxelFile;
            set
            {
                if (value != _isValidVoxelFile)
                {
                    _isValidVoxelFile = value;
                    OnPropertyChanged(nameof(IsValidVoxelFile));
                }
            }
        }

        public BindablePoint3DModel Position
        {
            get => _position;
            set
            {
                if (value != _position)
                {
                    _position = value;
                    OnPropertyChanged(nameof(Position));
                }
            }
        }

        public BindableVector3DModel Forward
        {
            get => _forward;
            set
            {
                if (value != _forward)
                {
                    _forward = value;
                    OnPropertyChanged(nameof(Forward));
                }
            }
        }

        public BindableVector3DModel Up
        {
            get => _up;
            set => SetProperty(ref _up, value, nameof(Up));
        }

        public MyPositionAndOrientation CharacterPosition
        {
            get => _characterPosition;
            set
            {
                //if (value != characterPosition) // Unable to check for equivilence, without long statement. And, mostly uncessary.
                _characterPosition = value;
                OnPropertyChanged(nameof(CharacterPosition));
            }
        }

        public bool IsStockVoxel
        {
            get => _isStockVoxel;
            set
            {
                if (value != _isStockVoxel)
                {
                    _isStockVoxel = value;
                    OnPropertyChanged(nameof(IsStockVoxel));
                }
            }
        }

        public bool IsFileVoxel
        {
            get => _isFileVoxel;
            set
            {
                if (value != _isFileVoxel)
                {
                    _isFileVoxel = value;
                    OnPropertyChanged(nameof(IsFileVoxel));
                }
            }
        }

        public bool IsSphere
        {
            get => _isSphere;
            set
            {
                if (value != _isSphere)
                {
                    _isSphere = value;
                    OnPropertyChanged(nameof(IsSphere));
                }
            }
        }

        public GenerateVoxelDetailModel StockVoxel
        {
            get => _stockVoxel;
            set
            {
                if (value != _stockVoxel)
                {
                    _stockVoxel = value;
                    OnPropertyChanged(nameof(StockVoxel));
                    IsStockVoxel = true;
                    StockMaterial ??= MaterialsCollection[0];
                }
            }
        }

        public List<GenerateVoxelDetailModel> VoxelFileList
        {
            get => _voxelFileList;
            set
            {
                if (value != _voxelFileList)
                {
                    _voxelFileList = value;
                    OnPropertyChanged(nameof(VoxelFileList));
                }
            }
        }

        public ObservableCollection<MaterialSelectionModel> MaterialsCollection
        {
            get => _materialsCollection;
         }

        public MaterialSelectionModel StockMaterial
        {
            get => _stockMaterial;
            set
            {
                if (value != _stockMaterial)
                {
                    _stockMaterial = value;
                    OnPropertyChanged(nameof(StockMaterial));
                }
            }
        }

        public int SphereRadius
        {
            get => _sphereRadius;

            set
            {
                if (value != _sphereRadius)
                {
                    _sphereRadius = value;
                    OnPropertyChanged(nameof(SphereRadius));
                }
            }
        }

        public int SphereShellRadius
        {
            get => _sphereShellRadius;

            set
            {
                if (value != _sphereShellRadius)
                {
                    _sphereShellRadius = value;
                    OnPropertyChanged(nameof(SphereShellRadius));
                }
            }
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
