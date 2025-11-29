using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Media3D;

using SEToolbox.Interfaces;
using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Models;
using SEToolbox.Services;
using SEToolbox.Support;
using VRageMath;
using Res = SEToolbox.Properties.Resources;
using IDType = VRage.MyEntityIdentifier.ID_OBJECT_TYPE;
using VRage.ObjectBuilders;
using VRage;
using VRage.Game;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace SEToolbox.ViewModels
{
    public class ImportVoxelViewModel : BaseViewModel
    {
        #region Fields

        private readonly IDialogService _dialogService;
        private readonly Func<IOpenFileDialog> _openFileDialogFactory;
        private readonly ImportVoxelModel _dataModel;

        private bool? _closeResult;
        private bool _isBusy;

        #endregion

        #region Constructors

        public ImportVoxelViewModel(BaseViewModel parentViewModel, ImportVoxelModel dataModel)
            : this(parentViewModel, dataModel, ServiceLocator.Resolve<IDialogService>(), ServiceLocator.Resolve<IOpenFileDialog>)
        {
        }

        public ImportVoxelViewModel(BaseViewModel parentViewModel, ImportVoxelModel dataModel, IDialogService dialogService, Func<IOpenFileDialog> openFileDialogFactory)
            : base(parentViewModel)
        {
            Contract.Requires(dialogService != null);
            Contract.Requires(openFileDialogFactory != null);

            _dialogService = dialogService;
            _openFileDialogFactory = openFileDialogFactory;
            _dataModel = dataModel;
            // Will bubble property change events from the Model to the ViewModel.
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Properties

        public ICommand BrowseVoxelCommand => new DelegateCommand(BrowseVoxelExecuted, BrowseVoxelCanExecute);

        public ICommand CreateCommand => new DelegateCommand(CreateExecuted, CreateCanExecute);

        public ICommand CancelCommand => new DelegateCommand(CancelExecuted, CancelCanExecute);

        /// <summary>
        /// Gets or sets the DialogResult of the View.  If True or False is passed, this initiates the Close().
        /// </summary>
        public bool? CloseResult
        {
            get => _closeResult;
            set => SetProperty(ref _closeResult, value, nameof(CloseResult));
        }

        public string FileName
        {
            get => _dataModel.FileName;
            set => _dataModel.FileName = value;
        }

        public string SourceFile
        {
            get => _dataModel.SourceFile;
            set => SetValue(_dataModel.SourceFile, value,() =>
                   SourceFileChanged());
        }

        public bool IsValidVoxelFile
        {
            get => _dataModel.IsValidVoxelFile;
            set => _dataModel.IsValidVoxelFile = value;
        }

        public BindablePoint3DModel Position
        {
            get => _dataModel.Position;
            set => _dataModel.Position = value;
        }

        public BindableVector3DModel Forward
        {
            get => _dataModel.Forward;
            set => _dataModel.Forward = value;
        }

        public BindableVector3DModel Up
        {
            get => _dataModel.Up;
            set => _dataModel.Up = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value, nameof(IsBusy),() =>
            {
                if (_isBusy)
                {
                    Application.DoEvents();
                }
            });
        }

        public bool IsStockVoxel
        {
            get => _dataModel.IsStockVoxel;
            set => _dataModel.IsStockVoxel = value;
        }

        public bool IsFileVoxel
        {
            get => _dataModel.IsFileVoxel;
            set => _dataModel.IsFileVoxel = value;
        }

        public bool IsSphere
        {
            get => _dataModel.IsSphere;
            set => _dataModel.IsSphere = value;
        }

        public GenerateVoxelDetailModel StockVoxel
        {
            get => _dataModel.StockVoxel;
            set => _dataModel.StockVoxel = value;
        }

        public List<GenerateVoxelDetailModel> VoxelFileList
        {
            get => _dataModel.VoxelFileList;
        }

        public ObservableCollection<MaterialSelectionModel> MaterialsCollection
        {
            get => _dataModel.MaterialsCollection;
        }

        public MaterialSelectionModel StockMaterial
        {
            get => _dataModel.StockMaterial;
            set => _dataModel.StockMaterial = value;
        }

        public int SphereRadius
        {
            get => _dataModel.SphereRadius;
            set => _dataModel.SphereRadius = value;
        }

        public int SphereShellRadius
        {
            get => _dataModel.SphereShellRadius;
            set => _dataModel.SphereShellRadius = value;
        }

        #endregion

        #region Command Methods

        public bool BrowseVoxelCanExecute()
        {
            return true;
        }

        public void BrowseVoxelExecuted()
        {
            BrowseVoxel();
        }

        public bool CreateCanExecute()
        {
            return (IsValidVoxelFile && IsFileVoxel)
                || (IsStockVoxel && StockVoxel != null)
                || (IsSphere && SphereRadius > 0);
        }

        public void CreateExecuted()
        {
            CloseResult = true;
        }

        public bool CancelCanExecute()
        {
            return true;
        }

        public void CancelExecuted()
        {
            CloseResult = false;
        }

        #endregion

        #region Helpers

        private void BrowseVoxel()
        {
            IsValidVoxelFile = false;
            var openFileDialog = _openFileDialogFactory();
            openFileDialog.Filter = AppConstants.VoxelAnyFilter;
            openFileDialog.Title = Res.DialogImportVoxelTitle;

            // Open the dialog
            var result = _dialogService.ShowOpenFileDialog(OwnerViewModel, openFileDialog);

            if (result == DialogResult.OK)
            {
                SourceFile = openFileDialog.FileName;
            }
        }

        private void SourceFileChanged()
        {
            ProcessSourceFile(SourceFile);
        }

        private void ProcessSourceFile(string fileName)
        {
            IsBusy = true;

            if (File.Exists(fileName))
            {
                IsValidVoxelFile =  MyVoxelMapBase.IsVoxelMapFile(fileName);
                IsFileVoxel = true;
            }
            else
            {
                IsValidVoxelFile = false;
                IsFileVoxel = false;
            }

            IsBusy = false;
        }

        public MyObjectBuilder_EntityBase BuildEntity()
        {
            VRageMath.Vector3D asteroidCenter = new();
            Vector3I asteroidSize = new();

            string originalFile = null;
            if (IsStockVoxel)
            {
                string stockfile = StockVoxel.SourceFileName;

                if (StockMaterial == null || StockMaterial.Value == null)
                {
                    SourceFile = stockfile;
                    originalFile = SourceFile;

                    using MyVoxelMapBase asteroid = new();
                    asteroid.Load(stockfile);
                    asteroidCenter = asteroid.BoundingContent.Center;
                    asteroidSize = asteroid.BoundingContent.Size + 1; // Content size
                }
                else
                {
                    using MyVoxelMapBase asteroid = new();
                    asteroid.Load(stockfile);
                    asteroid.ForceBaseMaterial(SpaceEngineersResources.GetDefaultMaterialName(), StockMaterial.Value);
                    SourceFile = TempFileUtil.NewFileName(MyVoxelMapBase.FileExtension.V2);
                    asteroid.Save(SourceFile);

                    originalFile = StockVoxel.SourceFileName;
                    asteroidCenter = asteroid.BoundingContent.Center;
                    asteroidSize = asteroid.BoundingContent.Size + 1; // Content size
                }
            }
            else if (IsFileVoxel)
            {
                originalFile = SourceFile;

                using MyVoxelMapBase asteroid = new();
                asteroid.Load(SourceFile);
                asteroidCenter = asteroid.BoundingContent.Center;
                asteroidSize = asteroid.BoundingContent.Size + 1; // Content size

                if (StockMaterial != null && StockMaterial.Value != null)
                {
                    asteroid.ForceBaseMaterial(SpaceEngineersResources.GetDefaultMaterialName(), StockMaterial.Value);
                    SourceFile = TempFileUtil.NewFileName( MyVoxelMapBase.FileExtension.V2);
                    asteroid.Save(SourceFile);
                }
            }
            else if (IsSphere)
            {
                byte materialIndex;
                if (StockMaterial?.MaterialIndex != null)
                    materialIndex = StockMaterial.MaterialIndex.Value;
                else
                    materialIndex = SpaceEngineersResources.GetDefaultMaterialIndex();

                string materialName = SpaceEngineersResources.GetMaterialName(materialIndex);

                originalFile = string.Format($"sphere_{materialName.ToLowerInvariant()}_{SphereRadius}_{SphereShellRadius}{MyVoxelMapBase.FileExtension.V2}" );

                using  MyVoxelMapBase asteroid = MyVoxelBuilder.BuildAsteroidSphere(SphereRadius > 32, SphereRadius, materialIndex, materialIndex, SphereShellRadius != 0, SphereShellRadius);
                // TODO: progress bar.
                asteroidCenter = asteroid.BoundingContent.Center;
                asteroidSize = asteroid.BoundingContent.Size + 1; // Content size
                SourceFile = TempFileUtil.NewFileName( MyVoxelMapBase.FileExtension.V2);
                asteroid.Save(SourceFile);
            }

            // automatically number all files, and check for duplicate fileNames.
            FileName = MainViewModel.CreateUniqueVoxelStorageName(originalFile);

            // Figure out where the Character is facing, and plant the new constrcut right in front.
            // Calculate the hypotenuse, as it will be the safest distance to place in front.
            double distance = Math.Sqrt(Math.Pow(asteroidSize.X, 2) + Math.Pow(asteroidSize.Y, 2) + Math.Pow(asteroidSize.Z, 2)) / 2;

            Vector3D vector = new BindableVector3DModel(_dataModel.CharacterPosition.Forward).Vector3D;
            vector.Normalize();
            vector = Vector3D.Multiply(vector, distance);
            Position = new BindablePoint3DModel(Point3D.Add(new BindablePoint3DModel(_dataModel.CharacterPosition.Position).Point3D, vector));
            //Forward = new BindableVector3DModel(_dataModel.CharacterPosition.Forward);
            //Up = new BindableVector3DModel(_dataModel.CharacterPosition.Up);
            Forward = new BindableVector3DModel(Vector3.Forward);  // Asteroids currently don't have any orientation.
            Up = new BindableVector3DModel(Vector3.Up);

            MyObjectBuilder_VoxelMap entity = new()
            {
                EntityId = SpaceEngineersApi.GenerateEntityId(IDType.ASTEROID),
                PersistentFlags = MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                StorageName = Path.GetFileNameWithoutExtension(FileName),
                PositionAndOrientation = new MyPositionAndOrientation
                {
                    Position = Position.ToVector3D() - asteroidCenter,
                    Forward = Forward.ToVector3(),
                    Up = Up.ToVector3()
                }
            };

            return entity;
        }

        #endregion
    }
}
