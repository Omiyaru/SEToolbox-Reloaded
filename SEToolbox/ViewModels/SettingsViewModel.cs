using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;

using SEToolbox.Interfaces;
using SEToolbox.Models;
using SEToolbox.Services;
using SEToolbox.Support;
using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        #region Fields

        private readonly SettingsModel _dataModel;
        private readonly IDialogService _dialogService;
        private readonly Func<IOpenFileDialog> _openFileDialogFactory;
        private readonly Func<IFolderBrowserDialog> _folderDialogFactory;
        private bool? _closeResult;
        private bool _isBusy;

        #endregion

        #region Constructors

        public SettingsViewModel(BaseViewModel parentViewModel, SettingsModel dataModel)
            : this(parentViewModel, dataModel, ServiceLocator.Resolve<IDialogService>(), ServiceLocator.Resolve<IOpenFileDialog>, ServiceLocator.Resolve<IFolderBrowserDialog>)
        {
        }

        public SettingsViewModel(BaseViewModel parentViewModel, SettingsModel dataModel, IDialogService dialogService, Func<IOpenFileDialog> openFileDialogFactory, Func<IFolderBrowserDialog> folderDialogFactory)
            : base(parentViewModel)
        {
            Contract.Requires(dialogService != null);
            Contract.Requires(openFileDialogFactory != null);

            _dialogService = dialogService;
            _openFileDialogFactory = openFileDialogFactory;
            _folderDialogFactory = folderDialogFactory;
            _dataModel = dataModel;
            // Will bubble property change events from the Model to the ViewModel.
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Command Properties

        public ICommand BrowseAppPathCommand
        {
            get => new DelegateCommand(BrowseAppPathExecuted, BrowseAppPathCanExecute);
        }


        public ICommand BrowseVoxelPathCommand
        {
            get => new DelegateCommand(BrowseVoxelPathExecuted, BrowseVoxelPathCanExecute);
        }

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

            set => SetProperty(ref _closeResult, value, nameof(CloseResult));
        }

        public string SEBinPath
        {
            get => _dataModel.SEBinPath;

            set => _dataModel.SEBinPath = value;
        }

        public string CustomVoxelPath
        {
            get => _dataModel.CustomVoxelPath;

            set => _dataModel.CustomVoxelPath = value;
        }

        public bool? AlwaysCheckForUpdates
        {
            get => _dataModel.AlwaysCheckForUpdates;

            set => _dataModel.AlwaysCheckForUpdates = value;
        }

        public bool? UseCustomResource
        {
            get => _dataModel.UseCustomResource;

            set => _dataModel.UseCustomResource = value;
        }

        public bool IsValid
        {
            get => _dataModel.IsValid;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;

            set
            {
                SetProperty(ref _isBusy,value, nameof(IsBusy));
                if (_isBusy)
                {
                    Application.DoEvents();
                }
            }
        }

        #endregion

        #region Command Methods

        public bool BrowseAppPathCanExecute()
        {
            return true;
        }

        public void BrowseAppPathExecuted()
        {
            string startPath = SEBinPath;
            if (string.IsNullOrEmpty(startPath))
            {
                startPath = ToolboxUpdater.GetSteamFilePath();
                if (!string.IsNullOrEmpty(startPath))
                {
                    startPath = Path.Combine(startPath, @"SteamApps\common");
                }
            }

            IOpenFileDialog openFileDialog = _openFileDialogFactory();
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.DefaultExt = "exe";
            openFileDialog.FileName = "SpaceEngineers";
            openFileDialog.Filter = AppConstants.SpaceEngineersApplicationFilter;
            openFileDialog.InitialDirectory = startPath;
            openFileDialog.Multiselect = false;
            openFileDialog.Title = Res.DialogLocateApplicationTitle;

            // Open the dialog
            if (_dialogService.ShowOpenFileDialog(this, openFileDialog) == DialogResult.OK)
            {
                string gameBinPath = openFileDialog.FileName;

                if (!string.IsNullOrEmpty(gameBinPath))
                {
                    try
                    {
                        string fullPath = Path.GetFullPath(gameBinPath);
                        if (File.Exists(fullPath))
                        {
                            gameBinPath = Path.GetDirectoryName(fullPath);
                        }
                    }
                    catch { }
                }

                SEBinPath = gameBinPath;
            }
        }

        public bool BrowseVoxelPathCanExecute()
        {
            return true;
        }

        public void BrowseVoxelPathExecuted()
        {
            IFolderBrowserDialog folderDialog = _folderDialogFactory();
            folderDialog.Description = Res.DialogLocationCustomVoxelFolder;
            folderDialog.SelectedPath = CustomVoxelPath;
            folderDialog.ShowNewFolderButton = true;

            // Open the dialog
            if (_dialogService.ShowFolderBrowserDialog(this, folderDialog) == DialogResult.OK)
            {
                CustomVoxelPath = folderDialog.SelectedPath;
            }
        }

        public bool OkayCanExecute()
        {
            return IsValid;
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
    }
}
