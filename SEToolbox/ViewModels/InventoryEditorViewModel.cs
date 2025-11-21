using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Windows.Input;
using SEToolbox.Interfaces;
using SEToolbox.Interop;
using SEToolbox.Models;
using SEToolbox.Services;
using SEToolbox.Views;
using VRage;
using VRage.Game;
using VRageMath;

namespace SEToolbox.ViewModels
{
    public class InventoryEditorViewModel : BaseViewModel
    {
        #region Fields

        private readonly IDialogService _dialogService;
        private readonly InventoryEditorModel _dataModel;
        private ObservableCollection<InventoryModel> _selections;

        #endregion

        #region Constructors

        public InventoryEditorViewModel(BaseViewModel parentViewModel, InventoryEditorModel dataModel)
            : this(parentViewModel, dataModel, ServiceLocator.Resolve<IDialogService>())
        {
        }

        public InventoryEditorViewModel(BaseViewModel parentViewModel, InventoryEditorModel dataModel, IDialogService dialogService)
            : base(parentViewModel)
        {
            Contract.Requires(dialogService != null);

            _dialogService = dialogService;
            _dataModel = dataModel;
            Selections = [];
            // Will bubble property change events from the Model to the ViewModel.
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Command Properties

        public ICommand AddItemCommand
        {
            get => new DelegateCommand(AddItemExecuted, AddItemCanExecute);
        }

        public ICommand DeleteItemCommand
        {
            get => new DelegateCommand(DeleteItemExecuted, DeleteItemCanExecute);
        }

        #endregion

        #region Properties

        public ObservableCollection<InventoryModel> Selections
        {
            get => _selections;
            set => SetProperty(ref _selections, nameof(Selections));
        }

        public ObservableCollection<InventoryModel> Items
        {
            get => _dataModel.Items;
            set => _dataModel.Items = value;
        }

        public InventoryModel SelectedRow
        {
            get => _dataModel.SelectedRow;
            set => _dataModel.SelectedRow = value;
        }

        public double TotalVolume
        {
            get => _dataModel.TotalVolume;
            set => _dataModel.TotalVolume = value;
        }

        public float MaxVolume
        {
            get => _dataModel.MaxVolume;
            set => _dataModel.MaxVolume = value;
        }

        public string Name
        {
            get => _dataModel.Name;
            set => _dataModel.Name = value;
        }

        public bool IsValid
        {
            get => _dataModel.IsValid;
            set => _dataModel.IsValid = value;
        }

        #endregion

        #region Command Methods

        public bool AddItemCanExecute()
        {
            return _dataModel.IsValid;
        }

        public void AddItemExecuted()
        {
            GenerateFloatingObjectModel model = new();
            MyPositionAndOrientation position = new(Vector3D.Zero, Vector3.Forward, Vector3.Up);
            MyObjectBuilder_SessionSettings settings = SpaceEngineersCore.WorldResource.Checkpoint.Settings;

            model.Load(position, settings.MaxFloatingObjects);
            var loadVm = new GenerateFloatingObjectViewModel(this, model);
            var result = _dialogService.ShowDialog<WindowGenerateFloatingObject>(this, loadVm);
            if (result == true)
            {
                var newEntities = loadVm.BuildEntities();
                if (loadVm.IsValidItemToImport)
                {
                    for (int i = 0; i < newEntities.Length; i++)
                    {
                        var item = ((MyObjectBuilder_FloatingObject)newEntities[i]).Item;
                        _dataModel.Additem(item);
                    }

                    // Bubble change up to MainViewModel.IsModified = true;
                    SetIsModifiedOnMainViewModel();
                }
            }
        }

        public bool DeleteItemCanExecute()
        {
            return SelectedRow != null;
        }

        public void DeleteItemExecuted()
        {
            int index = Items.IndexOf(SelectedRow);
            _dataModel.RemoveItem(index);
            // Bubble change up to MainViewModel.IsModified = true;
            SetIsModifiedOnMainViewModel();
        }

        /// <summary>
        /// Sets IsModified = true on the MainViewModel if available in the parent chain.
        /// </summary>
        private void SetIsModifiedOnMainViewModel()
        {
            BaseViewModel current = this;
            while (current != null)
            {
                if (current.GetType().Name == "MainViewModel")
                {
                    System.Reflection.PropertyInfo prop = current.GetType().GetProperty("IsModified");
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(current, true);
                    }
                    break;
                }
                current = current.OwnerViewModel;
            }
        }

        #endregion
    }
}
