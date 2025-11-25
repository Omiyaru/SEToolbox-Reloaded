using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using Sandbox.Definitions;
using SEToolbox.Interfaces;
using SEToolbox.Models;
using SEToolbox.Services;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using ICommand = System.Windows.Input.ICommand;

namespace SEToolbox.ViewModels
{
    public class CubeItemViewModel : BaseViewModel
    {
        #region Fields

        private readonly IDialogService _dialogService;
        private readonly CubeItemModel _dataModel;
        private Lazy<ObservableCollection<InventoryEditorViewModel>> _inventory;

        #endregion

        #region Constructors

        public CubeItemViewModel(BaseViewModel parentViewModel, CubeItemModel dataModel)
            : this(parentViewModel, dataModel, ServiceLocator.Resolve<IDialogService>())
        {
        }

        public CubeItemViewModel(BaseViewModel parentViewModel, CubeItemModel dataModel, IDialogService dialogService)
            : base(parentViewModel)
        {
            Contract.Requires(dialogService != null);
            _dialogService = dialogService;
            _dataModel = dataModel;

            InventoryEditorViewModel viewModelCreator(InventoryEditorModel model) => new(this, model);
            ObservableCollection<InventoryEditorViewModel> collectionCreator() => new ObservableViewModelCollection<InventoryEditorViewModel, InventoryEditorModel>(dataModel.Inventory, viewModelCreator);
            _inventory = new Lazy<ObservableCollection<InventoryEditorViewModel>>(collectionCreator);

            _dataModel.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "Inventory")
                {
                    collectionCreator();
                    _inventory = new Lazy<ObservableCollection<InventoryEditorViewModel>>(collectionCreator);
                }
                // Will bubble property change events from the Model to the ViewModel.
                OnPropertyChanged(e.PropertyName);
            };
        }

        #endregion

        #region Command Properties
        public ICommand ApplyCommand
        {
            get => new DelegateCommand<object>(ApplyExecuted, ApplyCanExecute);
        }

        public ICommand CancelCommand
        {
            get => new DelegateCommand<object>(CancelExecuted, CancelCanExecute);
        }

        private void ApplyExecuted(object parameter)
        {
            if (_dataModel != null)
            // Save changes to the data model
            _dataModel.FriendlyName = FriendlyName;
            _dataModel.Owner = Owner;
            _dataModel.BuiltBy = BuiltBy;
            _dataModel.ColorHue = ColorHue;
            _dataModel.ColorSaturation = ColorSaturation;
            _dataModel.ColorLuminance = ColorLuminance;
            _dataModel.Position = Position;

            // Notify the user
            _dialogService.ShowMessageBox(this, "Changes applied successfully.", "Apply Changes",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private bool ApplyCanExecute(object parameter)
        { 
            return _dataModel != null && !string.IsNullOrEmpty(FriendlyName);
        } 
        // Ensure the Apply command can execute only if the data model is valid

        private void CancelExecuted(object parameter)
        {
            if (_dataModel != null)
            // Revert changes
            FriendlyName = _dataModel.FriendlyName;
            Owner = _dataModel.Owner;
            BuiltBy = _dataModel.BuiltBy;
            ColorHue = _dataModel.ColorHue;
            ColorSaturation = _dataModel.ColorSaturation;
            ColorLuminance = _dataModel.ColorLuminance;
            Position = _dataModel.Position;

            // Notify the user
            _dialogService.ShowMessageBox(this, "Changes have been reverted.", "Cancel Changes",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private bool CancelCanExecute(object parameter) 
        {
            return _dataModel != null;
        } 
        // Ensure the Cancel command can execute only if the data model is valid


        #endregion

        #region Properties

        public CubeItemModel DataModel
        {
            get => _dataModel;
        }

        public bool IsSelected
        {
            get => _dataModel.IsSelected;
            set => _dataModel.IsSelected = value;
        }

        public MyObjectBuilder_CubeBlock Cube
        {
            get => _dataModel.Cube;
            set => _dataModel.Cube = value;
        }

        public MyObjectBuilderType TypeId
        {
            get => _dataModel.TypeId;
            set => _dataModel.TypeId = value;
        }

        public string SubtypeId
        {
            get => _dataModel.SubtypeId;
            set => _dataModel.SubtypeId = value;
        }

        public string TextureFile
        {
            get => _dataModel.TextureFile;
            set => _dataModel.TextureFile = value;
        }

        public MyCubeSize CubeSize
        {
            get => _dataModel.CubeSize;
            set => _dataModel.CubeSize = value;
        }

        public string FriendlyName
        {
            get => _dataModel.FriendlyName;
            set => _dataModel.FriendlyName = value;
        }

        public string OwnerName
        {
            get => _dataModel.OwnerName;
            set => _dataModel.OwnerName = value;
        }

        public long Owner
        {
            get => _dataModel.Owner;
            set => _dataModel.Owner = value;
        }

        public string BuiltByName
        {
            get => _dataModel.BuiltByName;
            set => _dataModel.BuiltByName = value;
        }

        public long BuiltBy
        {
            get => _dataModel.BuiltBy;
            set => _dataModel.BuiltBy = value;
        }

        public string ColorText
        {
            get => _dataModel.ColorText;
            set => _dataModel.ColorText = value;
        }

        public float ColorHue
        {
            get => _dataModel.ColorHue;
            set => _dataModel.ColorHue = value;
        }

        public float ColorSaturation
        {
            get => _dataModel.ColorSaturation;
            set => _dataModel.ColorSaturation = value;
        }

        public float ColorLuminance
        {
            get => _dataModel.ColorLuminance;
            set => _dataModel.ColorLuminance = value;
        }

        public BindablePoint3DIModel Position
        {
            get => _dataModel.Position;
            set => _dataModel.Position = value;
        }

        public double BuildPercent
        {
            get => _dataModel.BuildPercent;
            set => _dataModel.BuildPercent = value;
        }

        public System.Windows.Media.Brush Color
        {
            get => _dataModel.Color;
            set => _dataModel.Color = value;
        }

        public int PCU
        {
            get => _dataModel.PCU;
            set => _dataModel.PCU = value;
        }

        public ObservableCollection<InventoryEditorViewModel> Inventory
        {
            get => _inventory.Value;
        }

        public override string ToString()
        {
            return FriendlyName;
        }

        #endregion

        #region Methods

        public void UpdateColor(SerializableVector3 vector3)
        {
            _dataModel.UpdateColor(vector3);
        }

        public void UpdateBuildPercent(double buildPercent)
        {
            _dataModel.UpdateBuildPercent(buildPercent);
        }

        public bool ConvertFromLightToHeavyArmor()
        {
            return CubeItemModel.ConvertFromLightToHeavyArmor(_dataModel.Cube);
        }

        public bool ConvertFromHeavyToLightArmor()
        {
            return CubeItemModel.ConvertFromHeavyToLightArmor(_dataModel.Cube);
        }

        public MyObjectBuilder_CubeBlock CreateCube(MyObjectBuilderType typeId, string subTypeId, MyCubeBlockDefinition definition)
        {
            return _dataModel.CreateCube(typeId, subTypeId, definition);
        }

        public bool ChangeOwner(long newOwnerId)
        {
            return _dataModel.ChangeOwner(newOwnerId);
        }

        public bool ChangeBuiltBy(long newBuiltById)
        {
            return _dataModel.ChangeBuiltBy(newBuiltById);
        }

        #endregion
    }
}