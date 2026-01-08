using System.Collections.ObjectModel;
using System.Windows.Input;

using SEToolbox.Models;
using SEToolbox.Services;

namespace SEToolbox.ViewModels
{
    public class ChangeOwnerViewModel : BaseViewModel
    {
        #region Fields

        private readonly ChangeOwnerModel _dataModel;
        private bool? _closeResult;
        private bool _isBusy;

        #endregion

        #region Ctor

        public ChangeOwnerViewModel(BaseViewModel parentViewModel, ChangeOwnerModel dataModel)
            : base(parentViewModel)
        {

            _dataModel = dataModel;
            // Will bubble property change events from the Model to the ViewModel.
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Command Properties

        public ICommand ChangeCommand
        {
            get => new DelegateCommand(ChangeExecuted, ChangeCanExecute);
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
            set => SetValue(ref _closeResult, value, nameof(CloseResult));
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
                    System.Windows.Forms.Application.DoEvents();
                }
            });
        }

        public ObservableCollection<OwnerModel> PlayerList
        {
            get => _dataModel.PlayerList;
        }

        public OwnerModel SelectedPlayer
        {
            get => _dataModel.SelectedPlayer;
            set => _dataModel.SelectedPlayer = value;
        }

        public string Title
        {
            get => _dataModel.Title;
        }

        #endregion

        #region Methods

        #region Commands

        public bool ChangeCanExecute()
        {
            return SelectedPlayer != null;
        }

        public void ChangeExecuted()
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

        #endregion
    }
}
