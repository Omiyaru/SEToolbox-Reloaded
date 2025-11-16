using System;
using SEToolbox.Models;
using SEToolbox.Services;
using System.Windows.Forms;
using System.Windows.Input;

namespace SEToolbox.ViewModels
{
    public class ErrorDialogViewModel : BaseViewModel
    {
        #region Fields

        private readonly ErrorDialogModel _dataModel;
        private bool? _closeResult;
        private bool _isBusy;

        #endregion

        #region Ctor

        public ErrorDialogViewModel(BaseViewModel parentViewModel, ErrorDialogModel dataModel)
            : base(parentViewModel)
        {

            _dataModel = dataModel;
            // Will bubble property change events from the Model to the ViewModel.
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Command Properties

        public ICommand CopyCommand => new DelegateCommand(CopyExecuted, CopyCanExecute);

        public ICommand OkayCommand => new DelegateCommand(OkayExecuted, OkayCanExecute);

        public ICommand CancelCommand => new DelegateCommand(CancelExecuted, CancelCanExecute);

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

            set => SetProperty(ref _isBusy ,value, nameof(IsBusy),() => {
                if(_isBusy)
                {
                    Application.DoEvents();
                }
            });
        }
       
        
                




        public string ErrorDescription
        {
            get => _dataModel.ErrorDescription;
            set => _dataModel.ErrorDescription = value;
        }

        public string ErrorText
        {
            get => _dataModel.ErrorText;
            set => _dataModel.ErrorText = value;
        }

        public bool IsWarning
        {
            get => _dataModel.CanContinue;
            set => _dataModel.CanContinue = value;
        }

        public bool IsError
        {
            get => !_dataModel.CanContinue;
            set => _dataModel.CanContinue = !value;
        }

        #endregion

        #region Methods

        #region Commands

        public bool CopyCanExecute()
        {
            return true;
        }

        public void CopyExecuted()
        {
            Clipboard.SetText(_dataModel.ErrorDescription + Environment.NewLine + _dataModel.ErrorText);
        }

        public bool OkayCanExecute()
        {
            return true;
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
