using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

using SEToolbox.Models;
using SEToolbox.Services;

namespace SEToolbox.ViewModels
{
    public class SelectCubeViewModel : BaseViewModel
    {
        #region Fields

        private readonly SelectCubeModel _dataModel;
        private bool? _closeResult;
        private bool _isBusy;

        #endregion

        #region Ctor


        public SelectCubeViewModel(BaseViewModel parentViewModel, SelectCubeModel dataModel)
            : base(parentViewModel)
        {

            _dataModel = dataModel;
            // Will bubble property change events from the Model to the ViewModel.
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Command Properties

        public ICommand OkayCommand
        {
            get => new DelegateCommand(OkayExecuted, OkayCanExecute);
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
            set => SetProperty(ref _closeResult, nameof(CloseResult));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
             get => _isBusy;

            set
            {
                    SetProperty(ref _isBusy, nameof(IsBusy));
                    if (_isBusy)
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }
            }
        }

        public ObservableCollection<ComponentItemModel> CubeList
        {
            get => _dataModel.CubeList;
        }

        public ComponentItemModel CubeItem
        {
            get => _dataModel.CubeItem;
            set => _dataModel.CubeItem = value;
        }

        #endregion

        #region Methods

        #region Commands

        public bool OkayCanExecute()
        {
            return CubeItem != null;
        }

        public void OkayExecuted()
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
