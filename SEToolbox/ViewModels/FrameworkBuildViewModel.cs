using System.Windows.Input;

using SEToolbox.Models;
using SEToolbox.Services;

namespace SEToolbox.ViewModels
{
    public class FrameworkBuildViewModel : BaseViewModel
    {
        #region Fields

        private readonly FrameworkBuildModel _dataModel;
        private bool? _closeResult;
        private bool _isBusy;

        #endregion

        #region Ctor

        public FrameworkBuildViewModel(BaseViewModel parentViewModel, FrameworkBuildModel dataModel)
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
          get  => new DelegateCommand(CancelExecuted, CancelCanExecute);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the DialogResult of the View.  If True or False is passed, this initiates the Close().
        /// </summary>
        public bool? CloseResult
        {
            get => _closeResult;
            set => SetProperty( ref _closeResult, value, nameof(CloseResult));
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

        public double? BuildPercent
        {
            get => _dataModel.BuildPercent;
            set => _dataModel.BuildPercent = value;
        }

        #endregion

        #region Methods

        #region Commands

        public bool OkayCanExecute()
        {
            return BuildPercent.HasValue;
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
