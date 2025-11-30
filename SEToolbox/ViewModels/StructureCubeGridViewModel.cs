using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using SEToolbox.Interfaces;
using SEToolbox.Interop;
using SEToolbox.Models;
using SEToolbox.Services;
using SEToolbox.Support;
using SEToolbox.Views;
using VRage.Game;
using Sandbox.Game.Entities.Inventory;
using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.ViewModels
{
    public class StructureCubeGridViewModel : StructureBaseViewModel<StructureCubeGridModel>
    {
        #region Fields

        private readonly IDialogService _dialogService;
        private readonly Func<IColorDialog> _colorDialogFactory;
        private Lazy<ObservableCollection<CubeItemViewModel>> _cubeList;
        private ObservableCollection<CubeItemViewModel> _selections;
        private CubeItemViewModel _selectedCubeItem;
        private string[] _filerView;

        #endregion

        #region Ctor

        public StructureCubeGridViewModel(BaseViewModel parentViewModel, StructureCubeGridModel dataModel)
            : this(parentViewModel, dataModel, ServiceLocator.Resolve<IDialogService>(), ServiceLocator.Resolve<IColorDialog>)
        {
            Selections = [];
        }

        public StructureCubeGridViewModel(BaseViewModel parentViewModel, StructureCubeGridModel dataModel, IDialogService dialogService, Func<IColorDialog> colorDialogFactory)
            : base(parentViewModel, dataModel)
        {
            Contract.Requires(dialogService != null);
            Contract.Requires(colorDialogFactory != null);

            _dialogService = dialogService;
            _colorDialogFactory = colorDialogFactory;

            CubeItemViewModel ViewModelCreator(CubeItemModel model) => new(this, model);
            ObservableCollection<CubeItemViewModel> CollectionCreator() => new ObservableViewModelCollection<CubeItemViewModel, CubeItemModel>(dataModel.CubeList, ViewModelCreator);
            _cubeList = new Lazy<ObservableCollection<CubeItemViewModel>>(CollectionCreator);

            DataModel.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "CubeList")
                {
                    CollectionCreator();
                    _cubeList = new Lazy<ObservableCollection<CubeItemViewModel>>(CollectionCreator);
                }
                // Will bubble property change events from the Model to the ViewModel.
                OnPropertyChanged(e.PropertyName);
            };
        }

        #endregion

        #region Command Properties

        public ICommand OptimizeObjectCommand => new DelegateCommand(OptimizeObjectExecuted, OptimizeObjectCanExecute);

        public ICommand FindOverlappingBlocksCommand => new DelegateCommand(FindOverlappingBlocksExecuted, FindOverlappingBlocksCanExecute);

        public ICommand RemoveOverlappingBlocksCommand => new DelegateCommand(RemoveOverlappingBlocksExecuted, RemoveOverlappingBlocksCanExecute);

        public ICommand MoveOverlappingBlocksCommand => new DelegateCommand(MoveOverlappingBlocksExecuted, MoveOverlappingBlocksCanExecute);

        public ICommand ToggleExclusionsCommand => new DelegateCommand(ToggleExcludedBlocksExecuted, ToggleExcludedBlocksCanExecute);

        public ICommand RepairObjectCommand => new DelegateCommand(RepairObjectExecuted, RepairObjectCanExecute);

        public ICommand ResetLinearVelocityCommand => new DelegateCommand(ResetLinearVelocityExecuted, ResetLinearVelocityCanExecute);

        public ICommand ResetRotationVelocityCommand => new DelegateCommand(ResetRotationVelocityExecuted, ResetRotationVelocityCanExecute);

        public ICommand ResetVelocityCommand => new DelegateCommand(ResetVelocityExecuted, ResetVelocityCanExecute);

        public ICommand ReverseVelocityCommand => new DelegateCommand(ReverseVelocityExecuted, ReverseVelocityCanExecute);

        public ICommand MaxVelocityAtPlayerCommand => new DelegateCommand(MaxVelocityAtPlayerExecuted, MaxVelocityAtPlayerCanExecute);

        public ICommand ConvertGridCommand => new DelegateCommand(ConvertGridExecuted, ConvertGridCanExecute);

        public ICommand ConvertGridToHeavyArmorCommand => new DelegateCommand(ConvertGridToHeavyArmorExecuted, ConvertGridToHeavyArmorCanExecute);

        public ICommand ConvertGridToLightArmorCommand => new DelegateCommand(ConvertGridToLightArmorExecuted, ConvertGridToLightArmorCanExecute);

        public ICommand ConvertGridFrameworkCommand => new DelegateCommand(ConvertGridFrameworkExecuted, ConvertGridFrameworkCanExecute);

        public ICommand ConvertGridToFrameworkCommand => new DelegateCommand<double>(ConvertGridToFrameworkExecuted, ConvertGridToFrameworkCanExecute);

        public ICommand ConvertToStationCommand => new DelegateCommand(ConvertToStationExecuted, ConvertToStationCanExecute);

        public ICommand ReorientStationCommand => new DelegateCommand(ReorientStationExecuted, ReorientStationCanExecute);

        public ICommand RotateStructureYawPositiveCommand => new DelegateCommand(RotateStructureYawPositiveExecuted, RotateStructureYawPositiveCanExecute);

        public ICommand RotateStructureYawNegativeCommand => new DelegateCommand(RotateStructureYawNegativeExecuted, RotateStructureYawNegativeCanExecute);

        public ICommand RotateStructurePitchPositiveCommand => new DelegateCommand(RotateStructurePitchPositiveExecuted, RotateStructurePitchPositiveCanExecute);

        public ICommand RotateStructurePitchNegativeCommand => new DelegateCommand(RotateStructurePitchNegativeExecuted, RotateStructurePitchNegativeCanExecute);

        public ICommand RotateStructureRollPositiveCommand => new DelegateCommand(RotateStructureRollPositiveExecuted, RotateStructureRollPositiveCanExecute);

        public ICommand RotateStructureRollNegativeCommand => new DelegateCommand(RotateStructureRollNegativeExecuted, RotateStructureRollNegativeCanExecute);

        public ICommand RotateCubesYawPositiveCommand => new DelegateCommand(RotateCubesYawPositiveExecuted, RotateCubesYawPositiveCanExecute);

        public ICommand RotateCubesYawNegativeCommand => new DelegateCommand(RotateCubesYawNegativeExecuted, RotateCubesYawNegativeCanExecute);

        public ICommand RotateCubesPitchPositiveCommand => new DelegateCommand(RotateCubesPitchPositiveExecuted, RotateCubesPitchPositiveCanExecute);

        public ICommand RotateCubesPitchNegativeCommand => new DelegateCommand(RotateCubesPitchNegativeExecuted, RotateCubesPitchNegativeCanExecute);

        public ICommand RotateCubesRollPositiveCommand => new DelegateCommand(RotateCubesRollPositiveExecuted, RotateCubesRollPositiveCanExecute);

        public ICommand RotateCubesRollNegativeCommand => new DelegateCommand(RotateCubesRollNegativeExecuted, RotateCubesRollNegativeCanExecute);

        public ICommand ConvertToShipCommand => new DelegateCommand(ConvertToShipExecuted, ConvertToShipCanExecute);

        public ICommand ConvertToCornerArmorCommand => new DelegateCommand(ConvertToCornerArmorExecuted, ConvertToCornerArmorCanExecute);

        public ICommand ConvertToRoundArmorCommand => new DelegateCommand(ConvertToRoundArmorExecuted, ConvertToRoundArmorCanExecute);

        public ICommand MirrorStructureByPlaneCommand => new DelegateCommand(MirrorStructureByPlaneExecuted, MirrorStructureByPlaneCanExecute);

        public ICommand MirrorStructureGuessOddCommand => new DelegateCommand(MirrorStructureGuessOddExecuted, MirrorStructureGuessOddCanExecute);

        public ICommand MirrorStructureGuessEvenCommand => new DelegateCommand(MirrorStructureGuessEvenExecuted, MirrorStructureGuessEvenCanExecute);

        public ICommand CopyDetailCommand => new DelegateCommand(CopyDetailExecuted, CopyDetailCanExecute);

        public ICommand FilterStartCommand => new DelegateCommand(FilterStartExecuted, FilterStartCanExecute);

        public ICommand FilterTabStartCommand => new DelegateCommand(FilterTabStartExecuted, FilterTabStartCanExecute);

        public ICommand FilterClearCommand => new DelegateCommand(FilterClearExecuted, FilterClearCanExecute);

        public ICommand DeleteCubesCommand => new DelegateCommand(DeleteCubesExecuted, DeleteCubesCanExecute);

        public ICommand ConvertCubesCommand => new DelegateCommand(ConvertCubesExecuted, ConvertCubesCanExecute);

        public ICommand ConvertCubeToHeavyArmorCommand => new DelegateCommand(ConvertCubeToHeavyArmorExecuted, ConvertCubeToHeavyArmorCanExecute);

        public ICommand ConvertCubeToLightArmorCommand => new DelegateCommand(ConvertCubeToLightArmorExecuted, ConvertCubeToLightArmorCanExecute);

        public ICommand ConvertCubeToFrameworkDialogCommand => new DelegateCommand(ConvertCubeToFrameworkDialogExecuted, ConvertCubeToFrameworkDialogCanExecute);

        public ICommand ConvertCubeToFrameworkCommand => new DelegateCommand<double>(ConvertCubeToFrameworkExecuted, ConvertCubeToFrameworkCanExecute);

        public ICommand ReplaceCubesCommand => new DelegateCommand(ReplaceCubesExecuted, ReplaceCubesCanExecute);

        public ICommand ColorCubesCommand => new DelegateCommand(ColorCubesExecuted, ColorCubesCanExecute);

        public ICommand SetOwnerCommand => new DelegateCommand(SetOwnerExecuted, SetOwnerCanExecute);

        public ICommand SetBuiltByCommand => new DelegateCommand(SetBuiltByExecuted, SetBuiltByCanExecute);

        #endregion

        #region Properties

        public ObservableCollection<CubeItemViewModel> CubeList
        {
            get => _cubeList.Value;
        }

        public ObservableCollection<CubeItemViewModel> Selections
        {
            get => _selections;
            set => SetProperty(ref _selections, value, nameof(Selections));
        }

        public CubeItemViewModel SelectedCubeItem
        {
            get => _selectedCubeItem;
            set => SetProperty(ref _selectedCubeItem, value, nameof(SelectedCubeItem));
        }

        protected new StructureCubeGridModel DataModel
        {
            get => base.DataModel as StructureCubeGridModel;
        }

        public bool ToggleExcludedBlocks //why did i put this here??
        {
            get => DataModel.ToggleExcludedBlocks;
            set => SetProperty(DataModel.ToggleExcludedBlocks, value, () => 
            { 
                MainViewModel.IsModified = true;
            }, nameof(ToggleExcludedBlocks));
          
        }

        public bool IsDamaged
        {
            get => DataModel.IsDamaged;
        }

        public int DamageCount
        {
            get => DataModel.DamageCount;
        }

        public MyCubeSize GridSize
        {
            get => DataModel.GridSize;
            set => DataModel.GridSize = value;
        }

        public bool IsStatic
        {
            get => DataModel.IsStatic;
            set => DataModel.IsStatic = value;
        }

        public bool Dampeners
        {
            get => DataModel.Dampeners;

            set => SetValue(DataModel.Dampeners, value, () =>
                   MainViewModel.IsModified = true); 
            
        }

        public bool Destructible
        {
            get => DataModel.Destructible;
            set => SetValue(DataModel.Destructible, value, () =>
                   MainViewModel.IsModified = true); 
        }

        public Point3D Min
        {
            get => DataModel.Min;
            set => DataModel.Min = value;
        }

        public Point3D Max
        {
            get => DataModel.Max;
            set => DataModel.Max = value;
        }

        public Vector3D Scale
        {
            get => DataModel.Scale;
            set => DataModel.Scale = value;
        }

        public BindableSize3DModel Size
        {
            get =>  new(DataModel.Size);
        }

        public BindableVector3DModel Center
        {
            get => new(DataModel.Center);
            set => DataModel.Center = value.ToVector3();
        }

        public bool IsPiloted
        {
            get => DataModel.IsPiloted;
        }

        public override double LinearVelocity
        {
            get => DataModel.LinearVelocity;
        }

        public double AngularVelocity
        {
            get => DataModel.AngularVelocity;
        }

        public TimeSpan TimeToProduce
        {
            get => DataModel.TimeToProduce;
            set => DataModel.TimeToProduce = value;
        }

        public int PCUToProduce
        {
            get => DataModel.PCUToProduce;
            set => DataModel.PCUToProduce = value;
        }

        public string CockpitOrientation
        {
            get => DataModel.CockpitOrientation;
        }

        public List<CubeAssetModel> CubeAssets
        {
            get => DataModel.CubeAssets;
            set => DataModel.CubeAssets = value;
        }

        public List<CubeAssetModel> ComponentAssets
        {
            get => DataModel.ComponentAssets;
            set => DataModel.ComponentAssets = value;
        }

        public List<OreAssetModel> IngotAssets
        {
            get => DataModel.IngotAssets;
            set => DataModel.IngotAssets = value;
        }

        public List<OreAssetModel> OreAssets
        {
            get => DataModel.OreAssets;
            set => DataModel.OreAssets = value;
        }

        public string ActiveComponentFilter
        {
            get => DataModel.ActiveComponentFilter;
            set => DataModel.ActiveComponentFilter = value;
        }

        public string ComponentFilter
        {
            get => DataModel.ComponentFilter;
            set => DataModel.ComponentFilter = value;
        }

        public bool IsConstructionNotReady
        {
            get => DataModel.IsConstructionNotReady;
            set => DataModel.IsConstructionNotReady = value;
        }

        public bool IsSubsSystemNotReady
        {
            get => DataModel.IsSubsSystemNotReady;
            set => DataModel.IsSubsSystemNotReady = value;
        }

        #endregion

        #region Command Methods

        public bool OptimizeObjectCanExecute()
        {
            return true;
        }

        public void OptimizeObjectExecuted()
        {
            MainViewModel.OptimizeModel(this);
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
        }

        public static bool FindOverlappingBlocksCanExecute() => true;

        public void FindOverlappingBlocksExecuted()
        {
            MainViewModel.FindOverlappingBlocks(this);
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
        }

        public static bool RemoveOverlappingBlocksCanExecute()
        {
            return true;
        }

        public void RemoveOverlappingBlocksExecuted()
        {
            MainViewModel.RemoveOverlappingBlocks(this);
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
            MainViewModel.IsModified = true;
        }

        public static bool MoveOverlappingBlocksCanExecute()
        {
            return true;
        }

        public void MoveOverlappingBlocksExecuted()
        {
            MainViewModel.MoveOverlappingBlocks(this);
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
            MainViewModel.IsModified = true;
        }

        public bool ToggleExcludedBlocksCanExecute()
        {
            return DataModel != null && MainViewModel != null;
        }
        // Ensure the DataModel is valid and exclusions can be toggled

        public void ToggleExcludedBlocksExecuted()
        {
            if (MainViewModel.ToggleExcludedBlocks(this))
            {
                IsSubsSystemNotReady = true;
                DataModel.InitializeAsync();
            }
        }

        public bool RepairObjectCanExecute()
        {
            return IsDamaged;
        }

        public void RepairObjectExecuted()
        {
            DataModel.RepairAllDamage();
            MainViewModel.IsModified = true;
        }

        public bool ResetLinearVelocityCanExecute()
        {
            return DataModel.LinearVelocity != 0f;
        }

        public void ResetLinearVelocityExecuted()
        {
            DataModel.ResetLinearVelocity();
            MainViewModel.IsModified = true;
        }

        public bool ResetRotationVelocityCanExecute()
        {
            return DataModel.AngularVelocity != 0f;
        }

        public void ResetRotationVelocityExecuted()
        {
            DataModel.ResetRotationVelocity();
            MainViewModel.IsModified = true;
        }

        public bool ResetVelocityCanExecute()
        {
            return DataModel.LinearVelocity != 0f || DataModel.AngularVelocity != 0f;
        }

        public void ResetVelocityExecuted()
        {
            DataModel.ResetVelocity();
            MainViewModel.IsModified = true;
        }

        public bool ReverseVelocityCanExecute()
        {
            return DataModel.LinearVelocity != 0f || DataModel.AngularVelocity != 0f;
        }

        public void ReverseVelocityExecuted()
        {
            DataModel.ReverseVelocity();
            MainViewModel.IsModified = true;
        }

        public bool MaxVelocityAtPlayerCanExecute()
        {
            return MainViewModel.ThePlayerCharacter != null;
        }

        public void MaxVelocityAtPlayerExecuted()
        {
            var position = MainViewModel.ThePlayerCharacter.PositionAndOrientation.Value.Position;
            DataModel.MaxVelocityAtPlayer(position);
            MainViewModel.IsModified = true;
        }

        public bool ConvertGridCanExecute()
        {
            return true;
        }

        public static void ConvertGridExecuted()
        {
            //placeholder
        }

        public bool ConvertGridToHeavyArmorCanExecute()
        {
            return true;
        }

        public void ConvertGridToHeavyArmorExecuted()
        {
            if (DataModel.ConvertFromLightToHeavyArmor())
            {
                MainViewModel.IsModified = true;
            }
        }

        public bool ConvertCubeToHeavyArmorCanExecute()
        {
            return SelectedCubeItem != null;
        }

        public void ConvertCubeToHeavyArmorExecuted()
        {
            MainViewModel.IsBusy = true;
            MainViewModel.ResetProgress(0, Selections.Count);

            string contentPath = ToolboxUpdater.GetApplicationContentPath();
            bool changes = false;
            foreach (CubeItemViewModel cubeVm in Selections)
            {
                MainViewModel.Progress++;
                if (cubeVm.ConvertFromLightToHeavyArmor())
                {
                    changes = true;

                    var index = DataModel.CubeGrid.CubeBlocks.IndexOf(cubeVm.Cube);
                    var cubeDefinition = SpaceEngineersApi.GetCubeDefinition(cubeVm.Cube.TypeId, GridSize, cubeVm.Cube.SubtypeName);
                    var newCube = cubeVm.CreateCube(cubeVm.Cube.TypeId, cubeVm.Cube.SubtypeName, cubeDefinition);
                    cubeVm.TextureFile = (cubeDefinition.Icons == null || cubeDefinition.Icons.First() == null) ? null : SpaceEngineersCore.GetDataPathOrDefault(cubeDefinition.Icons.First(), Path.Combine(contentPath, cubeDefinition.Icons.First()));

                    DataModel.CubeGrid.CubeBlocks.RemoveAt(index);
                    DataModel.CubeGrid.CubeBlocks.Insert(index, newCube);
                }
            }

            MainViewModel.ClearProgress();
            if (changes)
                MainViewModel.IsModified = true;
            MainViewModel.IsBusy = false;
        }

        public bool ConvertGridToLightArmorCanExecute()
        {
            return true;
        }

        public void ConvertGridToLightArmorExecuted()
        {
            if (DataModel.ConvertFromHeavyToLightArmor())
            {
                MainViewModel.IsModified = true;
            }
        }

        public bool ConvertCubeToLightArmorCanExecute()
        {
            return SelectedCubeItem != null;
        }

        public void ConvertCubeToLightArmorExecuted()
        {
            MainViewModel.IsBusy = true;
            MainViewModel.ResetProgress(0, Selections.Count);

            string contentPath = ToolboxUpdater.GetApplicationContentPath();
            bool changes = false;
            foreach (CubeItemViewModel cubeVm in Selections)
            {
                MainViewModel.Progress++;
                if (cubeVm.ConvertFromHeavyToLightArmor())
                {
                    changes = true;

                    var index = DataModel.CubeGrid.CubeBlocks.IndexOf(cubeVm.Cube);
                    var cubeDefinition = SpaceEngineersApi.GetCubeDefinition(cubeVm.Cube.TypeId, GridSize, cubeVm.Cube.SubtypeName);
                    var newCube = cubeVm.CreateCube(cubeVm.Cube.TypeId, cubeVm.Cube.SubtypeName, cubeDefinition);
                    cubeVm.TextureFile = (cubeDefinition.Icons == null || cubeDefinition.Icons.First() == null) ? null : SpaceEngineersCore.GetDataPathOrDefault(cubeDefinition.Icons.First(), Path.Combine(contentPath, cubeDefinition.Icons.First()));

                    DataModel.CubeGrid.CubeBlocks.RemoveAt(index);
                    DataModel.CubeGrid.CubeBlocks.Insert(index, newCube);
                }
            }

            MainViewModel.ClearProgress();
            if (changes)
                MainViewModel.IsModified = true;
            MainViewModel.IsBusy = false;
        }

        public bool ConvertGridFrameworkCanExecute()
        {
            return true;
        }

        public static void ConvertGridFrameworkExecuted()
        {
            // placeholder for menu only.
        }

        public bool ConvertGridToFrameworkCanExecute(double value)
        {
            return true;
        }

        public void ConvertGridToFrameworkExecuted(double value)
        {
            DataModel.ConvertToFramework((float)value);
            MainViewModel.IsModified = true;
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
        }

        public bool ConvertCubeToFrameworkCanExecute(double value)
        {
            return SelectedCubeItem != null;
        }

        public void ConvertCubeToFrameworkExecuted(double value)
        {
            MainViewModel.IsBusy = true;
            MainViewModel.ResetProgress(0, Selections.Count);

            foreach (var cube in Selections)
            {
                MainViewModel.Progress++;
                cube.UpdateBuildPercent(value);
            }

            MainViewModel.ClearProgress();
            MainViewModel.IsModified = true;
            MainViewModel.IsBusy = false;
        }

        public bool ConvertToStationCanExecute()
        {
            return !DataModel.IsStatic;
        }

        public void ConvertToStationExecuted()
        {
            DataModel.ConvertToStation();
            MainViewModel.IsModified = true;
        }

        public int SetInertiaTensor(bool state)
        {
            int count = DataModel.SetInertiaTensor(state);
            if (count > 0)
                MainViewModel.IsModified = true;
            return count;
        }

        public bool ReorientStationCanExecute()
        {
            return true;
        }

        public void ReorientStationExecuted()
        {
            DataModel.ReorientStation();
            MainViewModel.IsModified = true;
        }

        public bool RotateStructureYawPositiveCanExecute()
        {
            return true;
        }

        public void RotateStructurePitchPositiveExecuted()
        {
            // +90 around X
            DataModel.RotateStructure(VRageMath.Quaternion.CreateFromYawPitchRoll(0, VRageMath.MathHelper.PiOver2, 0));
            MainViewModel.IsModified = true;
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
        }

        public bool RotateStructurePitchNegativeCanExecute()
        {
            return true;
        }

        public void RotateStructurePitchNegativeExecuted()
        {
            // -90 around X
            DataModel.RotateStructure(VRageMath.Quaternion.CreateFromYawPitchRoll(0, -VRageMath.MathHelper.PiOver2, 0));
            MainViewModel.IsModified = true;
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
        }

        public bool RotateStructureRollPositiveCanExecute()
        {
            return true;
        }

        public void RotateStructureYawPositiveExecuted()
        {
            // +90 around Y
            DataModel.RotateStructure(VRageMath.Quaternion.CreateFromYawPitchRoll(VRageMath.MathHelper.PiOver2, 0, 0));
            MainViewModel.IsModified = true;
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
        }

        public bool RotateStructureYawNegativeCanExecute()
        {
            return true;
        }

        public void RotateStructureYawNegativeExecuted()
        {
            // -90 around Y
            DataModel.RotateStructure(VRageMath.Quaternion.CreateFromYawPitchRoll(-VRageMath.MathHelper.PiOver2, 0, 0));
            MainViewModel.IsModified = true;
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
        }

        public bool RotateStructurePitchPositiveCanExecute()
        {
            return true;
        }

        public void RotateStructureRollPositiveExecuted()
        {
            // +90 around Z
            DataModel.RotateStructure(VRageMath.Quaternion.CreateFromYawPitchRoll(0, 0, VRageMath.MathHelper.PiOver2));
            MainViewModel.IsModified = true;
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
        }

        public bool RotateStructureRollNegativeCanExecute()
        {
            return true;
        }

        public void RotateStructureRollNegativeExecuted()
        {
            // -90 around Z
            DataModel.RotateStructure(VRageMath.Quaternion.CreateFromYawPitchRoll(0, 0, -VRageMath.MathHelper.PiOver2));
            MainViewModel.IsModified = true;
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
        }

        public bool RotateCubesYawPositiveCanExecute()
        {
            return true;
        }

        public void RotateCubesPitchPositiveExecuted()
        {
            // +90 around X
            DataModel.RotateCubes(VRageMath.Quaternion.CreateFromYawPitchRoll(0, VRageMath.MathHelper.PiOver2, 0));
            MainViewModel.IsModified = true;
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
        }

        public bool RotateCubesPitchNegativeCanExecute()
        {
            return true;
        }

        public void RotateCubesPitchNegativeExecuted()
        {
            // -90 around X
            DataModel.RotateCubes(VRageMath.Quaternion.CreateFromYawPitchRoll(0, -VRageMath.MathHelper.PiOver2, 0));
            MainViewModel.IsModified = true;
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
        }

        public bool RotateCubesRollPositiveCanExecute()
        {
            return true;
        }

        public void RotateCubesYawPositiveExecuted()
        {
            // +90 around Y
            DataModel.RotateCubes(VRageMath.Quaternion.CreateFromYawPitchRoll(VRageMath.MathHelper.PiOver2, 0, 0));
            MainViewModel.IsModified = true;
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
        }

        public bool RotateCubesYawNegativeCanExecute()
        {
            return true;
        }

        public void RotateCubesYawNegativeExecuted()
        {
            // -90 around Y
            DataModel.RotateCubes(VRageMath.Quaternion.CreateFromYawPitchRoll(-VRageMath.MathHelper.PiOver2, 0, 0));
            MainViewModel.IsModified = true;
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
        }

        public bool RotateCubesPitchPositiveCanExecute()
        {
            return true;
        }

        public void RotateCubesRollPositiveExecuted()
        {
            // +90 around Z
            DataModel.RotateCubes(VRageMath.Quaternion.CreateFromYawPitchRoll(0, 0, VRageMath.MathHelper.PiOver2));
            MainViewModel.IsModified = true;
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
        }

        public bool RotateCubesRollNegativeCanExecute()
        {
            return true;
        }

        public void RotateCubesRollNegativeExecuted()
        {
            // -90 around Z
            DataModel.RotateCubes(VRageMath.Quaternion.CreateFromYawPitchRoll(0, 0, -VRageMath.MathHelper.PiOver2));
            MainViewModel.IsModified = true;
            IsSubsSystemNotReady = true;
            DataModel.InitializeAsync();
        }

        public bool ConvertToShipCanExecute()
        {
            return DataModel.IsStatic;
        }

        public void ConvertToShipExecuted()
        {
            DataModel.ConvertToShip();
            MainViewModel.IsModified = true;
        }

        public bool ConvertToCornerArmorCanExecute()
        {
            return DataModel.GridSize == MyCubeSize.Large;
        }

        public void ConvertToCornerArmorExecuted()
        {
            if (DataModel.ConvertToCornerArmor())
            {
                MainViewModel.IsModified = true;
            }
        }

        public bool ConvertToRoundArmorCanExecute()
        {
            return DataModel.GridSize == MyCubeSize.Large;
        }

        public void ConvertToRoundArmorExecuted()
        {
            if (DataModel.ConvertToRoundArmor())
            {
                MainViewModel.IsModified = true;
            }
        }

        public bool MirrorStructureByPlaneCanExecute()
        {
            return true;
        }

        public void MirrorStructureByPlaneExecuted()
        {
            MainViewModel.IsBusy = true;
            if (DataModel.MirrorModel(true, false))
            {
                MainViewModel.IsModified = true;
                IsSubsSystemNotReady = true;
                IsConstructionNotReady = true;
                DataModel.InitializeAsync();
            }
            MainViewModel.IsBusy = false;
        }

        public bool MirrorStructureGuessOddCanExecute()
        {
            return true;
        }

        public void MirrorStructureGuessOddExecuted()
        {
            MainViewModel.IsBusy = true;
            if (DataModel.MirrorModel(false, true))
            {
                MainViewModel.IsModified = true;
                IsSubsSystemNotReady = true;
                IsConstructionNotReady = true;
                DataModel.InitializeAsync();
            }
            MainViewModel.IsBusy = false;
        }

        public bool MirrorStructureGuessEvenCanExecute()
        {
            return true;
        }

        public void MirrorStructureGuessEvenExecuted()
        {
            MainViewModel.IsBusy = true;
            if (DataModel.MirrorModel(false, false))
            {
                MainViewModel.IsModified = true;
                IsSubsSystemNotReady = true;
                IsConstructionNotReady = true;
                DataModel.InitializeAsync();
            }
            MainViewModel.IsBusy = false;
        }

        public bool CopyDetailCanExecute()
        {
            return true;
        }

        public void CopyDetailExecuted()
        {
            StringBuilder cubes = new();
            if (CubeAssets != null)
            {
                foreach (CubeAssetModel mat in CubeAssets)
                {
                    cubes.AppendFormat($"{mat.FriendlyName}\t{mat.Count:#,##0}\t{mat.Mass:#,##0.00} {Res.GlobalSIMassKilogram}\t{mat.Time:hh\\:mm\\:ss\\.ff}\t{mat.PCU:#,##0.00}\r\n");
                }

                StringBuilder components = new();
                if (ComponentAssets != null)
                {
                    foreach (CubeAssetModel mat in ComponentAssets)
                    {
                        components.AppendFormat($"{mat.FriendlyName}\t{mat.Mass:#,##0.00} {Res.GlobalSIMassKilogram}\t{mat.Volume:#,##0.00} {Res.GlobalSIMassKilogram}\r\n");
                    }
                }

                StringBuilder ingots = new();
                if (IngotAssets != null)
                {
                    foreach (OreAssetModel mat in IngotAssets)
                    {
                        ingots.AppendFormat($"{mat.FriendlyName}\t{mat.Amount:#,##0}\t{mat.Mass:#,##0.00} {Res.GlobalSIMassKilogram}\t{mat.Volume:#,##0.00} {Res.GlobalSIMassKilogram}\r\n");
                    }
                }

                StringBuilder ores = new();
                if (OreAssets != null)
                {
                    foreach (OreAssetModel mat in OreAssets)
                    {
                        ores.AppendFormat($"{mat.FriendlyName}\t{mat.Amount:#,##0}\t{mat.Mass:#,##0.00} {Res.GlobalSIMassKilogram}\t{mat.Volume:#,##0.00} {Res.GlobalSIMassKilogram}\r\n");
                    }
                }

                string detail = string.Format(Properties.Resources.CtlCubeDetail,
                    DisplayName,
                    ClassType,
                    IsPiloted,
                    DamageCount,
                    LinearVelocity,
                    PlayerDistance,
                    Scale.X, Scale.Y, Scale.Z,
                    Size.Width, Size.Height, Size.Depth,
                    Mass,
                    BlockCount,
                    PositionAndOrientation?.Position.X ?? 0d,
                    PositionAndOrientation?.Position.Y ?? 0d,
                    PositionAndOrientation?.Position.Z ?? 0d,
                    Center.X,
                    Center.Y,
                    Center.Z,
                    PCUToProduce,
                    TimeToProduce,
                    cubes.ToString(),
                    components.ToString(),
                    ingots.ToString(),
                    ores.ToString());

                try
                {
                    Clipboard.Clear();
                    Clipboard.SetText(detail);
                }
                catch
                {
                    // Ignore exception which may be generated by a Remote desktop session where Clipboard access has not been granted.
                }
            }
        }

        public bool FilterStartCanExecute()
        {
            return ActiveComponentFilter != ComponentFilter;
        }

        public void FilterStartExecuted()
        {
            ActiveComponentFilter = ComponentFilter;
            ApplyCubeFilter();
        }

        public bool FilterTabStartCanExecute()
        {
            return true;
        }

        public void FilterTabStartExecuted()
        {
            ActiveComponentFilter = ComponentFilter;
            ApplyCubeFilter();
            FrameworkExtension.FocusedElementMoveFocus();
        }

        public bool FilterClearCanExecute()
        {
            return !string.IsNullOrEmpty(ComponentFilter);
        }

        public void FilterClearExecuted()
        {
            ComponentFilter = string.Empty;
            ActiveComponentFilter = ComponentFilter;
            ApplyCubeFilter();
        }

        public bool DeleteCubesCanExecute()
        {
            return SelectedCubeItem != null;
        }

        public void DeleteCubesExecuted()
        {
            IsBusy = true;

            MainViewModel.ResetProgress(0, Selections.Count);

            while (Selections.Count > 0)
            {
                MainViewModel.Progress++;
                var cube = Selections[0];
                if (DataModel.CubeGrid.CubeBlocks.Remove(cube.Cube))
                    DataModel.CubeList.Remove(cube.DataModel);
            }

            MainViewModel.ClearProgress();
            IsBusy = false;
        }

        public bool ConvertCubesCanExecute()
        {
            return SelectedCubeItem != null;
        }

        public static void ConvertCubesExecuted()
        {
            //placeholder
        }

        public bool ReplaceCubesCanExecute()
        {
            return SelectedCubeItem != null;
        }

        public void ReplaceCubesExecuted()
        {
            SelectCubeModel model = new();
            SelectCubeViewModel loadVm = new(this, model);
            model.Load(GridSize, SelectedCubeItem.Cube.TypeId, SelectedCubeItem.SubtypeId);
            bool? result = _dialogService.ShowDialog<WindowSelectCube>(this, loadVm);
            if (result == true)
            {
                MainViewModel.IsBusy = true;
                string contentPath = ToolboxUpdater.GetApplicationContentPath();
                bool change = false;
                MainViewModel.ResetProgress(0, Selections.Count);

                foreach (CubeItemViewModel cube in Selections)
                {
                    MainViewModel.Progress++;
                    if (cube.TypeId != model.CubeItem.TypeId || cube.SubtypeId != model.CubeItem.SubtypeId)
                    {
                        int index = DataModel.CubeGrid.CubeBlocks.IndexOf(cube.Cube);
                        DataModel.CubeGrid.CubeBlocks.RemoveAt(index);

                        Sandbox.Definitions.MyCubeBlockDefinition cubeDefinition = SpaceEngineersApi.GetCubeDefinition(model.CubeItem.TypeId, GridSize, model.CubeItem.SubtypeId);
                        MyObjectBuilder_CubeBlock newCube = cube.CreateCube(model.CubeItem.TypeId, model.CubeItem.SubtypeId, cubeDefinition);
                        cube.TextureFile = (cubeDefinition.Icons == null || cubeDefinition.Icons.First() == null) ? null : SpaceEngineersCore.GetDataPathOrDefault(cubeDefinition.Icons.First(), Path.Combine(contentPath, cubeDefinition.Icons.First()));
                        DataModel.CubeGrid.CubeBlocks.Insert(index, newCube);

                        change = true;
                    }
                }

                MainViewModel.ClearProgress();
                if (change)
                {
                    MainViewModel.IsModified = true;
                }
                MainViewModel.IsBusy = false;
            }
        }

        public bool ColorCubesCanExecute()
        {
            return SelectedCubeItem != null;
        }

        public void ColorCubesExecuted()
        {
            IColorDialog colorDialog = _colorDialogFactory();
            colorDialog.FullOpen = true;
            colorDialog.BrushColor = SelectedCubeItem.Color as System.Windows.Media.SolidColorBrush;
            colorDialog.CustomColors = MainViewModel.CreativeModeColors;

            if (_dialogService.ShowColorDialog(OwnerViewModel, colorDialog) == System.Windows.Forms.DialogResult.OK)
            {
                MainViewModel.IsBusy = true;
                MainViewModel.ResetProgress(0, Selections.Count);

                foreach (CubeItemViewModel cube in Selections)
                {
                    MainViewModel.Progress++;
                    if (colorDialog.DrawingColor.HasValue)
                        cube.UpdateColor(colorDialog.DrawingColor.Value.FromPaletteColorToHsvMask());
                }

                MainViewModel.ClearProgress();
                MainViewModel.IsModified = true;
                MainViewModel.IsBusy = false;
            }

            MainViewModel.CreativeModeColors = colorDialog.CustomColors;
        }

        public bool ConvertCubeToFrameworkDialogCanExecute()
        {
            return SelectedCubeItem != null;
        }

        public void ConvertCubeToFrameworkDialogExecuted()
        {
            FrameworkBuildModel model = new() { BuildPercent = SelectedCubeItem.BuildPercent * 100 };
            FrameworkBuildViewModel loadVm = new(this, model);
            bool? result = _dialogService.ShowDialog<WindowFrameworkBuild>(this, loadVm);
            if (result == true)
            {
                MainViewModel.IsBusy = true;
                MainViewModel.ResetProgress(0, Selections.Count);

                foreach (CubeItemViewModel cube in Selections)
                {
                    MainViewModel.Progress++;
                    cube.UpdateBuildPercent(model.BuildPercent.Value / 100);
                }

                MainViewModel.ClearProgress();
                MainViewModel.IsModified = true;
                MainViewModel.IsBusy = false;
            }
        }

        public bool SetOwnerCanExecute()
        {
            return SelectedCubeItem != null;
        }

        public void SetOwnerExecuted()
        {
            ChangeOwnerModel model = new()
            {
                Title = Res.WnChangeOwnerTitle
            };
            model.Load(SelectedCubeItem.Owner);
            ChangeOwnerViewModel loadVm = new(this, model);
            bool? result = _dialogService.ShowDialog<WindowChangeOwner>(this, loadVm);
            if (result == true)
            {
                MainViewModel.IsBusy = true;
                MainViewModel.ResetProgress(0, Selections.Count);

                foreach (CubeItemViewModel cube in Selections)
                {
                    MainViewModel.Progress++;
                    cube.ChangeOwner(model.SelectedPlayer.PlayerId);
                }

                MainViewModel.ClearProgress();
                MainViewModel.IsModified = true;
                MainViewModel.IsBusy = false;
            }
        }

        public bool SetBuiltByCanExecute()
        {
            return SelectedCubeItem != null;
        }

        public void SetBuiltByExecuted()
        {
            ChangeOwnerModel model = new()
            {
                Title = Res.WnChangeBuiltByTitle
            };
            model.Load(SelectedCubeItem.BuiltBy);
            ChangeOwnerViewModel loadVm = new(this, model);
            bool? result = _dialogService.ShowDialog<WindowChangeOwner>(this, loadVm);
            if (result == true)
            {
                MainViewModel.IsBusy = true;
                MainViewModel.ResetProgress(0, Selections.Count);

                foreach (CubeItemViewModel cube in Selections)
                {
                    MainViewModel.Progress++;
                    cube.ChangeBuiltBy(model.SelectedPlayer.PlayerId);
                }

                MainViewModel.ClearProgress();
                MainViewModel.IsModified = true;
                MainViewModel.IsBusy = false;
            }
        }

        #endregion

        #region Methods

        private void ApplyCubeFilter()
        {
            // Prepare filter beforehand.
            if (string.IsNullOrEmpty(ActiveComponentFilter))
                _filerView = [];
            else
                _filerView = [.. ActiveComponentFilter.ToLowerInvariant().Split([' '], StringSplitOptions.RemoveEmptyEntries).Distinct()];

            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(CubeList);
            view.Filter = UserFilter;
        }

        private bool UserFilter(object item)
        {
            if (_filerView.Length == 0)
                return true;

            CubeItemViewModel cube = (CubeItemViewModel)item;
            return _filerView.All(s => ( cube.FriendlyName.ToLowerInvariant().Contains(s)) || cube.ColorText.ToLowerInvariant().Contains(s));
        }
        
        #endregion
    }
}
