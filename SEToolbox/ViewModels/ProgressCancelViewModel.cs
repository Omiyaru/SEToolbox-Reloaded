using System;
using System.ComponentModel;
using System.Windows.Input;

using SEToolbox.Models;
using SEToolbox.Services;

namespace SEToolbox.ViewModels
{
    public class ProgressCancelViewModel : BaseViewModel
    {
        #region Fields

        private readonly ProgressCancelModel _dataModel;
        private bool? _closeResult;

        #endregion

        #region Event Handlers

        public event EventHandler CloseRequested;

        #endregion

        #region Constructors

        public ProgressCancelViewModel(BaseViewModel parentViewModel, ProgressCancelModel dataModel)
            : base(parentViewModel)
        {
            _dataModel = dataModel;

            // Will bubble property change events from the Model to the ViewModel.
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Command Properties

        public ICommand ClosingCommand
        {
           get => new DelegateCommand<CancelEventArgs>(ClosingExecuted, ClosingCanExecute); 
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

        public string Title
        {
            get => _dataModel.Title;
            set => _dataModel.Title = value;
        }
        public string SubTitle
        {
            get => _dataModel.SubTitle;
            set => _dataModel.SubTitle = value;
        }

        public string DialogText
        {
            get => _dataModel.DialogText;
            set => _dataModel.DialogText = value;
        }

        public double Progress
        {
            get => _dataModel.Progress;
            set => _dataModel.Progress = value;
        }

        public double MaximumProgress
        {
            get => _dataModel.MaximumProgress;
            set => _dataModel.MaximumProgress = value;
        }

        public TimeSpan? EstimatedTimeLeft
        {
            get => _dataModel.EstimatedTimeLeft;
            set => _dataModel.EstimatedTimeLeft = value;
        }

        #endregion

        #region Command Methods

        public static bool ClosingCanExecute(CancelEventArgs e)
        {
            return true;
        }

        public void ClosingExecuted(CancelEventArgs e)
        {
            if (CloseResult == null)
            {
                CloseRequested?.Invoke(this, EventArgs.Empty);

                CloseResult = false;
            }

            _dataModel.ClearProgress();
        }

        public bool CancelCanExecute()
        {
            return true;
        }

        public void CancelExecuted()
        {

            CloseRequested?.Invoke(this, EventArgs.Empty);

            CloseResult = false;
        }

        #endregion

        public void Close()
        {
            CloseResult = true;
        }
    }
}