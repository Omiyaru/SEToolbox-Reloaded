
using Sandbox.Common.ObjectBuilders;
using SEToolbox.Interfaces;
using SEToolbox.Interop;

using SEToolbox.Interop.Asteroids;
using SEToolbox.Models;
using SEToolbox.Services;
using SEToolbox.Support;
using SEToolbox.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Shell;
using VRage;
using VRage.Game;

using VRage.Game.ObjectBuilders.Components;
using VRage.ObjectBuilders;
using VRage.ObjectBuilders.Private;
using VRageMath;
using WPFLocalizeExtension.Engine;
using Res = SEToolbox.Properties.Resources;
using MOBTypeIds = SEToolbox.Interop.SpaceEngineersTypes.MOBTypeIds;
//using System.ComponentModel.Composition.Primitives;
//using SEConsts = SEToolbox.Interop.SpaceEngineersConsts;
namespace SEToolbox.ViewModels
{
    public class ExplorerViewModel : BaseViewModel, IDropable, IMainView
    {
        #region Fields

        private readonly ExplorerModel _dataModel;
        private readonly IDialogService _dialogService;
        private readonly Func<IOpenFileDialog> _openFileDialogFactory;
        private readonly Func<ISaveFileDialog> _saveFileDialogFactory;
        private bool? _closeResult;

        private bool _ignoreUpdateSelection;
        private IStructureViewBase _selectedStructure;
        private IStructureViewBase _preSelectedStructure;
        private ObservableCollection<IStructureViewBase> _selections;
        private ObservableCollection<IStructureViewBase> _structures;
        private ObservableCollection<LanguageModel> _languages;

        // If true, when adding new models to the collection, the new models will be highlighted as selected in the UI.
        private bool _selectNewStructure;

        private IFactionBase _selectedFaction;
        private IFactionBase _selectedMember;
        private ObservableCollection<IFactionBase> _factions;
        private ObservableCollection<IFactionBase> _members;

        #endregion

        #region Event Handlers

        public event EventHandler CloseRequested;

        #endregion

        #region Constructors

        public ExplorerViewModel(ExplorerModel dataModel)
            : this(dataModel, ServiceLocator.Resolve<IDialogService>(), ServiceLocator.Resolve<IOpenFileDialog>, ServiceLocator.Resolve<ISaveFileDialog>)
        {
        }

        public ExplorerViewModel(ExplorerModel dataModel, IDialogService dialogService, Func<IOpenFileDialog> openFileDialogFactory, Func<ISaveFileDialog> saveFileDialogFactory)
            : base(null)
        {
            Contract.Requires(dialogService != null);
            Contract.Requires(openFileDialogFactory != null);
            Contract.Requires(saveFileDialogFactory != null);

            _dialogService = dialogService;
            _openFileDialogFactory = openFileDialogFactory;
            _saveFileDialogFactory = saveFileDialogFactory;
            _dataModel = dataModel;

            Selections = [];
            Selections.CollectionChanged += (sender, e) => OnPropertyChanged(nameof(IsMultipleSelections));

            Structures = [];
            foreach (IStructureBase item in _dataModel.Structures ?? Enumerable.Empty<IStructureBase>())
            {
                AddViewModel(item);
            }

            UpdateLanguages();
            _dataModel.Structures.CollectionChanged += Structures_CollectionChanged;
            // Will bubble property change events from the Model to the ViewModel.
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Command Properties

        public ICommand ClosingCommand => new DelegateCommand<CancelEventArgs>(ClosingExecuted, ClosingCanExecute);
        public ICommand OpenCommand => new DelegateCommand(OpenExecuted, OpenCanExecute);
        public ICommand SaveCommand => new DelegateCommand(SaveExecuted, SaveCanExecute);
        public ICommand SaveAsCommand => new DelegateCommand(SaveAsExecuted, SaveAsCanExecute);
        public ICommand ClearCommand => new DelegateCommand(ClearExecuted, ClearCanExecute);
        public ICommand ReloadCommand => new DelegateCommand(ReloadExecuted, ReloadCanExecute);

        //public ICommand OpenScriptEditorCommand => new DelegateCommand(OpenScriptEditorExecuted, OpenScriptEditorCanExecute);

        private bool OpenScriptEditorCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true };
        }

        // private void OpenScriptEditorExecuted()
        // {
        //     var window = new WindowScriptEditor();
        //     window.DataContext = new ScriptEditorViewModel(_dataModel);
        //     window.ShowDialog();
        // }

        public ICommand IsActiveCommand => new DelegateCommand(new Func<bool>(IsActiveCanExecute));

        public ICommand ImportVoxelCommand => new DelegateCommand(ImportVoxelExecuted, ImportVoxelCanExecute);
        public ICommand ImportImageCommand => new DelegateCommand(ImportImageExecuted, ImportImageCanExecute);
        public ICommand ImportModelCommand => new DelegateCommand(ImportModelExecuted, ImportModelCanExecute);
        public ICommand ImportAsteroidModelCommand => new DelegateCommand(ImportAsteroidModelExecuted, ImportAsteroidModelCanExecute);
        public ICommand ImportSandboxObjectCommand => new DelegateCommand(ImportSandboxObjectExecuted, ImportSandboxObjectCanExecute);
        public ICommand OpenComponentListCommand => new DelegateCommand(OpenComponentListExecuted, OpenComponentListCanExecute);
        public ICommand WorldReportCommand => new DelegateCommand(WorldReportExecuted, WorldReportCanExecute);
        public ICommand OpenFolderCommand => new DelegateCommand(OpenFolderExecuted, OpenFolderCanExecute);
        public ICommand ViewSandboxCommand => new DelegateCommand(ViewSandboxExecuted, ViewSandboxCanExecute);
        public ICommand OpenWorkshopCommand => new DelegateCommand(OpenWorkshopExecuted, OpenWorkshopCanExecute);
        public ICommand ExportSandboxObjectCommand => new DelegateCommand(ExportSandboxObjectExecuted, ExportSandboxObjectCanExecute);
        public ICommand ExportBasicSandboxObjectCommand => new DelegateCommand(ExportBasicSandboxObjectExecuted, ExportBasicSandboxObjectCanExecute);
        public ICommand ExportPrefabObjectCommand => new DelegateCommand(ExportPrefabObjectExecuted, ExportPrefabObjectCanExecute);
        public ICommand ExportSpawnGroupObjectCommand => new DelegateCommand(ExportSpawnGroupObjectExecuted, ExportSpawnGroupObjectCanExecute);
        public ICommand ExportBlueprintCommand => new DelegateCommand(ExportBlueprintExecuted, ExportBlueprintCanExecute);
        public ICommand CreateFloatingItemCommand => new DelegateCommand(CreateFloatingItemExecuted, CreateFloatingItemCanExecute);
        public ICommand GenerateVoxelFieldCommand => new DelegateCommand(GenerateVoxelFieldExecuted, GenerateVoxelFieldCanExecute);

        public ICommand Test1Command => new DelegateCommand(Test1Executed, Test1CanExecute);
        public ICommand Test2Command => new DelegateCommand(Test2Executed, Test2CanExecute);
        public ICommand Test3Command => new DelegateCommand(Test3Executed, Test3CanExecute);
        public ICommand Test4Command => new DelegateCommand(Test4Executed, Test4CanExecute);
        public ICommand Test5Command => new DelegateCommand(Test5Executed, Test5CanExecute);
        public ICommand Test6Command => new DelegateCommand(Test6Executed, Test6CanExecute);
        public ICommand OpenSettingsCommand => new DelegateCommand(OpenSettingsExecuted, OpenSettingsCanExecute);
        public ICommand OpenUpdatesLinkCommand => new DelegateCommand(OpenUpdatesLinkExecuted, OpenUpdatesLinkCanExecute);
        public ICommand OpenDocumentationLinkCommand => new DelegateCommand(OpenDocumentationLinkExecuted, OpenDocumentationLinkCanExecute);
        public ICommand OpenSupportLinkCommand => new DelegateCommand(OpenSupportLinkExecuted, OpenSupportLinkCanExecute);
        public ICommand AboutCommand => new DelegateCommand(AboutExecuted, AboutCanExecute);
        public ICommand LanguageCommand => new DelegateCommand(new Func<bool>(LanguageCanExecute));
        public ICommand SetLanguageCommand => new DelegateCommand<string>(SetLanguageExecuted, SetLanguageCanExecute);
        public ICommand DeleteObjectCommand => new DelegateCommand(DeleteObjectExecuted, DeleteObjectCanExecute);
        public ICommand CopyObjectGpsCommand => new DelegateCommand(CopyObjectGpsExecuted, CopyObjectGpsCanExecute);
        public ICommand SelectJoinedGridsCommand => new DelegateCommand<GridConnectionTypes>(SelectJoinedGridsExecuted, SelectJoinedGridsCanExecute);
        public ICommand GroupMoveCommand => new DelegateCommand(GroupMoveExecuted, GroupMoveCanExecute);

        public ICommand GroupMoveToNewPositionCommand => new DelegateCommand(GroupMoveToNewPositionExecuted, GroupMoveToNewPositionCanExecute);

        public ICommand RejoinShipCommand => new DelegateCommand(RejoinShipExecuted, RejoinShipCanExecute);

        public ICommand JoinShipPartsCommand => new DelegateCommand(JoinShipPartsExecuted, JoinShipPartsCanExecute);
        public ICommand VoxelMergeCommand => new DelegateCommand(VoxelMergeExecuted, VoxelMergeCanExecute);
        public ICommand RepairShipsCommand => new DelegateCommand(RepairShipsExecuted, RepairShipsCanExecute);
        public ICommand ResetVelocityCommand => new DelegateCommand(ResetVelocityExecuted, ResetVelocityCanExecute);
        public ICommand ConvertToShipCommand => new DelegateCommand(ConvertToShipExecuted, ConvertToShipCanExecute);
        public ICommand ConvertToStationCommand => new DelegateCommand(ConvertToStationExecuted, ConvertToStationCanExecute);
        public ICommand InertiaTensorOnCommand => new DelegateCommand<bool>(InertiaTensorExecuted, InertiaTensorCanExecute);

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


        public ObservableCollection<IFactionBase> Factions
        {
            get => _factions;
            set => SetProperty(ref _factions, value, nameof(Factions));
        }

        public IFactionBase SelectedFaction
        {
            get => _selectedFaction;
            set => SetProperty(ref _selectedFaction, value, nameof(SelectedFaction));
        }

        public IFactionBase SelectedMember
        {
            get => _selectedMember;
            set => SetProperty(ref _selectedMember, value, nameof(SelectedMember));
        }

        public ObservableCollection<IFactionBase> Members
        {
            get => _members;
            set => SetProperty(ref _members, value, nameof(Members));
        }

        public ObservableCollection<IStructureViewBase> Structures
        {
            get => _structures;
            set => SetProperty(ref _structures, value, nameof(Structures));
        }

        public IStructureViewBase SelectedStructure
        {
            get => _selectedStructure;
            set => SetProperty(ref _selectedStructure, value, () =>
                    {
                        if (_selectedStructure != null &&
                            !_ignoreUpdateSelection && _selectedStructure == value)
                            _selectedStructure.DataModel.InitializeAsync();
                    }, nameof(SelectedStructure));

        }

        public ObservableCollection<IStructureViewBase> Selections
        {
            get => _selections;
            set => SetProperty(ref _selections, value, nameof(Selections));
        }

        public bool? IsMultipleSelections
        {
            get => _selections.Count > 1;
        }

        public WorldResource ActiveWorld
        {
            get => _dataModel.ActiveWorld;
            set => _dataModel.ActiveWorld = value;
        }

        public StructureCharacterModel ThePlayerCharacter
        {
            get => _dataModel.ThePlayerCharacter;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is available.  This is based on the IsInError and IsBusy properties
        /// </summary>
        public bool IsActive
        {
            get => _dataModel.IsActive;
            set => _dataModel.IsActive = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => _dataModel.IsBusy;
            set => _dataModel.IsBusy = value;
        }

        public bool IsDebugger
        {
            get => Debugger.IsAttached;
        }

        public bool IsModified
        {
            get => _dataModel.IsModified;
            set => _dataModel.IsModified = value;
        }

        public bool IsBaseSaveChanged
        {
            get => _dataModel.IsBaseSaveChanged;
            set => _dataModel.IsBaseSaveChanged = value;
        }

        public ObservableCollection<LanguageModel> Languages
        {
            get => _languages;
            private set => SetProperty(ref _languages, value, nameof(Languages));
        }

        public bool? UseExcludedTypes { get; set; }

        public bool EnableExcludedBlocks
        {
            get => _dataModel.EnableExclusions;
            set => SetProperty(_dataModel.EnableExclusions, value, nameof(EnableExcludedBlocks));
        }

        //public Type DataType => throw new NotImplementedException();

        #endregion

        #region Command Methods

        public bool ClosingCanExecute(CancelEventArgs e)
        {
            return true;
        }

        public void ClosingExecuted(CancelEventArgs e)
        {
            if (IsModified)
            {
                DialogResult result = MessageBox.Show(
                    "There are unsaved changes. Do you want to save before closing?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                else if (result == DialogResult.Yes)
                {
                    SaveExecuted();
                }
            }

            if (!CheckCloseWindow())
            {
                e.Cancel = true;
                CloseResult = null;
                return;
            }

            CloseRequested.Invoke(this, EventArgs.Empty);
        }

        private bool CheckCloseWindow()
        {
            if (IsBusy)
            {
                MessageBox.Show(
                    "The application is currently busy. Please wait for the operation to complete before closing.",
                    "Operation in Progress",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }
            return true;
        }

        public bool OpenCanExecute()
        {
            return true;
        }

        public void OpenExecuted()
        {
            try
            {
                SelectWorldModel model = new();
                model.Load(SpaceEngineersConsts.BaseLocalPath, SpaceEngineersConsts.BaseDedicatedServerHostPath, SpaceEngineersConsts.BaseDedicatedServerServicePath);
                SelectWorldViewModel loadVm = new(this, model);

                bool? result = _dialogService.ShowDialog<WindowLoad>(this, loadVm);
                if (result == true)
                {
                    _dataModel.BeginLoad();
                    _dataModel.ActiveWorld = model.SelectedWorld;
                    ActiveWorld.LoadDefinitionsAndMods();
                    _dataModel.ParseSandBox();
                    _dataModel.EndLoad();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while opening the dialog: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public bool SaveCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } &&
                ((_dataModel.ActiveWorld.SaveType != SaveWorldType.DedicatedServerService && _dataModel.ActiveWorld.SaveType != SaveWorldType.CustomAdminRequired) ||
                ((_dataModel.ActiveWorld.SaveType == SaveWorldType.DedicatedServerService || _dataModel.ActiveWorld.SaveType == SaveWorldType.CustomAdminRequired) &&
                ToolboxUpdater.IsRunningElevated()));
        }

        public void SaveExecuted()
        {
            _dataModel?.SaveCheckPointAndSandBox();
        }

        public bool SaveAsCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } &&
                   _dataModel.ActiveWorld.SaveType != SaveWorldType.Custom &&
                 ((_dataModel.ActiveWorld.SaveType != SaveWorldType.DedicatedServerService &&
                   _dataModel.ActiveWorld.SaveType != SaveWorldType.CustomAdminRequired) ||
                 ((_dataModel.ActiveWorld.SaveType == SaveWorldType.DedicatedServerService ||
                   _dataModel.ActiveWorld.SaveType == SaveWorldType.CustomAdminRequired) &&
                ToolboxUpdater.IsRunningElevated()));
        }

        public void SaveAsExecuted()
        {
            if (_dataModel != null)
            {
                ISaveFileDialog saveFileDialog = _saveFileDialogFactory();
                saveFileDialog.Filter = "Save Files (*.save)|*.save|All Files (*.*)|*.*";
                saveFileDialog.Title = "Save As";
                saveFileDialog.OverwritePrompt = true;

                if (_dialogService.ShowSaveFileDialog(this, saveFileDialog) == DialogResult.OK)
                {
                    string newDirectory = Path.GetDirectoryName(saveFileDialog.FileName);
                    if (newDirectory != null)
                    {
                        Directory.CreateDirectory(newDirectory);

                        string[] array = Directory.GetFiles(_dataModel.ActiveWorld.SavePath);
                        foreach (var t in array)
                        {
                            _ = t;
                            _dataModel.ActiveWorld.SavePath = newDirectory;
                        }
                    }

                    _dataModel.SaveCheckPointAndSandBox();
                }
            }
        }

        public bool ClearCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true };
        }

        public void ClearExecuted()
        {
            if (_dataModel != null)
            {
                _dataModel.Structures.Clear();
                _dataModel.ActiveWorld = null;
                Selections.Clear();
                Structures.Clear();
                SelectedStructure = null;
                OnPropertyChanged(nameof(Structures));
                OnPropertyChanged(nameof(Selections));
            }
        }

        public bool ReloadCanExecute()
        {
            return _dataModel.ActiveWorld != null;
        }

        public void ReloadExecuted()
        {
            if (!IsSavePathValid())
            {
                _dialogService.ShowErrorDialog(this, Res.ErrorLoadSaveGameFileError, Res.ErrorSavePathInvalid, false);
                return;
            }

            _dataModel.BeginLoad();

            // Reload Checkpoint file.
            if (!ActiveWorld.LoadCheckpoint(out string errorInformation))
            {
                // leave world in Invalid state, allowing Reload to be called again.
                ActiveWorld.IsValid = false;
                _dialogService.ShowErrorDialog(this, Res.ErrorLoadSaveGameFileError, errorInformation, false);
                _dataModel.ParseSandBox();
                _dataModel.EndLoad();
                return;
            }

            // Reload Definitions, Mods, and clear out Materials, Textures.
            ActiveWorld.LoadDefinitionsAndMods();
            Converters.DDSConverter.ClearCache();

            // Load Sector file.
            if (!ActiveWorld.LoadSector(out errorInformation))
            {
                // leave world in Invalid state, allowing Reload to be called again.
                ActiveWorld.IsValid = false;

                _dialogService.ShowErrorDialog(this, Res.ErrorLoadSaveGameFileError, errorInformation, false);
            }

            _dataModel.ParseSandBox();
            _dataModel.EndLoad();
        }


        public bool IsSavePathValid()
        {
            var savePath = GetSavePath();
            return !string.IsNullOrWhiteSpace(savePath)
                   && Directory.Exists(savePath)
                   && !savePath.Contains("..", StringComparison.Ordinal)
                   && !Path.IsPathRooted(savePath); // ensure no directory traversal
        }

        public string GetSavePath()
        {
            return _dataModel.ActiveWorld.SavePath ?? string.Empty;
        }

        public bool IsActiveCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true };
        }

        public bool ImportVoxelCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true };
        }

        public void ImportVoxelExecuted()
        {
            ImportVoxelModel model = new();
            MyPositionAndOrientation position = ThePlayerCharacter != null ? ThePlayerCharacter.PositionAndOrientation.Value : new(Vector3D.Zero, Vector3.Forward, Vector3.Up);
            model.Load(position);
            ImportVoxelViewModel loadVm = new(this, model);

            bool? result = _dialogService.ShowDialog<WindowImportVoxel>(this, loadVm);
            if (result == true)
            {
                IsBusy = true;
                var newEntity = loadVm.BuildEntity();
                var structure = _dataModel.AddEntity(newEntity);
                ((StructureVoxelModel)structure).SourceVoxelFilePath = loadVm.SourceFile; // Set the temporary file location of the Source Voxel, as it hasn't been written yet.
                if (_preSelectedStructure != null)
                    SelectedStructure = _preSelectedStructure;
                IsBusy = false;
            }
        }

        public bool ImportImageCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true };
        }

        public void ImportImageExecuted()
        {
            ImportImageModel model = new();
            MyPositionAndOrientation position = ThePlayerCharacter != null ? ThePlayerCharacter.PositionAndOrientation.Value : new MyPositionAndOrientation(Vector3D.Zero, Vector3.Forward, Vector3.Up);
            model.Load(position);
            ImportImageViewModel loadVm = new(this, model);
            bool? result = _dialogService.ShowDialog<WindowImportImage>(this, loadVm);
            if (result == true)
            {
                IsBusy = true;
                var newEntity = loadVm.BuildEntity();
                // make sure resultant object has cubes.
                if (newEntity.CubeBlocks.Count != 0)
                {
                    _selectNewStructure = true;
                    _dataModel.CollisionCorrectEntity(newEntity);
                    _dataModel.AddEntity(newEntity);
                    _selectNewStructure = false;
                }
                IsBusy = false;
            }
        }

        public bool ImportModelCanExecute()
        {
            return _dataModel.ActiveWorld.IsValid;
        }

        public void ImportModelExecuted()
        {
            Import3DModelModel model = new();
            if (ThePlayerCharacter.PositionAndOrientation != null)
            {
                MyPositionAndOrientation position = ThePlayerCharacter?.PositionAndOrientation ?? new(Vector3D.Zero, Vector3.Forward, Vector3.Up);
                model.Load(position);
            }

            Import3DModelViewModel loadVm = new(this, model);
            bool? result = _dialogService.ShowDialog<WindowImportModel>(this, loadVm);
            if (result == true)
            {
                IsBusy = true;
                MyObjectBuilder_EntityBase newEntity = loadVm.BuildEntity();
                if (loadVm.IsValidModel)
                {
                    _dataModel.CollisionCorrectEntity(newEntity);
                    IStructureBase structure = _dataModel.AddEntity(newEntity);
                    {
                        if (structure is StructureVoxelModel model1)
                        {
                            model1.SourceVoxelFilePath = loadVm.SourceFile;
                        }

                        if (_preSelectedStructure != null)
                            SelectedStructure = _preSelectedStructure;
                    }
                    IsBusy = false;
                }
            }
        }

        public bool ImportAsteroidModelCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true };
        }

        public void ImportAsteroidModelExecuted()
        {
            Import3DAsteroidModel model = new();
            MyPositionAndOrientation position = ThePlayerCharacter?.PositionAndOrientation ?? new(Vector3D.Zero, Vector3.Forward, Vector3.Up);
            model.Load(position);
            Import3DAsteroidViewModel loadVm = new(this, model);
            bool? result = _dialogService.ShowDialog<WindowImportAsteroidModel>(this, loadVm);
            if (result == true && loadVm.IsValidEntity)
            {
                IsBusy = true;
                _dataModel.CollisionCorrectEntity(loadVm.NewEntity);
                IStructureBase structure = _dataModel.AddEntity(loadVm.NewEntity);
                ((StructureVoxelModel)structure).SourceVoxelFilePath = loadVm.SourceFile; // Set the temporary file location of the Source Voxel, as it hasn't been written yet.
                if (_preSelectedStructure != null)
                    SelectedStructure = _preSelectedStructure;

                if (loadVm.SaveWhenFinsihed)
                {
                    _dataModel.SaveCheckPointAndSandBox();
                }

                IsBusy = false;
            }
        }

        public bool ImportSandboxObjectCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true };
        }

        public void ImportSandboxObjectExecuted()
        {
            ImportSandboxObjectFromFile();
        }

        public bool OpenComponentListCanExecute()
        {
            return true;
        }

        public void OpenComponentListExecuted()
        {
            ComponentListModel model = new();
            model.Load();
            ComponentListViewModel loadVm = new(this, model);
            _dialogService.Show<WindowComponentList>(this, loadVm);
        }

        public bool WorldReportCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true };
        }

        public void WorldReportExecuted()
        {
            ResourceReportModel model = new();
            model.Load(_dataModel.ActiveWorld.SaveName, _dataModel.Structures);
            ResourceReportViewModel loadVm = new(this, model);
            _dialogService.ShowDialog<WindowResourceReport>(this, loadVm);
        }

        public bool OpenFolderCanExecute()
        {
            return _dataModel.ActiveWorld != null;
        }

        static void StartShellProcess(string fileName, string arguments)
        {
            Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = true });
        }

        static void StartShellProcess(string fileName)
        {
            Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
        }

        public void OpenFolderExecuted()
        {
            StartShellProcess("Explorer", $"\"{_dataModel.ActiveWorld.SavePath}\"");
        }

        public bool ViewSandboxCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true };
        }

        public void ViewSandboxExecuted()
        {
            if (_dataModel != null)
            {
                string fileName = _dataModel.SaveTemporarySandbox();
                Process.Start($"\"{fileName}\"");
            }
        }

        public bool OpenWorkshopCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && _dataModel.ActiveWorld.WorkshopId.HasValue && _dataModel.ActiveWorld.WorkshopId.Value != 0;
        }

        public void OpenWorkshopExecuted()
        {
            StartShellProcess(string.Format($"http://steamcommunity.com/sharedfiles/filedetails/?id={_dataModel.ActiveWorld.WorkshopId.Value}"));
        }

        public bool ExportSandboxObjectCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0;
        }

        public void ExportSandboxObjectExecuted()
        {
            ExportSandboxObjectToFile(false, [.. Selections]);
        }

        public bool ExportBasicSandboxObjectCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0;
        }

        public void ExportBasicSandboxObjectExecuted()
        {
            ExportSandboxObjectToFile(true, [.. Selections]);
        }

        public bool ExportPrefabObjectCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0 && Selections.Any(e => e is StructureCubeGridViewModel);
        }

        public void ExportPrefabObjectExecuted()
        {
            ExportPrefabObjectToFile(true, [.. Selections]);
        }

        public bool ExportSpawnGroupObjectCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0 && Selections.Any(e => e is StructureCubeGridViewModel || e is StructureVoxelViewModel);
        }

        public void ExportSpawnGroupObjectExecuted()
        {
            ExportSpawnGroupObjectToFile(true, [.. Selections]);
        }

        public bool ExportBlueprintCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0 && Selections.Any(e => e is StructureCubeGridViewModel);
        }

        public void ExportBlueprintExecuted()
        {
            ExportBlueprintToFile([.. Selections]);
        }

        public bool CreateFloatingItemCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true };
        }

        public void CreateFloatingItemExecuted()
        {
            GenerateFloatingObjectModel model = new();
            MyPositionAndOrientation position = ThePlayerCharacter != null ? ThePlayerCharacter.PositionAndOrientation.Value : new(Vector3D.Zero, Vector3.Forward, Vector3.Up); ;
            model.Load(position, _dataModel.ActiveWorld.Checkpoint.MaxFloatingObjects);
            GenerateFloatingObjectViewModel loadVm = new(this, model);
            bool? result = _dialogService.ShowDialog<WindowGenerateFloatingObject>(this, loadVm);
            if (result == true)
            {
                IsBusy = true;
                MyObjectBuilder_EntityBase[] newEntities = loadVm.BuildEntities();
                if (loadVm.IsValidItemToImport)
                {
                    _selectNewStructure = true;
                    foreach (var t in newEntities)
                    {
                        _dataModel.AddEntity(t);
                    }
                    _selectNewStructure = false;
                }
                IsBusy = false;
            }
        }

        public bool GenerateVoxelFieldCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true };
        }

        public void GenerateVoxelFieldExecuted()
        {
            GenerateVoxelFieldModel model = new();
            MyPositionAndOrientation position = ThePlayerCharacter != null ? ThePlayerCharacter.PositionAndOrientation.Value : new MyPositionAndOrientation(Vector3D.Zero, Vector3.Forward, Vector3.Up);
            model.Load(position);
            GenerateVoxelFieldViewModel loadVm = new(this, model);
            bool? result = _dialogService.ShowDialog<WindowGenerateVoxelField>(this, loadVm);
            model.Unload();
            if (result == true)
            {
                IsBusy = true;
                loadVm.BuildEntities(out string[] sourceVoxelFiles, out MyObjectBuilder_EntityBase[] newEntities);
                _selectNewStructure = true;
                ResetProgress(0, newEntities.Length);

                for (int i = 0; i < newEntities.Length; i++)
                {
                    IStructureBase structure = _dataModel.AddEntity(newEntities[i]);
                    StructureVoxelModel voxelModel = (StructureVoxelModel)structure;
                    voxelModel.SourceVoxelFilePath = sourceVoxelFiles[i];
                    voxelModel.InitializeAsync();
                    Progress++;
                }
                _selectNewStructure = false;
                IsBusy = false;
                ClearProgress();
            }
        }

        public bool OpenSettingsCanExecute()
        {
            return true;
        }

        public void OpenSettingsExecuted()
        {
            SettingsModel model = new();
            model.Load(GlobalSettings.Default.SEBinPath, GlobalSettings.Default.CustomVoxelPath, (bool)GlobalSettings.Default.AlwaysCheckForUpdates, (bool)GlobalSettings.Default.UseCustomResource);
            SettingsViewModel loadVm = new(this, model);
            if (_dialogService.ShowDialog<WindowSettings>(this, loadVm) == true)
            {
                bool reloadMods = GlobalSettings.Default.SEBinPath != model.SEBinPath;
                GlobalSettings.Default.SEBinPath = model.SEBinPath;
                GlobalSettings.Default.CustomVoxelPath = model.CustomVoxelPath;
                GlobalSettings.Default.AlwaysCheckForUpdates = model.AlwaysCheckForUpdates;
                bool resetLocalization = GlobalSettings.Default.UseCustomResource != model.UseCustomResource;
                GlobalSettings.Default.UseCustomResource = model.UseCustomResource;
                GlobalSettings.Default.Save();

                if (reloadMods)
                {
                    IsBusy = true;

                    if (ActiveWorld == null)
                    {
                        SpaceEngineersCore.Resources.LoadDefinitions();
                    }
                    else
                    {
                        // Reload the Mods.
                        ActiveWorld.LoadDefinitionsAndMods();
                    }

                    IsBusy = false;
                }

                if (resetLocalization)
                {
                    SpaceEngineersApi.LoadLocalization();
                    UpdateLanguages();
                }
            }
        }

        public bool OpenUpdatesLinkCanExecute()
        {
            return true;
        }

        public void OpenUpdatesLinkExecuted()
        {
            ApplicationRelease update = CodeRepositoryReleases.CheckForUpdates(new Version(), true);
            string message = string.Empty, title = string.Empty;
            switch (update)

            {
                case null when GlobalSettings.GetAppVersion() == null:
                    message = Res.DialogNoNetworkMessage;
                    title = Res.DialogNoNetworkTitle;
                    break;
                case { Version: var version } when version == GlobalSettings.GetAppVersion():
                    message = Res.DialogLatestVersionMessage;
                    title = Res.DialogNoNewVersionTitle;
                    break;
                case { Version: var prerelease } when prerelease < GlobalSettings.GetAppVersion():
                    message = Res.DialogPrereleaseVersionMessage;
                    title = Res.DialogNoNewVersionTitle;
                    break;
                default:
                    if (ConfirmUpdateAvailable(update.Version))
                    {
                        StartShellProcess(Res.GlobalUpdatesUrl);
                    }
                    break;
            }
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        //private void ShowMessage(string message, string title) => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        private bool ConfirmUpdateAvailable(Version newVersion)
        {
            DialogResult dialogResult = MessageBox.Show(string.Format(Res.DialogNewVersionMessage, newVersion), Res.DialogNewVersionTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            return dialogResult == DialogResult.Yes;
        }

        public bool OpenDocumentationLinkCanExecute()
        {
            return true;
        }

        public void OpenDocumentationLinkExecuted()
        {
            StartShellProcess(Res.GlobalDocumentationUrl);
        }

        public bool OpenSupportLinkCanExecute()
        {
            return true;
        }

        public void OpenSupportLinkExecuted()
        {
            StartShellProcess(Res.GlobalSupportUrl);
        }

        public bool LanguageCanExecute()
        {
            return true;
        }

        public bool SetLanguageCanExecute(string code)
        {
            return true;
        }

        public void SetLanguageExecuted(string code)
        {
            GlobalSettings.Default.LanguageCode = code;
            LocalizeDictionary.Instance.SetCurrentThreadCulture = false;
            LocalizeDictionary.Instance.Culture = CultureInfo.GetCultureInfoByIetfLanguageTag(code);
            Thread.CurrentThread.CurrentUICulture = LocalizeDictionary.Instance.Culture;
            SpaceEngineersApi.LoadLocalization();
            UpdateLanguages();

            // Causes all bindings to update.
            OnPropertyChanged("");
        }

        public bool AboutCanExecute()
        {
            return true;
        }

        public void AboutExecuted()
        {
            AboutViewModel loadVm = new(this);
            _dialogService.ShowDialog<WindowAbout>(this, loadVm);
        }

        public bool DeleteObjectCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0;
        }

        public void DeleteObjectExecuted()
        {
            DeleteModel([.. Selections]); ;
        }

        public bool CopyObjectGpsCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0;
        }

        public void CopyObjectGpsExecuted()
        {
            string text = string.Format(CultureInfo.InvariantCulture, $"GPS:{Selections[0].DataModel.DisplayName.Replace(":", "_").Replace("&", "_")}:{Selections[0].DataModel.PositionX:0.00}:{Selections[0].DataModel.PositionY:0.00}:{Selections[0].DataModel.PositionZ:0.00}:");
            try
            {
                Clipboard.Clear();
                Clipboard.SetText(text);
            }
            catch
            {
                // Ignore exception which may be generated by a Remote desktop session where Clipboard access has not been granted.
            }
        }

        public bool SelectJoinedGridsCanExecute(GridConnectionTypes minimumConnectionType)
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0;
        }

        public void SelectJoinedGridsExecuted(GridConnectionTypes minimumConnectionType)
        {
            _dataModel.BuildGridEntityNodes();
            Queue<StructureCubeGridModel> searchModels = [];
            List<IStructureViewBase> newSelectionModels = [];
            List<long> searchedIds = [];

            foreach (var t in Selections)
            {
                if (t.DataModel is StructureCubeGridModel gridModel)
                {
                    searchModels.Enqueue(gridModel);
                    newSelectionModels.Add(t);
                }
            }

            while (searchModels.Count > 0)
            {
                StructureCubeGridModel gridModel = searchModels.Dequeue();

                if (!searchedIds.Contains(gridModel.EntityId))
                {
                    List<MyObjectBuilder_CubeGrid> list = _dataModel.GetConnectedGridNodes(gridModel, minimumConnectionType);

                    var structures = Structures.ToDictionary(st => st.DataModel.EntityId, st => st);

                    foreach (MyObjectBuilder_CubeGrid cubegrid in list)
                    {
                        if (!searchedIds.Contains(cubegrid.EntityId))
                        {
                            if (structures.TryGetValue(cubegrid.EntityId, out var structure))
                            {
                                searchModels.Enqueue((StructureCubeGridModel)structure.DataModel);
                                newSelectionModels.Add(structure);
                            }
                        }
                    }

                    searchedIds.Add(gridModel.EntityId);
                }
            }

            // zero would mean the user selected a floating object or some other object that wasn't a grid, and it was filtered out of the final list.
            if (newSelectionModels.Count != 0)
            {
                Selections.Clear();
                foreach (IStructureViewBase structure in newSelectionModels)
                    Selections.Add(structure);
            }
        }

        public bool GroupMoveCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 1;
        }

        public void GroupMoveExecuted()
        {
            GroupMoveModel model = new();
            Vector3D position = ThePlayerCharacter.PositionAndOrientation.Value.Position;
            model.Load(Selections, position);
            GroupMoveViewModel loadVm = new(this, model);
            bool? result = _dialogService.ShowDialog<WindowGroupMove>(this, loadVm);

            if (result == true)
            {
                model.ApplyNewPositions();
                _dataModel.CalcDistances();
                IsModified = true;
            }
        }

        public bool MoveGroup { get; private set; }

        private bool GroupMoveToNewPositionCanExecute()
        {
            if (MoveGroup && Selections.Count >= 1)
            {
                return _dataModel?.ActiveWorld != null
                    && _dataModel.ActiveWorld.IsValid
                    && Selections?.Count > 1;
            }
            else if (_dataModel?.ActiveWorld == null ||
                    !_dataModel.ActiveWorld.IsValid ||
                    Selections?.Count == 0)
            {
                return false;
            }

            return true;
        }

        private void GroupMoveToNewPositionExecuted()
        {
            if (_dataModel?.ActiveWorld == null || !_dataModel.ActiveWorld.IsValid || Selections.Count == 0)
                return;

            if (ThePlayerCharacter.PositionAndOrientation != null)
            {
                Vector3D position = ThePlayerCharacter.PositionAndOrientation.Value.Position;
                if (MoveGroup && Selections.Count >= 1)
                {

                    GroupMoveModel model = new();
                    Vector3D centerPosition = new(Selections[0].DataModel.PositionX, Selections[0].DataModel.PositionY, Selections[0].DataModel.PositionZ);
                    model.Load(Selections, position, true, centerPosition);

                    GroupMoveViewModel loadVm = new(this, model);

                    bool? result = _dialogService.ShowDialog<WindowGroupMove>(this, loadVm);
                    if (result == true)
                    {
                        model.ApplyNewPositions();
                        _dataModel.CalcDistances();
                        IsModified = true;

                    }
                }
            }
        }

        public bool RejoinShipCanExecute() => _dataModel.ActiveWorld is { IsValid: true } && Selections.Count == 2 &&
                                              ((Selections[0].DataModel.ClassType == Selections[1].DataModel.ClassType && Selections[0].DataModel.ClassType == ClassType.LargeShip) ||
                                               (Selections[0].DataModel.ClassType == Selections[1].DataModel.ClassType && Selections[0].DataModel.ClassType == ClassType.SmallShip));


        public void RejoinShipExecuted()
        {
            IsBusy = true;
            RejoinShipModels(Selections[0], Selections[1]);
            IsBusy = false;
        }

        public bool JoinShipPartsCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count == 2 &&
                    ((Selections[0].DataModel.ClassType == Selections[1].DataModel.ClassType && Selections[0].DataModel.ClassType == ClassType.LargeShip) ||
                    (Selections[0].DataModel.ClassType == Selections[1].DataModel.ClassType && Selections[0].DataModel.ClassType == ClassType.SmallShip));
        }

        public void JoinShipPartsExecuted()
        {
            IsBusy = true;
            MergeShipPartModels(Selections[0], Selections[1]);
            IsBusy = false;
        }

        public bool VoxelMergeCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count == 2 &&
                   ((Selections[0].DataModel.ClassType == Selections[1].DataModel.ClassType && Selections[0].DataModel.ClassType == ClassType.Voxel && Selections[0].DataModel.IsValid) ||
                    (Selections[0].DataModel.ClassType == Selections[1].DataModel.ClassType && Selections[0].DataModel.ClassType == ClassType.Voxel && Selections[0].DataModel.IsValid));
        }

        public void VoxelMergeExecuted()
        {
            MergeVoxelModel model = new();
            IStructureViewBase item1 = Selections[0];
            IStructureViewBase item2 = Selections[1];
            model.Load(item1.DataModel, item2.DataModel);
            MergeVoxelViewModel loadVm = new(this, model);
            bool? result = _dialogService.ShowDialog<WindowVoxelMerge>(this, loadVm);
            if (result == true)
            {
                IsBusy = true;
                MyObjectBuilder_EntityBase newEntity = loadVm.BuildEntity();
                IStructureBase structure = _dataModel.AddEntity(newEntity);
                ((StructureVoxelModel)structure).SourceVoxelFilePath = loadVm.SourceFile; // Set the temporary file location of the Source Voxel, as it hasn't been written yet.
                if (_preSelectedStructure != null)
                    SelectedStructure = _preSelectedStructure;

                if (loadVm.RemoveOriginalAsteroids)
                {
                    DeleteModel(item1, item2);
                }

                IsBusy = false;
            }
        }

        public bool RepairShipsCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0;
        }

        public void RepairShipsExecuted()
        {
            IsBusy = true;
            RepairShips(Selections);
            IsBusy = false;
        }

        public bool ResetVelocityCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0;
        }

        public void ResetVelocityExecuted()
        {
            IsBusy = true;
            StopShips(Selections);
            IsBusy = false;
        }


        public bool ConvertToShipCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0;
        }

        public void ConvertToShipExecuted()
        {
            IsBusy = true;
            ConvertToShips(Selections);
            IsBusy = false;
        }

        public bool ConvertToStationCanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0;
        }

        public void ConvertToStationExecuted()
        {
            IsBusy = true;
            ConvertToStations(Selections);
            IsBusy = false;
        }

        public bool InertiaTensorCanExecute(bool state)
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0;
        }

        public void InertiaTensorExecuted(bool state)
        {
            IsBusy = true;
            int count = SetInertiaTensor(Selections, true);
            IsBusy = false;

            _dialogService.ShowMessageBox(this,
              string.Format(Res.ClsExplorerGridChangesMade, count),
              Res.ClsExplorerTitleChangesMade,
              System.Windows.MessageBoxButton.OK,
              System.Windows.MessageBoxImage.Information);
        }

        private void ShowMessage(string message)
        {
            System.Windows.MessageBox.Show(message, "Notification", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        public void FindOverlappingBlocks(params IStructureViewBase[] viewModels)
        {
            foreach (var viewModel in viewModels.OfType<StructureCubeGridViewModel>())
            {
                _dataModel.FindOverlappingBlocks(viewModel.DataModel as StructureCubeGridModel);
            }
        }

        public void RemoveOverlappingBlocks(params IStructureViewBase[] viewModels)
        {
            foreach (var viewModel in viewModels.OfType<StructureCubeGridViewModel>())
            {
                _dataModel.RemoveOverlappingBlocks(viewModel.DataModel as StructureCubeGridModel);
            }
        }

        public void MoveOverlappingBlocks(params IStructureViewBase[] viewModels)
        {
            foreach (var viewModel in viewModels.OfType<StructureCubeGridViewModel>())
            {
                _dataModel.MoveOverlappingBlocks(viewModel.DataModel as StructureCubeGridModel);
            }
        }

        public bool ToggleExcludedBlocks(params IStructureViewBase[] viewModels)
        {
            if (viewModels == null || viewModels.Length == 0)
            {
                ShowMessage("No structures selected to toggle excluded blocks.");
                return false;
            }

            foreach (var viewModel in viewModels)
            {
                if (viewModel is StructureCubeGridViewModel gridViewModel)
                {
                    _dataModel.ToggleExcludedBlocks((StructureCubeGridModel)gridViewModel.DataModel);
                }
            }
            return _dataModel.EnableExclusions;
        }

        #endregion
        //todo Move TestCommands to TestExplorerVeiwModel
        #region Test Command Methods

        public bool Test1CanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true };
        }

        public void Test1Executed()
        {
            Import3DModelModel model = new();

            var position = ThePlayerCharacter != null ? ThePlayerCharacter.PositionAndOrientation.Value : new MyPositionAndOrientation(Vector3D.Zero, Vector3.Forward, Vector3.Up);
            model.Load(position);
            Import3DModelViewModel loadVm = new(this, model);

            IsBusy = true;
            MyObjectBuilder_CubeGrid newEntity = loadVm.BuildTestEntity();

            // Split object where X=28|29.
            //newEntity.CubeBlocks.RemoveAll(c => c.Min.X <= 3);
            //newEntity.CubeBlocks.RemoveAll(c => c.Min.X > 4);

            _selectNewStructure = true;
            _dataModel.CollisionCorrectEntity(newEntity);
            _dataModel.AddEntity(newEntity);
            _selectNewStructure = false;
            IsBusy = false;
        }

        public bool Test2CanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0;
        }

        public void Test2Executed()
        {
            TestCalcCubesModel([.. Selections]);
            //OptimizeModel([..Selections]);
        }

        public bool Test3CanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true };
        }

        public void Test3Executed()
        {
            Import3DModelModel model = new();
            MyPositionAndOrientation position = ThePlayerCharacter?.PositionAndOrientation ?? new MyPositionAndOrientation(Vector3D.Zero, Vector3.Forward, Vector3.Up);
            model.Load(position);
            Import3DModelViewModel loadVm = new(this, model)
            {
                ArmorType = ImportArmorType.Light,
                BuildDistance = 10,
                ClassType = ImportModelClassType.SmallShip,
                FileName = @"D:\Development\SpaceEngineers\building 3D\models\algos.obj",
                Forward = new BindableVector3DModel(Vector3.Forward),
                IsMaxLengthScale = false,
                IsMultipleScale = true,
                IsValidModel = true,
                MultipleScale = 1,
                Up = new BindableVector3DModel(Vector3.Up)
            };


            IsBusy = true;
            MyObjectBuilder_EntityBase newEntity = loadVm.BuildEntity();

            // Split object where X=28|29.
            ((MyObjectBuilder_CubeGrid)newEntity).CubeBlocks.RemoveAll(c => c.Min.X <= 28);

            _selectNewStructure = true;
            _dataModel.CollisionCorrectEntity(newEntity);
            _dataModel.AddEntity(newEntity);
            _selectNewStructure = false;
            IsBusy = false;
        }

        public bool Test4CanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0;
        }

        public void Test4Executed()
        {
            MirrorModel(false, [.. Selections]);
        }

        public bool Test5CanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count > 0;
        }

        public void Test5Executed()
        {
            ExplorerModel.TestDisplayRotation(Selections[0].DataModel as StructureCubeGridModel);
        }

        public bool Test6CanExecute()
        {
            return _dataModel.ActiveWorld is { IsValid: true } && Selections.Count == 1 &&
                   ((Selections[0].DataModel.ClassType == ClassType.Planet && Selections[0].DataModel.IsValid));
        }

        public void Test6Executed()
        {
            _dataModel.TestResize(Selections[0].DataModel as StructurePlanetModel);
        }

        #endregion

        #region Methods

        void Structures_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add: AddViewModel(e.NewItems?[0] as IStructureBase); break;
                case NotifyCollectionChangedAction.Remove: RemoveViewModel(e.OldItems?[0] as IStructureBase); break;
                case NotifyCollectionChangedAction.Reset: _structures.Clear(); break;
                case NotifyCollectionChangedAction.Replace: ReplaceViewModel(e.OldItems?[0] as IStructureBase); break;
                case NotifyCollectionChangedAction.Move: MoveViewModel(e.OldItems?[0] as IStructureBase); break;
                default:
                    break;
            }
        }

        private void AddViewModel(IStructureBase structureBase)
        {
            if (structureBase == null)
            {
                throw new ArgumentNullException(nameof(structureBase), "Structure base cannot be null.");
            }

            IStructureViewBase item = structureBase switch
            {
                StructureCharacterModel characterModel => new StructureCharacterViewModel(this, characterModel),
                StructureCubeGridModel cubeGridModel => new StructureCubeGridViewModel(this, cubeGridModel),
                StructurePlanetModel planetModel => new StructurePlanetViewModel(this, planetModel),
                StructureVoxelModel voxelModel => new StructureVoxelViewModel(this, voxelModel),
                StructureFloatingObjectModel floatingObjectModel => new StructureFloatingObjectViewModel(this, floatingObjectModel),
                StructureMeteorModel meteorModel => new StructureMeteorViewModel(this, meteorModel),
                StructureInventoryBagModel inventoryBagModel => new StructureInventoryBagViewModel(this, inventoryBagModel),
                StructureUnknownModel unknownModel => new StructureUnknownViewModel(this, unknownModel),
                _ => throw new NotImplementedException("As yet undefined ViewModel has been called.")
            };

            _structures.Add(item);
            _preSelectedStructure = item;

            if (_selectNewStructure)
            {
                SelectedStructure = item;
            }
        }

        /// <summary>
        /// Find and remove ViewModel, with the specied Model.
        /// Remove the Entity also.
        /// </summary>
        /// <param name="model"></param>
        private void RemoveViewModel(IStructureBase model)
        {
            IStructureViewBase viewModel = Structures.FirstOrDefault(s => s.DataModel == model);
            if (viewModel != null && _dataModel.RemoveEntity(model.EntityBase))
            {
                Structures.Remove(viewModel);
            }
        }

        private void ReplaceViewModel(IStructureBase model)
        {
            IStructureViewBase viewModel = Structures.FirstOrDefault(s => s.DataModel == model);
            if (viewModel != null)
            {
                _structures.Remove(viewModel);
                _structures.Add(viewModel);
            }
        }

        private void MoveViewModel(IStructureBase model)
        {
            IStructureViewBase viewModel = Structures.FirstOrDefault(s => s.DataModel == model);
            if (viewModel != null)
            {
                //remove an move the viewmodel
                int oldIndex = Structures.IndexOf(viewModel);
                int newIndex = oldIndex + 1;
                if (newIndex < Structures.Count)
                {
                    Structures.Move(oldIndex, newIndex);
                }
            }
        }

        public void DeleteModel(params IStructureViewBase[] viewModels)
        {
            if (viewModels == null || viewModels.Length == 0) return;

            int index = Structures.IndexOf(viewModels[0]);
            _ignoreUpdateSelection = true;

            foreach (IStructureViewBase viewModel in viewModels)
            {
                if (viewModel == null || !CanDelete(viewModel)) continue;

                viewModel.DataModel.CancelAsync();
                _dataModel.Structures.Remove(viewModel.DataModel);
            }

            _ignoreUpdateSelection = false;
            SelectNextStructure(ref index);
        }

        private static bool CanDelete(IStructureViewBase viewModel)
        {
            return viewModel switch
            {
                StructureCharacterViewModel characterViewModel => !characterViewModel.IsPlayer,
                StructureCubeGridViewModel cubeGridViewModel => !cubeGridViewModel.IsPiloted,
                _ => true
            };
        }

        private void SelectNextStructure(ref int index)
        {
            while (index >= Structures.Count)
            {
                index--;
            }

            if (index > -1)
            {
                SelectedStructure = Structures[index];
            }
        }

        public void OptimizeModel(params IStructureViewBase[] viewModels)
        {
            foreach (var viewModel in viewModels.OfType<StructureCubeGridViewModel>())
            {
                _dataModel.OptimizeModel(viewModel.DataModel as StructureCubeGridModel);
            }
        }

        public void MirrorModel(bool oddMirror, params IStructureViewBase[] viewModels)
        {
            foreach (var model in viewModels.OfType<StructureCubeGridViewModel>())
            {
                ((StructureCubeGridModel)model.DataModel).MirrorModel(true, false);
            }
        }

        private void RejoinShipModels(IStructureViewBase viewModel1, IStructureViewBase viewModel2)
        {
            StructureCubeGridViewModel ship1 = (StructureCubeGridViewModel)viewModel1;
            StructureCubeGridViewModel ship2 = (StructureCubeGridViewModel)viewModel2;

            _dataModel.RejoinBrokenShip((StructureCubeGridModel)ship1.DataModel, (StructureCubeGridModel)ship2.DataModel);

            // Delete ship2.
            DeleteModel(viewModel2);

            // Deleting ship2 will also ensure the removal of any duplicate UniqueIds.
            // Any overlapping blocks between the two, will automatically be removed by Space Engineers when the world is loaded.
            //incase it doesn't work i have implemeented
            //RemoveOverlappingBlocks(viewModel1, viewModel2);
        }


        public void RemoveOverlappingShipBlocks(params IStructureViewBase[] viewModels)
        {
            foreach (var viewModel in viewModels.OfType<StructureCubeGridViewModel>())
            {
                _dataModel.RemoveOverlappingBlocks(viewModel.DataModel as StructureCubeGridModel);
            }
        }
        // private void RemoveOverlappingBlocks(IStructureViewBase viewModel1, IStructureViewBase viewModel2)
        // {
        //     StructureCubeGridViewModel ship1 = (StructureCubeGridViewModel)viewModel1;
        //     StructureCubeGridViewModel ship2 = (StructureCubeGridViewModel)viewModel2;

        //     var blocks1 = (StructureCubeGridModel)ship1.DataModel;
        //     var blocks2 = (StructureCubeGridModel)ship2.DataModel;

        //     foreach (var block1 in blocks1)
        //     {
        //         if (blocks2.Any(b2 => b2.Position == block1.Position))
        //         {
        //             blocks1.Remove(block1);
        //         }
        //     }

        //     foreach (var block2 in blocks2)
        //     {
        //         if (blocks1.Any(b1 => b1.Position == block2.Position))
        //         {
        //             blocks2.Remove(block2);
        //         }
        //     }
        // }

        private void MergeShipPartModels(IStructureViewBase viewModel1, IStructureViewBase viewModel2)
        {
            StructureCubeGridViewModel ship1 = (StructureCubeGridViewModel)viewModel1;
            StructureCubeGridViewModel ship2 = (StructureCubeGridViewModel)viewModel2;

            if (_dataModel.MergeShipParts((StructureCubeGridModel)ship1.DataModel, (StructureCubeGridModel)ship2.DataModel))
            {
                // Delete ship2.
                DeleteModel(viewModel2);

                // Deleting ship2 will also ensure the removal of any duplicate UniqueIds.
                // Any overlapping blocks between the two, will automatically be removed by Space Engineers when the world is loaded.
                //sometimes that doesnt work
                //RemoveOverlappingBlocks(viewModel1, viewModel2);


                viewModel1.DataModel.UpdateGeneralFromEntityBase();
            }
        }

        private void RepairShips(IEnumerable<IStructureViewBase> structures)
        {
            foreach (var structure in structures)
            {
                if (structure.DataModel.ClassType == ClassType.SmallShip
                    || structure.DataModel.ClassType == ClassType.LargeShip
                    || structure.DataModel.ClassType == ClassType.LargeStation
                    || structure.DataModel.ClassType == ClassType.SmallStation)
                {
                    ((StructureCubeGridViewModel)structure).RepairObjectExecuted();
                }
            }
        }

        private void StopShips(IEnumerable<IStructureViewBase> structures)
        {
            foreach (var structure in structures)
            {
                if (structure.DataModel.ClassType == ClassType.SmallShip
                    || structure.DataModel.ClassType == ClassType.LargeShip
                    || structure.DataModel.ClassType == ClassType.LargeStation
                    || structure.DataModel.ClassType == ClassType.SmallStation)
                {
                    ((StructureCubeGridViewModel)structure).ResetVelocityExecuted();
                }
            }
        }

        private static void ConvertToShips(IEnumerable<IStructureViewBase> structures)
        {
            foreach (var structure in structures)
            {
                if (structure.DataModel.ClassType == ClassType.LargeStation
                    || structure.DataModel.ClassType == ClassType.SmallStation)
                {
                    ((StructureCubeGridViewModel)structure).ConvertToShipExecuted();
                }
            }
        }

        private static void ConvertToStations(IEnumerable<IStructureViewBase> structures)
        {
            foreach (IStructureViewBase structure in structures)
            {
                if (structure.DataModel.ClassType == ClassType.SmallShip
                    || structure.DataModel.ClassType == ClassType.LargeShip)
                {
                    ((StructureCubeGridViewModel)structure).ConvertToStationExecuted();
                }
            }
        }

        private static int SetInertiaTensor(IEnumerable<IStructureViewBase> structures, bool state)
        {
            int count = 0;
            foreach (var structure in structures)
            {
                if (structure.DataModel.ClassType == ClassType.SmallShip
                    || structure.DataModel.ClassType == ClassType.LargeShip)
                {
                    count += ((StructureCubeGridViewModel)structure).SetInertiaTensor(state);
                }
            }
            return count;
        }

        /// <inheritdoc />
        public string CreateUniqueVoxelStorageName(string originalFile)
        {
            return _dataModel.CreateUniqueVoxelStorageName(originalFile, null);
        }

        public string CreateUniqueVoxelStorageName(string originalFile, MyObjectBuilder_EntityBase[] additionalList)
        {
            return _dataModel.CreateUniqueVoxelStorageName(originalFile, additionalList);
        }

        public List<IStructureBase> GetIntersectingEntities(BoundingBoxD box)
        {
            return [.. _dataModel.Structures.Where(item => item.WorldAabb.Intersects(box))];
        }

        public void ImportSandboxObjectFromFile()
        {
            IOpenFileDialog openFileDialog = _openFileDialogFactory();
            openFileDialog.Filter = AppConstants.SandboxObjectImportFilter;
            openFileDialog.Title = Res.DialogImportSandboxObjectTitle;
            openFileDialog.Multiselect = true;

            // Open the dialog
            DialogResult result = _dialogService.ShowOpenFileDialog(this, openFileDialog);

            if (result == DialogResult.OK)
            {

                List<string> badfiles = _dataModel.LoadEntities(openFileDialog.FileNames);

                foreach (string fileName in badfiles)
                {
                    _dialogService.ShowMessageBox(this, string.Format(Res.ClsImportInvalid, Path.GetFileName(fileName)), Res.ClsImportTitleFailed, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        public void ExportSandboxObjectToFile(bool blankOwnerAndMedBays, params IStructureViewBase[] viewModels)
        {
            foreach (var viewModel in viewModels)
            {
                var structure = (StructureCharacterViewModel)viewModel;
                ISaveFileDialog saveFileDialog = _saveFileDialogFactory();
                string filter = AppConstants.SandboxObjectExportFilter;
                string displayName = null;
                string description = null;
                string partname = string.IsNullOrEmpty(displayName) ? structure.EntityId.ToString() : displayName.Replace("|", "_").Replace("\\", "_").Replace("/", "_"); ;
                ClassType classType = ClassType.Unknown;
                string title = null;
                string fileName = string.Format($"{classType}_{description}");
                string res = null;
                switch (viewModel)
                {

                    case StructureVoxelViewModel voxelStructure:
                        filter = AppConstants.VoxelFilter;
                        title = Res.DialogExportVoxelTitle;
                        fileName = $"{voxelStructure.Name}{Interop.Asteroids.MyVoxelMapBase.FileExtension.V2}";
                        break;
                    case IStructureBase when viewModel is StructureCharacterViewModel characterStructure &&
                        classType == characterStructure.ClassType && displayName == characterStructure.DisplayName &&
                        description == characterStructure.Description:

                    case IStructureBase when viewModel is StructureFloatingObjectViewModel floatingObjectStructure &&
                        classType == floatingObjectStructure.ClassType && displayName == floatingObjectStructure.DisplayName &&
                        description == floatingObjectStructure.Description:

                    case IStructureBase when viewModel is StructureMeteorViewModel meteorStructure &&
                        classType == meteorStructure.ClassType && displayName == meteorStructure.DisplayName
                        && description == meteorStructure.Description:

                        title = string.Format(Res.DialogExportSandboxObjectTitle, classType, displayName, description);
                        fileName = $"{classType}_{description}";
                        break;
                    case IStructureBase when viewModel is StructureCubeGridViewModel cubeGridStructure &&
                        classType == cubeGridStructure.ClassType:
                        fileName = $"{classType}_{cubeGridStructure.DisplayName}";
                        break;

                    case IStructureBase when viewModel is StructureInventoryBagViewModel && res == Res.ClsExportInventoryBag:
                    case IStructureBase when viewModel is StructurePlanetViewModel && res == Res.ClsExportPlanet:
                    case IStructureBase when viewModel is StructureUnknownViewModel && res == Res.ClsExportUnknown:
                        _dialogService.ShowMessageBox(this, res, Res.ClsExportTitleFailed, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        continue;
                    default:

                        continue;

                }

                saveFileDialog.Filter = filter;
                saveFileDialog.Title = title;
                saveFileDialog.FileName = fileName;
                saveFileDialog.OverwritePrompt = true;
                string FileName = saveFileDialog.FileName;
                var ent = structure.DataModel.EntityBase;
                string prefix = ent.EntityId.ToString();

                if (_dialogService.ShowSaveFileDialog(this, saveFileDialog) == DialogResult.OK)
                {
                    switch (viewModel)
                    {
                        case StructureCharacterViewModel characterStructure:
                            _dataModel.SaveEntity(characterStructure.DataModel.EntityBase, FileName);
                            break;

                        case StructureVoxelViewModel voxelStructure:
                            StructureVoxelModel asteroid = (StructureVoxelModel)voxelStructure.DataModel;
                            string sourceFile = asteroid.SourceVoxelFilePath ?? asteroid.VoxelFilePath;
                            using (var stream = File.Open(sourceFile, FileMode.Open, FileAccess.Read))
                            {
                                using var targetStream = File.Open(FileName, FileMode.Create, FileAccess.Write);
                                stream.CopyToAsync(targetStream);
                            }
                            break;

                        case StructureCubeGridViewModel cubeGridStructure:
                            MyObjectBuilder_CubeGrid cloneEntity = (MyObjectBuilder_CubeGrid)cubeGridStructure.DataModel.EntityBase.Clone();
                            if (blankOwnerAndMedBays)
                            {
                                ClearMedicalRoomAndOwners(cloneEntity);
                            }
                            cloneEntity.RemoveHierarchyCharacter();
                            _dataModel.SaveEntity(cloneEntity, FileName);
                            break;
                        case IStructureBase when viewModel is StructureInventoryBagViewModel inventoryBagViewModel &&
                            ent == inventoryBagViewModel.DataModel.EntityBase &&
                            prefix == "exportedInventoryBag" &&
                            res == Res.ClsExportInventoryBag:
                        case IStructureBase when viewModel is StructurePlanetViewModel planetViewModel &&
                            ent == planetViewModel.DataModel.EntityBase &&
                            prefix == "exportedPlanet" &&
                            res == Res.ClsExportPlanet:

                            string name = ent.EntityId.ToString();

                            ExportEntity(ent, $"{prefix}_", name, res);
                            break;
                        case StructureUnknownViewModel _:
                            _dialogService.ShowMessageBox(this, Res.ClsExportUnknown, Res.ClsExportTitleFailed, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                            break;
                        default:
                            _dialogService.ShowMessageBox(this, Res.ClsExportTitleFailed, Res.ClsExportTitleFailed, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                            break;

                    }
                }
            }
        }

        public void ExportEntity(MyObjectBuilder_EntityBase ent, string prefix, string entName, string res)
        {
            string fileName = ent.EntityId.ToString();

            string exportedFileName = $"{prefix}_{entName}{fileName}.xml";
            using (var stream = new FileStream(exportedFileName, FileMode.Create, FileAccess.Write))
            {
                MyObjectBuilderSerializerKeen.SerializeXML(stream, ent);
            }
            _dialogService.ShowMessageBox(this, res, Res.ClsExportTitleFailed, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }



        private void ClearMedicalRoomAndOwners(MyObjectBuilder_CubeGrid cloneEntity)
        {
            // Clear Medical room SteamId.
            cloneEntity.CubeBlocks
              .Where(c => c.TypeId == MOBTypeIds.MedicalRoom)
              .ToList()
              .ForEach(c => ((MyObjectBuilder_MedicalRoom)c).SteamUserId = 0);

            // Clear Owners.
            cloneEntity.CubeBlocks
              .ToList()
              .ForEach(c => { c.Owner = 0; c.ShareMode = MyOwnershipShareModeEnum.None; });
        }

        public void ExportPrefabObjectToFile(bool blankOwnerAndMedBays, params IStructureViewBase[] viewModels)
        {
            ISaveFileDialog saveFileDialog = _saveFileDialogFactory();
            saveFileDialog.Filter = AppConstants.PrefabObjectFilter;
            saveFileDialog.Title = Res.DialogExportPrefabObjectTitle;
            saveFileDialog.FileName = "export prefab.sbc";
            saveFileDialog.OverwritePrompt = true;

            if (_dialogService.ShowSaveFileDialog(this, saveFileDialog) == DialogResult.OK)
            {
                bool isBinaryFile = (Path.GetExtension(saveFileDialog.FileName) ?? string.Empty).EndsWith(SpaceEngineersConsts.ProtobuffersExtension, StringComparison.OrdinalIgnoreCase);

                MyObjectBuilder_Definitions definition = new()
                {
                    Prefabs = new MyObjectBuilder_PrefabDefinition[1]
                };
                MyObjectBuilder_PrefabDefinition prefab;
                prefab = new MyObjectBuilder_PrefabDefinition();
                prefab.Id.TypeId = new MyObjectBuilderType(typeof(MyObjectBuilder_PrefabDefinition));
                prefab.Id.SubtypeId = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);

                List<MyObjectBuilder_CubeGrid> grids = [];

                foreach (IStructureViewBase viewModel in viewModels)
                {
                    if (viewModel is StructureCubeGridViewModel)
                    {
                        MyObjectBuilder_CubeGrid cloneEntity = (MyObjectBuilder_CubeGrid)viewModel.DataModel.EntityBase.Clone();

                        if (blankOwnerAndMedBays)
                        {
                            // Call to ToArray() to force Linq to update the value.
                            // Clear Medical room SteamId.
                            cloneEntity.CubeBlocks.Where(c => c.TypeId == MOBTypeIds.MedicalRoom).Select(c => { ((MyObjectBuilder_MedicalRoom)c).SteamUserId = 0; return c; }).ToArray();
                            // Clear Owners.
                            cloneEntity.CubeBlocks.Select(c => { c.Owner = 0; c.ShareMode = MyOwnershipShareModeEnum.None; return c; }).ToArray();
                        }

                        // Remove all pilots.
                        cloneEntity.RemoveHierarchyCharacter();

                        grids.Add(cloneEntity);
                    }
                }

                prefab.CubeGrids = [.. grids];
                definition.Prefabs[0] = prefab;

                if (isBinaryFile)
                    SpaceEngineersApi.WriteSpaceEngineersFilePB(definition, saveFileDialog.FileName, false);
                else
                    SpaceEngineersApi.WriteSpaceEngineersFile(definition, saveFileDialog.FileName);
            }
        }

        public void ExportSpawnGroupObjectToFile(bool blankOwnerAndMedBays, params IStructureViewBase[] viewModels)
        {
            string defaultName = null;
            bool hasGrids = false;
            bool hasVoxels = false;

            foreach (IStructureViewBase viewModel in viewModels)
            {
                if (viewModel is StructureCubeGridViewModel)
                {
                    hasGrids = true;
                    defaultName = viewModel.DataModel.DisplayName;
                    break;
                }
            }

            foreach (IStructureViewBase viewModel in viewModels)
            {
                if (viewModel is StructureVoxelViewModel)
                {
                    hasVoxels = true;
                    if (defaultName == null)
                    {
                        defaultName = viewModel.DataModel.DisplayName;
                        break;
                    }
                }
            }

            defaultName ??= "";
            defaultName = defaultName.Replace(' ', '_') ?? string.Empty;
            ISaveFileDialog saveFileDialog = _saveFileDialogFactory();
            saveFileDialog.Filter = AppConstants.PrefabObjectFilter;
            saveFileDialog.Title = Res.DialogExportSpawnGroupObjectTitle;
            saveFileDialog.FileName = defaultName + ".sbc";
            saveFileDialog.OverwritePrompt = true;

            if (_dialogService.ShowSaveFileDialog(this, saveFileDialog) == DialogResult.OK)
            {
                string name = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                string directory = Path.GetDirectoryName(saveFileDialog.FileName);
                bool isBinaryFile = (Path.GetExtension(saveFileDialog.FileName) ?? string.Empty).EndsWith(SpaceEngineersConsts.ProtobuffersExtension, StringComparison.OrdinalIgnoreCase);

                MyObjectBuilder_Definitions prefabDefinition = new()
                {
                    Prefabs = new MyObjectBuilder_PrefabDefinition[1]
                };
                var prefab = new MyObjectBuilder_PrefabDefinition();
                prefab.Id.TypeId = new MyObjectBuilderType(typeof(MyObjectBuilder_PrefabDefinition));
                prefab.Id.SubtypeId = name;

                MyObjectBuilder_Definitions spawngroupDefinition = new()
                {
                    SpawnGroups = new MyObjectBuilder_SpawnGroupDefinition[1]
                };
                MyObjectBuilder_SpawnGroupDefinition spawngroup;
                spawngroup = new MyObjectBuilder_SpawnGroupDefinition();
                spawngroup.Id.TypeId = new MyObjectBuilderType(typeof(MyObjectBuilder_SpawnGroupDefinition));
                spawngroup.Id.SubtypeId = name;
                spawngroup.Icons = [@"Textures\GUI\Icons\Fake.dds"];
                spawngroup.IsEncounter = false;
                spawngroup.IsPirate = false;
                spawngroup.Frequency = 0.001f;

                Vector3 grid1Position = Vector3.Zero;
                bool isGrid1PositionSet = false;

                List<MyObjectBuilder_CubeGrid> grids = [];
                Vector3 minimum = Vector3.MaxValue;

                foreach (IStructureViewBase viewModel in viewModels)
                {
                    if (viewModel is StructureCubeGridViewModel)
                    {
                        MyObjectBuilder_CubeGrid cloneEntity = (MyObjectBuilder_CubeGrid)viewModel.DataModel.EntityBase.Clone();

                        if (!isGrid1PositionSet)
                        {
                            grid1Position = new Vector3(cloneEntity.PositionAndOrientation.Value.Position.X, cloneEntity.PositionAndOrientation.Value.Position.Y, cloneEntity.PositionAndOrientation.Value.Position.Z);
                            isGrid1PositionSet = true;
                        }

                        if (blankOwnerAndMedBays)
                        {
                            // Call to ToArray() to force Linq to update the value.
                            //use ClearMedicalRoomAndOwners instead
                            // Clear Medical room SteamId.
                            cloneEntity.CubeBlocks.Where(c => c.TypeId == MOBTypeIds.MedicalRoom).Select(c => { ((MyObjectBuilder_MedicalRoom)c).SteamUserId = 0; return c; }).ToArray();
                            // Clear Owners.
                            cloneEntity.CubeBlocks.Select(c => { c.Owner = 0; c.ShareMode = MyOwnershipShareModeEnum.None; return c; }).ToArray();
                        }

                        // Remove all pilots.
                        cloneEntity.RemoveHierarchyCharacter();

                        grids.Add(cloneEntity);

                        switch (minimum)
                        {
                            case Vector3 value when value.X > cloneEntity.PositionAndOrientation.Value.Position.X:
                                minimum.X = (float)cloneEntity.PositionAndOrientation.Value.Position.X;
                                break;
                            case Vector3 value when value.Y > cloneEntity.PositionAndOrientation.Value.Position.Y:
                                minimum.Y = (float)cloneEntity.PositionAndOrientation.Value.Position.Y;
                                break;
                            case Vector3 value when value.Z > cloneEntity.PositionAndOrientation.Value.Position.Z:
                                minimum.Z = (float)cloneEntity.PositionAndOrientation.Value.Position.Z;
                                break;

                        }

                    }
                    else if (viewModel is StructureVoxelViewModel voxelViewModel)
                    {
                        switch (minimum)
                        {
                            case Vector3 value when value.X > viewModel.DataModel.PositionAndOrientation.Value.Position.X:
                                minimum.X = (float)viewModel.DataModel.PositionAndOrientation.Value.Position.X;
                                break;
                            case Vector3 value when value.Y > viewModel.DataModel.PositionAndOrientation.Value.Position.Y:
                                minimum.Y = (float)viewModel.DataModel.PositionAndOrientation.Value.Position.Y;
                                break;
                            case Vector3 value when value.Z > viewModel.DataModel.PositionAndOrientation.Value.Position.Z:
                                minimum.Z = (float)viewModel.DataModel.PositionAndOrientation.Value.Position.Z;
                                break;
                        }


                    }
                }

                if (minimum == Vector3.MaxValue)
                    minimum = Vector3.Zero;

                prefab.CubeGrids = [.. grids];
                prefabDefinition.Prefabs[0] = prefab;

                List<MyObjectBuilder_SpawnGroupDefinition.SpawnGroupVoxel> voxels = [];

                foreach (IStructureViewBase viewModel in viewModels)
                {
                    if (viewModel is StructureVoxelViewModel)
                    {
                        Vector3 pos = new(viewModel.DataModel.PositionAndOrientation.Value.Position.X, viewModel.DataModel.PositionAndOrientation.Value.Position.Y, viewModel.DataModel.PositionAndOrientation.Value.Position.Z);

                        // This is to set up the position values for "spawnAtOrigin", used by the game.
                        // See Sandbox.Game.World.MyPrefabManager.CreateGridsFromPrefab()
                        // spawnAtOrigin is only used with the Position of the First grid in the Prefab list.
                        // This will affect the voxel Offsets in the SpawnGroup.
                        if (isGrid1PositionSet)
                        {
                            pos -= grid1Position;
                        }

                        voxels.Add(new MyObjectBuilder_SpawnGroupDefinition.SpawnGroupVoxel
                        {
                            StorageName = viewModel.DataModel.DisplayName,
                            Offset = pos
                        });

                        // copy files.
                        StructureVoxelModel voxel = (StructureVoxelModel)viewModel.DataModel;

                        // note, there aren't any checks for existing files here.
                        string destinationFile = Path.Combine(directory, viewModel.DataModel.DisplayName + ".vx2");

                        if (voxel.SourceVoxelFilePath != null && File.Exists(voxel.SourceVoxelFilePath))
                            File.Copy(voxel.SourceVoxelFilePath, destinationFile, true);
                        else
                            File.Copy(voxel.VoxelFilePath, destinationFile, true);
                    }
                }

                if (hasGrids)
                {
                    spawngroup.Prefabs = [
                        new()
                        {
                            SubtypeId = name,
                            Position = Vector3.Zero,
                            Speed = 0,
                        }
                ];

                    if (hasVoxels)
                        spawngroup.Prefabs[0].PlaceToGridOrigin = true;
                    if (isBinaryFile)
                        SpaceEngineersApi.WriteSpaceEngineersFilePB(prefabDefinition, saveFileDialog.FileName, false);
                    else
                        SpaceEngineersApi.WriteSpaceEngineersFile(prefabDefinition, saveFileDialog.FileName);
                }

                spawngroupDefinition.SpawnGroups[0] = spawngroup;
                if (voxels.Count > 0)
                    spawngroup.Voxels = [.. voxels];

                string spawnGroupFile = Path.Combine(directory, "SpawnGroup " + name + ".sbc");
                SpaceEngineersApi.WriteSpaceEngineersFile(spawngroupDefinition, spawnGroupFile);
            }
        }

        public void ExportBlueprintToFile(params IStructureViewBase[] viewModels)
        {
            string localBlueprintsFolder = null;
            if (string.IsNullOrEmpty(_dataModel.ActiveWorld.DataPath.BlueprintsPath))
            {
                // There is no blueprints under Dedicated Server, so cannot find the blueprint folder to save to.
                _dialogService.ShowMessageBox(this, Res.ErrorNoBlueprintPath, Res.ErrorNoBlueprintPathTitle, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Hand);
                return;
            }
            localBlueprintsFolder = Path.Combine(_dataModel.ActiveWorld.DataPath.BlueprintsPath, SpaceEngineersConsts.Folders.LocalBlueprintsSubFolder);

            BlueprintDialogModel model = new()
            {
                BlueprintName = viewModels?[0].DataModel.DisplayName ?? string.Empty,
            };
            model.Load(Res.WnBlueprintSaveDialogTitle, true, localBlueprintsFolder);
            BlueprintDialogViewModel loadVm = new(this, model, _dialogService);
            bool? result = _dialogService.ShowDialog<WindowBlueprintDialog>(this, loadVm);

            if (result == true)
            {
                MyObjectBuilder_Definitions blueprintDefinition = new()
                {
                    ShipBlueprints = new MyObjectBuilder_ShipBlueprintDefinition[1]
                };
                MyObjectBuilder_ShipBlueprintDefinition prefab;
                prefab = new MyObjectBuilder_ShipBlueprintDefinition();
                prefab.Id.TypeId = new MyObjectBuilderType(typeof(MyObjectBuilder_ShipBlueprintDefinition));
                prefab.Id.SubtypeId = model.BlueprintName;
                prefab.DisplayName = "SEToolbox Export";  // Appears as AuthorName in game for the highlighted blueprint.
                prefab.OwnerSteamId = ActiveWorld?.Checkpoint?.AllPlayersData?.Dictionary
                                                  .Where(p => p.Value != null).Select(p => p.Value.SteamID).FirstOrDefault() ?? 0;
                // 0 is the default value for the owner, so it will be set to the current player when loaded in game.
                MyObjectBuilder_Definitions spawngroupDefinition = new()
                {
                    SpawnGroups = new MyObjectBuilder_SpawnGroupDefinition[1]
                };

                Vector3D grid1Position = new(viewModels[0].DataModel.PositionAndOrientation.Value.Position.X, viewModels[0].DataModel.PositionAndOrientation.Value.Position.Y, viewModels[0].DataModel.PositionAndOrientation.Value.Position.Z);

                List<MyObjectBuilder_CubeGrid> grids = [];
                Vector3 minimum = Vector3.MaxValue;

                foreach (IStructureViewBase viewModel in viewModels)
                {
                    if (viewModel is StructureCubeGridViewModel)
                    {
                        MyObjectBuilder_CubeGrid cloneEntity = (MyObjectBuilder_CubeGrid)viewModel.DataModel.EntityBase.Clone();

                        // move offsets of all grids to origin, based on first selected grid.
                        MyPositionAndOrientation p = cloneEntity.PositionAndOrientation ?? new MyPositionAndOrientation();
                        cloneEntity.PositionAndOrientation = new MyPositionAndOrientation
                        {
                            Position = p.Position - grid1Position,
                            Forward = p.Forward,
                            Up = p.Up
                        };

                        // Call to ToArray() to force Linq to update the value.

                        // Clear BuiltBy.
                        cloneEntity.CubeBlocks.Select(c => { c.BuiltBy = 0; return c; }).ToArray();

                        // Remove all pilots.
                        cloneEntity.RemoveHierarchyCharacter();
                        grids.Add(cloneEntity);

                        minimum = Vector3.Min(minimum, new Vector3((float)cloneEntity.PositionAndOrientation.Value.Position.X,
                                                                  (float)cloneEntity.PositionAndOrientation.Value.Position.Y,
                                                                  (float)cloneEntity.PositionAndOrientation.Value.Position.Z));
                    }
                    if (viewModel is StructureVoxelViewModel { PositionAndOrientation: not null })
                    {
                        minimum = Vector3.Min(minimum, new Vector3((float)viewModel.DataModel.PositionAndOrientation.Value.Position.X,
                                                      (float)viewModel.DataModel.PositionAndOrientation.Value.Position.Y,
                                                      (float)viewModel.DataModel.PositionAndOrientation.Value.Position.Z));
                    }
                }

                if (minimum == Vector3.MaxValue)
                    minimum = Vector3.Zero;

                prefab.CubeGrids = [.. grids];
                new MyObjectBuilder_Definitions()
                {
                    ShipBlueprints = [prefab]
                };

                var blueprintPath = Path.Combine(localBlueprintsFolder, model.BlueprintName);
                if (!Directory.Exists(blueprintPath))
                    Directory.CreateDirectory(blueprintPath);

                SpaceEngineersApi.WriteSpaceEngineersFile(new MyObjectBuilder_Definitions()
                {
                    ShipBlueprints = new MyObjectBuilder_ShipBlueprintDefinition[1]
                }, Path.Combine(blueprintPath, "bp.sbc"));

                SpaceEngineersApi.WriteSpaceEngineersFilePB(new MyObjectBuilder_Definitions()
                {
                    ShipBlueprints = new MyObjectBuilder_ShipBlueprintDefinition[1]
                }, Path.Combine(blueprintPath, $"bp.sbc{SpaceEngineersConsts.ProtobuffersExtension}"), false);
            }
        }

        public void TestCalcCubesModel(params IStructureViewBase[] viewModels)
        {
            StringBuilder bld = new();

            foreach (StructureCubeGridViewModel viewModel in viewModels.OfType<StructureCubeGridViewModel>())
            {
                //var list = model.CubeGrid.CubeBlocks.Where(b => b.SubtypeName.Contains("Red") ||
                //    b.SubtypeName.Contains("Blue") ||
                //    b.SubtypeName.Contains("Green") ||
                //    b.SubtypeName.Contains("Yellow") ||
                //    b.SubtypeName.Contains("White") ||
                //    b.SubtypeName.Contains("Black")).ToArray();

                if (viewModel.DataModel is StructureCubeGridModel model)
                {
                    MyObjectBuilder_CubeBlock[] list = [.. model.CubeGrid.CubeBlocks.Where(b => b is MyObjectBuilder_Cockpit)];
                    //var list = model.CubeGrid.CubeBlocks.Where(b => b.SubtypeName.Contains("Conveyor")).ToArray();

                    foreach (MyObjectBuilder_CubeBlock b in list)
                    {
                        CubeType cubeType = CubeType.Exterior;

                        switch (b.SubtypeName)
                        {
                            case string name when name.Contains("ArmorSlope"):
                                {
                                    var keys = Modelling.CubeOrientations.Keys.Where(k => k.ToString().Contains("Slope")).ToArray();
                                    cubeType = Modelling.CubeOrientations.FirstOrDefault(c => keys.Contains(c.Key) && c.Value.Forward == b.BlockOrientation.Forward && c.Value.Up == b.BlockOrientation.Up).Key;
                                    break;
                                }
                            case string name when name.Contains("ArmorCornerInv"):
                                {
                                    var keys = Modelling.CubeOrientations.Keys.Where(k => k.ToString().Contains("InverseCorner")).ToArray();
                                    cubeType = Modelling.CubeOrientations.FirstOrDefault(c => keys.Contains(c.Key) && c.Value.Forward == b.BlockOrientation.Forward && c.Value.Up == b.BlockOrientation.Up).Key;
                                    break;
                                }
                            case string name when name.Contains("ArmorCorner"):
                                {
                                    var keys = Modelling.CubeOrientations.Keys.Where(k => k.ToString().Contains("NormalCorner")).ToArray();
                                    cubeType = Modelling.CubeOrientations.FirstOrDefault(c => keys.Contains(c.Key) && c.Value.Forward == b.BlockOrientation.Forward && c.Value.Up == b.BlockOrientation.Up).Key;
                                    break;
                                }
                        }

                        //SpaceEngineersApi.CubeOrientations

                        // XYZ= (7, 15, 3)   Orientation = (0, 0, 0, 1)  SmallBlockArmorSlopeBlue               CubeType.SlopeCenterBackBottom
                        // XYZ= (8, 14, 3)   Orientation = (1, 0, 0, 0)  SmallBlockArmorCornerInvBlue           CubeType.InverseCornerRightFrontTop
                        // XYZ= (8, 15, 3)   Orientation = (0, 0, -0.7071068, 0.7071068)  SmallBlockArmorCornerBlue     CubeType.NormalCornerLeftBackBottom

                        // XYZ= (13, 9, 3)   Orientation = (1, 0, 0, 0)  SmallBlockArmorCornerInvGreen          CubeType.InverseCornerRightFrontTop
                        // XYZ= (14, 8, 3)   Orientation = (0, 0, -0.7071068, 0.7071068)  SmallBlockArmorSlopeGreen     CubeType.SlopeLeftCenterBottom
                        // XYZ= (14, 9, 3)   Orientation = (0, 0, -0.7071068, 0.7071068)  SmallBlockArmorCornerGreen        CubeType.NormalCornerLeftBackBottom


                        bld.AppendFormat($"// XYZ= ({b.Min.X}, {b.Min.Y}, {b.Min.Z})   Orientation = ({b.BlockOrientation.Forward}, {b.BlockOrientation.Up})  {b.SubtypeName}    CubeType.{cubeType}\r\n");
                    }
                    SConsole.Write(bld.ToString());
                }
            }
        }

        public void CalcDistances()
        {
            _dataModel.CalcDistances();
        }

        private void UpdateLanguages()
        {
            List<LanguageModel> list = [];

            foreach (KeyValuePair<string, string> kvp in AppConstants.SupportedLanguages)
            {
                CultureInfo culture = CultureInfo.GetCultureInfoByIetfLanguageTag(kvp.Key);
                list.Add(new LanguageModel { IetfLanguageTag = culture.IetfLanguageTag, LanguageName = culture.DisplayName, NativeName = culture.NativeName, ImageName = kvp.Value });
            }

            _languages = [.. list];
        }

        public IStructureBase AddEntity(MyObjectBuilder_EntityBase entity)
        {
            return _dataModel.AddEntity(entity);
        }

        #endregion

        #region IDragable Interface

        Type IDropable.DataType
        {
            get => typeof(DataBaseViewModel);
        }

        void IDropable.Drop(object data, int index)
        {
            _dataModel.MergeData((IList<IStructureBase>)data);
        }

        #endregion

        #region IMainView Interface

        public bool ShowProgress
        {
            get => _dataModel.ShowProgress;
            set => _dataModel.ShowProgress = value;
        }

        public double Progress
        {
            get => _dataModel.Progress;
            set => _dataModel.Progress = value;
        }

        public TaskbarItemProgressState ProgressState
        {
            get => _dataModel.ProgressState;
            set => _dataModel.ProgressState = value;
        }

        public double ProgressValue
        {
            get => _dataModel.ProgressValue;
            set => _dataModel.ProgressValue = value;
        }

        public double MaximumProgress
        {
            get => _dataModel.MaximumProgress;
            set => _dataModel.MaximumProgress = value;
        }

        public void ResetProgress(double initial, double maximumProgress)
        {
            _dataModel.ResetProgress(initial, maximumProgress);
        }

        public void ClearProgress()
        {
            _dataModel.ClearProgress();
        }

        public void IncrementProgress()
        {
            _dataModel.IncrementProgress();
        }

        public MyObjectBuilder_Checkpoint Checkpoint
        {
            get => ActiveWorld.Checkpoint;
            set => ActiveWorld.Checkpoint = value;
        }

        /// <summary>
        /// Read in current 'world' color Palette.
        /// </summary>
        public int[] CreativeModeColors
        {
            get => _dataModel.CreativeModeColors;
            set => _dataModel.CreativeModeColors = value;
        }

        #endregion
    }

    internal class ScriptEditorViewModel(ExplorerModel dataModel)
    {
        private readonly ExplorerModel dataModel = dataModel;
    }
}