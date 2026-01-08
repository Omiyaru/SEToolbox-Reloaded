using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Windows.Input;

using SEToolbox.Interfaces;
using SEToolbox.Models;
using SEToolbox.Services;


using VRageMath;

namespace SEToolbox.ViewModels
{
    public class GroupMoveViewModel : BaseViewModel
    {
        #region Fields

        private readonly IDialogService _dialogService;
        private readonly GroupMoveModel _dataModel;
        private bool? _closeResult;

        #endregion

        #region Constructors

        public GroupMoveViewModel(BaseViewModel parentViewModel, GroupMoveModel dataModel)
            : this(parentViewModel, dataModel, ServiceLocator.Resolve<IDialogService>())
        {
        }

        public GroupMoveViewModel(BaseViewModel parentViewModel, GroupMoveModel dataModel, IDialogService dialogService)
            : base(parentViewModel)
        {
            Contract.Requires(dialogService != null);
            _dialogService = dialogService;
            _dataModel = dataModel;

            // Will bubble property change events from the Model to the ViewModel.
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Command Properties

        public ICommand ApplyCommand
        {
            get => new DelegateCommand(ApplyExecuted, ApplyCanExecute);
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
            get => _dataModel.IsBusy;
            set => _dataModel.IsBusy = value;
        }

        public float GlobalOffsetPositionX
        {
            get => _dataModel.GlobalOffsetPositionX;
            set => SetValue(_dataModel.GlobalOffsetPositionX, value, () =>
                 _dataModel.CalcOffsetDistances());
        }

        public float GlobalOffsetPositionY
        {
            get => _dataModel.GlobalOffsetPositionY;

            set => SetValue(_dataModel.GlobalOffsetPositionY, value, () =>
                 _dataModel.CalcOffsetDistances());
        }

        public float GlobalOffsetPositionZ
        {
            get => _dataModel.GlobalOffsetPositionZ;
            set => SetValue(_dataModel.GlobalOffsetPositionZ, value, () =>
                 _dataModel.CalcOffsetDistances());
        }

        public bool IsGlobalOffsetPosition
        {
            get => _dataModel.IsGlobalOffsetPosition;
            set => SetValue(_dataModel.IsGlobalOffsetPosition, value, () =>
                  _dataModel.CalcOffsetDistances());
        }

        public float SinglePositionX
        {
            get => _dataModel.SinglePositionX;
            set => SetValue(_dataModel.SinglePositionX, value, () =>
                  _dataModel.CalcOffsetDistances());
        }

        public float SinglePositionY
        {
            get => _dataModel.SinglePositionY;
            set => SetValue(_dataModel.SinglePositionY, value, () =>
                    _dataModel.CalcOffsetDistances());
        }

        public float SinglePositionZ
        {
            get => _dataModel.SinglePositionZ;
            set => SetValue(_dataModel.SinglePositionZ, value, () =>
                   _dataModel.CalcOffsetDistances());
        }

        public bool IsSinglePosition
        {
            get => _dataModel.IsSinglePosition;
            set => SetValue(_dataModel.IsSinglePosition, value, () =>
                   _dataModel.CalcOffsetDistances());
        }

        public bool IsRelativePosition
        {
            get => _dataModel.IsRelativePosition;
            set => SetValue(_dataModel.IsRelativePosition, value, () =>
            {
                _dataModel.CalculateGroupCenter(_dataModel.CenterPosition);
                _dataModel.CalcOffsetDistances();
            });
        }

        public ObservableCollection<GroupMoveItemModel> Selections
        {
            get => _dataModel.Selections;
            set => _dataModel.Selections = value;
        }

        public Vector3D CenterPosition
        {
            get => _dataModel.CenterPosition;
            set => _dataModel.CenterPosition = value;
        }
        #endregion

        #region Methods

        public bool ApplyCanExecute()
        {
            return IsSinglePosition || IsRelativePosition ||
                  (IsGlobalOffsetPosition && (GlobalOffsetPositionX != 0 || GlobalOffsetPositionY != 0 || GlobalOffsetPositionZ != 0));
        }

        public void ApplyExecuted()
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
    }
}
