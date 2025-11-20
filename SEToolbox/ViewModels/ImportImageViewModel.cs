using System;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using SEToolbox.Interfaces;
using SEToolbox.Interop;
using SEToolbox.Models;
using SEToolbox.Services;
using SEToolbox.Support;

using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using IDType = VRage.MyEntityIdentifier.ID_OBJECT_TYPE;
using Res = SEToolbox.Properties.Resources;
using System.Linq;
using ImageHelper = SEToolbox.ImageLibrary.ImageHelper;

namespace SEToolbox.ViewModels
{
    public class ImportImageViewModel : BaseViewModel
    {
        #region Fields

        private readonly IDialogService _dialogService;
        private readonly Func<IOpenFileDialog> _openFileDialogFactory;
        private readonly ImportImageModel _dataModel;
        private readonly Func<IColorDialog> _colorDialogFactory;

        private bool? _closeResult;
        private Image _sourceImage;
        private BitmapImage _newImage;
        private bool _isBusy;

        #endregion

        #region Constructors

        public ImportImageViewModel(BaseViewModel parentViewModel, ImportImageModel dataModel)
            : this(parentViewModel, dataModel, ServiceLocator.Resolve<IDialogService>(), ServiceLocator.Resolve<IOpenFileDialog>, ServiceLocator.Resolve<IColorDialog>)
        {
        }

        public ImportImageViewModel(BaseViewModel parentViewModel, ImportImageModel dataModel, IDialogService dialogService, Func<IOpenFileDialog> openFileDialogFactory, Func<IColorDialog> colorDialogFactory)
            : base(parentViewModel)
        {
            Contract.Requires(dialogService != null);
            Contract.Requires(openFileDialogFactory != null);
            Contract.Requires(colorDialogFactory != null);

            _dialogService = dialogService;
            _openFileDialogFactory = openFileDialogFactory;
            _colorDialogFactory = colorDialogFactory;
            _dataModel = dataModel;
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Command Properties

        public ICommand BrowseImageCommand
        {
           get => new DelegateCommand(BrowseImageExecuted, BrowseImageCanExecute);
        }

        public ICommand SetToOriginalSizeCommand
        {
           get => new DelegateCommand(SetToOriginalSizeExecuted, SetToOriginalSizeCanExecute); 
        }

        public ICommand CreateCommand
        {
           get => new DelegateCommand(CreateExecuted, CreateCanExecute); 
        }

        public ICommand CancelCommand
        {
           get => new DelegateCommand(CancelExecuted, CancelCanExecute);
        }

        public ICommand ChangeKeyColorCommand
        {
           get => new DelegateCommand(ChangeKeyColorExecuted, ChangeKeyColorCanExecute); 
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the DialogResult of the View.  If True or False is passed, this initiates the Close().
        /// </summary>
        public bool? CloseResult
        {
            get => _closeResult;
            set => SetProperty(ref _closeResult, nameof(CloseResult)); 
        }

        public string FileName
        {
            get => _dataModel.FileName;

            set
            {
                _dataModel.FileName = value;
                FileNameChanged();
            }
        }

        public bool IsValidImage
        {
            get => _dataModel.IsValidImage;
            set => _dataModel.IsValidImage = value;
        }

        public Size OriginalImageSize
        {
            get => _dataModel.OriginalImageSize;
            set => _dataModel.OriginalImageSize = value;
        }

        public BindableSizeModel NewImageSize
        {
            get => _dataModel.NewImageSize;
            set => SetProperty( _dataModel.NewImageSize, nameof(NewImageSize), () => ProcessImage());
        }
            

        public BindablePoint3DModel Position
        {
            get => _dataModel.Position;
            set => _dataModel.Position = value;
        }

        public BindableVector3DModel Forward
        {
            get => _dataModel.Forward; set => _dataModel.Forward = value;
        }

        public BindableVector3DModel Up
        {
            get => _dataModel.Up;
            set => _dataModel.Up = value;
        }

        public ImportImageClassType ClassType
        {
            get => _dataModel.ClassType;
            set => _dataModel.ClassType = value;
        }

        public ImportArmorType ArmorType
        {
            get => _dataModel.ArmorType; set => _dataModel.ArmorType = value;
        }

        public BitmapImage NewImage
        {
            get => _newImage;
            set => SetProperty(ref _newImage, nameof(NewImage));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, nameof(IsBusy), () =>
            {
                 if(_isBusy)
                 {
                    Application.DoEvents();
                 }
            });
        }

        public int AlphaLevel
        {
            get => _dataModel.AlphaLevel;
            set => _dataModel.AlphaLevel = value;
        }

        public System.Windows.Media.Color KeyColor
        {
            get => _dataModel.KeyColor;
            set => _dataModel.KeyColor = value;
        }

        public bool IsAlphaLevel
        {
            get => _dataModel.IsAlphaLevel;
            set => _dataModel.IsAlphaLevel = value;
        }

        public bool IsKeyColor
        {
            get => _dataModel.IsKeyColor;
            set => _dataModel.IsKeyColor = value;
        }

        #endregion

        #region Methods

        public bool BrowseImageCanExecute()
        {
            return true;
        }

        public void BrowseImageExecuted()
        {
            IsValidImage = false;

            IOpenFileDialog openFileDialog = _openFileDialogFactory();
            openFileDialog.Filter = AppConstants.ImageFilter;
            openFileDialog.Title = Res.DialogImportImageTitle;

            // Open the dialog
            DialogResult result = _dialogService.ShowOpenFileDialog(this, openFileDialog);

            if (result == DialogResult.OK)
            {
                FileName = openFileDialog.FileName;
            }
        }

        private void FileNameChanged()
        {
            ProcessFileName(FileName);
        }

        public bool SetToOriginalSizeCanExecute()
        {
            return IsValidImage;
        }

        public void SetToOriginalSizeExecuted()
        {
            NewImageSize.Height = _sourceImage.Height;
            NewImageSize.Width = _sourceImage.Width;
        }

        public bool CreateCanExecute()
        {
            return IsValidImage;
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

        public bool ChangeKeyColorCanExecute()
        {
            return true;
        }

        public void ChangeKeyColorExecuted()
        {
            IColorDialog colorDialog = _colorDialogFactory();
            colorDialog.FullOpen = true;
            colorDialog.MediaColor = KeyColor;
            colorDialog.CustomColors = MainViewModel.CreativeModeColors;

            if (_dialogService.ShowColorDialog(OwnerViewModel, colorDialog) == System.Windows.Forms.DialogResult.OK)
            {
                KeyColor = colorDialog.MediaColor.Value;
            }

            MainViewModel.CreativeModeColors = colorDialog.CustomColors;
        }

        #endregion

        #region Methods

        private void ProcessFileName(string fileName)
        {
            IsBusy = true;

            if (File.Exists(fileName))
            {
                // Validate file is a real image

                //  Read image properties
                if (IsImageFile(fileName) && _sourceImage != null)
                {
                    _sourceImage.Dispose();

                    _sourceImage = Image.FromFile(fileName);
                    OriginalImageSize = new Size(_sourceImage.Width, _sourceImage.Height);

                    NewImageSize = new BindableSizeModel(_sourceImage.Width, _sourceImage.Height);
                    NewImageSize.PropertyChanged += (sender, e) => ProcessImage();

                    // Figure out where the Character is facing, and plant the new construct right in front, by "10" units, facing the Character.
                    Vector3D vector = new BindableVector3DModel(_dataModel.CharacterPosition.Forward).Vector3D;
                    vector.Normalize();
                    vector = Vector3D.Multiply(vector, 10);
                    Position = new BindablePoint3DModel(Point3D.Add(new BindablePoint3DModel(_dataModel.CharacterPosition.Position).Point3D, vector));
                    Forward = new BindableVector3DModel(_dataModel.CharacterPosition.Forward).Negate();
                    Up = new BindableVector3DModel(_dataModel.CharacterPosition.Up);

                    ClassType = ImportImageClassType.SmallShip;
                    ArmorType = ImportArmorType.Light;

                    IsValidImage = true;
                }
                else
                {
                    IsValidImage = false;
                    Position = new BindablePoint3DModel(0, 0, 0);
                    OriginalImageSize = new Size(0, 0);
                    NewImageSize = new BindableSizeModel(0, 0);
                }
            }
            else
            {
                IsValidImage = false;
                Position = new BindablePoint3DModel(0, 0, 0);
                OriginalImageSize = new Size(0, 0);
                NewImageSize = new BindableSizeModel(0, 0);
            }

            IsBusy = false;
        }

        private static bool IsImageFile(string fileName)
        {
            string[] validExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif"];// ".dds",
            string fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
            return validExtensions.Contains(fileExtension);
        }


        private void ProcessImage()
        {
            if (_sourceImage != null)
            {
                Bitmap image = ImageHelper.ResizeImage(_sourceImage, NewImageSize.Size);

                if (image != null)
                {
                    NewImage = ImageHelper.ConvertBitmapToBitmapImage(image);

                    //ImageHelper.SavePng(@"C:\temp\test.png", image);
                }
                else
                {
                    NewImage = null;
                }
            }
            else
            {
                NewImage = null;
            }
        }

        public MyObjectBuilder_CubeGrid BuildEntity()
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
                case ImportImageClassType.SmallShip:
                    entity.GridSizeEnum = MyCubeSize.Small;
                    blockPrefix += "Small";
                    entity.IsStatic = false;
                    break;

                case ImportImageClassType.SmallStation:
                    entity.GridSizeEnum = MyCubeSize.Small;
                    blockPrefix += "Small";
                    entity.IsStatic = true;
                    Position = Position.RoundOff(MyCubeSize.Small.ToLength());
                    Forward = Forward.RoundToAxis();
                    Up = Up.RoundToAxis();
                    break;

                case ImportImageClassType.LargeShip:
                    entity.GridSizeEnum = MyCubeSize.Large;
                    blockPrefix += "Large";
                    entity.IsStatic = false;
                    break;

                case ImportImageClassType.LargeStation:
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
                case ImportArmorType.Light: blockPrefix += "BlockArmor"; break;
                case ImportArmorType.Round: blockPrefix += "ArmorRound"; break;
                case ImportArmorType.Corner: blockPrefix += "Corner"; break;
                case ImportArmorType.Angled: blockPrefix += "Angled"; break;
                case ImportArmorType.Slope: blockPrefix += "Slope"; break;


            }

            entity.PositionAndOrientation = new MyPositionAndOrientation
            {
                // Reposition based on scale
                Position = Position?.ToVector3D() * entity.GridSizeEnum.ToLength() ?? new VRageMath.Vector3D(0, 0, 0), // Default to origin if null
                Forward = Forward?.ToVector3() ?? new VRageMath.Vector3(0, 0, 1), // Default to forward vector if null
                Up = Up?.ToVector3() ?? new VRageMath.Vector3(0, 1, 0) // Default to up vector if null
            };
            //see: Import3DModelViewModel for referenceand subtypeids

            // Large|BlockArmor|Corner
            // Large|RoundArmor_|Corner
            // Large|HeavyBlockArmor|Block,
            // Small|BlockArmor|Slope,
            // Small|HeavyBlockArmor|Corner,

            entity.CubeBlocks = [];
            Bitmap image = ImageHelper.ResizeImage(_sourceImage, NewImageSize.Size);

            using (Bitmap palatteImage = new(image))
            {
                // Optimal order load. from grid coordinate (0,0,0) and up.
                for (int x = palatteImage.Width - 1; x >= 0; x--)
                {
                    for (int y = palatteImage.Height - 1; y >= 0; y--)
                    {
                        const int z = 0;
                        Color color = palatteImage.GetPixel(x, y);

                        // Specifically ignore anything with less than half "Transparent" Alpha.
                        if (IsAlphaLevel && color.A < AlphaLevel)
                            continue;

                        if (IsKeyColor && color.R == KeyColor.R && color.G == KeyColor.G && color.B == KeyColor.B)
                            continue;

                        // Parse the string through the Enumeration to check that the 'subtypeid' is still valid in the game engine.
                        string armorString = blockPrefix + "Block";
                        if (Enum.IsDefined(typeof(SubtypeId), armorString))
                        {
                            SubtypeId armor = (SubtypeId)Enum.Parse(typeof(SubtypeId), armorString);
                            MyObjectBuilder_CubeBlock newCube;
                            entity.CubeBlocks.Add(newCube = new MyObjectBuilder_CubeBlock());
                            newCube.SubtypeName = armor.ToString();
                            newCube.EntityId = 0;
                            newCube.BlockOrientation = Modelling.GetCubeOrientation(CubeType.Cube);
                            newCube.Min = new VRageMath.Vector3I(palatteImage.Width - x - 1, palatteImage.Height - y - 1, z);
                            newCube.ColorMaskHSV = color.FromPaletteColorToHsvMask();
                        }

                    }
                }
            }

            return entity;
        }

        #endregion
    }
}
