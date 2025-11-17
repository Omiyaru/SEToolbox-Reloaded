using System;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Media;
using System.Windows.Forms;
using System.Windows.Input;

using SEToolbox.Interfaces;
using SEToolbox.Interop;
using SEToolbox.Models;
using SEToolbox.Services;
using SEToolbox.Support;
using System.Diagnostics;

using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.ViewModels
{
    public class SelectWorldViewModel : BaseViewModel
    {
        #region Fields

        private readonly IDialogService _dialogService;
        private readonly Func<IOpenFileDialog> _openFileDialogFactory;
        private readonly SelectWorldModel _dataModel;
        private bool? _closeResult;
        private bool _zoomThumbnail;

        #endregion

        #region Constructors

        public SelectWorldViewModel(BaseViewModel parentViewModel, SelectWorldModel dataModel)
            : this(parentViewModel, dataModel, ServiceLocator.Resolve<IDialogService>(), ServiceLocator.Resolve<IOpenFileDialog>)
        {
        }

        public SelectWorldViewModel(BaseViewModel parentViewModel, SelectWorldModel dataModel, IDialogService dialogService, Func<IOpenFileDialog> openFileDialogFactory)
            : base(parentViewModel)
        {
            Contract.Requires(dialogService != null);
            Contract.Requires(openFileDialogFactory != null);
            _dialogService = dialogService;
            _openFileDialogFactory = openFileDialogFactory;
            _dataModel = dataModel;
            // Will bubble property change events from the Model to the ViewModel.
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Command Properties

        public ICommand LoadCommand => new DelegateCommand(LoadExecuted, LoadCanExecute);

        public ICommand RefreshCommand => new DelegateCommand(RefreshExecuted, RefreshCanExecute);

        public ICommand CancelCommand => new DelegateCommand(CancelExecuted, CancelCanExecute);

        public ICommand RepairCommand => new DelegateCommand(RepairExecuted, RepairCanExecute);

        public ICommand BrowseCommand => new DelegateCommand(BrowseExecuted, BrowseCanExecute);

        public ICommand OpenFolderCommand => new DelegateCommand(OpenFolderExecuted, OpenFolderCanExecute);

        public ICommand OpenWorkshopCommand => new DelegateCommand(OpenWorkshopExecuted, OpenWorkshopCanExecute);

        public ICommand ZoomThumbnailCommand => new DelegateCommand(ZoomThumbnailExecuted, ZoomThumbnailCanExecute);

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

        public bool ZoomThumbnail
        {
            get => _zoomThumbnail;
            set => SetProperty(ref _zoomThumbnail, value, nameof(ZoomThumbnail));
        }

        public WorldResource SelectedWorld
        {
            get => _dataModel.SelectedWorld;
            set => _dataModel.SelectedWorld = value;
        }

        public ObservableCollection<WorldResource> Worlds
        {
            get=> _dataModel.Worlds; 
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => _dataModel.IsBusy;
            set => _dataModel.IsBusy = value;
        }

        #endregion

        #region Methods

        public bool LoadCanExecute() => SelectedWorld is { IsValid: true };
       
        public void LoadExecuted()
        {
            IsBusy = true;
            // Preload to world before cloasing dialog.
            if (_dataModel.SelectedWorld.LoadCheckpoint(out string errorInformation))
            {
                if (_dataModel.SelectedWorld.LoadSector(out errorInformation))
                {
                    IsBusy = false;
                    CloseResult = true;
                    return;
                }
            }

            IsBusy = false;
            SystemSounds.Beep.Play();
            _dialogService.ShowErrorDialog(this, Res.ErrorLoadSaveGameFileError, errorInformation, true);
        }

        public bool RefreshCanExecute()
        {
            return !IsBusy;
        }

        public void RefreshExecuted()
        {
            _dataModel.Refresh();
        }

        public bool CancelCanExecute()
        {
            return true;
        }

        public void CancelExecuted()
        {
            CloseResult = false;
        }

        public bool RepairCanExecute()
        {
            return SelectedWorld != null &&
                (SelectedWorld.SaveType != SaveWorldType.DedicatedServerService ||
                (SelectedWorld.SaveType == SaveWorldType.DedicatedServerService && ToolboxUpdater.IsRunningElevated()));
        }

        public void RepairExecuted()
        {
            IsBusy = true;
            string results = SpaceEngineersRepair.RepairSandBox(_dataModel.SelectedWorld);
            IsBusy = false;
            _dialogService.ShowMessageBox(this, results, Res.ClsRepairTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.None);
        }

        public bool BrowseCanExecute()
        {
            return true;
        }

        public void BrowseExecuted()
        {
            IOpenFileDialog openFileDialog = _openFileDialogFactory();
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.DefaultExt = "sbc";
            openFileDialog.FileName = "Sandbox.sbc";
            openFileDialog.Filter = AppConstants.SandboxFilter;
            openFileDialog.Multiselect = false;
            openFileDialog.Title = Res.DialogLocateSandboxTitle;

            if (_dialogService.ShowOpenFileDialog(this, openFileDialog) == DialogResult.OK)
            {
                IsBusy = true;
                string savePath = Path.GetDirectoryName(openFileDialog.FileName);
                string userName = Environment.UserName;
                SaveWorldType saveType = SaveWorldType.Custom;

                try
                {
                    using FileStream fs = File.OpenWrite(openFileDialog.FileName);
                    // test opening the file to verify that we have Write Access.
                }
                catch
                {
                    saveType = SaveWorldType.CustomAdminRequired;
                }

                // Determine the correct UserDataPath for this custom save game if at all possible for the mods.
                UserDataPath dataPath = UserDataPath.FindFromSavePath(savePath);

                WorldResource saveResource = SelectWorldModel.LoadSaveFromPath(savePath, userName, saveType, dataPath);
                if (saveResource.LoadCheckpoint(out string errorInformation))
                {
                    if (saveResource.LoadSector(out errorInformation))
                    {
                        SelectedWorld = saveResource;
                        IsBusy = false;
                        CloseResult = true;
                        return;
                    }
                }

                IsBusy = false;
                SystemSounds.Beep.Play();
                _dialogService.ShowErrorDialog(this, Res.ErrorLoadSaveGameFileError, errorInformation, true);
            }
        }

        public bool OpenFolderCanExecute()
        {
            return SelectedWorld != null;
        }

        public void OpenFolderExecuted()
        {
            Process.Start(new ProcessStartInfo("Explorer", string.Format($"\"{ SelectedWorld.SavePath}\"")) { UseShellExecute = true });
        }

        public bool OpenWorkshopCanExecute()
        {
            return SelectedWorld != null && SelectedWorld.WorkshopId.HasValue &&
                   SelectedWorld.WorkshopId.Value != 0;
        }

        public void OpenWorkshopExecuted()
        {
            if (SelectedWorld.WorkshopId.HasValue)
               Process.Start(new ProcessStartInfo(string.Format("http://steamcommunity.com/sharedfiles/filedetails/?id={0}", SelectedWorld.WorkshopId.Value)) { UseShellExecute = true });
        }

        public bool ZoomThumbnailCanExecute()
        {
            return SelectedWorld != null && SelectedWorld.ThumbnailImageFileName != null;
        }

        public void ZoomThumbnailExecuted()
        {
            ZoomThumbnail = !ZoomThumbnail;
        }

        #endregion
    }
}
