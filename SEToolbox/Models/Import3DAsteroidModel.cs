using System.Collections.ObjectModel;
using System.Windows.Media.Media3D;
using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using VRage;

namespace SEToolbox.Models
{
    public class Import3DAsteroidModel : BaseModel
    {
        #region Fields


        private string _fileName;
        private Model3D _model;
        private bool _isValidModel;
        private bool _isValidEntity;

        private BindableSize3DModel _originalModelSize;
        private BindableSize3DIModel _newModelSize;
        private BindablePoint3DModel _newModelScale;
        private BindablePoint3DModel _position;
        private BindableVector3DModel _forward;
        private BindableVector3DModel _up;
        private MyPositionAndOrientation _characterPosition;
        private TraceType _traceType;
        private TraceCount _traceCount;
        private TraceDirection _traceDirection;
        private double _multipleScale;
        private double _maxLengthScale;
        private double _buildDistance;
        private bool _isMultipleScale;
        private bool _isMaxLengthScale;
        private bool _isAbsolutePosition;
        private bool _isInfrontofPlayer;
        private readonly ObservableCollection<MaterialSelectionModel> _outsideMaterialsCollection;
        private readonly ObservableCollection<MaterialSelectionModel> _insideMaterialsCollection;
        private MaterialSelectionModel _outsideStockMaterial;
        private MaterialSelectionModel _insideStockMaterial;
        private int _outsideMaterialDepth;
        private string _sourceFile;
        private double _rotateYaw;
        private double _rotatePitch;
        private double _rotateRoll;
        private bool _beepWhenFinished;
        private bool _saveWhenFinsihed;
        private bool _shutdownWhenFinished;
        private bool _runInLowPrioity;
        //private bool _pauseWhenFinished;

        #endregion

        #region Ctor

        public Import3DAsteroidModel()
        {
            _outsideMaterialsCollection = [];
            _insideMaterialsCollection = [];

            foreach (var material in SpaceEngineersResources.VoxelMaterialDefinitions)
            {
                _outsideMaterialsCollection.Add(new MaterialSelectionModel { Value = material.Id.SubtypeName, DisplayName = material.Id.SubtypeName });
                _insideMaterialsCollection.Add(new MaterialSelectionModel { Value = material.Id.SubtypeName, DisplayName = material.Id.SubtypeName });
            }

            InsideStockMaterial = InsideMaterialsCollection[0];
            OutsideStockMaterial = OutsideMaterialsCollection[0];

            TraceType = TraceType.Odd;
            TraceCount = TraceCount.Trace5;
            TraceDirection = TraceDirection.X;

            BeepWhenFinished = true;
            RunInLowPrioity = false;
        }

        #endregion

        #region Properties

        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value, nameof(FileName));
        }

        public Model3D Model
        {
            get  => _model;
            set => SetProperty(ref _model, value, nameof(Model));
        }

        /// <summary>
        /// Indicates if the selected model file the user has specified is a valid model.
        /// </summary>
        public bool IsValidModel
        {
            get => _isValidModel;
            set => SetProperty(ref _isValidModel, value, nameof(IsValidModel));
        }

        /// <summary>
        /// Indicates if the Entity created at the end of processing is valid.
        /// </summary>
        public bool IsValidEntity
        {
            get => _isValidEntity;
            set => SetProperty(ref _isValidEntity, value, nameof(IsValidEntity));
        }

        public BindableSize3DModel OriginalModelSize
        {
            get => _originalModelSize;
            set => SetProperty(ref _originalModelSize, value, nameof(OriginalModelSize));
        }

        public BindableSize3DIModel NewModelSize
        {
            get => _newModelSize;
            set => SetProperty(ref _newModelSize, value, nameof(NewModelSize));
        }

        public BindablePoint3DModel NewModelScale
        {
            get => _newModelScale;
            set => SetProperty(ref _newModelScale, value, nameof(NewModelScale));
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
            // unable to checck for equivalence and mostly unnecessary
            set => SetProperty(ref _characterPosition, value, nameof(CharacterPosition));
        }

        public TraceType TraceType
        {
            get => _traceType;
            set => SetProperty(ref _traceType, value, nameof(TraceType));
        }

        public TraceCount TraceCount
        {
            get => _traceCount;
            set => SetProperty(ref _traceCount, value, nameof(TraceCount));
        }

        public TraceDirection TraceDirection
        {
            get => _traceDirection;
            set => SetProperty(ref _traceDirection, value, nameof(TraceDirection));
        }

        public double MultipleScale
        {
            get => _multipleScale;
            set => SetProperty(ref _multipleScale, value, nameof(MultipleScale));
        }

        public double MaxLengthScale
        {
            get => _maxLengthScale;
            set => SetProperty(ref _maxLengthScale, value, nameof(MaxLengthScale));
        }

        public double BuildDistance
        {
            get => _buildDistance;
            set => SetProperty(ref _buildDistance, value, nameof(BuildDistance));
        }

        public bool IsMultipleScale
        {
            get => _isMultipleScale;
            set => SetProperty(ref _isMultipleScale, value, nameof(IsMultipleScale));
        }

        public bool IsMaxLengthScale
        {
            get => _isMaxLengthScale;
            set => SetProperty(ref _isMaxLengthScale, value, nameof(IsMaxLengthScale));
        }

        public bool IsAbsolutePosition
        {
            get => _isAbsolutePosition;
            set => SetProperty(ref _isAbsolutePosition, value, nameof(IsAbsolutePosition));
        }

        public bool IsInfrontofPlayer
        {
            get => _isInfrontofPlayer;
            set => SetProperty(ref _isInfrontofPlayer, value, nameof(IsInfrontofPlayer));
        }

        public ObservableCollection<MaterialSelectionModel> OutsideMaterialsCollection
        {
            get => _outsideMaterialsCollection;
        }
        
        public int OutsideMaterialDepth
        {
            get => _outsideMaterialDepth;
            set => SetProperty(ref _outsideMaterialDepth, value, nameof(OutsideMaterialDepth));
        }

        public ObservableCollection<MaterialSelectionModel> InsideMaterialsCollection
        {
            get => _insideMaterialsCollection;
        }

        public MaterialSelectionModel OutsideStockMaterial
        {
            get => _outsideStockMaterial;
            set => SetProperty(ref _outsideStockMaterial, value, nameof(OutsideStockMaterial));
        }

        public MaterialSelectionModel InsideStockMaterial
        {
            get => _insideStockMaterial;
            set => SetProperty(ref _insideStockMaterial, value, nameof(InsideStockMaterial));
        }

        public string SourceFile
        {
            get => _sourceFile;
            set => SetProperty(ref _sourceFile, value, nameof(SourceFile));
        }

        public double RotateYaw
        {
            get => _rotateYaw;
            set => SetProperty(ref _rotateYaw, value, nameof(RotateYaw));
        }

        public double RotatePitch
        {
            get => _rotatePitch;
            set => SetProperty(ref _rotatePitch, value, nameof(RotatePitch));
        }

        public double RotateRoll
        {
            get => _rotateRoll; 

            set => SetProperty(ref _rotateRoll, value, nameof(RotateRoll));
        }

        public bool BeepWhenFinished
        {
            get => _beepWhenFinished;
            set => SetProperty(ref _beepWhenFinished, value, nameof(BeepWhenFinished));
        }

        public bool SaveWhenFinsihed
        {
            get => _saveWhenFinsihed;
            set => SetProperty(ref _saveWhenFinsihed, value, nameof(SaveWhenFinsihed));
        }

        public bool ShutdownWhenFinished
        {
            get => _shutdownWhenFinished;
            set => SetProperty(ref _shutdownWhenFinished, value, nameof(ShutdownWhenFinished));
        }

        public bool RunInLowPrioity 
        {
            get  => _runInLowPrioity;
            set => SetProperty(ref _runInLowPrioity, value, nameof(RunInLowPrioity));
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
