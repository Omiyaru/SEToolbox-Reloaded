using System.Collections.ObjectModel;
using System.Windows.Media.Media3D;
using SEToolbox.Interop;
using SEToolbox.Support;
using VRage;
using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.Models
{
    public class Import3DModelModel : BaseModel
    {
        #region Fields

        private string _fileName;
        private Model3D _model;
        private bool _isValidModel;

        private BindableSize3DModel _originalModelSize;
        private BindableSize3DIModel _newModelSize;
        private BindablePoint3DModel _newModelScale;
        private BindablePoint3DModel _position;
        private BindableVector3DModel _forward;
        private BindableVector3DModel _up;
        private ModelTraceVoxel _traceType;
        private ImportModelClassType _classType;
        private ImportArmorType _armorType;
        private MyPositionAndOrientation _characterPosition;
        private double _multipleScale;
        private double _maxLengthScale;
        private double _buildDistance;
        private bool _isMultipleScale;
        private bool _isMaxLengthScale;
        private readonly ObservableCollection<MaterialSelectionModel> _outsideMaterialsCollection;
        private readonly ObservableCollection<MaterialSelectionModel> _insideMaterialsCollection;
        private MaterialSelectionModel _outsideStockMaterial;
        private MaterialSelectionModel _insideStockMaterial;
        private string _sourceFile;
        private bool _fillObject;

        #endregion

        #region Ctor

        public Import3DModelModel()
        {
            TraceType = ModelTraceVoxel.ThinSmoothed;

            _outsideMaterialsCollection = [];
            _insideMaterialsCollection =
            [
                new() { Value = null, DisplayName = Res.WnImport3dModelEmpty }
            ];

            foreach (var material in SpaceEngineersResources.VoxelMaterialDefinitions)
            {
                _outsideMaterialsCollection.Add(new MaterialSelectionModel { Value = material.Id.SubtypeName, DisplayName = material.Id.SubtypeName });
                _insideMaterialsCollection.Add(new MaterialSelectionModel { Value = material.Id.SubtypeName, DisplayName = material.Id.SubtypeName });
            }

            InsideStockMaterial = InsideMaterialsCollection[0];
            OutsideStockMaterial = OutsideMaterialsCollection[0];
        }

        #endregion

        #region Properties

        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, nameof(FileName));
        }

        public Model3D Model
        {
            get => _model;
            set => SetProperty(ref _model, nameof(Model));
        }


        public bool IsValidModel
        {
            get => _isValidModel;
            set => SetProperty(ref _isValidModel, nameof(IsValidModel));

        }

        public BindableSize3DModel OriginalModelSize
        {
            get => _originalModelSize;
            set => SetProperty(ref _originalModelSize, nameof(OriginalModelSize));
        }

        public BindableSize3DIModel NewModelSize
        {
            get => _newModelSize;
            set => SetProperty(ref _newModelSize, nameof(NewModelSize));
        }

        public BindablePoint3DModel NewModelScale
        {
            get => _newModelScale;
            set => SetProperty(ref _newModelScale, nameof(NewModelScale));
        }

        public BindablePoint3DModel Position
        {
            get => _position;
            set => SetProperty(ref _position, nameof(Position));
        }

        public BindableVector3DModel Forward
        {
            get => _forward;
            set => SetProperty(ref _forward, nameof(Forward));
        }

        public BindableVector3DModel Up
        {
            get => _up;
            set => SetProperty(ref _up, nameof(Up));
        }

        public ModelTraceVoxel TraceType
        {
            get => _traceType;
            set => SetProperty(ref _traceType, nameof(TraceType));
        }

        public ImportModelClassType ClassType
        {
            get => _classType;
            set => SetProperty(ref _classType, nameof(ClassType));
        }

        public bool IsAsteroid
        {
            get => _classType == ImportModelClassType.Asteroid;
        }

        public bool IsShip
        {
            get => _classType != ImportModelClassType.Asteroid;
        }

        public ImportArmorType ArmorType
        {
            get => _armorType;
            set => SetProperty(ref _armorType, nameof(ArmorType));
        }


        public MyPositionAndOrientation CharacterPosition
        {
            get => _characterPosition;
            //unable to check for equivalence and is mostly unnecessary
            set => SetProperty(ref _characterPosition, nameof(CharacterPosition));
        }

        public double MultipleScale
        {
            get => _multipleScale;
            set => SetProperty(ref _multipleScale, nameof(MultipleScale));
        }

        public double MaxLengthScale
        {
            get => _maxLengthScale;
            set => SetProperty(ref _maxLengthScale, nameof(MaxLengthScale));
        }

        public double BuildDistance
        {
            get => _buildDistance;
            set => SetProperty(ref _buildDistance, nameof(BuildDistance));
        }

        public bool IsMultipleScale
        {
            get => _isMultipleScale;
            set => SetProperty(ref _isMultipleScale, nameof(IsMultipleScale));
        }

        public bool IsMaxLengthScale
        {
            get => _isMaxLengthScale;
            set => SetProperty(ref _isMaxLengthScale, nameof(IsMaxLengthScale));
        }

        public ObservableCollection<MaterialSelectionModel> OutsideMaterialsCollection
        {
            get => _outsideMaterialsCollection;
        }


        public ObservableCollection<MaterialSelectionModel> InsideMaterialsCollection
        {
            get => _insideMaterialsCollection;
        }

        public MaterialSelectionModel OutsideStockMaterial 
        {
            get => _outsideStockMaterial;
            set => SetProperty(ref _outsideStockMaterial, nameof(OutsideStockMaterial));
        }

        public MaterialSelectionModel InsideStockMaterial 
        {
            get => _insideStockMaterial;
            set => SetProperty(ref _insideStockMaterial, nameof(InsideStockMaterial));
        }

        public string SourceFile
        {
            get => _sourceFile;
            set => SetProperty(ref _sourceFile, nameof(SourceFile));
        }

        public bool FillObject 
        {
            get => _fillObject;
            set => SetProperty(ref _fillObject, nameof(FillObject));
        }

        #endregion

        #region Methods

        public void Load(MyPositionAndOrientation characterPosition)
        {
            CharacterPosition = characterPosition;
        }

        #endregion
    }
}
