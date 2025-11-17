using System;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Windows.Forms;
using System.Windows.Input;

using SEToolbox.Interfaces;
using SEToolbox.Models;
using SEToolbox.Services;
using SEToolbox.Support;
using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.ViewModels
{
    public class ComponentListViewModel : BaseViewModel
    {
        #region Fields

        private readonly IDialogService _dialogService;
        private readonly Func<ISaveFileDialog> _saveFileDialogFactory;
        private readonly ComponentListModel _dataModel;
        private bool? _closeResult;

        #endregion

        #region Constructors

        public ComponentListViewModel(BaseViewModel parentViewModel, ComponentListModel dataModel)
            : this(parentViewModel, dataModel, ServiceLocator.Resolve<IDialogService>(), ServiceLocator.Resolve<ISaveFileDialog>)
        {
        }

        public ComponentListViewModel(BaseViewModel parentViewModel, ComponentListModel dataModel, IDialogService dialogService, Func<ISaveFileDialog> saveFileDialogFactory)
            : base(parentViewModel)
        {
            Contract.Requires(dialogService != null);
            Contract.Requires(saveFileDialogFactory != null);

            _dialogService = dialogService;
            _saveFileDialogFactory = saveFileDialogFactory;
            _dataModel = dataModel;
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Command Properties

        public ICommand ExportReportCommand
        {
            get =>new DelegateCommand(ExportReportExecuted, ExportReportCanExecute); 
        }

        public ICommand CloseCommand
        {
            get => new DelegateCommand(CloseExecuted, CloseCanExecute);
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

        public ObservableCollection<ComponentItemModel> CubeAssets
        {
            get => _dataModel.CubeAssets;

            set => _dataModel.CubeAssets = value;
        }

        public ObservableCollection<ComponentItemModel> ComponentAssets
        {
            get => _dataModel.ComponentAssets;

            set => _dataModel.ComponentAssets = value;
        }

        public ObservableCollection<ComponentItemModel> ItemAssets
        {
            get => _dataModel.ItemAssets;

            set => _dataModel.ItemAssets = value;
        }

        public ObservableCollection<ComponentItemModel> MaterialAssets
        {
            get => _dataModel.MaterialAssets;

            set => _dataModel.MaterialAssets = value;
        }

        public ComponentItemModel SelectedCubeAsset
        {
            get => _dataModel.SelectedCubeAsset;

            set => _dataModel.SelectedCubeAsset = value;
        }

        #endregion

        #region Methods

        public bool ExportReportCanExecute()
        {
            return true;
        }

        public void ExportReportExecuted()
        {
            ISaveFileDialog saveFileDialog = _saveFileDialogFactory();
            saveFileDialog.Filter = AppConstants.HtmlFilter;
            saveFileDialog.Title = Res.DialogExportReportTitle;
            saveFileDialog.FileName = Res.DialogExportReportFileName;
            saveFileDialog.OverwritePrompt = true;

            // Open the dialog
            DialogResult result = _dialogService.ShowSaveFileDialog(this, saveFileDialog);

            if (result == DialogResult.OK)
            {
                _dataModel.GenerateHtmlReport(saveFileDialog.FileName);
            }
        }

        public bool CloseCanExecute()
        {
            return true;
        }

        public void CloseExecuted()
        {
            CloseResult = false;
        }

        #endregion
    }
}
