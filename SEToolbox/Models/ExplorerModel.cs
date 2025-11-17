using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.VisualBasic.FileIO;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using SEToolbox.Interfaces;
using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Services;
using SEToolbox.Support;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;
using IDType = VRage.MyEntityIdentifier.ID_OBJECT_TYPE;


///// <summary>
///// Static class to provide global access to sector objects
///// </summary>
//   public static class SectorData
//    {
//        /// <summary>
//        /// Collection of all sector objects in the current world
//        /// </summary>
//        public static List<MyObjectBuilder_EntityBase> SectorObjects { get; set; } = [];
//    }

namespace SEToolbox.Models
{

    public class ExplorerModel : BaseModel
    {
        #region Fields

        public static ExplorerModel Default { get; private set; }

        private bool _isActive;

        private bool _isBusy;

        private bool _isModified;

        private bool _isBaseSaveChanged;

        private StructureCharacterModel _thePlayerCharacter;

        /// <summary>
        /// Collection of <see cref="IStructureBase"/> objects that represent the builds currently configured.
        /// </summary>
        private ObservableCollection<IStructureBase> _structures;

        private bool _showProgress;

        private double _progress;

        private TaskbarItemProgressState _progressState;

        private double _progressValue;
        private readonly Stopwatch _timer;

        private double _maximumProgress;

        private List<int> _customColors;
        private readonly Dictionary<long, GridEntityNode> GridEntityNodes = [];

        #endregion

        #region Constructors

        public ExplorerModel()
        {
            Structures = [];
            _timer = new Stopwatch();
            _thePlayerCharacter = new(default); // Initialize with a default value
            _customColors = [];
            SetActiveStatus();
            Default = this;
        }

        #endregion

        #region Properties

        public ObservableCollection<IStructureBase> Structures
        {
            get => _structures;

            set => SetProperty(_structures = value, value, nameof(Structures));
        }

        public StructureCharacterModel ThePlayerCharacter
        {
            get => _thePlayerCharacter;

            set => SetProperty(ref _thePlayerCharacter, value, nameof(ThePlayerCharacter));
        }

        public WorldResource ActiveWorld
        {
            get => SpaceEngineersCore.WorldResource;
            set => SetProperty(SpaceEngineersCore.WorldResource, value,nameof(ActiveWorld));
        }
        /// <summary>
        /// Gets or sets a value indicating whether the View is available.  This is based on the IsInError and IsBusy properties
        /// </summary>
        public bool IsActive
        {
            get => _isActive;

            set => SetProperty(ref _isActive, value, nameof(IsActive));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;

            set => SetProperty(ref _isBusy, value, nameof(IsBusy), ()=>
                {

                    SetActiveStatus();
                    if (_isBusy)
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }
                });
        }
        


        /// <summary>
        /// Gets or sets a value indicating whether the View content has been changed.
        /// </summary>
        public bool IsModified
        {
            get => _isModified;

            set => SetProperty(ref _isModified, value, nameof(IsModified));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the base SE save content has changed.
        /// </summary>
        public bool IsBaseSaveChanged
        {
            get => _isBaseSaveChanged;

            set => SetProperty(ref _isBaseSaveChanged, value, nameof(IsBaseSaveChanged));
        }

        public bool ShowProgress
        {
            get => _showProgress;

            set => SetProperty(ref _showProgress, value, nameof(ShowProgress));
        }

        public double Progress
        {
            get => _progress;

            set =>
                    SetProperty(ref _progress, value, () =>
                     {
                         if (!_timer.IsRunning || _timer.ElapsedMilliseconds > 200 && value == _progress)
                         {
          
                                 ProgressValue = _progressValue;
                                 DispatcherHelper.DoEvents();
                                 _timer.Restart();
                             }
                         
                     }, nameof(Progress), nameof(ProgressValue)
                );
        }


        public TaskbarItemProgressState ProgressState
        {
            get => _progressState;

            set => SetProperty(ref _progressState, value, nameof(ProgressState));
        }

        public double ProgressValue
        {
            get => _progressValue;

            set => SetProperty(ref _progressValue, value, nameof(ProgressValue));
        }

        public double MaximumProgress
        {
            get => _maximumProgress;

            set => SetProperty(ref _maximumProgress, value, nameof(MaximumProgress));
        }

        public ObservableCollection<string> ScriptPaths { get; } = [];
        private string _selectedScriptPath;
        public string SelectedScriptPath
        {
            get => _selectedScriptPath;

            set => SetProperty(ref _selectedScriptPath, value, nameof(SelectedScriptPath));

        }


        /// <summary>
        /// Read in current 'world' color Palette.
        /// { 8421504, 9342617, 4408198, 4474015, 4677703, 5339473, 8414016, 10056001, 5803425, 5808314, 11447986, 12105932, 3815995, 5329241 }
        /// </summary>
        public readonly struct CustomColor(byte r, byte g, byte b)
        {
            public byte R { get; } = r;
            public byte G { get; } = g;
            public byte B { get; } = b;

            public static CustomColor FromArgb(byte r, byte g, byte b)
            {
                return new CustomColor(r, g, b);
            }
        }
        public int[] CreativeModeColors
        {
            get
            {
                if (_customColors == null)
                {
                    _customColors = [];
                    foreach (Vector3 hsv in ActiveWorld.Checkpoint.CharacterToolbar.ColorMaskHSVList)
                    {
                        var rgb = ((SerializableVector3)hsv).FromHsvMaskToPaletteColor();
                        _customColors.Add(((rgb.B << 0x10) | (rgb.G << 8) | rgb.R) & 0xffffff);
                    }
                }
                int[] value = [.. _customColors];
                return value;
            }

            set
            {
                _customColors = [.. value];
                foreach (int val in value)
                {
                    byte r = (byte)(val & 0xFFL);
                    byte g = (byte)((val >> 8) & 0xFFL);
                    byte b = (byte)((val >> 16) & 0xFFL);
                    CustomColor.FromArgb(r, g, b);

                    foreach (Vector3 hsv in ActiveWorld.Checkpoint.CharacterToolbar.ColorMaskHSVList)
                    {
                        // Add to  ColorMaskHSVList => c.ToSandboxHsvColor();
                        var c = new SerializableVector3(
                                      (float)Math.Round(hsv.X / 360, 6),
                                      (float)Math.Round(hsv.Y, 6),
                                      (float)Math.Round(hsv.Z, 6));

                        ActiveWorld.Checkpoint.CharacterToolbar.ColorMaskHSVList.Add(c);
                    }
                }
            }
        }

        #endregion

        #region Methods

        public void SetActiveStatus()
        {
            IsActive = !IsBusy;
        }

        public void BeginLoad()
        {
            IsBusy = true;
        }

        public void EndLoad()
        {
            IsModified = false;
            IsBusy = false;
        }

        public void ParseSandBox()
        {
            // make sure the LoadSector is called on the right thread for binding of data.
            Dispatcher.CurrentDispatcher.Invoke(
                DispatcherPriority.DataBind,
                new Action(LoadSectorDetail));
        }
        public void SaveCheckPointAndSandBox()
        {
            IsBusy = true;
            ActiveWorld.SaveCheckPointAndSector(true);

            // Manages the adding of new voxel files.
            foreach (IStructureBase entity in Structures ?? Enumerable.Empty<IStructureBase>())
            {
                if (entity is StructureVoxelModel voxel && File.Exists(voxel?.SourceVoxelFilePath))
                {
                    // Any asteroid that already exists with same name, must be removed.
                    if (File.Exists(voxel.VoxelFilePath))
                    {
                        FileSystem.DeleteFile(voxel.VoxelFilePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    }

                    if (Path.GetExtension(voxel.SourceVoxelFilePath).Equals(MyVoxelMapBase.FileExtension.V1, StringComparison.OrdinalIgnoreCase))
                    {
                        // Convert between formats.
                        MyVoxelMapBase.UpdateFileFormat(voxel.SourceVoxelFilePath, voxel.VoxelFilePath);
                    }
                    else
                    {
                        File.Copy(voxel.SourceVoxelFilePath, voxel.VoxelFilePath);
                    }
                    voxel.SourceVoxelFilePath = null;
                }

                if (entity is StructurePlanetModel planet && File.Exists(planet?.SourceVoxelFilePath))
                {
                    // Any asteroid that already exists with same name, must be removed.
                    if (File.Exists(planet.VoxelFilePath))
                    {
                        FileSystem.DeleteFile(planet.VoxelFilePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    }
                    File.Copy(planet.SourceVoxelFilePath, planet.VoxelFilePath);
                    planet.SourceVoxelFilePath = null;
                }
            }


            // Manages the removal old voxels files.
            foreach (string file in SpaceEngineersCore.ManageDeleteVoxelList)
            {
                string fileName = Path.Combine(ActiveWorld.SavePath, file);
                if (File.Exists(fileName))
                {
                    FileSystem.DeleteFile(fileName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
            }
            SpaceEngineersCore.ManageDeleteVoxelList.Clear();

            IsModified = false;
            IsBusy = false;
        }


        public string SaveTemporarySandbox()
        {
            IsBusy = true;

            string tempFileName = TempFileUtil.NewFileName(".xml");
            SpaceEngineersApi.WriteSpaceEngineersFile(ActiveWorld.SectorData, tempFileName);

            IsBusy = false;
            return tempFileName;
        }

        /// <summary>
        /// Loads the content from the directory and SE objects, creating object models.
        /// </summary>
        private void LoadSectorDetail()
        {
            Structures.Clear();
            ConnectedTopBlockCache.Clear();
            SpaceEngineersCore.ManageDeleteVoxelList.Clear();
            ThePlayerCharacter = null;
            _customColors = null;

            if (!Conditional.ConditionNull(ActiveWorld.SectorData, ActiveWorld.Checkpoint))
            {
                var entityBaseList = ActiveWorld.SectorData.SectorObjects.OfType<MyObjectBuilder_EntityBase>().ToList();
                foreach (var entityBase in entityBaseList)
                {
                    var structure = StructureBaseModel.Create(entityBase, ActiveWorld.SavePath);

                    if (structure is StructureCharacterModel character)
                    {
                        if (ActiveWorld.Checkpoint != null && character.EntityId == ActiveWorld.Checkpoint.ControlledObject)
                        {
                            character.IsPlayer = true;
                            ThePlayerCharacter = character;
                        }
                    }
                    else if (structure is StructureCubeGridModel cubeGrid)
                    {
                        var cockpitList = cubeGrid.GetActiveCockpits().ToList();
                        foreach (var cockpit in cockpitList)
                        {
                            cubeGrid.Pilots++;
                            // theoretically with the Hierarchy structure, there could be more than one character attached to a single cube.
                            // thus, more than 1 pilot.
                            List<MyObjectBuilder_Character> pilots = cockpit.GetHierarchyCharacters();
                            if (pilots?.Count > 0)
                            {
                                character = (StructureCharacterModel)StructureBaseModel.Create(pilots.First(), null);
                                character.IsPilot = true;

                                bool isControlledObject = ActiveWorld.Checkpoint?.ControlledObject == cockpit.EntityId;
                                if (isControlledObject)
                                {
                                    ThePlayerCharacter = character;
                                    ThePlayerCharacter.IsPlayer = true;
                                }

                                Structures.Add(character);
                            }
                        }
                    }
                    Structures.Add(structure);
                }

                CalcDistances();
            }

            OnPropertyChanged(nameof(Structures));
        }

        public void CalcDistances()
        {
            if (ActiveWorld.SectorData != null)
            {
                Vector3D position = ThePlayerCharacter.PositionAndOrientation.HasValue ? (Vector3D)ThePlayerCharacter.PositionAndOrientation.Value.Position : Vector3D.Zero;
                foreach (IStructureBase structure in Structures)
                {
                    structure.RecalcPosition(position);
                }
            }
        }

        public void SaveEntity(MyObjectBuilder_EntityBase entity, string fileName)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
            }

            bool isBinaryFile = Path.GetExtension(fileName).Equals(SpaceEngineersConsts.ProtobuffersExtension, StringComparison.OrdinalIgnoreCase);

            if (isBinaryFile)
            {
                SpaceEngineersApi.WriteSpaceEngineersFilePB(entity, fileName, false);
            }
            else
            {
                SaveEntityAsText(entity, fileName);
            }
        }

        private static readonly IEnumerable<Type> entityBaseList = new List<Type>()
        {
            typeof(MyObjectBuilder_CubeGrid),
            typeof(MyObjectBuilder_Character),
            typeof(MyObjectBuilder_FloatingObject),
            typeof(MyObjectBuilder_Meteor)
        }.Where(t => t != null);

        private static void SaveEntityAsText(MyObjectBuilder_EntityBase entity, string fileName)
        {
            foreach (var entityType in entityBaseList)
            {
                if (entity.GetType() == entityType)
                {
                    SpaceEngineersApi.WriteSpaceEngineersFile(entity, fileName);
                    break;
                }
                else
                {
                    throw new NotSupportedException($"Entity type {entity.GetType().Name} is not supported.");
                }
            }
        }


        public List<string> LoadEntities(string[] fileNames)
        {
            IsBusy = true;
            Dictionary<long, long> idReplacementTable = [];
            List<string> badfiles = [];

            IStructureBase newEntity;

            foreach (var fileName in fileNames)
            {

                MyObjectBuilder_EntityBase entityBase = null;
                MyObjectBuilder_Definitions definitionBase = null;

                MyObjectBuilder_Base BaseType = entityBase != null ? entityBase : definitionBase;

                // Try to read the file as a CubeGrid first, since it is the most common type.
                if (SpaceEngineersApi.TryReadSpaceEngineersFile(fileName, out BaseType, out bool isCompressed, out string errorInformation, false, true))
                {
                    if (BaseType is MyObjectBuilder_Definitions genericDefinitions &&
                         SpaceEngineersApi.TryReadSpaceEngineersFile(fileName, out BaseType, out isCompressed, out errorInformation, false, true))
                    {
                        var definitions = genericDefinitions.Prefabs ?? genericDefinitions.ShipBlueprints;

                        if (definitions != null && definitions == genericDefinitions.Prefabs || definitions == genericDefinitions.ShipBlueprints)
                        {
                            foreach (var definition in definitions)
                            {
                                if (definition.CubeGrid != null)
                                    MergeData(definition.CubeGrid, ref idReplacementTable);
                                if (definition.CubeGrids != null)
                                {
                                    foreach (var cubeGrid in definition.CubeGrids)
                                    {
                                        MergeData(cubeGrid, ref idReplacementTable);
                                    }
                                }
                            }
                        }
                    }

                    else if (BaseType is MyObjectBuilder_EntityBase entity)
                    {
                        foreach (var entityType in entityBaseList)
                        {

                            if (entity.GetType() == entityType)
                            {
                                newEntity = AddEntity(entity);
                                newEntity.EntityId = MergeId(entity.EntityId, ref idReplacementTable);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    badfiles.Add(fileName);
                }
            }

            IsBusy = false;
            return badfiles;
        }

        // Bounding box collision detection.
        public void CollisionCorrectEntity(MyObjectBuilder_EntityBase entity)
        {
            if (entity is MyObjectBuilder_CubeGrid cubeGrid)
            {
                BoundingBoxD entityBoundingBox = SpaceEngineersApi.GetBoundingBox(cubeGrid);

                foreach (MyObjectBuilder_EntityBase sectorObject in ActiveWorld.SectorData.SectorObjects)
                {
                    if (sectorObject is MyObjectBuilder_CubeGrid sectorCubeGrid && sectorCubeGrid != cubeGrid)
                    {

                        BoundingBoxD sectorBoundingBox = SpaceEngineersApi.GetBoundingBox(sectorCubeGrid);

                        if (entityBoundingBox.Intersects(sectorBoundingBox))
                        {
                            Vector3D adjustment = sectorBoundingBox.Max - entityBoundingBox.Min + new Vector3D(1, 1, 1); // Add a small offset
                            if (cubeGrid.PositionAndOrientation.HasValue)
                            {
                                Vector3D position = cubeGrid.PositionAndOrientation.Value.Position + adjustment;
                                cubeGrid.PositionAndOrientation = new MyPositionAndOrientation(
                                    position,
                                    cubeGrid.PositionAndOrientation.Value.Forward,
                                    cubeGrid.PositionAndOrientation.Value.Up
                                );
                            }
                            entityBoundingBox = SpaceEngineersApi.GetBoundingBox(cubeGrid);
                        }
                    }
                }
            }
        }

        private Stopwatch _elapsedTimer;
        public IStructureBase AddEntity(MyObjectBuilder_EntityBase entity)
        {
            if (entity != null)
            {
                ActiveWorld.SectorData.SectorObjects.Add(entity);

                IStructureBase structure = StructureBaseModel.Create(entity, ActiveWorld.SavePath);
                Vector3D position = ThePlayerCharacter != null ? (Vector3D)ThePlayerCharacter.PositionAndOrientation.Value.Position : Vector3D.Zero;

                _elapsedTimer = new Stopwatch();
                _elapsedTimer.Start();
                structure.PlayerDistance = (position - structure.PositionAndOrientation.Value.Position).Length();
                Structures.Add(structure);
                IsModified = true;
                return structure;
            }
            return null;
        }

        public bool RemoveEntity(MyObjectBuilder_EntityBase entity)
        {
            if (entity != null)
            {
                if (ActiveWorld.SectorData.SectorObjects.Contains(entity))
                {
                    if (entity is MyObjectBuilder_VoxelMap voxelMap)
                    {
                        SpaceEngineersCore.ManageDeleteVoxelList.Add(voxelMap.StorageName + MyVoxelMapBase.FileExtension.V2);
                    }

                    ActiveWorld.SectorData?.SectorObjects.Remove(entity);

                    // Sync with ActiveWorld
                    // ActiveWorld?.SectorData?.SectorObjects = SectorData.SectorObjects;
                    IsModified = true;
                    return true;
                }

                //rewritten as LINQ
                MyObjectBuilder_CubeGrid[] gridsWithPilot = [.. ActiveWorld.SectorData.SectorObjects
                        .OfType<MyObjectBuilder_CubeGrid>()
                        .Where(grid => grid.CubeBlocks
                        .OfType<MyObjectBuilder_Cockpit>()
                        .Any(cockpit => cockpit.Pilot == entity))];

                if (entity is MyObjectBuilder_Character character)
                {
                    foreach (MyObjectBuilder_CubeGrid sectorObject in ActiveWorld.SectorData.SectorObjects.OfType<MyObjectBuilder_CubeGrid>())
                    {
                        foreach (MyObjectBuilder_Cockpit cockpit in sectorObject.CubeBlocks.OfType<MyObjectBuilder_Cockpit>())
                        {
                            // Remove the character from the cockpit hierarchy
                            if (cockpit.RemoveHierarchyCharacter(character))
                            {
                                if (Structures.FirstOrDefault(s => s.EntityBase == sectorObject) is StructureCubeGridModel structure)
                                {
                                    structure.Pilots--;
                                }
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public bool ContainsVoxelFileName(string fileName, MyObjectBuilder_EntityBase[] additionalList)
        {
            var voxelFileNameUpper = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
            bool contains = Structures.Any(s => s is StructureVoxelModel model && model.Name.ToUpperInvariant() == voxelFileNameUpper)
            || SpaceEngineersCore.ManageDeleteVoxelList.Any(f => Path.GetFileNameWithoutExtension(f).ToUpperInvariant() == voxelFileNameUpper);

            if (contains || additionalList == null)
            {
                return contains;
            }

            contains |= additionalList.Any(s => s is MyObjectBuilder_VoxelMap map && Path.GetFileNameWithoutExtension(map.StorageName).ToUpper() == Path.GetFileNameWithoutExtension(fileName).ToUpper());

            return contains;
        }

        /// <summary>
        /// automatically number all voxel files, and check for duplicate filenames.
        /// </summary>
        /// <param name="originalFile"></param>
        /// <param name="additionalList"></param>
        /// <returns></returns>
        public string CreateUniqueVoxelStorageName(string originalFile, MyObjectBuilder_EntityBase[] additionalList)
        {
            string filePartName = Path.GetFileNameWithoutExtension(originalFile).ToLower();
            string extension = Path.GetExtension(originalFile).ToLower();
            int index = 0;

            if (!ContainsVoxelFileName(originalFile, additionalList))
                return originalFile;

            string fileName = $"{filePartName}{index}{extension}";

            while (ContainsVoxelFileName(fileName, additionalList))
            {
                index++;
                fileName = $"{filePartName}{index}{extension}";
            }

            return fileName;
        }


        public void MergeData(IList<IStructureBase> data)
        {
            Dictionary<long, long> idReplacementTable = [];

            foreach (IStructureBase item in data)
            {
                string entName = string.Empty;

                switch (item)
                {
                    case StructureCubeGridModel ship:
                        MergeData(ship.CubeGrid, ref idReplacementTable);
                        break;
                    case IStructureBase when item is StructureVoxelModel asteroid && entName == asteroid.Name:
                    case IStructureBase when item is StructurePlanetModel planet && entName == planet.Name:
                        var voxModel = item as StructureVoxelModel;

                        if (ContainsVoxelFileName(entName, null))
                        {
                            _ = CreateUniqueVoxelStorageName(entName, null);
                        }

                        AddEntity(voxModel.VoxelMap);
                        ((IStructureBase)null).EntityId = MergeId(voxModel.EntityId, ref idReplacementTable);
                        ((IStructureBase)null).SourceVoxelFilePath = voxModel.SourceVoxelFilePath ?? voxModel.VoxelFilePath;
                        break;

                    case IStructureBase when item is StructureFloatingObjectModel:
                    case IStructureBase when item is StructureMeteorModel:
                    case IStructureBase when item is StructureInventoryBagModel:
                    case IStructureBase when item is StructureUnknownModel:
                        AddEntity(item.EntityBase);
                        item.EntityId = MergeId(item.EntityId, ref idReplacementTable);
                        break;
                    default:
                        break;
                        // ignore the StructureCharacterModel.
                }
            }
        }

        private void MergeData(MyObjectBuilder_CubeGrid cubeGridObject, ref Dictionary<long, long> idReplacementTable)
        {
            if (cubeGridObject == null)
                return;

            var newId = MergeId(cubeGridObject.EntityId, ref idReplacementTable);
            cubeGridObject.EntityId = newId;

            var blockList = cubeGridObject.CubeBlocks.ToList();

            var toolbarType = typeof(MyObjectBuilder_Toolbar);
            var functionalTypes = new HashSet<Type>([
              typeof(MyObjectBuilder_ButtonPanel),//MyObjectBuilder_FunctionalBlock
                typeof(MyObjectBuilder_TimerBlock),//MyObjectBuilder_FunctionalBlock
                typeof(MyObjectBuilder_SensorBlock),//MyObjectBuilder_FunctionalBlock
                typeof(MyObjectBuilder_ShipController)//,//MyObjectBuilder_FunctionalBlock
                //typeof(MyObjectBuilder_ExtendedPistonBase),//MyObjectBuilder_MechanicalConnectionBlock
                //typeof(MyObjectBuilder_MechanicalConnectionBlock),//MyObjectBuilder_FunctionalBlock
            ]);
            var types = new HashSet<Type>(blockList.Select(b => b.GetType()));
            var blocktypes = new HashSet<Type>
            {
                typeof(MyObjectBuilder_CubeBlock),//MyObjectBuilder_Base
                typeof(MyObjectBuilder_CubeGrid),//MyObjectBuilder_EntityBase

                typeof(MyObjectBuilder_MotorAdvancedStator), //MyObjectBuilder_MotorStator
                typeof(MyObjectBuilder_MotorSuspension), //MyObjectBuilder_MotorBase
                typeof(MyObjectBuilder_MotorStator), //MyObjectBuilder_MotorBase
                typeof(MyObjectBuilder_MotorAdvancedRotor), //MyObjectBuilder_MotorRotor
                
                typeof(MyObjectBuilder_PistonTop), //MyObjectBuilder_AttachableTopBlockBase
                typeof(MyObjectBuilder_ExtendedPistonBase),//MyObjectBuilder_PistonBase
                

                
            };

            var baseTypes = new HashSet<Type>
            {
                typeof(MyObjectBuilder_MotorBase),//MyObjectBuilder_MechanicalConnectionBlock
                typeof(MyObjectBuilder_PistonBase), //MyObjectBuilder_MechanicalConnectionBlock
                typeof(MyObjectBuilder_MotorRotor), //MyObjectBuilder_AttachableTopBlockBase
                typeof(MyObjectBuilder_ShipConnector) //MyObjectBuilder_FunctionalBlock
            };
            var rootTypes = new HashSet<Type>
            {
                typeof(MyObjectBuilder_MechanicalConnectionBlock),
                typeof(MyObjectBuilder_AttachableTopBlockBase),
                typeof(MyObjectBuilder_FunctionalBlock),
            };

            foreach (var block in blockList)
            {

                var blockType = block.GetType();
                if (functionalTypes.Contains(blockType))
                {
                    var toolbarProperty = blockType.GetProperty(typeof(MyObjectBuilder_Toolbar).Name);
                    if (toolbarProperty != null)
                    {
                        var toolbar = (MyObjectBuilder_Toolbar)toolbarProperty.GetValue(block);
                        RenumberToolbar(toolbar, ref idReplacementTable);
                    }
                }
                else
                {
                    switch (block)
                    {
                        case MyObjectBuilder_CubeBlock when block is MyObjectBuilder_Cockpit cockpit:
                            cockpit.RemoveHierarchyCharacter();
                            break;
                        case MyObjectBuilder_CubeBlock when block is MyObjectBuilder_MotorBase:
                        case MyObjectBuilder_CubeBlock when block is MyObjectBuilder_PistonBase:
                        case MyObjectBuilder_CubeBlock when block is MyObjectBuilder_ShipConnector:
                            MergeId(block.EntityId, ref idReplacementTable);// reattach functionalBlock to correct entity.
                            break;
                    }
                }

            }

            AddEntity(cubeGridObject);
        }

        // TODO: cubeGridObject.MultiBlockId

        //multiblock has to deal with
        // at least 200+ blocks.fom ooking into the code
        // seemingly rooted in  MySlimBlock.MultiBlockId, and  MySlimBlock.MultiBlockDefinition , dont know whether it neeeds implementation
        //also has something to do with fracturing




        private static void RenumberToolbar(MyObjectBuilder_Toolbar toolbar, ref Dictionary<Int64, Int64> idReplacementTable)
        {
            if (toolbar == null)
                return;
            foreach (MyObjectBuilder_Toolbar.Slot item in toolbar.Slots)
            {
                if (item.Data is MyObjectBuilder_ToolbarItemTerminalGroup terminalGroup && terminalGroup != null)
                {
                    // GridEntityId does not require remapping. accoring to IL on ToolbarItemTerminalGroup.
                    //terminalGroup.GridEntityId = MergeId(terminalGroup.GridEntityId, ref idReplacementTable);
                    terminalGroup.BlockEntityId = MergeId(terminalGroup.BlockEntityId, ref idReplacementTable);
                }
                MyObjectBuilder_ToolbarItemTerminalBlock terminalBlock = item.Data as MyObjectBuilder_ToolbarItemTerminalBlock;

                terminalBlock?.BlockEntityId = MergeId(terminalBlock.BlockEntityId, ref idReplacementTable);

            }
        }

        private static long MergeId(long currentId, ref Dictionary<long, long> idReplacementTable)
        {
            if (currentId == 0)
                return 0;

            if (idReplacementTable.ContainsKey(currentId))
                return idReplacementTable[currentId];

            idReplacementTable[currentId] = SpaceEngineersApi.GenerateEntityId(IDType.ENTITY);
            return idReplacementTable[currentId];
        }

        //         public static int MergeId(int currentId, ref Dictionary<int, int> idReplacementTable)
        // {
        //     if (currentId == 0)
        //         return 0;

        //     if (!idReplacementTable.ContainsKey(currentId))
        //     {
        //         //todo: Generate a unique multiblock ID estimate of logic
        //         idReplacementTable[currentId] = GenerateUniqueMultiblockId();
        //     }

        //     return idReplacementTable[currentId];
        // }


        //         private static int GenerateUniqueMultiblockId()
        //         {
        //             // todo :Implement logic to generate a unique multiblock ID
        //            throw new NotImplementedException();
        //         }


        public void OptimizeModel(StructureCubeGridModel viewModel)
        {
            if (viewModel == null) return;

            // Optimize ordering of CubeBlocks within structure for faster loading based on {X+, Y+, Z+}
            var neworder = viewModel.CubeGrid.CubeBlocks = [.. viewModel.CubeGrid.CubeBlocks
                //.GroupBy(c => c.Min)
                //.Select(c => c.First()) // Keep only one block at each position
                .OrderBy(c => c.Min.Z)
                .ThenBy(c => c.Min.Y)
                .ThenBy(c => c.Min.X)];

            viewModel.CubeGrid.CubeBlocks = neworder;
            IsModified = true;

        }


        private HashSet<Type> ExcludedBlockTypes()
        {
            return [
            typeof(MyObjectBuilder_MotorRotor),
            typeof(MyObjectBuilder_MotorAdvancedRotor),
            typeof(MyObjectBuilder_PistonTop),
            typeof(MyObjectBuilder_MotorSuspension),
            typeof(MyObjectBuilder_MotorStator),
            typeof(MyObjectBuilder_MotorAdvancedStator),
            typeof(MyObjectBuilder_ExtendedPistonBase)
            ];
        }
        /// <summary>
        /// Finds overlapping blocks in the given CubeGrid model, optionally logs them, and performs actions based on the operation.
        /// </summary>
        /// <param name="viewModel">The CubeGrid model to scan for overlapping blocks.</param>
        /// <param name="excludedTypes">Optional: Types of blocks to exclude from detection.</param>
        /// <param name="trackProgress">Whether to track progress during the operation.</param>
        /// <param name="operation">The operation to perform: "Scan", "Remove", or "Move".</param>
        /// do i needd this or can i juust implement this ddirectly in thefunctions
        // public void HandleOverlappingBlocks(StructureCubeGridModel viewModel, HashSet<Type> excludedTypes = null,
        //     bool trackProgress = false,
        //     string operation = "Scan")
        // {
        //     if (viewModel == null || viewModel.CubeGrid.CubeBlocks == null)
        //     {
        //         return;
        //     }

        //     IsBusy = true;
        //     List<MyObjectBuilder_CubeBlock> overlappingBlocks = FindOverlappingBlocks(viewModel, excludedTypes, trackProgress);

        //     // Step 2: Perform the specified operation
        //     switch (operation.ToLower())
        //     {
        //         case "scan":
        //             FindOverlappingBlocks(viewModel, excludedTypes, trackProgress);
        //             if (overlappingBlocks.Count == 0)
        //             {
        //                 IsBusy = false;
        //                 return;
        //             }
        //             break;

        //         case "remove":
        //             RemoveBlocks(viewModel, overlappingBlocks);
        //             break;

        //         case "move":
        //             MoveOverlappingBlocks(viewModel);
        //             break;

        //         default:
        //             break;
        //     }
        // }

        public List<MyObjectBuilder_CubeBlock> FindOverlappingBlocks(
            StructureCubeGridModel viewModel,
            HashSet<Type> excludedTypes = null,
            bool trackProgress = false)
        {
            if (viewModel == null) return [];

            ConcurrentDictionary<Vector3I, MyObjectBuilder_CubeBlock> occupiedPositions = new();
            ConcurrentBag<MyObjectBuilder_CubeBlock> overlappingBlocks = [];
            int totalBlocks = viewModel.CubeGrid.CubeBlocks.Count;

            if (trackProgress)
            {
                ResetProgress(0, viewModel.CubeGrid.CubeBlocks.Count);
            }

            Parallel.ForEach(viewModel.CubeGrid.CubeBlocks, block =>
            {
                // Skip blocks of excluded types
                if (excludedTypes.Contains(block.GetType()))
                {
                    if (trackProgress) IncrementProgress();
                    return;
                }

                // Check if the block's position is already occupied
                if (!occupiedPositions.TryAdd(block.Min, block))
                {
                    overlappingBlocks.Add(block);
                }

                if (trackProgress) IncrementProgress();
            });

            if (trackProgress)
            {
                ClearProgress();
            }

            return [.. overlappingBlocks];
        }

        private static readonly object _cubeBlocksLock = new();

        private static void RemoveBlocks(StructureCubeGridModel viewModel, List<MyObjectBuilder_CubeBlock> blocksToRemove)
        {
            if (viewModel == null || viewModel.CubeGrid?.CubeBlocks == null || blocksToRemove == null)
                return;

            HashSet<MyObjectBuilder_CubeBlock> blocksToRemoveSet = [.. blocksToRemove];
            foreach (var block in blocksToRemoveSet)

                Parallel.ForEach(blocksToRemoveSet, block =>
                {
                    lock (viewModel.CubeGrid.CubeBlocks)
                    {
                        viewModel.CubeGrid.CubeBlocks.Remove(block);
                    }
                });
        }

        public void RemoveOverlappingBlocks(StructureCubeGridModel viewModel, bool ToggleExcludedTypes = true, ConcurrentBag<MyObjectBuilder_CubeBlock> overlappingBlocks = null)
        {
            List<MyObjectBuilder_CubeBlock> blocksToRemove = [];
            if (viewModel == null) return;

            IsBusy = true;
            _ = ToggleExcludedTypes ? ExcludedBlockTypes() : null;
            foreach (var block in overlappingBlocks)
            {
                //add tp the list of blocks to remove
                blocksToRemove.Add(block);
            }
            _ = blocksToRemove ?? [];
            RemoveBlocks(viewModel, blocksToRemove);
            IsModified = true;
            IsBusy = false;
        }
        public void MoveOverlappingBlocks(StructureCubeGridModel originalModel)
        {
            if (originalModel == null) return;

            IsBusy = true;

            HashSet<Type> excludedTypes = ExcludedBlockTypes();
            List<MyObjectBuilder_CubeBlock> overlappingBlocks = FindOverlappingBlocks(originalModel, excludedTypes);

            if (overlappingBlocks.Count == 0)
            {
                IsBusy = false;
                return;
            }

            BoundingBoxD originalBoundingBox = CalculateBoundingBox(originalModel.CubeGrid.CubeBlocks);

            Vector3D newPosition = originalBoundingBox.Max + new Vector3D(200, 0, 0);
            MyObjectBuilder_CubeGrid newCubeGrid = new()
            {
                EntityId = SpaceEngineersApi.GenerateEntityId(),
                GridSizeEnum = originalModel.CubeGrid.GridSizeEnum,
                IsStatic = true,
                PositionAndOrientation = new MyPositionAndOrientation(
                    newPosition,
                    originalModel.PositionAndOrientation.Value.Forward,
                    originalModel.PositionAndOrientation.Value.Up),
                CubeBlocks = []
            };

            foreach (MyObjectBuilder_CubeBlock block in overlappingBlocks)
            {
                originalModel.CubeGrid.CubeBlocks.Remove(block);
                newCubeGrid.CubeBlocks.Add(block);
            }

            new StructureCubeGridModel(newCubeGrid);
            AddEntity(newCubeGrid);

            IsModified = true;
            IsBusy = false;
        }

        public bool EnableExclusions { get; set; } = true;
        public bool RelativeMoveCanExecute()
        {
            return IsModified && IsBusy;
        }

        public bool ToggleExcludedBlocks(StructureCubeGridModel viewModel)
        {
            if (viewModel == null)
            {
                return false;
            }

            EnableExclusions = !EnableExclusions;

            HashSet<Type> excludedTypes = EnableExclusions ? ExcludedBlockTypes() : null;

            if (excludedTypes?.Count > 0)
            {
                FindOverlappingBlocks(viewModel, excludedTypes, trackProgress: true);
            }
            else
            {
                FindOverlappingBlocks(viewModel, null, trackProgress: true);
            }

            return EnableExclusions;
        }

        private static void LogOverlappingBlocksToFile(List<MyObjectBuilder_CubeBlock> overlappingBlocks)
        {
            string filePath = "OverlappingBlocks.txt";
            using StreamWriter writer = new(filePath);
            writer.WriteLine($"Found {overlappingBlocks.Count} overlapping blocks:");
            foreach (MyObjectBuilder_CubeBlock block in overlappingBlocks)
            {
                writer.WriteLine($"- Block at position {block.Min} with SubtypeName: {block.SubtypeName}");
            }
        }

        private BoundingBoxD CalculateBoundingBox(IEnumerable<MyObjectBuilder_CubeBlock> blocks)
        {
            Vector3D min = new(double.MaxValue, double.MaxValue, double.MaxValue);
            Vector3D max = new(double.MinValue, double.MinValue, double.MinValue);

            foreach (MyObjectBuilder_CubeBlock block in blocks)
            {
                SerializableVector3I blockMin = block.Min;
                Vector3I blockMax = block.Min + new Vector3I(1, 1, 1);

                min = Vector3D.Min(min, blockMin.ToVector3D());
                max = Vector3D.Max(max, blockMax);
            }

            return new BoundingBoxD(min, max);
        }


        //todo move all test code to a separate test folder, files,classes. with references to their original locations
        public static void TestDisplayRotation(StructureCubeGridModel viewModel)
        {
            //var corners = viewModel.CubeGrid.CubeBlocks.Where(b => b.SubtypeName.Contains("ArmorCorner")).ToList();
            //var corners = viewModel.CubeGrid.CubeBlocks.OfType<MyObjectBuilder_CubeBlock>().ToArray();
            //var corners = viewModel.CubeGrid.CubeBlocks.Where(b => StructureCubeGridModel.TubeCurvedRotationBlocks.Contains(b.SubtypeName)).ToList();
            List<MyObjectBuilder_CubeBlock> corners = [.. viewModel.CubeGrid.CubeBlocks.Where(b => b.SubtypeName.Contains("ArmorCorner"))];

            foreach (MyObjectBuilder_CubeBlock corner in corners)
            {
                SConsole.WriteLine($"{corner.SubtypeName}\t = \tAxis24_{corner.BlockOrientation.Forward}_{corner.BlockOrientation.Up}");
            }
        }

        public void TestConvert(StructureCubeGridModel viewModel)
        {
            // Trim Horse image.
            //viewModel.CubeGrid.CubeBlocks.RemoveAll(b => b.SubtypeName.EndsWith("White"));

            //foreach (var block in viewModel.CubeGrid.CubeBlocks)
            //{
            //    if (block.SubtypeName == SubtypeId.SmallBlockArmorBlock.ToString())
            //    {
            //        block.SubtypeName = SubtypeId.SmallBlockArmorBlockRed.ToString();
            //    }
            //}
            //IsModified = true;

            //viewModel.CubeGrid.CubeBlocks.RemoveAll(b => b.SubtypeName == SubtypeId.SmallLight.ToString());

            _ = new List<MyObjectBuilder_CubeBlock>();

            foreach (MyObjectBuilder_CubeBlock block in viewModel.CubeGrid.CubeBlocks)
            {
                if (block.SubtypeName == SubtypeId.SmallBlockArmorBlock.ToString())
                {
                    //block.SubtypeName = SubtypeId.SmallBlockArmorBlockBlack.ToString();

                    //var light = block as MyObjectBuilder_ReflectorLight;
                    //light.Intensity = 5;
                    //light.Radius = 5;
                }
                //if (block.SubtypeName == SubtypeId.LargeBlockArmorBlockBlack.ToString())
                //{
                //    for (var i = 0; i < 3; i++)
                //    {
                //        var newBlock = new MyObjectBuilder_CubeBlock()
                //        {
                //            SubtypeName = block.SubtypeName, // SubtypeId.LargeBlockArmorBlockWhite.ToString(),
                //            EntityId = block.EntityId == 0 ? 0 : SpaceEngineersApi.GenerateEntityId(),
                //            PersistentFlags = block.PersistentFlags,
                //            Min = new Vector3I(block.Min.X, block.Min.Y, block.Min.Z + 1 + i),
                //            Max = new Vector3I(block.Max.X, block.Max.Y, block.Max.Z + 1 + i),
                //            Orientation = Quaternion.CreateFromRotationMatrix(MatrixD.CreateLookAt(Vector3D.Zero, Vector3.Forward, Vector3.Up))
                //        };

                //        newBlocks.Add(newBlock);
                //    }
                //}

                //if (block.SubtypeName == SubtypeId.LargeBlockArmorBlockWhite.ToString())
                //{
                //    var newBlock = new MyObjectBuilder_CubeBlock()
                //    {
                //        SubtypeName = block.SubtypeName, // SubtypeId.LargeBlockArmorBlockWhite.ToString(),
                //        EntityId = block.EntityId == 0 ? 0 : SpaceEngineersApi.GenerateEntityId(),
                //        PersistentFlags = block.PersistentFlags,
                //        Min = new Vector3I(block.Min.X, block.Min.Y, block.Min.Z + 3),
                //        Max = new Vector3I(block.Max.X, block.Max.Y, block.Max.Z + 3),
                //        Orientation = Quaternion.CreateFromRotationMatrix(MatrixD.CreateLookAt(Vector3D.Zero, Vector3.Forward, Vector3.Up))
                //    };

                //    newBlocks.Add(newBlock);
                //}

                //if (block.Min.Z == 3 && block.Min.X % 2 == 1 && block.Min.Y % 2 == 1)
                //{
                //    var newBlock = new MyObjectBuilder_InteriorLight()
                //    {
                //        SubtypeName = SubtypeId.SmallLight.ToString(),
                //        EntityId = SpaceEngineersApi.GenerateEntityId(),
                //        PersistentFlags = MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                //        Min = new Vector3I(block.Min.X, block.Min.Y, 1),
                //        Max = new Vector3I(block.Max.X, block.Max.Y, 1),
                //        Orientation = new Quaternion(1, 0, 0, 0),
                //        Radius = 3.6f,
                //        Falloff = 1.3f,
                //        Intensity = 1.5f,
                //        PositionAndOrientation = new MyPositionAndOrientation()
                //        {
                //            Position = new Vector3D(),
                //            //Position = new Vector3D(-7.5f, -10, 27.5f),
                //            Forward = new Vector3(0,-1,0),
                //            Up = new Vector3(1,0,0)
                //        }

                //    };

                //    newBlocks.Add(newBlock);
                //}
            }

            //viewModel.CubeGrid.CubeBlocks.AddRange(newBlocks);

            OptimizeModel(viewModel);
        }

        public void TestResize(StructurePlanetModel viewModel)
        {
            viewModel.RegeneratePlanet(0, 120000);
            IsModified = true;
        }

        /// <summary>
        /// Copy blocks from ship2 into ship1.
        /// </summary>
        /// <param name="model1"></param>
        /// <param name="model2"></param>
        internal void RejoinBrokenShip(StructureCubeGridModel model1, StructureCubeGridModel model2)
        {
            // Copy blocks from ship2 into ship1.
            model1.CubeGrid.CubeBlocks.AddRange(model2.CubeGrid.CubeBlocks);

            // Merge Groupings
            foreach (MyObjectBuilder_BlockGroup group in model2.CubeGrid.BlockGroups)
            {
                MyObjectBuilder_BlockGroup existingGroup = model1.CubeGrid.BlockGroups.FirstOrDefault(bg => bg.Name == group.Name);
                if (existingGroup == null)
                {
                    model1.CubeGrid.BlockGroups.Add(group);
                }
                else
                {
                    existingGroup.Blocks.AddRange(group.Blocks);
                }
            }

            // Merge ConveyorLines
            model1.CubeGrid.ConveyorLines.AddRange(model2.CubeGrid.ConveyorLines);
        }

        /// <summary>
        /// Merges and copies blocks from ship2 into ship1.
        /// </summary>
        /// <param name="model1"></param>
        /// <param name="model2"></param>
        /// <returns></returns>
        internal bool MergeShipParts(StructureCubeGridModel model1, StructureCubeGridModel model2)
        {
            // find closest major axis for both parts.
            Quaternion q1 = Quaternion.CreateFromRotationMatrix(Matrix.CreateFromDir(model1.PositionAndOrientation.Value.Forward.RoundToAxis(), model1.PositionAndOrientation.Value.Up.RoundToAxis()));
            Quaternion q2 = Quaternion.CreateFromRotationMatrix(Matrix.CreateFromDir(model2.PositionAndOrientation.Value.Forward.RoundToAxis(), model2.PositionAndOrientation.Value.Up.RoundToAxis()));

            // Calculate the rotation between the two.
            Quaternion fixRotate = Quaternion.Inverse(q2) * q1;
            fixRotate.Normalize();

            // Rotate the orientation of model2 to (closely) match model1.
            // It's Inverse, as the ship is actually rotated inverse in response to rotation of the cubes.
            model2.RotateCubes(Quaternion.Inverse(fixRotate));

            // At this point ship2 has been reoriented around to closely match ship1.
            // The cubes in ship2 have be reoriended in reverse, so effectly there is no visual difference in ship2, except now all the cubes are aligned to the same X,Y,Z axis as ship1.

            // find two cubes, one from each ship that are closest to each other to use as the reference.
            Vector3D pos1 = (Vector3D)model1.PositionAndOrientation.Value.Position;
            Vector3D pos2 = (Vector3D)model2.PositionAndOrientation.Value.Position;
            Quaternion orient1 = model1.PositionAndOrientation.Value.ToQuaternion();
            Quaternion orient2 = model2.PositionAndOrientation.Value.ToQuaternion();
            float multi1 = model1.GridSize.ToLength();
            float multi2 = model2.GridSize.ToLength();

            float maxDistance = float.MaxValue;
            MyObjectBuilder_CubeBlock maxCube1 = null;
            MyObjectBuilder_CubeBlock maxCube2 = null;

            foreach (MyObjectBuilder_CubeBlock cube1 in model1.CubeGrid.CubeBlocks)
            {
                foreach (MyObjectBuilder_CubeBlock cube2 in model2.CubeGrid.CubeBlocks)
                {
                    Vector3D cPos1 = pos1 + Vector3.Transform(cube1.Min.ToVector3() * multi1, orient1);
                    Vector3D cPos2 = pos2 + Vector3.Transform(cube2.Min.ToVector3() * multi2, orient2);

                    float d = Vector3.Distance(cPos1, cPos2);
                    if (maxDistance > d)
                    {
                        maxDistance = d;
                        maxCube1 = cube1;
                        maxCube2 = cube2;
                    }
                }
            }

            // Ignore ships that are too far away from one another.
            // A distance of 4 cubes to allow for large cubes, as we are only using the Min as position, not the entire size of a cube.
            if (maxDistance < (model1.GridSize.ToLength() * 5))
                if (maxDistance < (model1.GridSize.ToLength() * 5))
                {
                    Vector3D cPos1 = pos1 + Vector3.Transform(maxCube1.Min.ToVector3() * multi1, orient1);
                    Vector3D cPos2 = pos2 + Vector3.Transform(maxCube2.Min.ToVector3() * multi2, orient2);
                    Vector3 adjustedPos = Vector3.Transform(cPos2 - pos1, VRageMath.Quaternion.Inverse(orient1)) / multi1;
                    Vector3I offset = adjustedPos.RoundToVector3I() - maxCube2.Min.ToVector3I();

                    foreach (MyObjectBuilder_CubeBlock cube2 in model2.CubeGrid.CubeBlocks)
                    {
                        MyObjectBuilder_CubeBlock newcube = (MyObjectBuilder_CubeBlock)cube2.Clone();
                        newcube.Min = cube2.Min + offset;
                        model1.CubeGrid.CubeBlocks.Add(newcube);
                    }

                    foreach (MyObjectBuilder_BlockGroup group in model2.CubeGrid.BlockGroups)
                    {
                        MyObjectBuilder_BlockGroup existingGroup = model1.CubeGrid.BlockGroups.FirstOrDefault(bg => bg.Name == group.Name);
                        if (existingGroup == null)
                        {
                            existingGroup = new MyObjectBuilder_BlockGroup { Name = group.Name };
                            model1.CubeGrid.BlockGroups.Add(existingGroup);
                        }

                        foreach (Vector3I block in group.Blocks)
                        {
                            existingGroup.Blocks.Add(block + offset);
                        }
                    }

                    //Merge Bones
                    if (model2.CubeGrid.Skeleton != null)
                    {
                        model1.CubeGrid.Skeleton ??= [];

                        foreach (BoneInfo bone in model2.CubeGrid.Skeleton)
                        {
                            model1.CubeGrid.Skeleton.Insert(0, new BoneInfo
                            {
                                BonePosition = bone.BonePosition + offset,
                                BoneOffset = bone.BoneOffset
                            });
                        }
                    }

                    //  Merge ConveyorLines
                    if (model2.CubeGrid.ConveyorLines != null)
                    {
                        Quaternion Rotation = Quaternion.Inverse(model2.PositionAndOrientation.Value.ToQuaternion()) * model1.PositionAndOrientation.Value.ToQuaternion();
                        var newLines = model2.CubeGrid.ConveyorLines.Select(line => new MyObjectBuilder_ConveyorLine
                        {
                            StartPosition = Vector3I.Transform(line.StartPosition, Rotation) + offset,
                            EndPosition = Vector3I.Transform(line.EndPosition, Rotation) + offset,
                            StartDirection = Base6Directions.GetDirection(Vector3.Transform(Base6Directions.GetVector(line.StartDirection), Rotation)),
                            EndDirection = Base6Directions.GetDirection(Vector3.Transform(Base6Directions.GetVector(line.EndDirection), Rotation))
                        }).ToList();
                        model1.CubeGrid.ConveyorLines.AddRange(newLines);
                    }


                    return true;
                }
            return false;
        }

        #endregion

        public void ResetProgress(double initial, double maximumProgress)
        {
            MaximumProgress = maximumProgress;
            Progress = initial;
            ShowProgress = true;
            ProgressState = TaskbarItemProgressState.Normal;
            _timer.Restart();
            System.Windows.Forms.Application.DoEvents();
        }

        public void IncrementProgress()
        {
            Progress++;
        }

        public void ClearProgress()
        {
            _timer.Stop();
            ShowProgress = false;
            Progress = 0;
            ProgressState = TaskbarItemProgressState.None;
            ProgressValue = 0;
        }

        private readonly Dictionary<long, MyObjectBuilder_CubeGrid> ConnectedTopBlockCache = [];

        public MyObjectBuilder_CubeGrid FindConnectedTopBlock<T>(long topBlockId)
            where T : MyObjectBuilder_MechanicalConnectionBlock
        {
            if (ConnectedTopBlockCache.TryGetValue(topBlockId, out MyObjectBuilder_CubeGrid value))
                return value;
            {
                foreach (MyObjectBuilder_EntityBase entBase in ActiveWorld.SectorData.SectorObjects)
                {
                    if (entBase is MyObjectBuilder_CubeGrid grid)
                    {
                        for (int j = 0; j < grid.CubeBlocks.Count; j++)
                        {
                            MyObjectBuilder_MechanicalConnectionBlock mechanicalBlock = grid.CubeBlocks[j] as T;
                            if (mechanicalBlock?.TopBlockId == topBlockId)
                            {
                                ConnectedTopBlockCache[topBlockId] = grid;
                                return grid;
                            }
                        }
                    }
                }
            }
            ConnectedTopBlockCache[topBlockId] = null;
            return null;
        }

        private class CubeEntityNode
        {
            public long EntityId;
            public MyObjectBuilder_CubeBlock Entity;

            public MyObjectBuilder_CubeGrid RemoteParentEntity;
            public long RemoteParentEntityId;
            public long RemoteEntityId;
            public MyObjectBuilder_CubeBlock RemoteEntity;
            public GridConnectionTypes GridConnectionType;
        }

        private class GridEntityNode
        {
            public long ParentEntityId;
            public MyObjectBuilder_CubeGrid ParentEntity;
            public Dictionary<long, CubeEntityNode> CubeEntityNodes = [];
        }

        public void BuildGridEntityNodes()
        {
            GridEntityNodes.Clear();

            // Build the main list of entities
            foreach (StructureCubeGridModel structure in Structures.OfType<StructureCubeGridModel>())
            {
                // Create a new GridEntityNode for each StructureCubeGridModel
                GridEntityNode gridEntityNode = new()
                {
                    ParentEntityId = structure.EntityId,
                    ParentEntity = structure.CubeGrid
                };

                // Add the GridEntityNode to the dictionary using the structure's EntityId as the key
                GridEntityNodes[structure.EntityId] = gridEntityNode;


                foreach (MyObjectBuilder_CubeBlock block in structure.CubeGrid.CubeBlocks)
                {
                    AddCubeEntityNode(gridEntityNode, block);
                }
            }

            // Crosscheck the remote entities to establish connections between blocks
            foreach (StructureCubeGridModel structure in Structures.OfType<StructureCubeGridModel>())
            {
                foreach (MyObjectBuilder_CubeBlock block in structure.CubeGrid.CubeBlocks)
                {
                    CrosscheckRemoteEntities(structure, block);
                }
            }
        }
        private void CrosscheckRemoteEntities(StructureCubeGridModel gridModel, MyObjectBuilder_CubeBlock block)
        {
            foreach (var kvp in GridEntityNodes)
            {
                var node = kvp.Value.CubeEntityNodes.Values.FirstOrDefault(e => e.RemoteEntityId == block.EntityId);
                if (node != null)
                {
                    node.RemoteParentEntity = gridModel.CubeGrid;
                    node.RemoteParentEntityId = gridModel.CubeGrid.EntityId;
                    node.RemoteEntity = block;
                    break;
                }

                if (block is MyObjectBuilder_MechanicalConnectionBlock mechanicalConnection && mechanicalConnection.TopBlockId.HasValue)
                {
                    CubeEntityNode topBlockNode = kvp.Value.CubeEntityNodes.Values.FirstOrDefault(e => e.EntityId == mechanicalConnection.TopBlockId.Value);
                    if (topBlockNode != null)
                    {
                        topBlockNode.RemoteParentEntity = gridModel.CubeGrid;
                        topBlockNode.RemoteParentEntityId = gridModel.CubeGrid.EntityId;
                        topBlockNode.RemoteEntityId = block.EntityId;
                        topBlockNode.RemoteEntity = block;
                    }
                }
            }

        }

        /// <summary>
        /// Adds a CubeBlock to the GridEntityNode and determines its connection type.
        /// </summary>
        /// <param name="gridEntityNode">The GridEntityNode to which the block belongs.</param>
        /// <param name="block">The CubeBlock to add.</param>
        private static void AddCubeEntityNode(GridEntityNode gridEntityNode, MyObjectBuilder_CubeBlock block)
        {
            // Determine the type of the block and add it to the CubeEntityNodes dictionary
            switch (block)
            {
                case MyObjectBuilder_Wheel:
                    // Add a mechanical connection for wheels
                    gridEntityNode.CubeEntityNodes[block.EntityId] = new CubeEntityNode
                    {
                        GridConnectionType = GridConnectionTypes.Mechanical,
                        EntityId = block.EntityId,
                        Entity = block
                    };
                    break;
                case MyObjectBuilder_MechanicalConnectionBlock:
                    // Add a mechanical connection for pistons or similar blocks
                    gridEntityNode.CubeEntityNodes[block.EntityId] = new CubeEntityNode
                    {
                        GridConnectionType = GridConnectionTypes.Mechanical,
                        EntityId = block.EntityId,
                        Entity = block,
                        RemoteEntityId = ((MyObjectBuilder_MechanicalConnectionBlock)block).TopBlockId ?? 0
                    };
                    break;

                case MyObjectBuilder_ShipConnector when ((MyObjectBuilder_ShipConnector)block).MasterToSlaveTransform != null:
                    // Add a connector lock for ship connectors
                    gridEntityNode.CubeEntityNodes[block.EntityId] = new CubeEntityNode
                    {
                        GridConnectionType = GridConnectionTypes.ConnectorLock,
                        EntityId = block.EntityId,
                        Entity = block,
                        RemoteEntityId = ((MyObjectBuilder_ShipConnector)block).ConnectedEntityId
                    };
                    break;
                case MyObjectBuilder_MotorAdvancedRotor:
                case MyObjectBuilder_MotorRotor:
                case MyObjectBuilder_PistonTop:
                case MyObjectBuilder_AttachableTopBlockBase:

                    // Add a mechanical connection for rotors
                    gridEntityNode.CubeEntityNodes[block.EntityId] = new CubeEntityNode
                    {
                        GridConnectionType = GridConnectionTypes.Mechanical,
                        EntityId = block.EntityId,
                        Entity = block,
                        RemoteEntityId = ((MyObjectBuilder_AttachableTopBlockBase)block).ParentEntityId
                    };
                    break;
            }
        }

        public List<MyObjectBuilder_CubeGrid> GetConnectedGridNodes(StructureCubeGridModel structureCubeGrid, GridConnectionTypes minimumConnectionType)
        {
            List<MyObjectBuilder_CubeGrid> list = [];
            GridEntityNode parentNode = GridEntityNodes[structureCubeGrid.EntityId];
            if (parentNode != null)
            {
                IEnumerable<MyObjectBuilder_CubeGrid> remoteEntities = parentNode.CubeEntityNodes
                .Where(e => minimumConnectionType.HasFlag(e.Value.GridConnectionType) && e.Value.RemoteParentEntity != null)
                .Select(e => e.Value.RemoteParentEntity ?? throw new InvalidOperationException("RemoteParentEntity is null"));
                foreach (MyObjectBuilder_CubeGrid cubeGrid in remoteEntities)
                {
                    if (cubeGrid != null && !list.Contains(cubeGrid))
                        list.Add(cubeGrid);
                }
            }
            return list;
        }
    }
}