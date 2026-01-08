using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;

using SEToolbox.Interop;
using SEToolbox.Support;

using SpaceEngineers.Game.Entities.Blocks;

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
            get => CubeGrid.Skeleton?.Count > 0 ? CubeGrid.Skeleton.Count : 0;
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
            double scaleMultiplier = CubeGrid.GridSizeEnum.ToLength();

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
                if (cubeDefinition == null || cubeDefinition.Size == Vector3I.One)
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

                foreach (MyCubeBlockDefinition.Component component in cubeBlockDefinition?.Components)
                {
                    float componentMass = component.Definition.Mass * component.Count;
                    cubeMass += componentMass;
                }

                totalMass += cubeMass;
            }

            string cockpitOrientation = Res.ClsCockpitOrientationNone;
            MyObjectBuilder_CubeBlock[] cockpits = [.. CubeGrid.CubeBlocks.Where(b => b is MyObjectBuilder_Cockpit)];
            if (cockpits.Length > 0)
            {
                int count = cockpits.Count(b => b.BlockOrientation.Forward == cockpits[0].BlockOrientation.Forward &&
                                                b.BlockOrientation.Up == cockpits[0].BlockOrientation.Up);
                bool allSameOrientation = cockpits.All(b => b.BlockOrientation.Forward == cockpits[0].BlockOrientation.Forward &&
                                                            b.BlockOrientation.Up == cockpits[0].BlockOrientation.Up);
                string orientAxisForward  = $"{cockpits[0].BlockOrientation.Forward} ({GetAxisIndicator(cockpits[0].BlockOrientation.Forward)})";
                string orientAxisUp = $"{cockpits[0].BlockOrientation.Up} ({GetAxisIndicator(cockpits[0].BlockOrientation.Up)})";

                cockpitOrientation = allSameOrientation ? $"{Res.ClsCockpitOrientationForward} = {orientAxisForward}, {Res.ClsCockpitOrientationUp} = {orientAxisUp}" : Res.ClsCockpitOrientationMixed;
            }
            CockpitOrientation = cockpitOrientation;

            var scale = max - min;
            scale.X++;
            scale.Y++;
            scale.Z++;

            if (CubeGrid.CubeBlocks.Count == 0)
            {
                scale = new System.Windows.Media.Media3D.Vector3D();
            }

            Min = min;
            Max = max;
            Scale = scale;
            Size = new Size3D(scale.X * scaleMultiplier, scale.Y * scaleMultiplier, scale.Z * scaleMultiplier);
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
            MyObjectBuilder_CubeBlock[] broadcasters = [.. CubeGrid.CubeBlocks.Where(b => b is MyObjectBuilder_Beacon || b is MyObjectBuilder_RadioAntenna)];
            float broadcastRadius = Math.Max(CubeGrid.CubeBlocks.OfType<MyObjectBuilder_Beacon>().Max(b => b.BroadcastRadius), CubeGrid.CubeBlocks.OfType<MyObjectBuilder_RadioAntenna>().Max(b => b.BroadcastRadius));
            string broadcastNames = string.Empty;
            if (broadcasters.Length > 0)
            {
                var beacons = broadcasters.OfType<MyObjectBuilder_Beacon>();
                var antennas = broadcasters.OfType<MyObjectBuilder_RadioAntenna>();

                string[] beaconNames = [.. beacons.Select(b => b.CustomName ?? "Beacon")];
                string[] antennaNames = [.. antennas.Select(a => a.CustomName ?? "Antenna")];
                broadcastNames = string.Join("|", beaconNames.Concat(antennaNames).OrderBy(s => s));
            }

            if (CubeGrid.CubeBlocks.Count == 1 && CubeGrid.CubeBlocks.First() is MyObjectBuilder_Wheel)
                {
                    MyObjectBuilder_CubeGrid grid = ExplorerModel.Default.FindConnectedTopBlock<MyObjectBuilder_MotorSuspension>(CubeGrid.CubeBlocks[0].EntityId);
                 Description = grid == null ? Res.ClsCubeGridWheelDetached : string.Format(Res.ClsCubeGridWheelAttached, grid.DisplayName);
                    return;
  
            }

           Description =  string.IsNullOrEmpty(broadcastNames) ? $"{Scale.X}x{Scale.Y}x{Scale.Z}": $"{broadcastNames} {Scale.X}x{Scale.Y}x{Scale.Z}";


            bool reflectorsOn = CubeGrid.CubeBlocks.OfType<MyObjectBuilder_ReflectorLight>().Any(light => light.Enabled);

            float speed = CubeGrid.LinearVelocity.ToVector3().Length();

            float totalPowerUsage = 0;
            foreach (IMyPowerProducer block in CubeGrid.CubeBlocks.OfType<IMyPowerProducer>())
            {
                totalPowerUsage += block.CurrentOutput;
            }

            float totalReactorOutput = 0;
            foreach (IMyPowerProducer reactor in CubeGrid.CubeBlocks.OfType<IMyReactor>())
            {
                totalReactorOutput += reactor.CurrentOutput;
            }

            int thrustCount = CubeGrid.CubeBlocks.OfType<MyObjectBuilder_Thrust>().Count();
            int gyroCount = CubeGrid.CubeBlocks.OfType<MyObjectBuilder_Gyro>().Count();
            float totalFuelTime = 0;
          
            foreach (IMyPowerProducer block in CubeGrid.CubeBlocks.OfType<IMyPowerProducer>())
            {
                _ = block switch
                {
                    IMyPowerProducer when block is IMyReactor reactor => totalFuelTime += reactor.CurrentOutput,
                    IMyPowerProducer when block is IMyBatteryBlock battery => totalFuelTime += battery.CurrentOutput,
                    IMyPowerProducer when block is MySolarPanel solarPanel => totalFuelTime += solarPanel.CurrentOutput,
                    IMyPowerProducer when block is IMyWindTurbine windTurbine => totalFuelTime += windTurbine.CurrentOutput,
                    _ => totalFuelTime += 0

                };
            }

            decimal totalFuel = 0;
            decimal totalPower = 0;    
            foreach (IMyPowerProducer block in CubeGrid.CubeBlocks.OfType<IMyPowerProducer>())
            {
                if (block == null)
                {
                    continue;
                }

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
            /// 
            // Report for debugging
            // Log.WriteLine($"CubeGrid: {DisplayName}");
            // Log.WriteLine($"Description: {Description}");
            // Log.WriteLine($"Reflectors On: {reflectorsOn}");
            // Log.WriteLine($"Mass: {Mass} Kg");
            // Log.WriteLine($"Speed: {speed} m/s");
            // Log.WriteLine($"Power Usage: {totalPowerUsage}%");
            // Log.WriteLine($"Reactors: {totalReactorOutput} GW");
            // Log.WriteLine($"Thrusts: {thrustCount}");
            // Log.WriteLine($"Gyros: {gyroCount}");
            // Log.WriteLine($"Fuel Time: {totalFuelTime} sec");

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


                            foreach (MyCubeBlockDefinition.Component component in cubeBlockDefinition?.Components)
                            {
                                SpaceEngineersApi.AccumulateCubeBlueprintRequirements(component.Definition.Id.SubtypeName, component.Definition.Id.TypeId, component.Count, ingotRequirements, out TimeSpan componentTime);
                                timeTaken += componentTime;

                                float componentMass = component.Definition.Mass * component.Count;
                                float componentVolume = component.Definition.Volume * SpaceEngineersConsts.VolumeMultiplier * component.Count;
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
                            SpaceEngineersApi.AccumulateCubeBlueprintRequirements(kvp.Value.SubtypeName, kvp.Value.Id.TypeId, kvp.Value.Amount, oreRequirements, out TimeSpan ingotTime);
                            MyDefinitionBase myDefBase = MyDefinitionManager.Static.GetDefinition(kvp.Value.Id);
                            if (myDefBase.Id.TypeId.IsNull)
                            {
                                continue;
                            }

                            MyPhysicalItemDefinition physItemDef = (MyPhysicalItemDefinition)myDefBase;
                            string componentTexture = SpaceEngineersCore.GetDataPathOrDefault(physItemDef.Icons.First(), Path.Combine(contentPath, physItemDef.Icons.First()));
                            double volume = (double)kvp.Value.Amount * physItemDef.Volume * SpaceEngineersConsts.VolumeMultiplier;
                            OreAssetModel ingotAsset = new() { Name = physItemDef.DisplayNameText, Amount = kvp.Value.Amount, Mass = (double)kvp.Value.Amount * physItemDef.Mass, Volume = volume, Time = ingotTime, TextureFile = componentTexture };
                            ingotAssets.Add(ingotAsset);
                            timeTaken += ingotTime;
                        }

                        foreach (KeyValuePair<string, BlueprintRequirement> kvp in oreRequirements)
                        {
                            if (MyDefinitionManager.Static.GetDefinition(kvp.Value.Id) is MyPhysicalItemDefinition physItemDef)
                            {
                                if (physItemDef != null)
                                {
                                    string componentTexture = SpaceEngineersCore.GetDataPathOrDefault(physItemDef.Icons.First(), Path.Combine(contentPath, physItemDef.Icons.First()));
                                    double volume = (double)kvp.Value.Amount * physItemDef.Volume * SpaceEngineersConsts.VolumeMultiplier;
                                    OreAssetModel oreAsset = new() { Name = physItemDef.DisplayNameText, Amount = kvp.Value.Amount, Mass = (double)kvp.Value.Amount * physItemDef.Mass, Volume = volume, TextureFile = componentTexture };
                                    oreAssets.Add(oreAsset);
                                }
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
                if (cube.ComponentContainer?.Components?.FirstOrDefault(e => e.TypeId == "MyHierarchyComponentBase")?.Component is MyObjectBuilder_HierarchyComponentBase hierarchyBase && hierarchyBase.Children.Any(e => e is MyObjectBuilder_Character))
                {
                    list.Add((MyObjectBuilder_Cockpit)cube);
                }
            }

            return list;
        }

        public void RepairAllDamage()
        {
            CubeGrid.Skeleton ??= [];
            CubeGrid.Skeleton.Clear();

            foreach (MyObjectBuilder_CubeBlock cube in CubeGrid.CubeBlocks)
            {
                cube.IntegrityPercent = cube.BuildPercent;
                // No need to set bones for individual blocks like rounded armor, as this is taken from the definition within the game itself.
            }

            OnPropertyChanged(nameof(IsDamaged), nameof(DamageCount));
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
            CubeGrid.LinearVelocity = new Vector3(CubeGrid.LinearVelocity.X * -1,
                                                  CubeGrid.LinearVelocity.Y * -1,
                                                  CubeGrid.LinearVelocity.Z * -1);
            CubeGrid.AngularVelocity = new Vector3(CubeGrid.AngularVelocity.X * -1,
                                                   CubeGrid.AngularVelocity.Y * -1,
                                                   CubeGrid.AngularVelocity.Z * -1);
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
            {
                changes |= CubeItemModel.ConvertFromLightToHeavyArmor(cube);
            }

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
            {
                changes |= CubeItemModel.ConvertFromHeavyToLightArmor(cube);
            }

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
                if (cube is MyObjectBuilder_MechanicalConnectionBlock mechanicalBlock && mechanicalBlock?.ShareInertiaTensor != state)
                {
                    count++;
                    mechanicalBlock.ShareInertiaTensor = state;
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
            VRageMath.Quaternion orient = CubeGrid.PositionAndOrientation.Value.ToQuaternion() * quaternion;
            orient.Normalize();
            MyPositionAndOrientation pos = new(orient.ToMatrix());

            CubeGrid.PositionAndOrientation = new MyPositionAndOrientation
            {
                Position = CubeGrid.PositionAndOrientation.Value.Position,
                Forward = pos.Forward,
                Up = pos.Up
            };
            UpdateGeneralFromEntityBase();
        }

        public void RotateCubes(VRageMath.Quaternion quaternion)
        {
            foreach (MyObjectBuilder_CubeBlock cube in CubeGrid.CubeBlocks)
            {
                MyCubeBlockDefinition definition = SpaceEngineersApi.GetCubeDefinition(cube.TypeId, CubeGrid.GridSizeEnum, cube.SubtypeName);

                if (definition.Size == Vector3I.One)
                {
                    // rotate position around origin.
                    cube.Min = Vector3I.Transform(cube.Min.ToVector3I(), quaternion);
                }
                else
                {
                    // resolve size of component, and transform to original orientation.
                    Vector3I orientSize = definition.Size.Add(-1).Transform(cube.BlockOrientation).Abs();

                    Vector3I min = Vector3I.Transform(cube.Min.ToVector3I(), quaternion);
                    Vector3I blockMax = new(cube.Min.X + orientSize.X,
                                            cube.Min.Y + orientSize.Y,
                                            cube.Min.Z + orientSize.Z);
                    Vector3I max = Vector3I.Transform(blockMax, quaternion);

                    cube.Min = new SerializableVector3I(Math.Min(min.X, max.X),
                                                        Math.Min(min.Y, max.Y),
                                                        Math.Min(min.Z, max.Z));
                }
                VRageMath.Quaternion quat = quaternion * cube.BlockOrientation.ToQuaternion();
                quat.Normalize();
                cube.BlockOrientation = new SerializableBlockOrientation(ref quat);

                RotateGroupings(quaternion);
                RotateSkeleton(quaternion);
                RotateConveyorLines(quaternion);
                NormalizeRotation(quaternion);
                UpdateGeneralFromEntityBase();
            }
        }

        private void RotateGroupings(VRageMath.Quaternion quaternion)
        {
            foreach (MyObjectBuilder_BlockGroup group in CubeGrid.BlockGroups)
            {
                group.Blocks = [.. group.Blocks.Select(b => Vector3I.Transform(b, quaternion))];
            }
        }

        private void RotateSkeleton(VRageMath.Quaternion quaternion)
        {

            for (int i = 0; i < CubeGrid.Skeleton?.Count; i++)
            {
                BoneInfo bone = CubeGrid.Skeleton[i];
                bone.BonePosition = Vector3I.Transform(bone.BonePosition, quaternion);
                bone.BoneOffset = bone.BoneOffset.Transform(VRageMath.Quaternion.Inverse(quaternion));
                CubeGrid.Skeleton[i] = bone; // Reassign the modified bone back to the collection
            }
        }

        private void RotateConveyorLines(VRageMath.Quaternion quaternion)
        {
            var newLines = CubeGrid.ConveyorLines.Select(line => new MyObjectBuilder_ConveyorLine
            {
                StartPosition = Vector3I.Transform(line.StartPosition, quaternion),
                StartDirection = Base6Directions.GetDirection(Vector3.Transform(Base6Directions.GetVector(line.StartDirection), quaternion)),
                EndPosition = Vector3I.Transform(line.EndPosition, quaternion),
                EndDirection = Base6Directions.GetDirection(Vector3.Transform(Base6Directions.GetVector(line.EndDirection), quaternion))
            }).ToList();
            CubeGrid.ConveyorLines = newLines;
        }

        private void NormalizeRotation(VRageMath.Quaternion quaternion)
        {
            // Rotate the ship also to maintain the appearance that it has not changed.
            VRageMath.Quaternion orient = CubeGrid.PositionAndOrientation.Value.ToQuaternion() * VRageMath.Quaternion.Inverse(quaternion);
            orient.Normalize();
            MyPositionAndOrientation pos = new(orient.ToMatrix());

            CubeGrid.PositionAndOrientation = new MyPositionAndOrientation
            {
                Position = CubeGrid.PositionAndOrientation.Value.Position,
                Forward = pos.Forward,
                Up = pos.Up
            };
        }

        public void ConvertToShip()
        {
            CubeGrid.IsStatic = false;
            UpdateGeneralFromEntityBase();
        }
        List<SubTypeId> armorTypes =
        [
            SubTypeId.LargeRoundArmor_Corner,
            SubTypeId.LargeRoundArmor_Slope,
            SubTypeId.LargeRoundArmor_CornerInv,

            SubTypeId.SmallBlockArmorSlope,
            SubTypeId.SmallBlockArmorCorner,
            SubTypeId.SmallBlockArmorCornerInv
            
        ];
        public bool ConvertToCornerArmor()
        {
            int count = 0;
            foreach (var block in armorTypes)
            {
                count += CubeGrid.CubeBlocks.Count(c => c.SubtypeName == block.ToString());
            }
            return count > 0;
        }

        public bool ConvertToRoundArmor()
        {
            int count = 0;
            foreach (var block in armorTypes)
            {
                count += CubeGrid.CubeBlocks.Count(c => c.SubtypeName == block.ToString());
            }
            return count > 0;
        }

        #region Mirror

        public bool MirrorModel(bool usePlane, bool oddMirror)
        {   
            var mirror = Mirror.None;
            var axisValue = 0;
            var (xMirror, xAxis, yMirror ,yAxis, zMirror, zAxis) = (Mirror.None, 0, Mirror.None, 0, Mirror.None, 0);
            int count = 0;

            if (!usePlane)
            {
                // Find mirror Axis.    
                
                var (min, max) = (CubeGrid.CubeBlocks.Min(c => c.Min), CubeGrid.CubeBlocks.Max(c => c.Min));
                var axisValues =  CubeGrid.CubeBlocks.GroupBy(c => c.Min).Select(c => c).ToArray();
                int[] cubeCounts = [.. CubeGrid.CubeBlocks.GroupBy(c => c.Min).Select(c => c.Count())];
                        
                var maxIndex = cubeCounts.ToList().IndexOf(cubeCounts.Max());
            
            Mirror[] mirrorTypes = [Mirror.EvenDown, Mirror.EvenUp, Mirror.Odd];
           
                for (int i = 0; i < cubeCounts.Length; i++)
                {
                    if (cubeCounts[i] > maxIndex && mirrorTypes[i] != Mirror.None) 
                    {
                        
          
                            mirror = oddMirror ? Mirror.Odd : mirrorTypes[maxIndex];
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

          
            MyObjectBuilder_CubeBlock[] cubeBlock = null;

            switch (true)
            {
                case var _ when CubeGrid.XMirroxPlane.HasValue &&
                                mirror == (CubeGrid.XMirroxOdd ? Mirror.Odd : Mirror.EvenDown) &&
                                axisValue == CubeGrid.XMirroxPlane.Value.X &&
                                cubeBlock == MirrorCubes(this, true, xMirror, xAxis, Mirror.None, 0, Mirror.None, 0).ToArray():
                ///
                case var _ when CubeGrid.YMirroxPlane.HasValue &&
                                mirror == (CubeGrid.YMirroxOdd ? Mirror.EvenDown : Mirror.Odd) &&
                                axisValue == CubeGrid.YMirroxPlane.Value.Y &&
                ///
                                cubeBlock == MirrorCubes(this, true, Mirror.None, 0, yMirror, yAxis, Mirror.None, 0).ToArray():
                case var _ when CubeGrid.ZMirroxPlane.HasValue && mirror == (CubeGrid.ZMirroxOdd ? Mirror.EvenDown : Mirror.Odd) &&
                                axisValue == CubeGrid.ZMirroxPlane.Value.Z &&
                                cubeBlock == MirrorCubes(this, true, Mirror.None, 0, Mirror.None, 0, zMirror, zAxis).ToArray():
                    ///

                    CubeGrid.CubeBlocks.AddRange(cubeBlock);
                    count += cubeBlock.Length;
                    MirrorBlockGroups(CubeGrid, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                    MirrorConveyorLines(CubeGrid, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                    break;
            }
            
            UpdateGeneralFromEntityBase();
            OnPropertyChanged(nameof(BlockCount));
            return count > 0;
        }

        public void MirrorBlockGroups(MyObjectBuilder_CubeGrid CubeGrid, Mirror xMirror, int xAxis, Mirror yMirror, int yAxis, Mirror zMirror, int zAxis)
        {
            foreach (MyObjectBuilder_BlockGroup group in CubeGrid.BlockGroups)
            {
                MyObjectBuilder_BlockGroup mirroredGroup = new()
                {
                    Name = group.Name + "_Mirrored",
                    Blocks = [.. group.Blocks.Select(pos => {
                                MyObjectBuilder_CubeBlock block = CubeGrid.CubeBlocks.FirstOrDefault(b => b.Min.ToVector3I() == pos);
                                return block != null ? MirrorCube(block, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis).Min.ToVector3I() : pos;
                            })]
                };
                CubeGrid.BlockGroups?.Add(mirroredGroup);

            }
        }

        private void MirrorConveyorLines(MyObjectBuilder_CubeGrid CubeGrid, Mirror xMirror, int xAxis, Mirror yMirror, int yAxis, Mirror zMirror, int zAxis)
        {
            // Mirror ConveyorLines

            foreach (MyObjectBuilder_ConveyorLine conveyorLine in CubeGrid.ConveyorLines)
            {
                MyObjectBuilder_ConveyorLine mirroredLine = new()
                {
                    StartPosition = MirrorPosition(conveyorLine.StartPosition, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis),
                    EndPosition = MirrorPosition(conveyorLine.EndPosition, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis),
                    ConveyorLineType = conveyorLine.ConveyorLineType,
                };
                CubeGrid.ConveyorLines?.Add(mirroredLine);
            }
        }
       
       private static MyObjectBuilder_CubeBlock UpdateEntityId(MyObjectBuilder_CubeBlock newBlock, MyObjectBuilder_CubeBlock block)
       {
        newBlock ??=  new MyObjectBuilder_CubeBlock() ?? block.Clone() as MyObjectBuilder_CubeBlock;
        switch (block)
            {
                case MyObjectBuilder_CubeBlock when block.EntityId == ((MyObjectBuilder_MotorBase)newBlock).RotorEntityId:
                case MyObjectBuilder_CubeBlock when block.EntityId == ((MyObjectBuilder_PistonBase)newBlock).TopBlockId:
                    newBlock.EntityId = block.EntityId == 0 ? 0 : SpaceEngineersApi.GenerateEntityId(IDType.ENTITY);
                    break;
                default:
                    break;
            }
            return newBlock;
        }
        // Helper method to mirror a block
        private static MyObjectBuilder_CubeBlock MirrorCube(MyObjectBuilder_CubeBlock block, Mirror xMirror, int xAxis, Mirror yMirror, int yAxis, Mirror zMirror, int zAxis)
        {   
            MyObjectBuilder_CubeBlock newBlock = block.Clone() as MyObjectBuilder_CubeBlock;
            UpdateEntityId(newBlock, block);
            MyObjectBuilder_CubeGrid cubeGrid = new();
            MyCubeBlockDefinition definition = SpaceEngineersApi.GetCubeDefinition(block.TypeId, cubeGrid.GridSizeEnum, block.SubtypeName);
            MirrorCubeOrientation(definition, block.BlockOrientation, xMirror, yMirror, zMirror, out MyCubeBlockDefinition mirrorDefinition, out newBlock.BlockOrientation);

            newBlock.SubtypeName = mirrorDefinition.Id.SubtypeName;

            SerializableVector3I min, max;
            if (definition.Size == Vector3I.One)
            {
                newBlock.Min = block.Min.Mirror(xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                _ = newBlock.Min;
            }
            else
            {
                Vector3I orientSize = definition.Size.Add(-1).Transform(block.BlockOrientation).Abs();
                min = block.Min.Mirror(xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);

                SerializableVector3I blockMax = new(block.Min.X + orientSize.X,
                                                    block.Min.Y + orientSize.Y,
                                                    block.Min.Z + orientSize.Z
                                                    );
                max = blockMax.Mirror(xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);

                newBlock.Min = new SerializableVector3I(xMirror != Mirror.None ? max.X : min.X,
                                                        yMirror != Mirror.None ? max.Y : min.Y,
                                                        zMirror != Mirror.None ? max.Z : min.Z
                                                        );
            }
            return newBlock;
        }
        // Helper method to mirror a position

        private static Vector3I MirrorPosition(Vector3I position, Mirror xMirror, int xAxis, Mirror yMirror, int yAxis, Mirror zMirror, int zAxis)
        {
            Vector3I axis = new(xAxis, yAxis, zAxis);
            Vector3I mirroredPosition = ((xMirror,yMirror, zMirror) == (Mirror.None, Mirror.None, Mirror.None) ? position : axis - position                 
            );
            return mirroredPosition;
        }

        private static IEnumerable<MyObjectBuilder_CubeBlock> MirrorCubes(StructureCubeGridModel viewModel, bool integrate, Mirror xMirror, int xAxis, Mirror yMirror, int yAxis, Mirror zMirror, int zAxis)
        {
            List<MyObjectBuilder_CubeBlock> blocks = [];

            if ((xMirror , yMirror, zMirror) == (Mirror.None, Mirror.None, Mirror.None))
            {
                return blocks;
            }

            foreach (MyObjectBuilder_CubeBlock block in viewModel.CubeGrid.CubeBlocks)
            {
                MyObjectBuilder_CubeBlock newBlock = MirrorCube(block, xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                UpdateEntityId(newBlock, block);
                MyCubeBlockDefinition definition = SpaceEngineersApi.GetCubeDefinition(block.TypeId, viewModel.GridSize, block.SubtypeName);
                MirrorCubeOrientation(definition, block.BlockOrientation, xMirror, yMirror, zMirror, out MyCubeBlockDefinition mirrorDefinition, out newBlock.BlockOrientation);

                newBlock.SubtypeName = mirrorDefinition.Id.SubtypeName;

                SerializableVector3I min, max;
                if (definition.Size.Equals(new Vector3I(1,1,1)))
                {
                    newBlock.Min = block.Min.Mirror(xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                    max = newBlock.Min;
                }
                else
                {
                    Vector3I orientSize = definition.Size.Add(-1).Transform(block.BlockOrientation).Abs();
                    min = block.Min.Mirror(xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);
                    SerializableVector3I blockMax = block.Min + orientSize;
                    max = blockMax.Mirror(xMirror, xAxis, yMirror, yAxis, zMirror, zAxis);

                    newBlock.Min = new(xMirror != Mirror.None ? max.X : min.X,
                                       yMirror != Mirror.None ? max.Y : min.Y,
                                       zMirror != Mirror.None ? max.Z : min.Z
                                       );
                }

                    if (viewModel.CubeGrid.CubeBlocks.Any(b => b.Min == newBlock.Min) && blocks.Any(b => b.Min == newBlock.Min))
                    {
                        continue;
                    }

                    blocks.Add(newBlock);
                }
                return blocks;
            } 

        private static void MirrorCubeOrientation(MyCubeBlockDefinition definition, SerializableBlockOrientation orientation, Mirror xMirror, Mirror yMirror, Mirror zMirror, out MyCubeBlockDefinition mirrorDefinition, out SerializableBlockOrientation mirrorOrientation)
        {
            // Determine the mirrored block definition
            mirrorDefinition = string.IsNullOrEmpty(definition.MirroringBlock) ? definition : SpaceEngineersApi.GetCubeDefinition(definition.Id.TypeId, definition.CubeSize, definition.MirroringBlock);

            // Create the source matrix from the block orientation
            Matrix sourceMatrix = Matrix.CreateFromDir(Base6Directions.GetVector(orientation.Forward),
                                                       Base6Directions.GetVector(orientation.Up));

            Vector3 mirrorNormal = xMirror != Mirror.None ? Vector3.Right :
                                   yMirror != Mirror.None ? Vector3.Up :
                                   zMirror != Mirror.None ? Vector3.Forward : Vector3.Zero;


            var blockMirrorAxis = GetBlockMirrorAxis(mirrorNormal);
            var blockMirrorOption = GetBlockMirrorOption(definition, blockMirrorAxis);
            Matrix targetMatrix = CalculateTargetMatrix(blockMirrorOption, sourceMatrix);

            mirrorOrientation = new SerializableBlockOrientation(Base6Directions.GetForward(ref targetMatrix),
                                                                 Base6Directions.GetUp(ref targetMatrix)
            );
        }

        private static MySymmetryAxisEnum GetBlockMirrorAxis(Vector3 mirrorNormal)
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
