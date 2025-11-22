using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using SEToolbox.Interfaces;
using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Models;
using SEToolbox.Services;
using SEToolbox.Support;
using SEToolbox.Views;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;
using IDType = VRage.MyEntityIdentifier.ID_OBJECT_TYPE;
using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.ViewModels
{


    public class Import3DAsteroidViewModel : BaseViewModel
    {
        #region Fields

        private readonly IDialogService _dialogService;
        private readonly Func<IOpenFileDialog> _openFileDialogFactory;
        private readonly Import3DAsteroidModel _dataModel;

        private bool? _closeResult;
        private bool _isBusy;
        private Rect3D _originalBounds;

        #endregion

        #region Constructors

        public Import3DAsteroidViewModel(BaseViewModel parentViewModel, Import3DAsteroidModel dataModel)
            : this(parentViewModel, dataModel, ServiceLocator.Resolve<IDialogService>(), ServiceLocator.Resolve<IOpenFileDialog>)
        {
        }

        public Import3DAsteroidViewModel(BaseViewModel parentViewModel, Import3DAsteroidModel dataModel, IDialogService dialogService, Func<IOpenFileDialog> openFileDialogFactory)
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
            OutsideMaterialDepth = 1;
            IsInfrontofPlayer = true;
            Position = new BindablePoint3DModel();
            BuildDistance = 10;
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

            set => SetProperty(_dataModel.FileName, () => FileNameChanged());
        }

        public Model3D Model
        {
            get => _dataModel.Model;
            set => _dataModel.Model = value;
        }

        public bool IsValidModel
        {
            get => _dataModel.IsValidModel;
            set => SetProperty(_dataModel.IsValidModel, value, nameof(IsWrongModel));
        }

        public bool IsValidEntity
        {
            get => _dataModel.IsValidEntity;
            set => _dataModel.IsValidEntity = value;
        }

        public bool IsWrongModel
        {
            get => !_dataModel.IsValidModel;
        }

        public BindableSize3DModel OriginalModelSize
        {
            get => _dataModel.OriginalModelSize;
            set => _dataModel.OriginalModelSize = value;
        }

        public BindableSize3DIModel NewModelSize
        {
            get => _dataModel.NewModelSize;
            set => SetProperty(_dataModel.NewModelSize, () => ProcessModelScale());
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

        public TraceType TraceType
        {
            get => _dataModel.TraceType;
            set => _dataModel.TraceType = value;
        }

        public TraceCount TraceCount
        {
            get => _dataModel.TraceCount;
            set => _dataModel.TraceCount = value;
        }

        public TraceDirection TraceDirection
        {
            get => _dataModel.TraceDirection;
            set => _dataModel.TraceDirection = value;
        }

        public double MultipleScale
        {
            get => _dataModel.MultipleScale;
            set => SetProperty(_dataModel.MultipleScale, () => ProcessModelScale());
        }

        public double MaxLengthScale
        {
            get => _dataModel.MaxLengthScale;
            set => SetProperty(_dataModel.MaxLengthScale, () => ProcessModelScale());
        }

        public double BuildDistance
        {
            get => _dataModel.BuildDistance;
            set => SetProperty(_dataModel.BuildDistance, () => ProcessModelScale());
        }

        public bool IsMultipleScale
        {
            get => _dataModel.IsMultipleScale;
            set => SetProperty(_dataModel.IsMultipleScale, () => ProcessModelScale());
        }

        public bool IsMaxLengthScale
        {
            get => _dataModel.IsMaxLengthScale;
            set => SetProperty(_dataModel.IsMaxLengthScale, () => ProcessModelScale());
        }

        public bool IsAbsolutePosition
        {
            get => _dataModel.IsAbsolutePosition;
            set => _dataModel.IsAbsolutePosition = value;
        }

        public bool IsInfrontofPlayer
        {
            get => _dataModel.IsInfrontofPlayer;
            set => _dataModel.IsInfrontofPlayer = value;
        }

        public ObservableCollection<MaterialSelectionModel> OutsideMaterialsCollection
        {
            get => _dataModel.OutsideMaterialsCollection;
        }

        public int OutsideMaterialDepth
        {
            get => _dataModel.OutsideMaterialDepth;
            set => _dataModel.OutsideMaterialDepth = value;
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

        public double RotatePitch
        {
            get => _dataModel.RotatePitch;
            set => SetProperty(_dataModel.RotatePitch, () => ProcessModelScale());
        }

        public double RotateYaw
        {
            get => _dataModel.RotateYaw;
            set => SetProperty(_dataModel.RotateYaw, () => ProcessModelScale());
        }

        public double RotateRoll
        {
            get => _dataModel.RotateRoll;
            set => SetProperty(_dataModel.RotateRoll, () => ProcessModelScale());
        }

        public bool BeepWhenFinished
        {
            get => _dataModel.BeepWhenFinished;
            set => _dataModel.BeepWhenFinished = value;
        }

        public bool SaveWhenFinsihed
        {
            get => _dataModel.SaveWhenFinsihed;
            set => _dataModel.SaveWhenFinsihed = value;
        }

        public bool ShutdownWhenFinished
        {
            get => _dataModel.ShutdownWhenFinished;
            set => _dataModel.ShutdownWhenFinished = value;
        }

        public bool RunInLowPrioity
        {
            get => _dataModel.RunInLowPrioity;
            set => _dataModel.RunInLowPrioity = value;
        }

        public MyObjectBuilder_VoxelMap NewEntity { get; set; }

        #endregion

        #region Command Methods

        public bool Browse3DModelCanExecute()
        {
            return true;
        }

        public void Browse3DModelExecuted()
        {
            IsValidModel = false;
            IsValidEntity = false;

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
            var buildEntity = BuildEntity();

            // do not close if cancelled.
            if (buildEntity)
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
            IsValidEntity = false;
            IsBusy = true;

            OriginalModelSize = new BindableSize3DModel(0, 0, 0);
            NewModelSize = new BindableSize3DIModel(0, 0, 0);
            Position = new BindablePoint3DModel(0, 0, 0);

            if (File.Exists(fileName))
            {
                // validate file is a real model.
                // read model properties.
                _originalBounds = Modelling.PreviewVolumetricModel(fileName, out Model3D model);

                if (!_originalBounds.IsEmpty && _originalBounds.SizeX != 0 && _originalBounds.SizeY != 0 && _originalBounds.SizeZ != 0)
                {
                    Model = model;
                    Transform3D rotateTransform = MeshHelper.TransformVector(new System.Windows.Media.Media3D.Vector3D(0, 0, 0), -RotateRoll, RotateYaw - 90, RotatePitch + 90);
                    Rect3D bounds = _originalBounds;
                    if (rotateTransform != null)
                    {
                        bounds = rotateTransform.TransformBounds(bounds);
                    }

                    OriginalModelSize = new BindableSize3DModel(bounds);
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
                Transform3D rotateTransform = MeshHelper.TransformVector(new System.Windows.Media.Media3D.Vector3D(0, 0, 0), -RotateRoll, RotateYaw - 90, RotatePitch + 90);
                Rect3D bounds = _originalBounds;
                if (rotateTransform != null)
                {
                    bounds = rotateTransform.TransformBounds(bounds);
                }

                BindableSize3DModel newSize = new(bounds);

                if (IsMaxLengthScale)
                {
                    double factor = MaxLengthScale / Math.Max(Math.Max(newSize.Height, newSize.Width), newSize.Depth);

                    NewModelSize.Height = (int)(factor * newSize.Height);
                    NewModelSize.Width = (int)(factor * newSize.Width);
                    NewModelSize.Depth = (int)(factor * newSize.Depth);
                }
                else if (IsMultipleScale)
                {
                    NewModelSize.Height = (int)(MultipleScale * newSize.Height);
                    NewModelSize.Width = (int)(MultipleScale * newSize.Width);
                    NewModelSize.Depth = (int)(MultipleScale * newSize.Depth);
                }

                NewModelScale = new BindablePoint3DModel(NewModelSize.Width, NewModelSize.Height, NewModelSize.Depth);
            }
        }

        #endregion

        #region BuildEntity

        private bool BuildEntity()
        {
            string FileNamePart = Path.GetFileNameWithoutExtension(FileName);
            string fileName = MainViewModel.CreateUniqueVoxelStorageName(FileNamePart + MyVoxelMapBase.FileExtension.V2);

            double multiplier;

            if (IsMultipleScale)
            {
                multiplier = MultipleScale;
            }
            else
            {
                multiplier = MaxLengthScale / Math.Max(Math.Max(OriginalModelSize.Height, OriginalModelSize.Width), OriginalModelSize.Depth);
            }

            Size3D scale = new(multiplier, multiplier, multiplier);
            Matrix3D rotateTransform = MeshHelper.TransformVector(new System.Windows.Media.Media3D.Vector3D(0, 0, 0), -RotateRoll, RotateYaw - 90, RotatePitch + 90).Value;

            SourceFile = TempFileUtil.NewFileName(MyVoxelMapBase.FileExtension.V2);

            Model3DGroup model = MeshHelper.Load(FileName, ignoreErrors: true);

            #region Dialog and Processing

            CancellationTokenSource cancelTokenSource = new();

            ProgressCancelModel progressModel = new()
            {
                Title = Res.WnProgressTitle,
                SubTitle = Res.WnProgressTitle,
                DialogText = Res.WnProgressTxtTimeRemain + " " + Res.WnProgressTxtTimeCalculating
            };

            ProgressCancelViewModel progressVm = new(this, progressModel);
            progressVm.CloseRequested += (sender, e) => cancelTokenSource.Cancel();

            void CompletedAction()
            {
                progressVm.Close();
            }

            Task<MyVoxelMapBase> voxelMapTask = null;

            void GenerateAsteroidAsync()
            {
                MyVoxelRayTracer.Model info = new(model, scale, rotateTransform, InsideStockMaterial.MaterialIndex ?? 0);

                voxelMapTask = Task.Factory.StartNew(() =>
                {
                    return MyVoxelRayTracer.GenerateVoxelMapFromModel(info, rotateTransform, TraceType, TraceCount, TraceDirection,
                        progressModel.ResetProgress, progressModel.IncrementProgress, CompletedAction, cancelTokenSource.Token);
                }, TaskCreationOptions.LongRunning);
            }

            if (RunInLowPrioity)
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;

            _dialogService.ShowDialog<WindowProgressCancel>(this, progressVm, GenerateAsteroidAsync);

            if (RunInLowPrioity)
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;

            #endregion

            MyVoxelMapBase voxelMap = voxelMapTask.IsCanceled ? null : voxelMapTask.Result;

            if (cancelTokenSource.IsCancellationRequested || voxelMap == null)
            {
                IsValidEntity = false;
                NewEntity = null;
            }
            else
            {
                voxelMap.ForceShellMaterial(OutsideStockMaterial.Value, (byte)OutsideMaterialDepth);
                voxelMap.Save(SourceFile);

                VRageMath.Vector3D position = VRageMath.Vector3D.Zero;
                Vector3 forward = Vector3.Forward;
                Vector3 up = Vector3.Up;

                if (IsAbsolutePosition)
                {
                    position = Position.ToVector3();
                }
                else if (IsInfrontofPlayer)
                {
                    // Figure out where the Character is facing, and plant the new construct centered in front of the Character, but "BuildDistance" units out in front.
                    VRageMath.Vector3D lookVector = (VRageMath.Vector3D)_dataModel.CharacterPosition.Forward.ToVector3();
                    lookVector.Normalize();

                    BoundingBoxD content = voxelMap.BoundingContent.ToBoundingBoxD();
                    VRageMath.Vector3D? boundingIntersectPoint = content.IntersectsRayAt(content.Center, -lookVector * 5000d);

                    if (!boundingIntersectPoint.HasValue)
                    {
                        boundingIntersectPoint = content.Center;
                    }

                    double distance = VRageMath.Vector3D.Distance(boundingIntersectPoint.Value, content.Center) + (float)BuildDistance;
                    VRageMath.Vector3D vector = lookVector * distance;
                    position = VRageMath.Vector3D.Add(_dataModel.CharacterPosition.Position, vector) - content.Center;
                }

                MyObjectBuilder_VoxelMap entity = new(position, fileName)
                {
                    EntityId = SpaceEngineersApi.GenerateEntityId(IDType.ASTEROID),
                    PersistentFlags = MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                    StorageName = Path.GetFileNameWithoutExtension(fileName),
                    PositionAndOrientation = new MyPositionAndOrientation
                    {
                        Position = position,
                        Forward = forward,
                        Up = up
                    }
                };

                IsValidEntity = voxelMap.BoundingContent.Size.Volume() > 0;
                NewEntity = entity;

                if (BeepWhenFinished)
                    System.Media.SystemSounds.Asterisk.Play();
            }

            return !cancelTokenSource.IsCancellationRequested;
        }

        #endregion
    }
}

