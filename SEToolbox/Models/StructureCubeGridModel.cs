using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using SEToolbox.Interop;
using SEToolbox.Support;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using VRage;
using VRage.Game;
using VRage.Game.ObjectBuilders.Components;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using IDType = VRage.MyEntityIdentifier.ID_OBJECT_TYPE;
using Res = SEToolbox.Properties.Resources;
using SpaceEngineers.Game.Entities.Blocks;

namespace SEToolbox.Models
{
    [Serializable]
    public class StructureCubeGridModel : StructureBaseModel
    {
        #region Fields

        // Fields are marked as NonSerialized, as they aren't required during the drag-drop operation.

        [NonSerialized]
        private Point3D _min;

        [NonSerialized]
        private Point3D _max;

        [NonSerialized]
        private System.Windows.Media.Media3D.Vector3D _scale;

        [NonSerialized]
        private Size3D _size;

        [NonSerialized]
        private int _pilots;

        [NonSerialized]
        private TimeSpan _timeToProduce;

        [NonSerialized]
        private int _pcuToProduce;

        [NonSerialized]
        private string _cockpitOrientation;

        [NonSerialized]
        private List<CubeAssetModel> _cubeAssets;

        [NonSerialized]
        private List<CubeAssetModel> _componentAssets;

        [NonSerialized]
        private List<OreAssetModel> _ingotAssets;

        [NonSerialized]
        private List<OreAssetModel> _oreAssets;

        [NonSerialized]
        private string _activeComponentFilter;

        [NonSerialized]
        private string _componentFilter;

        [NonSerialized]
        private ObservableCollection<CubeItemModel> _cubeList;

        [NonSerialized]
        private static readonly object Locker = new();

        [NonSerialized]
        private bool _isSubsSystemNotReady;

        [NonSerialized]
        private bool _isConstructionNotReady;

        #endregion

        #region Ctor

        public StructureCubeGridModel(MyObjectBuilder_EntityBase entityBase)
            : base(entityBase)
        {
            IsSubsSystemNotReady = true;
            IsConstructionNotReady = true;
        }

        #endregion

        #region Properties

        public MyObjectBuilder_CubeGrid CubeGrid
        {
            get => EntityBase as MyObjectBuilder_CubeGrid;
        }

        public MyCubeSize GridSize
        {
            get => CubeGrid.GridSizeEnum;
            set => SetProperty(CubeGrid.GridSizeEnum, value, nameof(GridSize));
        }

        public bool IsStatic
        {
            get => CubeGrid.IsStatic;
            set => SetProperty(CubeGrid.IsStatic, value, nameof(IsStatic));
        }

        public bool Dampeners
        {
            get => CubeGrid.DampenersEnabled;
            set => SetProperty(CubeGrid.DampenersEnabled, value, nameof(Dampeners));
        }

        public bool Destructible
        {
            get => CubeGrid.DestructibleBlocks;
            set => SetProperty(CubeGrid.DestructibleBlocks, value, nameof(Destructible));
        }


        public override string DisplayName
        {
            get => base.DisplayName;
            set
            {
                base.DisplayName = value;
                CubeGrid.DisplayName = value;
            }
        }

        public Point3D Min
        {
            get => _min;
            set => SetProperty(ref _min, value, nameof(Min));
        }

        public Point3D Max
        {
            get => _max;
            set => SetProperty(ref _max, value, nameof(Max));
        }

        public System.Windows.Media.Media3D.Vector3D Scale
        {
            get => _scale;
            set => SetProperty(ref _scale, value, nameof(Scale));
        }

        public Size3D Size
        {
            get => _size;
            set => SetProperty(ref _size, value, nameof(Size));
        }

        public int Pilots
        {
            get => _pilots;
            set => SetProperty(ref _pilots, value, nameof(Pilots));    
            }

        public bool IsPiloted
        {
            get => Pilots > 0;
        }

        public bool IsDamaged
        {
            //check if any block is damaged per its IntegrityPercent
            get => CubeGrid.CubeBlocks.Any(cube => cube.IntegrityPercent < 1);
        }

        public int DamageCount
        {
            get => CubeGrid.CubeBlocks.Count(cube => cube.IntegrityPercent < 1);
        }

        public int SkeletonCount
        {
            get => CubeGrid.Skeleton.Count > 0 ? CubeGrid.Skeleton.Count : 0;
        }

        public override double LinearVelocity
        {
            get => CubeGrid.LinearVelocity.ToVector3().LinearVector();
        }

        /// This is not to be taken as an accurate representation.
        public double AngularVelocity
        {
            get => CubeGrid.AngularVelocity.ToVector3().LinearVector();
        }

        public TimeSpan TimeToProduce
        {
            get => _timeToProduce;
            set => SetProperty(ref _timeToProduce, value, nameof(TimeToProduce));
        }

        public int PCUToProduce
        {
            get => _pcuToProduce;
            set => SetProperty(ref _pcuToProduce, value, nameof(PCUToProduce));
        }

        public override int BlockCount
        {
            get => CubeGrid.CubeBlocks.Count;
        }

        public string CockpitOrientation
        {
            get => _cockpitOrientation;
            set => SetProperty(ref _cockpitOrientation, value, nameof(CockpitOrientation));
        }

        /// <summary>
        /// This is detail of the breakdown of cubes in the ship.
        /// </summary>
        public List<CubeAssetModel> CubeAssets
        {
            get => _cubeAssets;

            set => SetProperty(ref _cubeAssets, value, nameof(CubeAssets));
        }

        /// <summary>
        /// This is detail of the breakdown of components in the ship.
        /// </summary>
        public List<CubeAssetModel> ComponentAssets
        {
            get => _componentAssets;
            set => SetProperty(ref _componentAssets, value, nameof(ComponentAssets));

        }

        /// <summary>
        /// This is detail of the breakdown of ingots in the ship.
        /// </summary>
        public List<OreAssetModel> IngotAssets
        {
            get => _ingotAssets;
            set => SetProperty(ref _ingotAssets, value, nameof(IngotAssets));
        }

        /// <summary>
        /// This is detail of the breakdown of ore in the ship.
        /// </summary>
        public List<OreAssetModel> OreAssets
        {
            get => _oreAssets;
            set => SetProperty(ref _oreAssets, value, nameof(OreAssets));
        }

        public string ActiveComponentFilter
        {
            get => _activeComponentFilter;
            set => SetProperty(ref _activeComponentFilter, value, nameof(ActiveComponentFilter));
        }

        public string ComponentFilter
        {
            get => _componentFilter;
            set => SetProperty(ref _componentFilter, value, nameof(ComponentFilter));
        }

        public ObservableCollection<CubeItemModel> CubeList
        {
            get => _cubeList;
            set => SetProperty(ref _cubeList, value, nameof(CubeList));
        }

        public bool IsSubsSystemNotReady
        {
            get => _isSubsSystemNotReady;
            set => SetProperty(ref _isSubsSystemNotReady, value, nameof(IsSubsSystemNotReady));
        }

        public bool IsConstructionNotReady
        {
            get => _isConstructionNotReady;
            set => SetProperty(ref _isConstructionNotReady, value, nameof(IsConstructionNotReady));
        }

        public bool ToggleExcludedBlocks { get; set; }

        #endregion

        #region Methods

        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            SerializedEntity = SpaceEngineersApi.Serialize<MyObjectBuilder_CubeGrid>(CubeGrid ?? new MyObjectBuilder_CubeGrid());
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            EntityBase = SpaceEngineersApi.Deserialize<MyObjectBuilder_CubeGrid>(SerializedEntity);
        }

        public override void UpdateGeneralFromEntityBase()
        {
            double scaleMultiplyer = CubeGrid.GridSizeEnum.ToLength();

            _ = (IsStatic, CubeGrid.GridSizeEnum) switch
            {
                (true, MyCubeSize.Large) => ClassType = ClassType.LargeStation,
                (true, MyCubeSize.Small) => ClassType = ClassType.SmallStation,
                (false, MyCubeSize.Large) => ClassType = ClassType.LargeShip,
                (false, MyCubeSize.Small) => ClassType = ClassType.SmallShip,
                _ => ClassType = ClassType.Unknown,
            };


            Point3D min = new(int.MaxValue, int.MaxValue, int.MaxValue);
            Point3D max = new(int.MinValue, int.MinValue, int.MinValue);
            float totalMass = 0;

            foreach (MyObjectBuilder_CubeBlock block in CubeGrid.CubeBlocks)
            {
                min.X = Math.Min(min.X, block.Min.X);
                min.Y = Math.Min(min.Y, block.Min.Y);
                min.Z = Math.Min(min.Z, block.Min.Z);

                MyCubeBlockDefinition cubeDefinition = SpaceEngineersApi.GetCubeDefinition(block.TypeId, CubeGrid.GridSizeEnum, block.SubtypeName);

                // definition is null when the block no longer exists in the Cube definitions. Ie, Ladder, or a Mod that was removed.
                if (cubeDefinition == null || (cubeDefinition.Size.X == 1 && cubeDefinition.Size.Y == 1 && cubeDefinition.Size.Z == 1))
                {
                    max.X = Math.Max(max.X, block.Min.X);
                    max.Y = Math.Max(max.Y, block.Min.Y);
                    max.Z = Math.Max(max.Z, block.Min.Z);
                }
                else
                {
                    // resolve the cube size acording to the cube's orientation.
                    Vector3I orientSize = cubeDefinition.Size.Add(-1).Transform(block.BlockOrientation).Abs();
                    max.X = Math.Max(max.X, block.Min.X + orientSize.X);
                    max.Y = Math.Max(max.Y, block.Min.Y + orientSize.Y);
                    max.Z = Math.Max(max.Z, block.Min.Z + orientSize.Z);
                }

                MyCubeBlockDefinition cubeBlockDefinition = SpaceEngineersApi.GetCubeDefinition(block.TypeId, CubeGrid.GridSizeEnum, block.SubtypeName);

                float cubeMass = 0;
                if (cubeBlockDefinition != null)
                {
                    foreach (MyCubeBlockDefinition.Component component in cubeBlockDefinition.Components)
                    {
                        float componentMass = component.Definition.Mass * component.Count;
                        cubeMass += componentMass;
                    }
                }

                totalMass += cubeMass;
            }

            string cockpitOrientation = Res.ClsCockpitOrientationNone;
            MyObjectBuilder_CubeBlock[] cockpits = [.. CubeGrid.CubeBlocks.Where(b => b is MyObjectBuilder_Cockpit)];
            if (cockpits.Length > 0)
            {
                int count = cockpits.Count(b => b.BlockOrientation.Forward == cockpits[0].BlockOrientation.Forward && b.BlockOrientation.Up == cockpits[0].BlockOrientation.Up);
                if (cockpits.Length == count)
                {
                    // All cockpits share the same orientation.
                    cockpitOrientation = $"{Res.ClsCockpitOrientationForward}={cockpits[0].BlockOrientation.Forward} ({GetAxisIndicator(cockpits[0].BlockOrientation.Forward)}), Up={cockpits[0].BlockOrientation.Up} ({GetAxisIndicator(cockpits[0].BlockOrientation.Up)})";
                }
                else
                {
                    // multiple cockpits are present, and do not share a common orientation.
                    cockpitOrientation = Res.ClsCockpitOrientationMixed;
                }
            }
            CockpitOrientation = cockpitOrientation;

            var scale = max - min;
            scale.X++;
            scale.Y++;
            scale.Z++;

            if (CubeGrid.CubeBlocks.Count == 0)
                scale = new System.Windows.Media.Media3D.Vector3D();

            Min = min;
            Max = max;
            Scale = scale;
            Size = new Size3D(scale.X * scaleMultiplyer, scale.Y * scaleMultiplyer, scale.Z * scaleMultiplyer);
            Mass = totalMass;

            QuaternionD quaternion = CubeGrid.PositionAndOrientation.Value.ToQuaternionD();
            VRageMath.Vector3D p1 = (min.ToVector3D() * CubeGrid.GridSizeEnum.ToLength()).Transform(quaternion) + CubeGrid.PositionAndOrientation.Value.Position - (CubeGrid.GridSizeEnum.ToLength() / 2);
            VRageMath.Vector3D p2 = ((min.ToVector3D() + Scale.ToVector3D()) * CubeGrid.GridSizeEnum.ToLength()).Transform(quaternion) + CubeGrid.PositionAndOrientation.Value.Position - (CubeGrid.GridSizeEnum.ToLength() / 2);
            //var p1 = VRageMath.Vector3D.Transform(min.ToVector3D() * CubeGrid.GridSizeEnum.ToLength(), quaternion) + CubeGrid.PositionAndOrientation.Value.Position - (CubeGrid.GridSizeEnum.ToLength() / 2);
            //var p2 = VRageMath.Vector3D.Transform((min.ToVector3D() + Scale.ToVector3D()) * CubeGrid.GridSizeEnum.ToLength(), quaternion) + CubeGrid.PositionAndOrientation.Value.Position - (CubeGrid.GridSizeEnum.ToLength() / 2);
            WorldAabb = new BoundingBoxD(VRageMath.Vector3D.Min(p1, p2), VRageMath.Vector3D.Max(p1, p2));
            Center = WorldAabb.Center;

            DisplayName = CubeGrid.DisplayName;

            // Add Beacon or Antenna detail for the Description.
            MyObjectBuilder_CubeBlock[] broadcasters = [.. CubeGrid.CubeBlocks.Where(b => b.SubtypeName == SubtypeId.LargeBlockBeacon.ToString()
                || b.SubtypeName == SubtypeId.SmallBlockBeacon.ToString()
                || b.SubtypeName == SubtypeId.LargeBlockRadioAntenna.ToString()
                || b.SubtypeName == SubtypeId.SmallBlockRadioAntenna.ToString())];
            string broadcastNames = string.Empty;
            if (broadcasters.Length > 0)
            {
                string[] beaconNames = [.. broadcasters.Where(b => b is MyObjectBuilder_Beacon).Select(b => ((MyObjectBuilder_Beacon)b).CustomName ?? "Beacon")];
                string[] antennaNames = [.. broadcasters.Where(b => b is MyObjectBuilder_RadioAntenna).Select(b => ((MyObjectBuilder_RadioAntenna)b).CustomName ?? "Antenna")];
                broadcastNames = string.Join("|", beaconNames.Concat(antennaNames).OrderBy(s => s));
            }

            if (CubeGrid.CubeBlocks.Count == 1)
            {
                if (CubeGrid.CubeBlocks[0] is MyObjectBuilder_Wheel)
                {
                    MyObjectBuilder_CubeGrid grid = ExplorerModel.Default.FindConnectedTopBlock<MyObjectBuilder_MotorSuspension>(CubeGrid.CubeBlocks[0].EntityId);

                    if (grid == null)
                        Description = Res.ClsCubeGridWheelDetached;
                    else
                        Description = string.Format(Res.ClsCubeGridWheelAttached, grid.DisplayName);
                    return;
                }
            }

            if (string.IsNullOrEmpty(broadcastNames))
                Description = string.Format($"{Scale.X}x{Scale.Y}x{Scale.Z}");
            else

                Description = $"{broadcastNames} {Scale.X}x{Scale.Y}x{Scale.Z}";


            // Reflectors Status
            bool reflectorsOn = CubeGrid.CubeBlocks.OfType<MyObjectBuilder_ReflectorLight>().Any(light => light.Enabled);

            // Speed Calculation
            float speed = CubeGrid.LinearVelocity.ToVector3().Length();

            // Power Usage
            float totalPowerUsage = 0;
            foreach (IMyPowerProducer block in CubeGrid.CubeBlocks.OfType<IMyPowerProducer>())
            {
                totalPowerUsage += block.CurrentOutput;
            }

            // Reactors Output
            float totalReactorOutput = 0;
            foreach (IMyPowerProducer reactor in CubeGrid.CubeBlocks.OfType<IMyPowerProducer>())
            {
                totalReactorOutput += reactor.CurrentOutput;
            }

            // Thrusts
            int thrustCount = CubeGrid.CubeBlocks.OfType<MyObjectBuilder_Thrust>().Count();

            // Gyros
            int gyroCount = CubeGrid.CubeBlocks.OfType<MyObjectBuilder_Gyro>().Count();

            // Fuel Time
            float totalFuelTime = 0;
            foreach (IMyPowerProducer block in CubeGrid.CubeBlocks.OfType<IMyPowerProducer>())

                _ = block switch
                {
                    IMyPowerProducer when block is IMyReactor reactor => totalFuelTime += reactor.CurrentOutput,
                    IMyPowerProducer when block is IMyBatteryBlock battery => totalFuelTime += battery.CurrentOutput,
                    IMyPowerProducer when block is MySolarPanel solarPanel => totalFuelTime += solarPanel.CurrentOutput,
                    IMyPowerProducer when block is IMyWindTurbine windTurbine => totalFuelTime += windTurbine.CurrentOutput,
                    _ => totalFuelTime += 0

                };


            decimal totalFuel = 0;
            decimal totalPower = 0;

            foreach (IMyPowerProducer block in CubeGrid.CubeBlocks.OfType<IMyPowerProducer>())
            {
                if (block == null) continue;

                float currentOutput = block.CurrentOutput;

                _ = block switch
                {
                    _ when block is IMyReactor reactor => totalFuel += (decimal)reactor.CurrentOutput,
                    _ when block is IMyBatteryBlock battery => totalFuel += (decimal)battery.CurrentStoredPower,
                    _ when block is MySolarPanel solarPanel => totalFuel += (decimal)currentOutput,
                    _ when block is IMyWindTurbine windTurbine => totalFuel += (decimal)currentOutput,
                    _ => totalFuel += 0
                };

                totalPower += (decimal)currentOutput;
            }

            // Report
            SConsole.WriteLine($"Reflectors On: {reflectorsOn}");
            SConsole.WriteLine($"Mass: {Mass} Kg");
            SConsole.WriteLine($"Speed: {speed} m/s");
            SConsole.WriteLine($"Power Usage: {totalPowerUsage}%");
            SConsole.WriteLine($"Reactors: {totalReactorOutput} GW");
            SConsole.WriteLine($"Thrusts: {thrustCount}");
            SConsole.WriteLine($"Gyros: {gyroCount}");
            SConsole.WriteLine($"Fuel Time: {totalFuelTime} sec");

        }



        public override void InitializeAsync()
        {
            BackgroundWorker worker = new();

            worker.DoWork += delegate (object s, DoWorkEventArgs workArgs)
            {
                lock (Locker)
                {
                    // Because I've bound models to the view, this is going to get messy.
                    string contentPath = ToolboxUpdater.GetApplicationContentPath();

                    if (IsConstructionNotReady)
                    {
                        Dictionary<string, BlueprintRequirement> ingotRequirements = [];
                        Dictionary<string, BlueprintRequirement> oreRequirements = [];
                        TimeSpan timeTaken = new();
                        Dictionary<string, CubeAssetModel> cubeAssetDict = [];
                        Dictionary<string, CubeAssetModel> componentAssetDict = [];
                        List<CubeAssetModel> cubeAssets = [];
                        List<CubeAssetModel> componentAssets = [];
                        List<OreAssetModel> ingotAssets = [];
                        List<OreAssetModel> oreAssets = [];
                        int pcuUsed = 0;

                        foreach (MyObjectBuilder_CubeBlock block in CubeGrid.CubeBlocks)
                        {
                            string blockName = block.SubtypeName;
                            if (string.IsNullOrEmpty(blockName))
                            {
                                blockName = block.TypeId.ToString();
                            }

                            MyCubeBlockDefinition cubeBlockDefinition = SpaceEngineersApi.GetCubeDefinition(block.TypeId, CubeGrid.GridSizeEnum, block.SubtypeName);

                            float cubeMass = 0;
                            TimeSpan blockTime = TimeSpan.Zero;
                            string blockTexture = null;
                            int pcu = 0;

                            if (cubeBlockDefinition != null)
                            {
                                foreach (MyCubeBlockDefinition.Component component in cubeBlockDefinition.Components)
                                {
                                    SpaceEngineersApi.AccumulateCubeBlueprintRequirements(component.Definition.Id.SubtypeName, component.Definition.Id.TypeId, component.Count, ingotRequirements, out TimeSpan componentTime);
                                    timeTaken += componentTime;

                                    float componentMass = component.Definition.Mass * component.Count;
                                    float componentVolume = component.Definition.Volume * SpaceEngineersConsts.VolumeMultiplyer * component.Count;
                                    cubeMass += componentMass;

                                    string componentName = component.Definition.Id.SubtypeName;
                                    if (componentAssetDict.ContainsKey(componentName))
                                    {
                                        componentAssetDict[componentName].Count += component.Count;
                                        componentAssetDict[componentName].Mass += componentMass;
                                        componentAssetDict[componentName].Volume += componentVolume;
                                        componentAssetDict[componentName].Time += componentTime;
                                    }
                                    else
                                    {
                                        string componentTexture = SpaceEngineersCore.GetDataPathOrDefault(component.Definition.Icons.First(), Path.Combine(contentPath, component.Definition.Icons.First()));
                                        CubeAssetModel m = new() { Name = component.Definition.DisplayNameText, Mass = componentMass, Volume = componentVolume, Count = component.Count, Time = componentTime, TextureFile = componentTexture };
                                        componentAssets.Add(m);
                                        componentAssetDict.Add(componentName, m);
                                    }
                                }

                                blockTime = TimeSpan.FromSeconds(cubeBlockDefinition.IntegrityPointsPerSec != 0 ? cubeBlockDefinition.MaxIntegrity / cubeBlockDefinition.IntegrityPointsPerSec : 0);
                                blockTexture = (cubeBlockDefinition.Icons == null || cubeBlockDefinition.Icons.First() == null) ? null : SpaceEngineersCore.GetDataPathOrDefault(cubeBlockDefinition.Icons.First(), Path.Combine(contentPath, cubeBlockDefinition.Icons.First()));
                                pcu = cubeBlockDefinition.PCU;
                                pcuUsed += cubeBlockDefinition.PCU;
                            }

                            timeTaken += blockTime;

                            if (cubeAssetDict.ContainsKey(blockName))
                            {
                                cubeAssetDict[blockName].Count++;
                                cubeAssetDict[blockName].Mass += cubeMass;
                                cubeAssetDict[blockName].Time += blockTime;
                                cubeAssetDict[blockName].PCU += pcu;
                            }
                            else
                            {
                                CubeAssetModel m = new() { Name = cubeBlockDefinition == null ? blockName : cubeBlockDefinition.DisplayNameText, Mass = cubeMass, Count = 1, TextureFile = blockTexture, Time = blockTime, PCU = pcu };
                                cubeAssets.Add(m);
                                cubeAssetDict.Add(blockName, m);
                            }
                        }

                        foreach (KeyValuePair<string, BlueprintRequirement> kvp in ingotRequirements)
                        {
                            SpaceEngineersApi.AccumulateCubeBlueprintRequirements(kvp.Value.SubtypeId, kvp.Value.Id.TypeId, kvp.Value.Amount, oreRequirements, out TimeSpan ingotTime);
                            MyDefinitionBase mydb = MyDefinitionManager.Static.GetDefinition(kvp.Value.Id);
                            if (mydb.Id.TypeId.IsNull)
                                continue;
                            MyPhysicalItemDefinition cd = (MyPhysicalItemDefinition)mydb;
                            string componentTexture = SpaceEngineersCore.GetDataPathOrDefault(cd.Icons.First(), Path.Combine(contentPath, cd.Icons.First()));
                            double volume = (double)kvp.Value.Amount * cd.Volume * SpaceEngineersConsts.VolumeMultiplyer;
                            OreAssetModel ingotAsset = new() { Name = cd.DisplayNameText, Amount = kvp.Value.Amount, Mass = (double)kvp.Value.Amount * cd.Mass, Volume = volume, Time = ingotTime, TextureFile = componentTexture };
                            ingotAssets.Add(ingotAsset);
                            timeTaken += ingotTime;
                        }

                        foreach (KeyValuePair<string, BlueprintRequirement> kvp in oreRequirements)
                        {
                            if (MyDefinitionManager.Static.GetDefinition(kvp.Value.Id) is MyPhysicalItemDefinition cd)
                                if (cd != null)
                                {
                                    string componentTexture = SpaceEngineersCore.GetDataPathOrDefault(cd.Icons.First(), Path.Combine(contentPath, cd.Icons.First()));
                                    double volume = (double)kvp.Value.Amount * cd.Volume * SpaceEngineersConsts.VolumeMultiplyer;
                                    OreAssetModel oreAsset = new() { Name = cd.DisplayNameText, Amount = kvp.Value.Amount, Mass = (double)kvp.Value.Amount * cd.Mass, Volume = volume, TextureFile = componentTexture };
                                    oreAssets.Add(oreAsset);
                                }
                        }

                        _dispatcher.Invoke(DispatcherPriority.Input, (Action)delegate
                        {
                            CubeAssets = cubeAssets;
                            ComponentAssets = componentAssets;
                            IngotAssets = ingotAssets;
                            OreAssets = oreAssets;
                            TimeToProduce = timeTaken;
                            PCUToProduce = pcuUsed;
                        });

                        IsConstructionNotReady = false;
                    }

                    if (IsSubsSystemNotReady)
                    {
                        List<CubeItemModel> cubeList = [];

                        foreach (MyObjectBuilder_CubeBlock block in CubeGrid.CubeBlocks)
                        {
                            MyCubeBlockDefinition cubeDefinition = SpaceEngineersApi.GetCubeDefinition(block.TypeId, CubeGrid.GridSizeEnum, block.SubtypeName);

                            _dispatcher.Invoke(DispatcherPriority.Input, (Action)delegate
                            {
                                cubeList.Add(new CubeItemModel(block, cubeDefinition)
                                {
                                    TextureFile = (cubeDefinition == null || cubeDefinition.Icons == null || cubeDefinition.Icons.First() == null) ? null : SpaceEngineersCore.GetDataPathOrDefault(cubeDefinition.Icons.First(), Path.Combine(contentPath, cubeDefinition.Icons.First()))
                                });
                            });
                        }

                        _dispatcher.Invoke(DispatcherPriority.Input, (Action)delegate
                        {
                            CubeList = new ObservableCollection<CubeItemModel>(cubeList);
                        });

                        IsSubsSystemNotReady = false;
                    }
                }
            };

            worker.RunWorkerAsync();
        }

        /// <summary>
        /// Find any Cockpits that have player character/s in them.
        /// </summary>
        /// <returns></returns>
        public List<MyObjectBuilder_Cockpit> GetActiveCockpits()
        {
            List<MyObjectBuilder_Cockpit> list = [];

            foreach (MyObjectBuilder_CubeBlock cube in CubeGrid.CubeBlocks.Where(e => e is MyObjectBuilder_Cockpit))
            {
                if (cube.ComponentContainer?.Components?.FirstOrDefault(e => e.TypeId == "MyHierarchyComponentBase")?.Component is MyObjectBuilder_HierarchyComponentBase hierarchyBase)
                {
                    if (hierarchyBase.Children.Any(e => e is MyObjectBuilder_Character))
                        list.Add((MyObjectBuilder_Cockpit)cube);
                }
            }

            return list;
        }

        public void RepairAllDamage()
        {
            if (CubeGrid.Skeleton == null)
                CubeGrid.Skeleton = [];
            else
                CubeGrid.Skeleton.Clear();

            foreach (MyObjectBuilder_CubeBlock cube in CubeGrid.CubeBlocks)
            {
                cube.IntegrityPercent = cube.BuildPercent;
                // No need to set bones for individual blocks like rounded armor, as this is taken from the definition within the game itself.
            }

            OnPropertyChanged(nameof(IsDamaged));
            OnPropertyChanged(nameof(DamageCount));
        }

        public void ResetLinearVelocity()
        {
            CubeGrid.LinearVelocity = new Vector3(0, 0, 0);
            OnPropertyChanged(nameof(LinearVelocity));
        }

        public void ResetRotationVelocity()
        {
            CubeGrid.AngularVelocity = new Vector3(0, 0, 0);
            OnPropertyChanged(nameof(AngularVelocity));
        }

        public void ResetVelocity()
        {
            CubeGrid.LinearVelocity = new Vector3(0, 0, 0);
            CubeGrid.AngularVelocity = new Vector3(0, 0, 0);
            OnPropertyChanged(nameof(LinearVelocity));
        }

        public void ReverseVelocity()
        {
            CubeGrid.LinearVelocity = new Vector3(CubeGrid.LinearVelocity.X * -1, CubeGrid.LinearVelocity.Y * -1, CubeGrid.LinearVelocity.Z * -1);
            CubeGrid.AngularVelocity = new Vector3(CubeGrid.AngularVelocity.X * -1, CubeGrid.AngularVelocity.Y * -1, CubeGrid.AngularVelocity.Z * -1);
            OnPropertyChanged(nameof(LinearVelocity));
        }

        public void MaxVelocityAtPlayer(VRageMath.Vector3D playerPosition)
        {
            VRageMath.Vector3D v = playerPosition - CubeGrid.PositionAndOrientation.Value.Position;
            v.Normalize();
            v = Vector3.Multiply(v, SpaceEngineersConsts.MaxShipVelocity);

            CubeGrid.LinearVelocity = (Vector3)v;
            CubeGrid.AngularVelocity = new Vector3(0, 0, 0);
            OnPropertyChanged(nameof(LinearVelocity));
        }

        public bool ConvertFromLightToHeavyArmor()
        {
            bool changes = false;
            foreach (MyObjectBuilder_CubeBlock cube in CubeGrid.CubeBlocks)
                changes |= CubeItemModel.ConvertFromLightToHeavyArmor(cube);

            if (changes)
            {
                IsSubsSystemNotReady = true;
                IsConstructionNotReady = true;
                UpdateGeneralFromEntityBase();
                InitializeAsync();
            }
            return changes;
        }

        public bool ConvertFromHeavyToLightArmor()
        {
            bool changes = false;
            foreach (MyObjectBuilder_CubeBlock cube in CubeGrid.CubeBlocks)
                changes |= CubeItemModel.ConvertFromHeavyToLightArmor(cube);

            if (changes)
            {
                IsSubsSystemNotReady = true;
                IsConstructionNotReady = true;
                UpdateGeneralFromEntityBase();
                InitializeAsync();
            }
            return changes;
        }

        public void ConvertToFramework(float value)
        {
            foreach (MyObjectBuilder_CubeBlock cube in CubeGrid.CubeBlocks)
            {
                cube.IntegrityPercent = value;
                cube.BuildPercent = value;
            }

            UpdateGeneralFromEntityBase();
        }

        public void ConvertToStation()
        {
            ResetVelocity();
            CubeGrid.IsStatic = true;
            UpdateGeneralFromEntityBase();
        }

        public int SetInertiaTensor(bool state)
        {
            int count = 0;

            foreach (MyObjectBuilder_CubeBlock cube in CubeGrid.CubeBlocks)
            {
                if (cube is MyObjectBuilder_MechanicalConnectionBlock mechanicalBlock)
                {
                    if (mechanicalBlock.ShareInertiaTensor != state)
                    {
                        count++;
                        mechanicalBlock.ShareInertiaTensor = state;
                    }
                }
            }

            return count;
        }

        public void ReorientStation()
        {
            MyPositionAndOrientation pos = CubeGrid.PositionAndOrientation.Value;
            pos.Position = pos.Position.RoundOff(MyCubeSize.Large.ToLength());
            pos.Forward = new SerializableVector3(-1, 0, 0); // The Station orientation has to be fixed, otherwise it glitches when you copy the object in game.
            pos.Up = new SerializableVector3(0, 1, 0);
            CubeGrid.PositionAndOrientation = pos;
        }

        public void RotateStructure(VRageMath.Quaternion quaternion)
        {
            // Rotate the ship/station in specified direction.
            VRageMath.Quaternion o = CubeGrid.PositionAndOrientation.Value.ToQuaternion() * quaternion;
            o.Normalize();
            MyPositionAndOrientation p = new(o.ToMatrix());

            CubeGrid.PositionAndOrientation = new MyPositionAndOrientation
            {
                Position = CubeGrid.PositionAndOrientation.Value.Position,
                Forward = p.Forward,
                Up = p.Up
            };

            UpdateGeneralFromEntityBase();
        }

        public void RotateCubes(VRageMath.Quaternion quaternion)
        {
            foreach (MyObjectBuilder_CubeBlock cube in CubeGrid.CubeBlocks)
            {
                MyCubeBlockDefinition definition = SpaceEngineersApi.GetCubeDefinition(cube.TypeId, CubeGrid.GridSizeEnum, cube.SubtypeName);

                if (definition.Size.X == 1 && definition.Size.Y == 1 && definition.Size.Z == 1)
                {
                    // rotate position around origin.
                    cube.Min = Vector3I.Transform(cube.Min.ToVector3I(), quaternion);
                }
                else
                {
                    // resolve size of component, and transform to original orientation.
                    Vector3I orientSize = definition.Size.Add(-1).Transform(cube.BlockOrientation).Abs();

                    Vector3I min = Vector3I.Transform(cube.Min.ToVector3I(), quaternion);
                    Vector3I blockMax = new(cube.Min.X + orientSize.X, cube.Min.Y + orientSize.Y, cube.Min.Z + orientSize.Z);
                    Vector3I max = Vector3I.Transform(blockMax, quaternion);

                    cube.Min = new SerializableVector3I(Math.Min(min.X, max.X), Math.Min(min.Y, max.Y), Math.Min(min.Z, max.Z));
                }

                // rotate BlockOrientation.
                VRageMath.Quaternion q = quaternion * cube.BlockOrientation.ToQuaternion();
                q.Normalize();
                cube.BlockOrientation = new SerializableBlockOrientation(ref q);
            }

            // Rotate Groupings.
            foreach (MyObjectBuilder_BlockGroup group in CubeGrid.BlockGroups)
            {
                for (int i = 0; i < group.Blocks.Count; i++)
                {
                    // The Group location is in the center of the cube.
                    // It doesn't have to be exact though, as it appears SE is just doing a location test of whatever object is at that location.
                    group.Blocks[i] = Vector3I.Transform(group.Blocks[i], quaternion);
                }
            }

            // Rotate Bones if Skeleton is not null
            if (CubeGrid.Skeleton != null)
            {
                for (int i = 0; i < CubeGrid.Skeleton.Count; i++)
                {
                    BoneInfo bone = CubeGrid.Skeleton[i];
                    bone.BonePosition = Vector3I.Transform(bone.BonePosition, quaternion);
                    bone.BoneOffset = bone.BoneOffset.Transform(VRageMath.Quaternion.Inverse(quaternion));
                    CubeGrid.Skeleton[i] = bone; // Reassign the modified bone back to the collection
                }
            }


            // Rotate ConveyorLines
            if (CubeGrid.ConveyorLines != null)
            {
                foreach (MyObjectBuilder_ConveyorLine ConveyorLine in CubeGrid.ConveyorLines)
                {
                    ConveyorLine.StartPosition = Vector3I.Transform(ConveyorLine.StartPosition, quaternion);
                    ConveyorLine.EndPosition = Vector3I.Transform(ConveyorLine.EndPosition, quaternion);

                    {
                        Vector3 startDirectionVector = Base6Directions.GetVector(ConveyorLine.StartDirection);
                        startDirectionVector = Vector3.Transform(startDirectionVector, quaternion);
                        ConveyorLine.StartDirection = Base6Directions.GetDirection(startDirectionVector);

                        Vector3 endDirectionVector = Base6Directions.GetVector(ConveyorLine.EndDirection);
                        endDirectionVector = Vector3.Transform(endDirectionVector, quaternion);
                        ConveyorLine.EndDirection = Base6Directions.GetDirection(endDirectionVector);
                    }
                }
            }

            // Rotate the ship also to maintain the appearance that it has not changed.
            VRageMath.Quaternion o = CubeGrid.PositionAndOrientation.Value.ToQuaternion() * VRageMath.Quaternion.Inverse(quaternion);
            o.Normalize();
            MyPositionAndOrientation p = new(o.ToMatrix());

            CubeGrid.PositionAndOrientation = new MyPositionAndOrientation
            {
                Position = CubeGrid.PositionAndOrientation.Value.Position,
                Forward = p.Forward,
                Up = p.Up
            };

            UpdateGeneralFromEntityBase();
        }

        public void ConvertToShip()
        {
            CubeGrid.IsStatic = false;
            UpdateGeneralFromEntityBase();
        }

        public bool ConvertToCornerArmor()
        {
            int count = 0;
            count += CubeGrid.CubeBlocks.Where(c => c.SubtypeName == SubtypeId.LargeRoundArmor_Corner.ToString()).Select(c => { c.SubtypeName = SubtypeId.LargeBlockArmorCorner.ToString(); return c; }).ToList().Count;
            count += CubeGrid.CubeBlocks.Where(c => c.SubtypeName == SubtypeId.LargeRoundArmor_Slope.ToString()).Select(c => { c.SubtypeName = SubtypeId.LargeBlockArmorSlope.ToString(); return c; }).ToList().Count;
            count += CubeGrid.CubeBlocks.Where(c => c.SubtypeName == SubtypeId.LargeRoundArmor_CornerInv.ToString()).Select(c => { c.SubtypeName = SubtypeId.LargeBlockArmorCornerInv.ToString(); return c; }).ToList().Count;
            return count > 0;
        }

        public bool ConvertToRoundArmor()
        {
            int count = 0;
            count += CubeGrid.CubeBlocks.Where(c => c.SubtypeName == SubtypeId.LargeBlockArmorCorner.ToString()).Select(c => { c.SubtypeName = SubtypeId.LargeRoundArmor_Corner.ToString(); return c; }).ToList().Count;
            count += CubeGrid.CubeBlocks.Where(c => c.SubtypeName == SubtypeId.LargeBlockArmorSlope.ToString()).Select(c => { c.SubtypeName = SubtypeId.LargeRoundArmor_Slope.ToString(); return c; }).ToList().Count;
            count += CubeGrid.CubeBlocks.Where(c => c.SubtypeName == SubtypeId.LargeBlockArmorCornerInv.ToString()).Select(c => { c.SubtypeName = SubtypeId.LargeRoundArmor_CornerInv.ToString(); return c; }).ToList().Count;
            return count > 0;
        }

        #region Mirror

        public bool MirrorModel(bool usePlane, bool oddMirror)
        {
            (Mirror xMirror, Mirror yMirror, Mirror zMirror) = (Mirror.None, Mirror.None, Mirror.None);
            (int xAxis, int yAxis, int zAxis) = (0, 0, 0);
            int count = 0;

            if (!usePlane)
            {
                // Find mirror Axis.
                if (!CubeGrid.XMirroxPlane.HasValue && !CubeGrid.YMirroxPlane.HasValue && !CubeGrid.ZMirroxPlane.HasValue)
                {
                    // Find the largest contiguous exterior surface to use as the mirror.

                    (int minX, int maxX) = (CubeGrid.CubeBlocks.Min(c => c.Min.X), CubeGrid.CubeBlocks.Max(c => c.Min.X));
                    (int minY, int maxY) = (CubeGrid.CubeBlocks.Min(c => c.Min.Y), CubeGrid.CubeBlocks.Max(c => c.Min.Y));
                    (int minZ, int maxZ) = (CubeGrid.CubeBlocks.Min(c => c.Min.Z), CubeGrid.CubeBlocks.Max(c => c.Min.Z));

                    int[] counts =
                    [
                        CubeGrid.CubeBlocks.Count(c => c.Min.X == minX),
                        CubeGrid.CubeBlocks.Count(c => c.Min.Y == minY),
                        CubeGrid.CubeBlocks.Count(c => c.Min.Z == minZ),
                        CubeGrid.CubeBlocks.Count(c => c.Min.X == maxX),
                        CubeGrid.CubeBlocks.Count(c => c.Min.Y == maxY),
                        CubeGrid.CubeBlocks.Count(c => c.Min.Z == maxZ)
                    ];


                    int[] axis = [minX, minY, minZ, maxX, maxY, maxZ];
                    Mirror[] mirrorTypes = [Mirror.EvenDown, Mirror.EvenUp, Mirror.Odd];

                    for (int i = 0; i < counts.Length; i++)
                    {
                        if (counts[i] > counts.Max())
                        {
                            if (i < 3) // Min values
                            {
                                xMirror = oddMirror ? Mirror.Odd : mirrorTypes[0];
                                xAxis = axis[i];
                            }
                            else // Max values
                            {
                                xMirror = oddMirror ? Mirror.Odd : mirrorTypes[1];
                                xAxis = axis[i];
                            }
                            break;
                        }
                    }


                    MyObjectBuilder_CubeBlock[] cubes = [.. MirrorCubes(this, false, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis)];
                    CubeGrid.CubeBlocks.AddRange(cubes);
                    count += cubes.Length;

                    UpdateGeneralFromEntityBase();
                    OnPropertyChanged(nameof(BlockCount));
                    return count > 0;
                }

                // Use the built-in Mirror plane defined in the game.
                switch (true)
                {
                    case var _ when CubeGrid.XMirroxPlane.HasValue:
                        xMirror = CubeGrid.XMirroxOdd ? Mirror.EvenDown : Mirror.Odd;
                        xAxis = CubeGrid.XMirroxPlane.Value.X;
                        MyObjectBuilder_CubeBlock[] cubesX = [.. MirrorCubes(this, true, xMirror, xAxis, Mirror.None, 0, Mirror.None, 0)];
                        CubeGrid.CubeBlocks.AddRange(cubesX);
                        count += cubesX.Length;
                        MirrorBlockGroups(CubeGrid, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                        MirrorConveyorLines(CubeGrid, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                        break;

                    case var _ when CubeGrid.YMirroxPlane.HasValue:
                        yMirror = CubeGrid.YMirroxOdd ? Mirror.EvenDown : Mirror.Odd;
                        yAxis = CubeGrid.YMirroxPlane.Value.Y;
                        MyObjectBuilder_CubeBlock[] cubesY = [.. MirrorCubes(this, true, Mirror.None, 0, yMirror, yAxis, Mirror.None, 0)];
                        CubeGrid.CubeBlocks.AddRange(cubesY);
                        count += cubesY.Length;
                        MirrorBlockGroups(CubeGrid, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                        MirrorConveyorLines(CubeGrid, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                        break;

                    case var _ when CubeGrid.ZMirroxPlane.HasValue:
                        zMirror = CubeGrid.ZMirroxOdd ? Mirror.EvenUp : Mirror.Odd;
                        zAxis = CubeGrid.ZMirroxPlane.Value.Z;
                        MyObjectBuilder_CubeBlock[] cubesZ = [.. MirrorCubes(this, true, Mirror.None, 0, Mirror.None, 0, zMirror, zAxis)];
                        CubeGrid.CubeBlocks.AddRange(cubesZ);
                        count += cubesZ.Length;
                        MirrorBlockGroups(CubeGrid, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                        MirrorConveyorLines(CubeGrid, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                        break;
                }
            }


            UpdateGeneralFromEntityBase();
            OnPropertyChanged(nameof(BlockCount));
            return count > 0;
        }

        public void MirrorBlockGroups(MyObjectBuilder_CubeGrid CubeGrid, Mirror xMirror, int xAxis, Mirror yMirror, int yAxis, Mirror zMirror, int zAxis)
        {
            if (CubeGrid.BlockGroups != null)
            {
                foreach (MyObjectBuilder_BlockGroup group in CubeGrid.BlockGroups)
                {
                    MyObjectBuilder_BlockGroup mirroredGroup = new()
                    {
                        Name = group.Name + "_Mirrored",
                        Blocks = [.. group.Blocks.Select(pos => {
                                MyObjectBuilder_CubeBlock block = CubeGrid.CubeBlocks.FirstOrDefault(b => b.Min.X == pos.X && b.Min.Y == pos.Y && b.Min.Z == pos.Z);
                                return block != null ? MirrorBlock(block, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis).Min.ToVector3I() : pos;
                            })]
                    };
                    CubeGrid.BlockGroups.Add(mirroredGroup);
                }
            }
        }

        private void MirrorConveyorLines(MyObjectBuilder_CubeGrid CubeGrid, Mirror xMirror, int xAxis, Mirror yMirror, int yAxis, Mirror zMirror, int zAxis)
        {
            // Mirror ConveyorLines
            if (CubeGrid.ConveyorLines != null)
            {
                foreach (MyObjectBuilder_ConveyorLine conveyorLine in CubeGrid.ConveyorLines)
                {
                    MyObjectBuilder_ConveyorLine mirroredLine = new()
                    {
                        StartPosition = MirrorPosition(conveyorLine.StartPosition, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis),
                        EndPosition = MirrorPosition(conveyorLine.EndPosition, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis),
                        ConveyorLineType = conveyorLine.ConveyorLineType,
                    };
                    CubeGrid.ConveyorLines.Add(mirroredLine);
                }
            }

        }


        // Helper method to mirror a block
        private MyObjectBuilder_CubeBlock MirrorBlock(MyObjectBuilder_CubeBlock block, Mirror xMirror, int xAxis, Mirror yMirror, int yAxis, Mirror zMirror, int zAxis)
        {
            MyObjectBuilder_CubeBlock newBlock = block.Clone() as MyObjectBuilder_CubeBlock;
            newBlock.EntityId = block.EntityId == 0 ? 0 : SpaceEngineersApi.GenerateEntityId(IDType.ENTITY);

            if (block is MyObjectBuilder_MotorBase motorBlock)
            {
                ((MyObjectBuilder_MotorBase)newBlock).RotorEntityId = motorBlock.RotorEntityId == 0 ? 0 : SpaceEngineersApi.GenerateEntityId(IDType.ENTITY);
            }

            if (block is MyObjectBuilder_PistonBase pistonBlock)
            {
                ((MyObjectBuilder_PistonBase)newBlock).TopBlockId = pistonBlock.TopBlockId == 0 ? 0 : SpaceEngineersApi.GenerateEntityId(IDType.ENTITY);
            }



            MyCubeBlockDefinition definition = SpaceEngineersApi.GetCubeDefinition(block.TypeId, CubeGrid.GridSizeEnum, block.SubtypeName);
            MirrorCubeOrientation(definition, block.BlockOrientation, xMirror, yMirror, zMirror, out MyCubeBlockDefinition mirrorDefinition, out newBlock.BlockOrientation);

            newBlock.SubtypeName = mirrorDefinition.Id.SubtypeName;

            SerializableVector3I min, max;
            if (definition.Size.X == 1 && definition.Size.Y == 1 && definition.Size.Z == 1)
            {
                newBlock.Min = block.Min.Mirror(xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                _ = newBlock.Min;
            }
            else
            {
                Vector3I orientSize = definition.Size.Add(-1).Transform(block.BlockOrientation).Abs();
                min = block.Min.Mirror(xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                SerializableVector3I blockMax = new(block.Min.X + orientSize.X, block.Min.Y + orientSize.Y, block.Min.Z + orientSize.Z);
                max = blockMax.Mirror(xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);

                newBlock.Min = new SerializableVector3I(
                    xMirror != Mirror.None ? max.X : min.X,
                    yMirror != Mirror.None ? max.Y : min.Y,
                    zMirror != Mirror.None ? max.Z : min.Z
                );
            }

            return newBlock;
        }
        // Helper method to mirror a position

        private static Vector3I MirrorPosition(Vector3I position, Mirror xMirror, int xAxis, Mirror yMirror, int yAxis, Mirror zMirror, int zAxis)
        {
            Vector3I mirroredPosition = new(
                xMirror == Mirror.None ? position.X : xAxis - position.X,
                yMirror == Mirror.None ? position.Y : yAxis - position.Y,
                zMirror == Mirror.None ? position.Z : zAxis - position.Z
            );
            return mirroredPosition;
        }
        private static IEnumerable<MyObjectBuilder_CubeBlock> MirrorCubes(StructureCubeGridModel viewModel, bool integrate, Mirror xMirror, int xAxis, Mirror yMirror, int yAxis, Mirror zMirror, int zAxis)
        {
            List<MyObjectBuilder_CubeBlock> blocks = [];

            if (xMirror == Mirror.None && yMirror == Mirror.None && zMirror == Mirror.None)
                return blocks;

            foreach (MyObjectBuilder_CubeBlock block in viewModel.CubeGrid.CubeBlocks)
            {
                MyObjectBuilder_CubeBlock newBlock = block.Clone() as MyObjectBuilder_CubeBlock;
                newBlock.EntityId = block.EntityId == 0 ? 0 : SpaceEngineersApi.GenerateEntityId(IDType.ENTITY);

                switch (block)
                {
                    case MyObjectBuilder_MotorBase motorBlock:
                        ((MyObjectBuilder_MotorBase)newBlock).RotorEntityId = motorBlock.RotorEntityId == 0 ? 0 : SpaceEngineersApi.GenerateEntityId(IDType.ENTITY);
                        break;
                    case MyObjectBuilder_PistonBase pistonBlock:
                        ((MyObjectBuilder_PistonBase)newBlock).TopBlockId = pistonBlock.TopBlockId == 0 ? 0 : SpaceEngineersApi.GenerateEntityId(IDType.ENTITY);
                        break;
                    default:
                        break;
                }

                MyCubeBlockDefinition definition = SpaceEngineersApi.GetCubeDefinition(block.TypeId, viewModel.GridSize, block.SubtypeName);
                MirrorCubeOrientation(definition, block.BlockOrientation, xMirror, yMirror, zMirror, out MyCubeBlockDefinition mirrorDefinition, out newBlock.BlockOrientation);

                newBlock.SubtypeName = mirrorDefinition.Id.SubtypeName;

                SerializableVector3I min, max;
                if (definition.Size.X == 1 && definition.Size.Y == 1 && definition.Size.Z == 1)
                {
                    newBlock.Min = block.Min.Mirror(xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                    max = newBlock.Min;
                }
                else
                {
                    Vector3I orientSize = definition.Size.Add(-1).Transform(block.BlockOrientation).Abs();
                    min = block.Min.Mirror(xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                    SerializableVector3I blockMax = new(block.Min.X + orientSize.X, block.Min.Y + orientSize.Y, block.Min.Z + orientSize.Z);
                    max = blockMax.Mirror(xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);

                    newBlock.Min = new SerializableVector3I(
                        xMirror != Mirror.None ? max.X : min.X,
                        yMirror != Mirror.None ? max.Y : min.Y,
                        zMirror != Mirror.None ? max.Z : min.Z
                    );
                }

                // Don't place a block if one already exists there in the mirror.
                if (integrate && viewModel.CubeGrid.CubeBlocks.Any(b => b.Min.X == newBlock.Min.X && b.Min.Y == newBlock.Min.Y && b.Min.Z == newBlock.Min.Z /*|| b.Max == newBlock.Min*/))  // TODO: check cubeblock size.
                    continue;

                blocks.Add(newBlock);
            }
            return blocks;
        }

        private static void MirrorCubeOrientation(MyCubeBlockDefinition definition, SerializableBlockOrientation orientation, Mirror xMirror, Mirror yMirror, Mirror zMirror, out MyCubeBlockDefinition mirrorDefinition, out SerializableBlockOrientation mirrorOrientation)
        {
            // Determine the mirrored block definition
            mirrorDefinition = string.IsNullOrEmpty(definition.MirroringBlock)
                ? definition
                : SpaceEngineersApi.GetCubeDefinition(definition.Id.TypeId, definition.CubeSize, definition.MirroringBlock);

            // Create the source matrix from the block orientation
            Matrix sourceMatrix = Matrix.CreateFromDir(
                Base6Directions.GetVector(orientation.Forward),
                Base6Directions.GetVector(orientation.Up)
            );

            Vector3 mirrorNormal = xMirror != Mirror.None ? Vector3.Right :
                                   yMirror != Mirror.None ? Vector3.Up :
                                   zMirror != Mirror.None ? Vector3.Forward : Vector3.Zero;


            var blockMirrorAxis = GetBlockMirrorAxis(sourceMatrix, mirrorNormal);
            var blockMirrorOption = GetBlockMirrorOption(definition, blockMirrorAxis);
            Matrix targetMatrix = CalculateTargetMatrix(blockMirrorOption, sourceMatrix);

            mirrorOrientation = new SerializableBlockOrientation(
                Base6Directions.GetForward(ref targetMatrix),
                Base6Directions.GetUp(ref targetMatrix)
            );
        }

        private static MySymmetryAxisEnum GetBlockMirrorAxis(Matrix sourceMatrix, Vector3 mirrorNormal)
        {
            return mirrorNormal switch
            {
                { X: 1.0f or -1.0f } => MySymmetryAxisEnum.X,
                { Y: 1.0f or -1.0f } => MySymmetryAxisEnum.Y,
                { Z: 1.0f or -1.0f } => MySymmetryAxisEnum.Z,
                _ => MySymmetryAxisEnum.None,
            };
        }

        private static MySymmetryAxisEnum GetBlockMirrorOption(MyCubeBlockDefinition definition, MySymmetryAxisEnum blockMirrorAxis)
        {
            return blockMirrorAxis switch
            {
                MySymmetryAxisEnum.X => definition.SymmetryX,
                MySymmetryAxisEnum.Y => definition.SymmetryY,
                MySymmetryAxisEnum.Z => definition.SymmetryZ,
                _ => throw new InvalidOperationException("Invalid block mirror axis.")
            };
        }

        private static Matrix CalculateTargetMatrix(MySymmetryAxisEnum blockMirrorOption, Matrix sourceMatrix)
        {
            return blockMirrorOption switch
            {
                MySymmetryAxisEnum.X => Matrix.CreateRotationX(MathHelper.Pi) * sourceMatrix,
                MySymmetryAxisEnum.Y or MySymmetryAxisEnum.YThenOffsetX => Matrix.CreateRotationY(MathHelper.Pi) * sourceMatrix,
                MySymmetryAxisEnum.Z or MySymmetryAxisEnum.ZThenOffsetX => Matrix.CreateRotationZ(MathHelper.Pi) * sourceMatrix,
                MySymmetryAxisEnum.HalfX => Matrix.CreateRotationX(-MathHelper.PiOver2) * sourceMatrix,
                MySymmetryAxisEnum.HalfY => Matrix.CreateRotationY(-MathHelper.PiOver2) * sourceMatrix,
                MySymmetryAxisEnum.HalfZ => Matrix.CreateRotationZ(-MathHelper.PiOver2) * sourceMatrix,
                MySymmetryAxisEnum.XHalfY => Matrix.CreateRotationX(MathHelper.Pi) * sourceMatrix * Matrix.CreateRotationY(MathHelper.PiOver2),
                MySymmetryAxisEnum.YHalfY => Matrix.CreateRotationY(MathHelper.Pi) * sourceMatrix * Matrix.CreateRotationY(MathHelper.PiOver2),
                MySymmetryAxisEnum.ZHalfY => Matrix.CreateRotationZ(MathHelper.Pi) * sourceMatrix * Matrix.CreateRotationY(MathHelper.PiOver2),
                MySymmetryAxisEnum.XHalfX => Matrix.CreateRotationX(MathHelper.Pi) * sourceMatrix * Matrix.CreateRotationX(-MathHelper.PiOver2),
                MySymmetryAxisEnum.YHalfX => Matrix.CreateRotationY(MathHelper.Pi) * sourceMatrix * Matrix.CreateRotationX(-MathHelper.PiOver2),
                MySymmetryAxisEnum.ZHalfX => Matrix.CreateRotationZ(MathHelper.Pi) * sourceMatrix * Matrix.CreateRotationX(-MathHelper.PiOver2),
                MySymmetryAxisEnum.XHalfZ => Matrix.CreateRotationX(MathHelper.Pi) * sourceMatrix * Matrix.CreateRotationZ(-MathHelper.PiOver2),
                MySymmetryAxisEnum.YHalfZ => Matrix.CreateRotationY(MathHelper.Pi) * sourceMatrix * Matrix.CreateRotationZ(-MathHelper.PiOver2),
                MySymmetryAxisEnum.ZHalfZ => Matrix.CreateRotationZ(MathHelper.Pi) * sourceMatrix * Matrix.CreateRotationZ(-MathHelper.PiOver2),
                MySymmetryAxisEnum.XMinusHalfZ => Matrix.CreateRotationX(MathHelper.Pi) * sourceMatrix * Matrix.CreateRotationZ(MathHelper.PiOver2),
                MySymmetryAxisEnum.YMinusHalfZ => Matrix.CreateRotationY(MathHelper.Pi) * sourceMatrix * Matrix.CreateRotationZ(MathHelper.PiOver2),
                MySymmetryAxisEnum.ZMinusHalfZ => Matrix.CreateRotationZ(MathHelper.Pi) * sourceMatrix * Matrix.CreateRotationZ(MathHelper.PiOver2),
                MySymmetryAxisEnum.XMinusHalfX => Matrix.CreateRotationX(MathHelper.Pi) * sourceMatrix * Matrix.CreateRotationX(MathHelper.PiOver2),
                MySymmetryAxisEnum.YMinusHalfX => Matrix.CreateRotationY(MathHelper.Pi) * sourceMatrix * Matrix.CreateRotationX(MathHelper.PiOver2),
                MySymmetryAxisEnum.ZMinusHalfX => Matrix.CreateRotationZ(MathHelper.Pi) * sourceMatrix * Matrix.CreateRotationX(MathHelper.PiOver2),
                MySymmetryAxisEnum.MinusHalfX => Matrix.CreateRotationX(MathHelper.PiOver2) * sourceMatrix,
                MySymmetryAxisEnum.MinusHalfY => Matrix.CreateRotationY(MathHelper.PiOver2) * sourceMatrix,
                MySymmetryAxisEnum.MinusHalfZ => Matrix.CreateRotationZ(MathHelper.PiOver2) * sourceMatrix,
                _ => sourceMatrix // Default case or MySymmetryAxisEnum.None
            };
        }

        #endregion

        private static string GetAxisIndicator(Base6Directions.Direction direction)
        {
            Vector3 vector = Base6Directions.GetVector(direction);
            return Base6Directions.GetAxis(direction) switch
            {
                Base6Directions.Axis.LeftRight => vector.X < 0 ? "-X" : "+X",
                Base6Directions.Axis.UpDown => vector.Y < 0 ? "-Y" : "+Y",
                Base6Directions.Axis.ForwardBackward => vector.Z < 0 ? "-Z" : "+Z",
                _ => null
            };
        }

        #endregion
    }
}
