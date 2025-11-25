using System;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using SEToolbox.Interfaces;
using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Models;
using SEToolbox.Services;
using SEToolbox.Support;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using IDType = VRage.MyEntityIdentifier.ID_OBJECT_TYPE;
using Res = SEToolbox.Properties.Resources;
using Mod = SEToolbox.Support.Modelling;

namespace SEToolbox.ViewModels
{
    public class Import3DModelViewModel : BaseViewModel
    {
        #region Fields

        private readonly IDialogService _dialogService;
        private readonly Func<IOpenFileDialog> _openFileDialogFactory;
        private readonly Import3DModelModel _dataModel;

        private bool? _closeResult;
        private bool _isBusy;

        #endregion

        #region Constructors

        public Import3DModelViewModel(BaseViewModel parentViewModel, Import3DModelModel dataModel)
            : this(parentViewModel, dataModel, ServiceLocator.Resolve<IDialogService>(), ServiceLocator.Resolve<IOpenFileDialog>)
        {
        }

        public Import3DModelViewModel(BaseViewModel parentViewModel, Import3DModelModel dataModel, IDialogService dialogService, Func<IOpenFileDialog> openFileDialogFactory)
            : base(parentViewModel)
        {
            Contract.Requires(dialogService != null);
            Contract.Requires(openFileDialogFactory != null);

            _dialogService = dialogService;
            _openFileDialogFactory = openFileDialogFactory;
            _dataModel = dataModel;
            // Will bubble property change events from the Model to the ViewModel.
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);

            IsMultipleScale = true;
            MultipleScale = 1;
            MaxLengthScale = 100;
            ClassType = ImportModelClassType.SmallShip;
            ArmorType = ImportArmorType.Light;
        }

        #endregion

        #region Command Properties

        public ICommand Browse3DModelCommand
        {
            get => new DelegateCommand(Browse3DModelExecuted, Browse3DModelCanExecute);
        }

        public ICommand CreateCommand
        {
            get => new DelegateCommand(CreateExecuted, CreateCanExecute);
        }

        public ICommand CancelCommand
        {
            get => new DelegateCommand(CancelExecuted, CancelCanExecute);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the DialogResult of the View.  If True or False is passed, this initiates the Close().
        /// </summary>
        public bool? CloseResult
        {
            get => _closeResult;
            set => SetProperty(ref _closeResult, value, nameof(CloseResult));
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
                    Application.DoEvents();
                }
            });

        }


        public string FileName
        {
            get => _dataModel.FileName;
            set => SetProperty(_dataModel.FileName, value, () =>
                   FileNameChanged());
        }

        public Model3D Model
        {
            get => _dataModel.Model;
            set => _dataModel.Model = value;
        }

        public bool IsValidModel
        {
            get => _dataModel.IsValidModel;
            set => _dataModel.IsValidModel = value;
        }

        public BindableSize3DModel OriginalModelSize
        {
            get => _dataModel.OriginalModelSize;
            set => _dataModel.OriginalModelSize = value;
        }

        public BindableSize3DIModel NewModelSize
        {
            get => _dataModel.NewModelSize;
            set => SetProperty(_dataModel.NewModelSize, value, () =>
                   ProcessModelScale());
        }

        public BindablePoint3DModel NewModelScale
        {
            get => _dataModel.NewModelScale;
            set => _dataModel.NewModelScale = value;
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

        public ModelTraceVoxel TraceType
        {
            get => _dataModel.TraceType;
            set => _dataModel.TraceType = value;
        }

        public ImportModelClassType ClassType
        {
            get => _dataModel.ClassType;
            set => SetProperty(_dataModel.ClassType, value, () =>
                   ProcessModelScale());

        }

        public bool IsAsteroid
        {
            get => _dataModel.IsAsteroid;
        }

        public bool IsShip
        {
            get => _dataModel.IsShip;
        }

        public ImportArmorType ArmorType
        {
            get => _dataModel.ArmorType;
            set => _dataModel.ArmorType = value;
        }


        public double MultipleScale
        {
            get => _dataModel.MultipleScale;
            set => SetProperty(_dataModel.MultipleScale, value, () =>
                   ProcessModelScale());
        }

        public double MaxLengthScale
        {
            get => _dataModel.MaxLengthScale;
            set => SetProperty(_dataModel.MaxLengthScale, value, () =>
                   ProcessModelScale());
        }

        public double BuildDistance
        {
            get => _dataModel.BuildDistance;

            set
            {
                _dataModel.BuildDistance = value;
                ProcessModelScale();
            }
        }

        public bool IsMultipleScale
        {
            get => _dataModel.IsMultipleScale;
            set => SetProperty(_dataModel.IsMultipleScale, value, () =>
                   ProcessModelScale());
        }

        public bool IsMaxLengthScale
        {
            get => _dataModel.IsMaxLengthScale;
            set => SetProperty(_dataModel.IsMaxLengthScale, value, () =>
            
                   ProcessModelScale());
        }

        public ObservableCollection<MaterialSelectionModel> OutsideMaterialsCollection
        {
            get => _dataModel.OutsideMaterialsCollection;
        }


        public ObservableCollection<MaterialSelectionModel> InsideMaterialsCollection
        {
            get => _dataModel.InsideMaterialsCollection;
        }

        public MaterialSelectionModel OutsideStockMaterial
        {
            get => _dataModel.OutsideStockMaterial;
            set => _dataModel.OutsideStockMaterial = value;
        }

        public MaterialSelectionModel InsideStockMaterial
        {
            get => _dataModel.InsideStockMaterial;
            set => _dataModel.InsideStockMaterial = value;
        }

        public string SourceFile
        {
            get => _dataModel.SourceFile;
            set => _dataModel.SourceFile = value;
        }

        public bool FillObject
        {
            get => _dataModel.FillObject;
            set => _dataModel.FillObject = value;
        }

        #endregion

        #region Command Methods

        public bool Browse3DModelCanExecute()
        {
            return true;
        }

        public void Browse3DModelExecuted()
        {
            IsValidModel = false;

            IOpenFileDialog openFileDialog = _openFileDialogFactory();
            openFileDialog.Filter = AppConstants.ModelFilter;
            openFileDialog.Title = Res.DialogImportModelTitle;

            // Open the dialog
            if (_dialogService.ShowOpenFileDialog(this, openFileDialog) == DialogResult.OK)
            {
                FileName = openFileDialog.FileName;
            }
        }

        private void FileNameChanged()
        {
            ProcessFileName(FileName);
        }

        public bool CreateCanExecute()
        {
            return IsValidModel;
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

        #region Methods

        private void ProcessFileName(string fileName)
        {
            IsValidModel = false;
            IsBusy = true;

            OriginalModelSize = new BindableSize3DModel(0, 0, 0);
            NewModelSize = new BindableSize3DIModel(0, 0, 0);
            Position = new BindablePoint3DModel(0, 0, 0);

            if (File.Exists(fileName))
            {
                // validate file is a real model.
                // read model properties.
                Rect3D bounds = Mod.PreviewVolumetricModel(fileName, out Model3D model);
                BindableSize3DModel size = new(bounds);
                Model = model;

                if (size != null && size.Height != 0 && size.Width != 0 && size.Depth != 0)
                {
                    OriginalModelSize = size;
                    BuildDistance = 10;
                    IsValidModel = true;
                    ProcessModelScale();
                }
            }

            IsBusy = false;
        }

        private void ProcessModelScale()
        {
            if (IsValidModel)
            {
                if (IsMaxLengthScale)
                {
                    double factor = MaxLengthScale / Math.Max(Math.Max(OriginalModelSize.Height, OriginalModelSize.Width), OriginalModelSize.Depth);

                    NewModelSize.Height = (int)(factor * OriginalModelSize.Height);
                    NewModelSize.Width = (int)(factor * OriginalModelSize.Width);
                    NewModelSize.Depth = (int)(factor * OriginalModelSize.Depth);
                }
                else if (IsMultipleScale)
                {
                    NewModelSize.Height = (int)(MultipleScale * OriginalModelSize.Height);
                    NewModelSize.Width = (int)(MultipleScale * OriginalModelSize.Width);
                    NewModelSize.Depth = (int)(MultipleScale * OriginalModelSize.Depth);
                }

                double vectorDistance = BuildDistance;
                double scaleMultiplier = 1;

                switch (ClassType)
                {
                    case ImportModelClassType.SmallShip: scaleMultiplier = MyCubeSize.Small.ToLength(); break;
                    case ImportModelClassType.SmallStation: scaleMultiplier = MyCubeSize.Small.ToLength(); break;
                    case ImportModelClassType.LargeShip: scaleMultiplier = MyCubeSize.Large.ToLength(); break;
                    case ImportModelClassType.LargeStation: scaleMultiplier = MyCubeSize.Large.ToLength(); break;
                    case ImportModelClassType.Asteroid: scaleMultiplier = 1; break;
                }
                vectorDistance += NewModelSize.Depth * scaleMultiplier;
                NewModelScale = new BindablePoint3DModel(NewModelSize.Width * scaleMultiplier, NewModelSize.Height * scaleMultiplier, NewModelSize.Depth * scaleMultiplier);

                // Figure out where the Character is facing, and plant the new construct right in front, by "10" units, facing the Character.
                Vector3D vector = new BindableVector3DModel(_dataModel.CharacterPosition.Forward).Vector3D;
                vector.Normalize();
                vector = Vector3D.Multiply(vector, vectorDistance);
                Position = new BindablePoint3DModel(Point3D.Add(new BindablePoint3DModel(_dataModel.CharacterPosition.Position).Point3D, vector));
                Forward = new BindableVector3DModel(_dataModel.CharacterPosition.Forward);
                Up = new BindableVector3DModel(_dataModel.CharacterPosition.Up);
            }
        }

        #endregion

        #region BuildTestEntity

        public MyObjectBuilder_CubeGrid BuildTestEntity()
        {
            MyObjectBuilder_CubeGrid entity = new()
            {
                EntityId = SpaceEngineersApi.GenerateEntityId(IDType.ENTITY),
                PersistentFlags = MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                Skeleton = [],
                LinearVelocity = new VRageMath.Vector3(0, 0, 0),
                AngularVelocity = new VRageMath.Vector3(0, 0, 0),
                GridSizeEnum = MyCubeSize.Large
            };

            string blockPrefix = entity.GridSizeEnum.ToString();
            string cornerBlockPrefix = entity.GridSizeEnum.ToString();
            //string blockContains = entity.GridSizeEnum.ToString().Contains();

            entity.IsStatic = false;
            blockPrefix += "BlockArmor";        // HeavyBlockArmor|BlockArmor;
            cornerBlockPrefix += "BlockArmor"; // HeavyBlockArmor|BlockArmor|RoundArmor_;

            // Figure out where the Character is facing, and plant the new constrcut right in front, by "10" units, facing the Character.
            Vector3D vector = new BindableVector3DModel(_dataModel.CharacterPosition.Forward).Vector3D;
            vector.Normalize();
            vector = Vector3D.Multiply(vector, 6);
            Position = new BindablePoint3DModel(Point3D.Add(new BindablePoint3DModel(_dataModel.CharacterPosition.Position).Point3D, vector));
            Forward = new BindableVector3DModel(_dataModel.CharacterPosition.Forward);
            Up = new BindableVector3DModel(_dataModel.CharacterPosition.Up);

            entity.PositionAndOrientation = new MyPositionAndOrientation
            {
                Position = Position.ToVector3D(),
                Forward = Forward.ToVector3(),
                Up = Up.ToVector3()
            };

            // Large|BlockArmor|Corner
            // Large|RoundArmor|Corner
            // Large|HeavyBlockArmor|Block,
            // Small|BlockArmor|Slope,
            // Small|HeavyBlockArmor|Corner,

            SubtypeId blockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "Block");
            SubtypeId slopeBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), cornerBlockPrefix + "Slope");
            SubtypeId cornerBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), cornerBlockPrefix + "Corner");
            SubtypeId inverseCornerBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), cornerBlockPrefix + "CornerInv");


            entity.CubeBlocks = [];

            bool smoothObject = true;
            bool subtractiveSmoothObject = false;

            // Read in voxel and set main cube space.
            //var cubic = TestCreateSplayedDiagonalPlane();
            //var cubic = TestCreateSlopedDiagonalPlane();
            //var cubic = TestCreateStaggeredStar();
            CubeType[][][] cubic = Mod.TestCreateTrayShape();
            //var cubic = ReadVolumetricModel(@"..\..\..\..\..\..\building 3D\models\Rhino_corrected.obj", 10, null, ModelTraceVoxel.ThickSmoothedDown);

            bool fillObject = false;

            if (smoothObject)
            {
                Mod.CalculateAddedInverseCorners(cubic);
                Mod.CalculateAddedSlopes(cubic);
                Mod.CalculateAddedCorners(cubic);
            }

            if (subtractiveSmoothObject)
            {
                Mod.CalculateSubtractedCorners(cubic);
                Mod.CalculateSubtractedSlopes(cubic);
                Mod.CalculateSubtractedInverseCorners(cubic);
            }

            Mod.BuildStructureFromCubic(entity, cubic, fillObject, blockType, slopeBlockType, cornerBlockType, inverseCornerBlockType);

            return entity;
        }

        #endregion

        #region BuildEntity

        public MyObjectBuilder_EntityBase BuildEntity()
        {
            if (ClassType == ImportModelClassType.Asteroid)
            {
                return BuildAsteroidEntity();
            }

            return BuildShipEntity();
        }

        private MyObjectBuilder_VoxelMap BuildAsteroidEntity()
        {
            string fileNamePart = Path.GetFileNameWithoutExtension(FileName);
            string fileName = MainViewModel.CreateUniqueVoxelStorageName(fileNamePart + MyVoxelMapBase.FileExtension.V2);
            Position = Position.RoundOff(1.0);
            Forward = Forward.RoundToAxis();
            Up = Up.RoundToAxis();

            var entity = new MyObjectBuilder_VoxelMap(Position.ToVector3(), fileName)
            {
                EntityId = SpaceEngineersApi.GenerateEntityId(IDType.ASTEROID),
                PersistentFlags = MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                StorageName = Path.GetFileNameWithoutExtension(fileName)
            };

            double multiplier;
            if (IsMultipleScale)
            {
                multiplier = MultipleScale;
            }
            else
            {
                multiplier = MaxLengthScale / Math.Max(Math.Max(OriginalModelSize.Height, OriginalModelSize.Width), OriginalModelSize.Depth);
            }

            var transform = MeshHelper.TransformVector(new Vector3D(0, 0, 0), 0, 0, 0);
            SourceFile = TempFileUtil.NewFileName(MyVoxelMapBase.FileExtension.V2);

            var baseMaterial = SpaceEngineersResources.VoxelMaterialDefinitions.FirstOrDefault(m => m.IsRare == false) ?? SpaceEngineersResources.VoxelMaterialDefinitions.FirstOrDefault();

            var voxelMap = MyVoxelBuilder.BuildAsteroidFromModel(true, FileName, OutsideStockMaterial.MaterialIndex.Value, baseMaterial.Index, InsideStockMaterial.Value != null, InsideStockMaterial.MaterialIndex, ModelTraceVoxel.ThinSmoothed, multiplier, transform, MainViewModel.ResetProgress, MainViewModel.IncrementProgress);
            voxelMap.Save(SourceFile);

            MainViewModel.ClearProgress();

            entity.PositionAndOrientation = new MyPositionAndOrientation
            {
                Position = Position.ToVector3D(),
                Forward = Forward.ToVector3(),
                Up = Up.ToVector3()
            };

            IsValidModel = voxelMap.BoundingContent.Size.Volume() > 0;

            return entity;
        }

        private MyObjectBuilder_CubeGrid BuildShipEntity()
        {
            MyObjectBuilder_CubeGrid entity = new()
            {
                EntityId = SpaceEngineersApi.GenerateEntityId(IDType.ENTITY),
                PersistentFlags = MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                Skeleton = [],
                LinearVelocity = new VRageMath.Vector3(0, 0, 0),
                AngularVelocity = new VRageMath.Vector3(0, 0, 0)
            };

            string blockPrefix = "";
            switch (ClassType)
            {
                case ImportModelClassType.SmallShip:
                    entity.GridSizeEnum = MyCubeSize.Small;
                    blockPrefix += "Small";
                    entity.IsStatic = false;
                    break;

                case ImportModelClassType.LargeShip:
                    entity.GridSizeEnum = MyCubeSize.Large;
                    blockPrefix += "Large";
                    entity.IsStatic = false;
                    break;

                case ImportModelClassType.SmallStation:
                    entity.GridSizeEnum = MyCubeSize.Small;
                    blockPrefix += "Small";
                    entity.IsStatic = true;
                    Position = Position.RoundOff(MyCubeSize.Small.ToLength());
                    Forward = Forward.RoundToAxis();
                    Up = Up.RoundToAxis();
                    break;

                case ImportModelClassType.LargeStation:
                    entity.GridSizeEnum = MyCubeSize.Large;
                    blockPrefix += "Large";
                    entity.IsStatic = true;
                    Position = Position.RoundOff(MyCubeSize.Large.ToLength());
                    Forward = Forward.RoundToAxis();
                    Up = Up.RoundToAxis();
                    break;
            }

            switch (ArmorType)
            {
                case ImportArmorType.Heavy: blockPrefix += "HeavyBlockArmor"; break;
                case ImportArmorType.Light: blockPrefix += "BlockArmor"; break;                // Currently in development, and only specified as 'Light' on the 'Large' structures.
                //Round Armor
                case ImportArmorType.HeavyRounded:
                case ImportArmorType.LightRounded:
                    blockPrefix += "Rounded";
                    break;
                // Angled
                case ImportArmorType.HeavyAngled:
                case ImportArmorType.LightAngled:
                    blockPrefix += "Angled"; 
                    break;
                // Slope
                case ImportArmorType.HeavySlope:
                case ImportArmorType.LightSlope:
                    blockPrefix += "Slope";
                    break;
                // Corner
                case ImportArmorType.HeavyCorner:
                case ImportArmorType.LightCorner:
                    blockPrefix += "Corner"; 
                    break;
            }

            // Large|BlockArmor|Corner
            // Large|RoundArmor_|Corner
            // Large|HeavyBlockArmor|Block,
            // Small|BlockArmor|Slope,
            // Small|HeavyBlockArmor|Corner,

            SubtypeId blockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "Block");
            SubtypeId smallBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "SmallBlock");
            SubtypeId heavyBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "SmallHeavyBlock");
            SubtypeId largeBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "BlockArmor");
            SubtypeId roundBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "ArmorRound");
            SubtypeId angledBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "Angled");
            SubtypeId slopeBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "Slope");
            SubtypeId cornerBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "Corner");
            SubtypeId inverseCornerBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "CornerInv");

            entity.CubeBlocks = [];

            double multiplier;
            if (IsMultipleScale)
            {
                multiplier = MultipleScale;
            }
            else
            {
                multiplier = MaxLengthScale / Math.Max(Math.Max(OriginalModelSize.Height, OriginalModelSize.Width), OriginalModelSize.Depth);
            }

            CubeType[][][] cubic = Mod.ReadVolumetricModel(FileName, multiplier, null, TraceType, MainViewModel.ResetProgress, MainViewModel.IncrementProgress);

            Mod.BuildStructureFromCubic(entity, cubic, FillObject, blockType, slopeBlockType, cornerBlockType, inverseCornerBlockType);

            MainViewModel.ClearProgress();

            entity.PositionAndOrientation = new MyPositionAndOrientation
            {
                // TODO: reposition based scale.
                Position = Position.ToVector3D(),
                Forward = Forward.ToVector3(),
                Up = Up.ToVector3()
            };

            IsValidModel = entity.CubeBlocks.Count > 0;

            return entity;
        }

        #endregion
    }
}
