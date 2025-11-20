using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;

using SEToolbox.ImageLibrary;
using SEToolbox.Interfaces;
using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Support;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;

using VRage.FileSystem;
using VRage.Game;
using VRage.ObjectBuilders;

using VRageMath;

using Ext = SEToolbox.Support.HtmlExtensions;
using MOBTypeIds = SEToolbox.Interop.SpaceEngineersTypes.MOBTypeIds;
using Res = SEToolbox.Properties.Resources;
using TexUtil = SEToolbox.ImageLibrary.ImageTextureUtil;

namespace SEToolbox.Models
{
    public class ResourceReportModel : BaseModel
    {
        private const string CssStyle = @"
body { background-color: #F6F6FA }
b { font-family: Arial, Helvetica, sans-serif; }
p { font-family: Arial, Helvetica, sans-serif; }
h1,h2,h3 { font-family: Arial, Helvetica, sans-serif; }
table { background-color: #FFFFFF; }
table tr td { font-family: Arial, Helvetica, sans-serif; font-size: small; line-height: normal; color: #000000; }
table thead td { background-color: #BABDD6; font-weight: bold; Color: #000000; }
td.right { text-align: right; }";

        #region Fields

        private bool _isBusy;
        private bool _isActive;
        private bool _isReportReady;
        private string _reportHtml = string.Empty;
        private readonly Stopwatch _timer = new();
        private bool _showProgress;
        private double _progress;
        private double _maximumProgress;

        private string _saveName = string.Empty;
        private DateTime _generatedDate;
        private IList<IStructureBase> _entities = [];

        /// <summary>
        /// untouched (in all asteroids), measured in m³.
        /// </summary>

        private List<VoxelMaterialAssetModel> _untouchedOre = [];
        /// <summary>
        /// untouched (by asteroid), measured in m³.
        /// </summary>
        private List<AsteroidContent> _untouchedOreByAsteroid = [];

        /// <summary>
        /// unused (ore and ingot), measured in Kg and L.
        /// </summary>
        private List<OreContent> _unusedOre = [];
        /// <summary>
        /// used (component, tool, cube), measured in Kg and L.
        /// Everything is measured in it's regressed state. Ie., how much ore was used/needed to build this item.
        /// </summary>
        private List<OreContent> _usedOre = [];
        /// <summary>
        /// player controlled (inventory), measured in Kg and L.
        /// </summary>
        private List<OreContent> _playerOre = [];
        /// <summary>
        /// npc (everything currently in an AI controlled ship with ore, ingot, component, tool, cube), measured in Kg and L.
        /// </summary>
        private List<OreContent> _npcOre = [];
        /// <summary>
        /// tally of cubes to indicate time spent to construct.
        /// </summary>
        private List<ComponentItemModel> _allCubes = [];
        /// <summary>
        /// tally of components to indicate time spent to construct.
        /// </summary>
        private List<ComponentItemModel> _allComponents = [];
        /// <summary>
        /// tally of items, ingots, tools, ores, to indicate time spent to construct or mine.
        /// </summary>
        private List<ComponentItemModel> _allItems = [];
        private List<ShipContent> _allShips = [];

        private decimal _totalCubes;
        private int _totalPCU;
        private readonly object typeId;
        private readonly MyObjectBuilder_EntityBase tallyItem;
        public ResourceReportModel(object typeId)
        {
            this.typeId = typeId ?? throw new ArgumentNullException(nameof(typeId));
            tallyItem = null;
        }


        public ResourceReportModel()
        {
            typeId = new object();
            _timer = new Stopwatch();
            Progress = 0;
            MaximumProgress = 100;
        }
        #endregion

        #region Ctor



        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                SetProperty(ref _isBusy, nameof(IsBusy), () =>
                {
                    SetActiveStatus();
                    if (_isBusy)
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }
                });
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is available.  This is based on the IsInError and IsBusy properties
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, nameof(IsActive));
        }

        public string SaveName => _saveName;

        public bool IsReportReady
        {
            get => _isReportReady;
            set => SetProperty(ref _isReportReady, nameof(IsReportReady));
        }

        public string ReportHtml
        {
            get => _reportHtml;
            set => SetProperty(ref _reportHtml, nameof(ReportHtml));
        }

        public bool ShowProgress
        {
            get => _showProgress;
            set => SetProperty(ref _showProgress, nameof(ShowProgress));
        }

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, () =>
                    {
                        System.Windows.Forms.Application.DoEvents();
                        _timer.Restart();
                    }, nameof(Progress));
        }



        public double MaximumProgress
        {
            get => _maximumProgress;
            set => SetProperty(ref _maximumProgress, nameof(MaximumProgress));
        }

        #endregion

        #region Methods

        private static readonly Dictionary<string, ReportType> _reportTypeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".txt", ReportType.Text },
            { ".htm", ReportType.Html },
            { ".html", ReportType.Html },
            { ".xml", ReportType.Xml },
        };

        internal static ReportType GetReportType(string reportExtension)
        {
            return _reportTypeMap.TryGetValue(reportExtension?.ToUpperInvariant() ?? string.Empty, out var result) ? result : ReportType.Unknown;
        }

        public void Load(string saveName, IList<IStructureBase> entities)
        {
            _saveName = saveName ?? throw new ArgumentNullException(nameof(saveName));
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));
            SetActiveStatus();
        }

        public void SetActiveStatus()
        {
            IsActive = !IsBusy;
        }

        public void ResetProgress(double initial, double maximumProgress)
        {
            MaximumProgress = maximumProgress;
            Progress = initial;
            ShowProgress = true;
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
        }

        public void GenerateReport()
        {
            IsReportReady = false;
            _generatedDate = DateTime.Now;
            string contentPath = ToolboxUpdater.GetApplicationContentPath();
            SortedDictionary<string, long> accumulateOres = [];
            List<AsteroidContent> accumulateAsteroidOres = [];
            SortedDictionary<string, OreContent> accumulateUnusedOres = [];
            SortedDictionary<string, OreContent> accumulateUsedOres = [];
            SortedDictionary<string, OreContent> accumulatePlayerOres = [];
            SortedDictionary<string, OreContent> accumulateNpcOres = [];
            SortedDictionary<string, ComponentItemModel> accumulateItems = [];
            SortedDictionary<string, ComponentItemModel> accumulateComponents = [];
            SortedDictionary<string, ComponentItemModel> accumulateCubes = [];
            List<ShipContent> accumulateShips = [];
            _totalCubes = 0;
            _totalPCU = 0;

            ResetProgress(0, _entities.Count);

            foreach (IStructureBase entity in _entities)
            {
                Progress++;

                if (entity is StructureVoxelModel asteroid)
                {
                    #region Untouched Ores (asteroids)

                    Dictionary<string, long> details;

                    string fileName = asteroid.SourceVoxelFilePath;
                    if (string.IsNullOrEmpty(fileName))
                        fileName = asteroid.VoxelFilePath;

                    try
                    {
                        details = MyVoxelMapBase.GetMaterialAssetDetails(fileName);
                    }
                    catch
                    {
                        details = null;
                    }

                    // Accumulate into untouched.
                    if (details != null)
                    {
                        Dictionary<string, long> ores = [];
                        foreach (KeyValuePair<string, long> kvp in details)
                        {
                            MyVoxelMaterialDefinition bp = SpaceEngineersResources.VoxelMaterialDefinitions.FirstOrDefault(b => b.Id.SubtypeName == kvp.Key && b.Id.TypeId == MOBTypeIds.VoxelMaterialDefinition);

                            if (bp != null && bp.CanBeHarvested)
                            {
                                MyDefinitionBase cd = MyDefinitionManager.Static.GetDefinition(MOBTypeIds.Ore, bp.MinedOre);

                                if (cd != null)
                                {
                                    // stock ores require DisplayNameEnum. Modded ores require DisplayNameString.
                                    string key = cd.DisplayNameEnum != null ? cd?.DisplayNameEnum.Value.String : cd.DisplayNameString;

                                    if (ores.ContainsKey(key))
                                        ores[key] += kvp.Value;
                                    else
                                        ores.Add(key, kvp.Value);
                                }
                            }
                        }

                        foreach (KeyValuePair<string, long> kvp in ores)
                        {
                            if (accumulateOres.ContainsKey(kvp.Key))
                            {
                                accumulateOres[kvp.Key] += kvp.Value;
                            }
                            else
                            {
                                accumulateOres.Add(kvp.Key, kvp.Value);
                            }
                        }

                        long oreSum = ores.Values.ToList().Sum();
                        accumulateAsteroidOres.Add(new AsteroidContent()
                        {
                            Name = Path.GetFileNameWithoutExtension(fileName),
                            Position = asteroid.PositionAndOrientation.Value.Position,
                            UntouchedOreList = [.. ores.Select(kvp => new VoxelMaterialAssetModel
                            {
                                MaterialName = SpaceEngineersApi.GetResourceName(kvp.Key) ?? string.Empty,
                                Volume = Math.Round((double)kvp.Value / 255, 7),
                                Percent = kvp.Value / (double)oreSum
                            })]
                        });
                    }

                    #endregion
                }
                else if (entity is StructureFloatingObjectModel floating)
                {
                    if (floating.FloatingObject.Item.PhysicalContent.TypeId == MOBTypeIds.Ore || floating.FloatingObject.Item.PhysicalContent.TypeId == MOBTypeIds.Ingot)
                    {
                        TallyItems(floating.FloatingObject.Item.PhysicalContent.TypeId, floating.FloatingObject.Item.PhysicalContent.SubtypeName, (decimal)floating.FloatingObject.Item.Amount, contentPath, accumulateUnusedOres, accumulateItems, accumulateComponents);
                    }
                    else
                    {
                        TallyItems(floating.FloatingObject.Item.PhysicalContent.TypeId, floating.FloatingObject.Item.PhysicalContent.SubtypeName, (decimal)floating.FloatingObject.Item.Amount, contentPath, accumulateUsedOres, accumulateItems, accumulateComponents);
                    }
                }
                else if (entity is StructureCharacterModel character && !character.IsPilot && character.Character.Inventory != null) // ignore pilots,
                {

                    foreach (var item in character.Character.Inventory.Items)
                    {
                        TallyItems(item.PhysicalContent.TypeId, item.PhysicalContent.SubtypeName, (decimal)item.Amount, contentPath, accumulatePlayerOres, accumulateItems, accumulateComponents);
                    }
                }

                else if (entity is StructureCubeGridModel ship)
                {
                    bool isNpc = ship.CubeGrid.CubeBlocks.Any(e => e is MyObjectBuilder_Cockpit cockpit && cockpit.Autopilot != null);

                    int pcuToProduce = 0;

                    foreach (MyObjectBuilder_CubeBlock block in ship.CubeGrid.CubeBlocks)
                    {
                        MyCubeBlockDefinition cubeBlockDefinition = SpaceEngineersApi.GetCubeDefinition(block.TypeId, ship.CubeGrid.GridSizeEnum, block.SubtypeName);
                        if (cubeBlockDefinition != null)
                            pcuToProduce += cubeBlockDefinition.PCU;
                    }

                    ShipContent shipContent = new()
                    {
                        DisplayName = ship.DisplayName,
                        Position = ship.PositionAndOrientation.Value.Position,
                        EntityId = ship.EntityId,
                        BlockCount = ship.BlockCount,
                        PCU = pcuToProduce
                    };

                    foreach (MyObjectBuilder_CubeBlock block in ship.CubeGrid.CubeBlocks)
                    {
                        Type blockType = block.GetType();
                        MyCubeBlockDefinition cubeBlockDefinition = SpaceEngineersApi.GetCubeDefinition(block.TypeId, ship.CubeGrid.GridSizeEnum, block.SubtypeName);
                        TimeSpan blockTime = TimeSpan.Zero;
                        string blockTexture = null;
                        float cubeMass = 0;
                        int pcu = 0;

                        // Unconstructed portion.
                        if (block?.ConstructionStockpile.Items.Length > 0)
                        {
                            foreach (MyObjectBuilder_StockpileItem item in block.ConstructionStockpile.Items)
                            {
                                if (isNpc)
                                {
                                    TallyItems(item.PhysicalContent.TypeId, item.PhysicalContent.SubtypeName, item.Amount, contentPath, accumulateNpcOres, null, null);
                                }
                                else
                                {
                                    TallyItems(item.PhysicalContent.TypeId, item.PhysicalContent.SubtypeName, item.Amount, contentPath, accumulateUsedOres, null, null);
                                }

                                MyDefinitionBase def = MyDefinitionManager.Static.GetDefinition(item.PhysicalContent.TypeId, item.PhysicalContent.SubtypeName);
                                float componentMass = 0;
                                if (def is MyComponentDefinition cd)
                                {
                                    componentMass = cd.Mass * item.Amount;
                                }
                                else if (def is MyPhysicalItemDefinition pd)
                                {
                                    componentMass = pd.Mass * item.Amount;
                                }

                                cubeMass += componentMass;
                            }
                        }

                        if (cubeBlockDefinition != null)
                        {
                            if (block.BuildPercent < 1f)
                            {
                                // break down the components, to get a accurate counted on the number of components actually in the cube.
                                var componentList = new List<MyComponentDefinition>();

                                foreach (MyCubeBlockDefinition.Component component in cubeBlockDefinition.Components)
                                {
                                    for (int i = 0; i < component.Count; i++)
                                        componentList.Add(component.Definition);
                                }

                                // Round up to nearest whole number.
                                double completeCount = Math.Min(componentList.Count, Math.Ceiling((double)componentList.Count * (double)block.BuildPercent));

                                // count only the components used to reach the BuildPercent, 1 component at a time.
                                for (int i = 0; i < completeCount; i++)
                                {
                                    #region Used Ore Value

                                    if (isNpc)
                                    {
                                        TallyItems(componentList[i].Id.TypeId, componentList[i].Id.SubtypeName, 1, contentPath, accumulateNpcOres, null, null);
                                    }
                                    else
                                    {
                                        TallyItems(componentList[i].Id.TypeId, componentList[i].Id.SubtypeName, 1, contentPath, accumulateUsedOres, null, null);
                                    }

                                    #endregion

                                    float componentMass = componentList[i].Mass * 1;
                                    cubeMass += componentMass;
                                }
                            }
                            else
                            {
                                // Fully armed and operational cube.
                                foreach (var component in cubeBlockDefinition.Components)
                                {
                                    MyComponentDefinition cd = MyDefinitionManager.Static.GetDefinition(component.Definition.Id) as MyComponentDefinition;
                                    #region Used Ore Value

                                    if (isNpc)
                                    {
                                        TallyItems(component.Definition.Id.TypeId, component.Definition.Id.SubtypeName, component.Count, contentPath, accumulateNpcOres, null, null);
                                    }
                                    else
                                    {
                                        TallyItems(component.Definition.Id.TypeId, component.Definition.Id.SubtypeName, component.Count, contentPath, accumulateUsedOres, null, null);
                                    }

                                    #endregion

                                    float componentMass = cd.Mass * component.Count;
                                    cubeMass += componentMass;
                                }
                            }

                            blockTime = TimeSpan.FromSeconds(cubeBlockDefinition.IntegrityPointsPerSec != 0 ? cubeBlockDefinition.MaxIntegrity / cubeBlockDefinition.IntegrityPointsPerSec * block.BuildPercent : 0);
                            blockTexture = (cubeBlockDefinition.Icons == null || cubeBlockDefinition.Icons.First() == null) ? null : SpaceEngineersCore.GetDataPathOrDefault(cubeBlockDefinition.Icons.First(), Path.Combine(contentPath, cubeBlockDefinition.Icons.First()));
                            pcu = cubeBlockDefinition.PCU;
                        }

                        if (!blockType.Equals(typeof(MyObjectBuilder_CubeBlockDefinition)))
                        {
                            FieldInfo[] fields = blockType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

                            #region Inventories

                            FieldInfo[] inventoryFields = [.. fields.Where(f => f.FieldType == typeof(MyObjectBuilder_Inventory))];
                            foreach (FieldInfo field in inventoryFields)
                            {
                                if (field.GetValue(block) is MyObjectBuilder_Inventory inventory)
                                {
                                    foreach (MyObjectBuilder_InventoryItem item in inventory.Items)
                                    {
                                        if (isNpc)
                                        {
                                            TallyItems(item.PhysicalContent.TypeId, item.PhysicalContent.SubtypeName, (decimal)item.Amount, contentPath, accumulateNpcOres, accumulateItems, accumulateComponents);
                                        }
                                        else
                                        {
                                            if (item.PhysicalContent.TypeId == MOBTypeIds.Ore || item.PhysicalContent.TypeId == MOBTypeIds.Ingot)
                                            {
                                                TallyItems(item.PhysicalContent.TypeId, item.PhysicalContent.SubtypeName, (decimal)item.Amount, contentPath, accumulateUnusedOres, accumulateItems, accumulateComponents);
                                            }
                                            else
                                            {
                                                TallyItems(item.PhysicalContent.TypeId, item.PhysicalContent.SubtypeName, (decimal)item.Amount, contentPath, accumulateUsedOres, accumulateItems, accumulateComponents);
                                            }
                                        }
                                    }
                                }
                            }

                            #endregion

                            #region Character inventories

                            FieldInfo[] characterFields = [.. fields.Where(f => f.FieldType == typeof(MyObjectBuilder_Character))];
                            foreach (FieldInfo field in characterFields)
                            {
                                if (field.GetValue(block) is MyObjectBuilder_Character currentCharacter)
                                {
                                    foreach (MyObjectBuilder_InventoryItem item in currentCharacter.Inventory.Items)
                                    {
                                        TallyItems(item.PhysicalContent.TypeId, item.PhysicalContent.SubtypeName, (decimal)item.Amount, contentPath, accumulatePlayerOres, accumulateItems, accumulateComponents);
                                    }
                                }
                            }

                            #endregion
                        }

                        #region Tally Cubes

                        if (cubeBlockDefinition != null)
                        {
                            string itemsKey = cubeBlockDefinition.DisplayNameText;
                            _totalCubes += 1;
                            _totalPCU += pcu;

                            if (accumulateCubes.TryGetValue(itemsKey, out ComponentItemModel value))
                            {
                                value.Count += 1;
                                value.Mass += cubeMass;
                                value.Time += blockTime;
                                value.PCU += pcu;
                            }
                            else
                            {
                                accumulateCubes.Add(itemsKey, new ComponentItemModel { Name = cubeBlockDefinition.DisplayNameText, Count = 1, Mass = cubeMass, TypeId = cubeBlockDefinition.Id.TypeId, SubtypeId = cubeBlockDefinition.Id.SubtypeName, TextureFile = blockTexture, Time = blockTime, PCU = pcu });
                            }
                        }

                        #endregion
                    }

                    accumulateShips.Add(shipContent);
                }
            }

            #region Build Lists

            long sum = accumulateOres.Values.ToList().Sum();
            _untouchedOre = [.. accumulateOres.Select(kvp => new VoxelMaterialAssetModel { MaterialName = SpaceEngineersApi.GetResourceName(kvp.Key), Volume = Math.Round((double)kvp.Value / 255, 7), Percent = kvp.Value / (double)sum })];

            _untouchedOreByAsteroid = accumulateAsteroidOres;
            _unusedOre = [.. accumulateUnusedOres.Values];
            _usedOre = [.. accumulateUsedOres.Values];
            _playerOre = [.. accumulatePlayerOres.Values];
            _npcOre = [.. accumulateNpcOres.Values];
            _allCubes = [.. accumulateCubes.Values];
            _allComponents = [.. accumulateComponents.Values];
            _allItems = [.. accumulateItems.Values];
            _allShips = accumulateShips;

            #endregion

            #region Create Report
            IsReportReady = true;
            ClearProgress();
        }

        #endregion


        #region Tally Items

        private void TallyItems(MyObjectBuilderType tallyTypeId, string tallySubTypeId, decimal amountDecimal, string contentPath, SortedDictionary<string, OreContent> accumulateOres, SortedDictionary<string, ComponentItemModel> accumulateItems, SortedDictionary<string, ComponentItemModel> accumulateComponents)
        {
            if (MyDefinitionManager.Static.GetDefinition(tallyTypeId, tallySubTypeId) is not MyPhysicalItemDefinition cd)
            {
                // A component, gun, ore that doesn't exist (Depricated by KeenSH, or Mod that isn't loaded).
                return;
            }

            string componentTexture = SpaceEngineersCore.GetDataPathOrDefault(cd.Icons.First(), Path.Combine(contentPath, cd.Icons.First()));
            if (tallyTypeId == MOBTypeIds.Ore)
            {
                double mass = Math.Round((double)amountDecimal * cd.Mass, 7);
                double volume = Math.Round((double)amountDecimal * cd.Volume * SpaceEngineersConsts.VolumeMultiplyer, 7);

                #region Unused Ore Value

                string unusedKey = tallySubTypeId;
                if (accumulateOres.TryGetValue(unusedKey, out OreContent oreValue))
                {
                    oreValue.Amount += amountDecimal;
                    oreValue.Mass += mass;
                    oreValue.Volume += volume;
                }
                else
                {
                    accumulateOres.Add(unusedKey, value: new OreContent { Name = cd.DisplayNameText, Amount = amountDecimal, Mass = mass, Volume = volume, TextureFile = componentTexture });
                }

                #endregion

                #region Tally Items

                if (accumulateItems != null)
                {
                    string itemsKey = cd.DisplayNameText;
                    if (accumulateItems.TryGetValue(itemsKey, out ComponentItemModel compValue))
                    {
                        compValue.Count += amountDecimal;
                        compValue.Mass += mass;
                        compValue.Volume += volume;
                    }
                    else
                    {
                        accumulateItems.Add(itemsKey, value: new ComponentItemModel { Name = cd.DisplayNameText, Count = amountDecimal, Mass = mass, Volume = volume, TypeId = tallyTypeId, SubtypeId = tallySubTypeId, TextureFile = componentTexture, Time = TimeSpan.Zero });
                    }
                }

                #endregion
            }
            else if (tallyTypeId == MOBTypeIds.Ingot)
            {
                double mass = Math.Round((double)amountDecimal * cd.Mass, 7);
                double volume = Math.Round((double)amountDecimal * cd.Volume * SpaceEngineersConsts.VolumeMultiplyer, 7);
                var bp = SpaceEngineersApi.GetBlueprint(tallyTypeId, tallySubTypeId);
                TimeSpan timeToMake = TimeSpan.Zero;

                // no blueprint, means the item is not built by players, but generated by the environment.
                if (bp?.Results?.Length != 0)
                {
                    timeToMake = TimeSpan.FromSeconds(bp.BaseProductionTimeInSeconds * (double)amountDecimal / (double)bp.Results[0].Amount);
                }

                #region Unused Ore Value

                var oreRequirements = new Dictionary<string, BlueprintRequirement>();
                SpaceEngineersApi.AccumulateCubeBlueprintRequirements(tallySubTypeId, tallyTypeId, amountDecimal, oreRequirements, out _);

                foreach (KeyValuePair<string, BlueprintRequirement> item in oreRequirements)
                {
                    TallyItems(item.Value.Id.TypeId, item.Value.SubtypeId, item.Value.Amount, contentPath, accumulateOres, null, null);
                }

                #endregion

                #region Tally Items

                if (accumulateItems != null)
                {
                    string itemsKey = cd.DisplayNameText;
                    if (accumulateItems.TryGetValue(itemsKey, out ComponentItemModel value))
                    {
                        value.Count += amountDecimal;
                        value.Mass += mass;
                        value.Volume += volume;
                        value.Time += timeToMake;
                    }
                    else
                    {
                        accumulateItems.Add(itemsKey, new ComponentItemModel { Name = cd.DisplayNameText, Count = amountDecimal, Mass = mass, Volume = volume, TypeId = tallyTypeId, SubtypeId = tallySubTypeId, TextureFile = componentTexture, Time = timeToMake });
                    }
                }

                #endregion
            }
            else if (tallyTypeId == MOBTypeIds.AmmoMagazine ||
                tallyTypeId == MOBTypeIds.PhysicalGunObject ||
                tallyTypeId == MOBTypeIds.OxygenContainerObject)
            {
                double mass = Math.Round((double)amountDecimal * cd.Mass, 7);
                double volume = Math.Round((double)amountDecimal * cd.Volume * SpaceEngineersConsts.VolumeMultiplyer, 7);
                var bp = SpaceEngineersApi.GetBlueprint(tallyTypeId, tallySubTypeId);
                TimeSpan timeToMake = TimeSpan.FromSeconds(bp == null ? 0 : bp.BaseProductionTimeInSeconds * (double)amountDecimal);

                #region Unused Ore Value

                var oreRequirements = new Dictionary<string, BlueprintRequirement>();
                SpaceEngineersApi.AccumulateCubeBlueprintRequirements(tallySubTypeId, tallyTypeId, amountDecimal, oreRequirements, out _);

                foreach (KeyValuePair<string, BlueprintRequirement> item in oreRequirements)
                {
                    TallyItems(item.Value.Id.TypeId, item.Value.SubtypeId, item.Value.Amount, contentPath, accumulateOres, null, accumulateComponents);
                }

                #endregion

                #region Tally Items

                if (accumulateItems != null)
                {
                    string itemsKey = cd.DisplayNameText;
                    if (accumulateItems.TryGetValue(itemsKey, out ComponentItemModel value))
                    {
                        value.Count += amountDecimal;
                        value.Mass += mass;
                        value.Volume += volume;
                        value.Time += timeToMake;
                    }
                    else
                    {
                        accumulateItems.Add(itemsKey, new ComponentItemModel() { Name = cd.DisplayNameText, Count = amountDecimal, Mass = mass, Volume = volume, TypeId = tallyTypeId, SubtypeId = tallySubTypeId, TextureFile = componentTexture, Time = timeToMake });
                    }
                }

                #endregion
            }
            else if (tallyTypeId == MOBTypeIds.Component)
            {
                double mass = Math.Round((double)amountDecimal * cd.Mass, 7);
                double volume = Math.Round((double)amountDecimal * cd.Volume * SpaceEngineersConsts.VolumeMultiplyer, 7);
                MyBlueprintDefinitionBase bp = SpaceEngineersApi.GetBlueprint(tallyTypeId, tallySubTypeId);
                TimeSpan timeToMake = new();

                // mod provides no blueprint for component.
                if (bp != null)
                    timeToMake = TimeSpan.FromSeconds(bp.BaseProductionTimeInSeconds * (double)amountDecimal);

                #region Unused Ore Value

                var oreRequirements = new Dictionary<string, BlueprintRequirement>();
                SpaceEngineersApi.AccumulateCubeBlueprintRequirements(tallySubTypeId, tallyTypeId, amountDecimal, oreRequirements, out _);

                foreach (KeyValuePair<string, BlueprintRequirement> item in oreRequirements)
                {
                    TallyItems(item.Value.Id.TypeId, item.Value.SubtypeId, item.Value.Amount, contentPath, accumulateOres, null, null);
                }

                #endregion

                #region Tally Items

                if (accumulateComponents != null)
                {
                    string itemsKey = cd.DisplayNameText;
                    if (accumulateComponents.TryGetValue(itemsKey, out ComponentItemModel value))
                    {
                        value.Count += amountDecimal;
                        value.Mass += mass;
                        value.Volume += volume;
                        value.Time += timeToMake;
                    }
                    else
                    {
                        accumulateComponents.Add(itemsKey, new ComponentItemModel() { Name = cd.DisplayNameText, Count = amountDecimal, Mass = mass, Volume = volume, TypeId = tallyTypeId, SubtypeId = tallySubTypeId, TextureFile = componentTexture, Time = timeToMake });
                    }
                }

                #endregion

            }
            else if (typeId is MyObjectBuilderType type && type == MOBTypeIds.CubeBlock)
            {
                double mass = Math.Round((double)amountDecimal * cd.Mass, 7);
                double volume = Math.Round((double)amountDecimal * cd.Volume * SpaceEngineersConsts.VolumeMultiplyer, 7);

                if (accumulateItems != null)
                {
                    string itemsKey = cd.DisplayNameText;
                    if (accumulateItems.TryGetValue(itemsKey, out ComponentItemModel value))
                    {
                        value.Count += amountDecimal;
                        value.Mass += mass;
                        value.Volume += volume;
                    }
                    else
                    {
                        accumulateItems.Add(itemsKey, new ComponentItemModel() { Name = cd.DisplayNameText, Count = amountDecimal, Mass = mass, Volume = volume, TypeId = tallyTypeId, SubtypeId = tallySubTypeId, TextureFile = componentTexture });
                    }
                }
            }
            else if (tallyItem is not null)
            {
                double mass = Math.Round((double)amountDecimal * cd.Mass, 7);
                double volume = Math.Round((double)amountDecimal * cd.Volume * SpaceEngineersConsts.VolumeMultiplyer, 7);

                if (accumulateItems != null)
                {
                    string itemsKey = cd.DisplayNameText;
                    if (accumulateItems.TryGetValue(itemsKey, out var value))
                    {
                        value.Count += amountDecimal;
                        value.Mass += mass;
                        value.Volume += volume;
                    }
                    else
                    {
                        accumulateItems.Add(itemsKey, new ComponentItemModel { Name = cd.DisplayNameText, Count = amountDecimal, Mass = mass, Volume = volume, TypeId = tallyTypeId, SubtypeId = tallySubTypeId, TextureFile = componentTexture });
                    }
                }
            }
        }

        internal string CreateReport(ReportType reportType)
        {
            return reportType switch
            {
                ReportType.Text => CreateTextReport(),
                ReportType.Html => CreateHtmlReport(),
                ReportType.Xml => CreateXmlReport(),
                _ => string.Empty,
            };
        }

        internal string CreateTextReport()
        {
            StringBuilder bld = new();

            bld.AppendLine(Res.ClsReportTitle);
            bld.AppendFormat($"{Res.ClsReportSaveWorld} {SaveName}\r\n");
            bld.AppendFormat($"{Res.ClsReportDate} {_generatedDate}\r\n");
            bld.AppendLine();

            #region In-Game Resources

            bld.AppendLine(Res.ClsReportHeaderInGameResources);
            bld.AppendLine(Res.ClsReportTextInGameResources);
            bld.AppendLine();

            bld.AppendFormat($"{Res.ClsReportHeadingUntouchedOre}\r\n");
            bld.AppendFormat($"{Res.ClsReportColMaterialName}\t{Res.ClsReportColVolume + " " + Res.GlobalSIVolumeCubicMetre}\r\n");
            foreach (VoxelMaterialAssetModel item in _untouchedOre)
            {
                bld.AppendFormat($"{item.MaterialName}\t{item.Volume:#,##0.000}\r\n");
            }

            Dictionary<string, List<OreContent>> oreHeaders = new()
            {
                {Res.ClsReportHeaderUnusedUnrefinedOre, _unusedOre},
                {Res.ClsReportHeaderUnusedRefinerdOre, _usedOre},
                {Res.ClsReportHeaderUsedPlayerOre, _playerOre},
                {Res.ClsReportHeaderUsedNpcOre, _npcOre},
            };
            foreach (var oreHader in oreHeaders)
            {

                bld.AppendLine();
                bld.AppendLine(oreHader.Key);
                bld.AppendFormat($"{Res.ClsReportColOreName}\t{Res.ClsReportColMass} {Res.GlobalSIMassKilogram}\t{Res.ClsReportColVolume} {Res.GlobalSIVolumeLitre}\r\n");
                foreach (OreContent item in oreHader.Value)
                {
                    bld.AppendFormat($"{item.FriendlyName}\t{item.Mass:#,##0.000}\t{item.Volume:#,##0.000}\r\n");
                }
            }
            #endregion
            Dictionary<string, (string, List<ComponentItemModel>)> allHeaders = new()
            {
                {Res.ClsReportHeaderAllCubes, (Res.ClsReportColCubeName, _allCubes)},
                {Res.ClsReportHeaderAllComponents,(Res.ClsReportColComponentName, _allComponents)},
                {Res.ClsReportHeaderAllItems, (Res.ClsReportColAllItemsName, _allItems)},
            };
            #region In-Game Assets

            bld.AppendLine();
            bld.AppendLine(Res.ClsReportHeaderInGameAssets);
            bld.AppendLine(Res.ClsReportTextInGameAssets);

            bld.AppendLine();
            bld.AppendLine(Res.ClsReportHeaderTotalCubes);
            bld.AppendLine($"{Res.ClsReportColCount}\t{Res.ClsReportColPCU}");
            bld.AppendLine($"{_totalCubes:#,##0}\t{_totalPCU:#,##0}");

            bld.AppendLine();
            foreach (var header in allHeaders)
            {
                foreach (ComponentItemModel item in header.Value.Item2)
                {
                    bld.AppendLine();
                    bld.AppendLine(header.Key);
                    if (header.Key == Res.ClsReportHeaderAllCubes)
                        bld.AppendLine($"{Res.ClsReportColCubeName}\t{Res.ClsReportColCount}\t{Res.ClsReportColMass} {Res.GlobalSIMassKilogram}\t{Res.ClsReportColTime}\t{Res.ClsReportColPCU}");
                    else
                        bld.AppendFormat($"{item.FriendlyName}\t{item.Count:#,##0}\t{item.Mass:#,##0.000}\t{item.Volume:#,##0.000}\t{item.Time}\r\n");
                }
            }

            #endregion

            #region Asteroid Breakdown

            bld.AppendLine();
            bld.AppendFormat($"{Res.ClsReportHeadingUntouchedOre}\r\n");
            bld.AppendFormat($"{Res.ClsReportColAsteroid}\t${Res.ClsReportColOreName}\t{Res.ClsReportColVolume} {Res.GlobalSIVolumeCubicMetre}\r\n");


            foreach (AsteroidContent asteroid in _untouchedOreByAsteroid)
            {
                foreach (VoxelMaterialAssetModel item in asteroid.UntouchedOreList ?? [])
                {
                    bld.AppendFormat($"{asteroid.Name}\t{item.MaterialName}\t{item.Volume:#,##0.000}\r\n");
                }
            }

            #endregion

            #region Ship Breakdown
            // SHIP BREAKDOWN
            bld.AppendLine();
            bld.AppendLine(Res.ClsReportColHeaderShips);
            bld.AppendFormat($"{Res.ClsReportColShip}\t{Res.ClsReportColEntityId}\t{Res.ClsReportColMass} {Res.GlobalSIMassKilogram}\t{Res.ClsReportColVolume} {Res.ClsReportColBlockCount}\t{Res.ClsReportColTime}\t{Res.ClsReportColPCU}\r\n");

            foreach (ShipContent item in _allShips)
            {
                bld.AppendFormat($"{item.DisplayName}\t{item.EntityId}\t{item.Mass:#,##0.000}\t{item.BlockCount}\r\n");
            }
            #endregion

            return bld.ToString();
        }

        #endregion

        #region CreateHtmlReport

        internal string CreateHtmlReport()
        {
            var writer = new StringWriter();
            #region Header

            writer.BeginDocument($"{Res.ClsReportTitle} - {_saveName}", CssStyle);

            #endregion

            writer.RenderElement("h1", Res.ClsReportTitle);

            writer.RenderElement($"{Res.ClsReportSaveWorld} {SaveName}");
            writer.RenderElement($"{Res.ClsReportDate} {_generatedDate}");

            #region In-Game Resources

            writer.RenderElement("h2", Res.ClsReportHeaderInGameResources);
            writer.RenderElement("p", Res.ClsReportTextInGameResources);

            writer.RenderElement("h3", Res.ClsReportHeadingUntouchedOre);
            writer.BeginTable("1", "3", "0", [Res.ClsReportColMaterialName, Res.ClsReportColVolume + " " + Res.GlobalSIVolumeCubicMetre]);
            foreach (VoxelMaterialAssetModel item in _untouchedOre)
            {
                writer.RenderTagStart("tr");
                writer.RenderElement("td", item.MaterialName);
                writer.AddAttribute("class", "right");
                writer.RenderElement("td", $"{item.Volume:#,##0.000}");
                writer.RenderTagEnd("tr");
            }
            writer.EndTable();

            Dictionary<string, List<OreContent>> oreHeaders = new()
            {
                {Res.ClsReportHeaderUnusedUnrefinedOre, _unusedOre},
                {Res.ClsReportHeaderUnusedRefinerdOre, _usedOre},
                {Res.ClsReportHeaderUsedPlayerOre, _playerOre},
                {Res.ClsReportHeaderUsedNpcOre, _npcOre},
            };

            foreach (var oreHeader in oreHeaders)
            {
                if (oreHeader.Value.Count == 0)
                    continue;

                writer.RenderElement("h3", oreHeaders.Keys);
                writer.BeginTable("1", "3", "0", [Res.ClsReportColOreName, Res.ClsReportColMass + " " + Res.GlobalSIMassKilogram, Res.ClsReportColVolume + " " + Res.GlobalSIVolumeLitre]);

                foreach (OreContent item in oreHeader.Value)
                {
                    writer.RenderTagStart("tr");
                    writer.RenderElement("td", item.FriendlyName);
                    writer.AddAttribute("class", "right");
                    writer.RenderElement("td", $"{item.Mass:#,##0.000}");
                    writer.AddAttribute("class", "right");
                    writer.RenderElement("td", $"{item.Volume:#,##0.000}");
                    writer.RenderTagEnd("tr");
                }
                writer.EndTable();
            }

            #endregion

            writer.RenderElement("h2");
            writer.RenderElement("hr");

            #region In-Game Assets

            writer.RenderElement("h2", Res.ClsReportHeaderInGameAssets);
            writer.RenderElement("p", Res.ClsReportTextInGameAssets);

            writer.RenderElement("h3", Res.ClsReportHeaderTotalCubes);
            writer.BeginTable("1", "3", "0", [Res.ClsReportColCount, Res.ClsReportColPCU]);

            writer.RenderTagStart("tr");
            writer.AddAttribute("class", "right");
            writer.RenderElement("td", $"{_totalCubes:#,##0}");

            writer.AddAttribute("class", "right");
            writer.RenderElement("td", $"{_totalPCU:#,##0}");

            writer.RenderTagEnd("tr"); // Tr
            writer.EndTable();

            Dictionary<string, List<ComponentItemModel>> allHeaders = new()
            {
               {Res.ClsReportHeaderAllCubes, _allCubes},
               {Res.ClsReportHeaderAllComponents, _allComponents},
               {Res.ClsReportHeaderAllItems, _allItems},
            };

            foreach (var header in allHeaders)
            {
                if (header.Value.Count == 0)
                    continue;

                writer.RenderElement("h3", allHeaders.Keys);
                writer.BeginTable("1", "3", "0", [Res.ClsReportColIcon, Res.ClsReportColCubeName, Res.ClsReportColCount, Res.ClsReportColMass + " " + Res.GlobalSIMassKilogram, Res.ClsReportColTime, Res.ClsReportColPCU]);
                foreach (ComponentItemModel item in header.Value)
                {
                    writer.RenderTagStart("tr");
                    writer.RenderTagStart("td");
                    if (item.TextureFile != null)
                    {
                        string texture = GetTextureToBase64(item.TextureFile, 32, 32);
                        if (!string.IsNullOrEmpty(texture))
                        {
                            writer.AddAttribute("src", "data:image/png;base64," + texture);
                            writer.AddAttribute("width", "32");
                            writer.AddAttribute("height", "32");
                            writer.AddAttribute("alt", Path.GetFileNameWithoutExtension(item.TextureFile));
                            writer.RenderTagStart("img");
                            writer.RenderTagEnd("tr");
                        }
                    }
                    writer.RenderTagEnd("td"); // Td

                    writer.RenderElement("td", item.FriendlyName);
                    writer.AddAttribute("class", "right");
                    writer.RenderElement("td", $"{item.Count:#,##0}");
                    writer.AddAttribute("class", "right");
                    writer.RenderElement("td", $"{item.Mass:#,##0.000}");
                    writer.RenderElement("td", $"{item.Time}");
                    writer.AddAttribute("class", "right");
                    writer.RenderElement("td", $"{item.PCU:#,##0}");
                    writer.RenderTagEnd("tr"); // Tr
                }

                writer.EndTable();
            }


            writer.RenderElement("br");
            writer.RenderElement("hr");

            writer.RenderElement("h2", Res.ClsReportHeadingResourcesBreakdown);

            writer.RenderElement("h3", Res.ClsReportHeadingUntouchedOre);
            writer.BeginTable("1", "3", "0", [Res.ClsReportColAsteroid, Res.ClsReportColPosition, Res.ClsReportColOreName, Res.ClsReportColVolume + " " + Res.GlobalSIVolumeCubicMetre]);
            foreach (AsteroidContent asteroid in _untouchedOreByAsteroid)
            {
                int inx = 0;
                foreach (VoxelMaterialAssetModel item in asteroid.UntouchedOreList ?? [])
                {
                    writer.RenderTagStart("tr");
                    if (inx == 0)
                    {
                        writer.AddAttribute("rowspan", $"{asteroid.UntouchedOreList.Count}");
                        writer.RenderElement("td", asteroid.Name);
                        writer.AddAttribute("rowspan", $"{asteroid.UntouchedOreList.Count}");
                        writer.RenderElement("td", $"{asteroid.Position.X},{asteroid.Position.Y},{asteroid.Position.Z}");
                    }
                    writer.RenderElement("td", item.MaterialName);
                    writer.AddAttribute("class", "right");
                    writer.RenderElement("td", $"{item.Volume:#,##0.000}");
                    writer.RenderTagEnd("tr"); // Tr
                    inx++;
                }
            }
            writer.EndTable();

            #endregion

            writer.RenderElement("br");
            writer.RenderElement("hr");

            #region Ship Breakdown

            writer.BeginTable("1", "3", "0", [Res.ClsReportColShip, Res.ClsReportColEntityId, Res.ClsReportColPosition, Res.ClsReportColBlockCount, Res.ClsReportColPCU]);

            foreach (ShipContent ship in _allShips)
            {
                writer.RenderTagStart("tr");
                writer.RenderElement("td", ship.DisplayName);
                writer.RenderElement("td", ship.EntityId);
                writer.RenderElement("td", $"{ship.Position.X},{ship.Position.Y},{ship.Position.Z}");

                writer.AddAttribute("class", "right");
                writer.RenderElement("td", ship.BlockCount);
                writer.AddAttribute("class", "right");
                writer.RenderElement("td", $"{ship.PCU:#,##0}");
                writer.AddAttribute("class", "right");
                writer.RenderElement("td", $"{ship.Mass:#,##0.000}");
                writer.AddAttribute("class", "right");
                writer.RenderElement("td", $"{ship.Volume:#,##0.000}");
                writer.RenderTagEnd("tr"); // Tr
            }
            writer.EndTable();

            #endregion

            writer.EndDocument();

            return writer.ToString();
        }

        #endregion

        #region CreateXmlReport

        internal string CreateXmlReport()
        {
            XmlWriterSettings settingsDestination = new()
            {
                Indent = true,
                Encoding = new UTF8Encoding(false)
            };

            StringWriter stringWriter = new();

            using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, settingsDestination))
            {
                xmlWriter.WriteStartElement("report");
                xmlWriter.WriteAttributeString("title", Res.ClsReportTitle);
                xmlWriter.WriteAttributeString("world", SaveName);
                xmlWriter.WriteAttributeFormat("date", $"{_generatedDate:o}");

                #region In-Game Resources

                foreach (VoxelMaterialAssetModel item in _untouchedOre)
                {
                    xmlWriter.WriteStartElement("untouched");
                    xmlWriter.WriteElementFormat("orename", $"{item.MaterialName}");
                    xmlWriter.WriteElementFormat("volume", $"{item.Volume:0.000}");
                    xmlWriter.WriteEndElement();
                }

                List<List<OreContent>> _oreContents = new()
                {
                    _unusedOre,
                    _usedOre,
                    _playerOre,
                    _npcOre
                };
                foreach (var oreContent in _oreContents)
                {
                    foreach (var item in oreContent)
                    {
                        xmlWriter.WriteStartElement("unused");
                        xmlWriter.WriteElementString("name", $"{item.FriendlyName}");
                        xmlWriter.WriteElementString("mass", $"{item.Mass:0.000}");
                        xmlWriter.WriteElementString("volume", $"{item.Volume:0.000}");
                        xmlWriter.WriteEndElement();
                    }
                }

                #endregion

                #region In-Game Assets
                Dictionary<string, List<ComponentItemModel>> allHeaders = new()
                {
                    {"cubes", _allCubes},
                    {"components", _allComponents},
                    {"items", _allItems}
                };

                foreach (var header in allHeaders)
                {
                    if (header.Value.Count == 0) continue;
                    foreach (ComponentItemModel item in header.Value)
                    {

                        xmlWriter.WriteStartElement(header.Key);
                        xmlWriter.WriteElementFormat("friendlyname", $"{item.FriendlyName}");
                        xmlWriter.WriteElementFormat("name", $"{item.Name}");
                        xmlWriter.WriteElementFormat("typeid", $"{item.TypeId}");
                        xmlWriter.WriteElementFormat("subtypeid", $"{item.SubtypeId}");
                        xmlWriter.WriteElementFormat("count", $"{item.Count:0}");
                        xmlWriter.WriteElementFormat("mass", $"{item.Mass:0.000}");
                        xmlWriter.WriteElementFormat("time", $"{item.Time}");
                        if (header.Key == "cubes")
                        {
                            xmlWriter?.WriteElementFormat("pcu", $"{item.PCU}");
                        }

                        xmlWriter.WriteEndElement();
                    }

                }
                #endregion

                #region Asteroid breakdown

                foreach (AsteroidContent asteroid in _untouchedOreByAsteroid)
                {
                    xmlWriter.WriteStartElement("asteroids");
                    xmlWriter.WriteAttributeString("name", asteroid.Name);

                    foreach (VoxelMaterialAssetModel item in asteroid.UntouchedOreList ?? [])
                    {
                        xmlWriter.WriteStartElement("untouched");
                        xmlWriter.WriteElementFormat("orename", $"{item.MaterialName}");
                        xmlWriter.WriteElementFormat("volume", $"{item.Volume:0.000}");
                        xmlWriter.WriteEndElement();
                    }

                    xmlWriter.WriteEndElement();
                }

                #endregion

                #region Ship breakdown

                // TODO:

                #endregion

                xmlWriter.WriteEndElement();
            }

            return stringWriter.ToString();
        }

        #endregion

        #endregion

        #region CreateErrorReport

        internal string CreateErrorReport(ReportType reportType, string errorContent)
        {
            _generatedDate = DateTime.Now;
            return reportType switch
            {
                ReportType.Text => CreateTextErrorReport(errorContent),
                ReportType.Html => CreateHtmlErrorReport(errorContent),
                ReportType.Xml => CreateXmlErrorReport(errorContent),
                _ => string.Empty,
            };
        }

        internal string CreateTextErrorReport(string errorContent)
        {
            StringBuilder bld = new();

            bld.AppendLine(Res.ClsReportTitle);
            bld.AppendFormat($"{Res.ClsReportDate} {_generatedDate}\r\n");
            bld.AppendFormat($"{Res.ClsReportError} {errorContent}\r\n");
            bld.AppendLine();
            return bld.ToString();
        }

        internal string CreateHtmlErrorReport(string errorContent)

        {
            {
                using var writer = new StringWriter();
                writer.RenderElement("h1", Res.ClsReportTitle);
                writer.RenderElement("p", $"{Res.ClsReportDate}{_generatedDate}");
                writer.RenderElement("p", $"{Res.ClsReportError}{errorContent}");

                return writer.ToString();
            }
        }

        internal string CreateXmlErrorReport(string errorContent)
        {
            XmlWriterSettings settingsDestination = new()
            {
                Indent = true,
                Encoding = new UTF8Encoding(false)
            };

            StringWriter stringWriter = new();

            using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, settingsDestination))
            {
                xmlWriter.WriteStartElement("report");
                xmlWriter.WriteAttributeString("title", Res.ClsReportTitle);
                xmlWriter.WriteAttributeFormat("date", $"{_generatedDate:o}");
                xmlWriter.WriteAttributeString("error", errorContent);

                xmlWriter.WriteEndElement();
            }

            return stringWriter.ToString();
        }

        #endregion



        #region GenerateOfflineReport

        /// <summary>
        /// Command line driven method.
        /// <example>
        /// /WR "Easy Start Report" "c:\temp\Easy Start Report.txt"
        /// /WR "C:\Users\%USERNAME%\AppData\Roaming\SpaceEngineersDedicated\Saves\Super Excellent Map\sandbox.sbc" "c:\temp\Super Excellent Map.txt"
        /// /WR "C:\Users\%USERNAME%\AppData\Roaming\SpaceEngineersDedicated\Saves\Super Excellent Map" "c:\temp\Super Excellent Map.txt"
        /// /WR "\\SERVER\Dedicated Saves\Super Excellent Map" "\\SERVER\Reports\Super Excellent Map.txt"
        /// </example>
        /// </summary>
        /// <param name="baseModel"></param>
        /// <param name="args"></param>
        public static void GenerateOfflineReport(ExplorerModel baseModel, string[] args)
        {
            string delimiter = "/" ?? "-";
            List<string> argList = [.. args];
            string[] comArgs = [.. args.Where(a => a.Equals($"{delimiter}WR", StringComparison.CurrentCultureIgnoreCase) || a.Equals("-WR", StringComparison.CurrentCultureIgnoreCase))];
            foreach (string a in comArgs) argList.Remove(a);

            if (argList.Count < 2)
            {
                Environment.Exit(2);
            }

            string findSession = argList[0].ToUpper();
            string reportFile = argList[1];
            ReportType reportType = GetReportType(Path.GetExtension(reportFile));

            if (reportType == ReportType.Unknown)
            {
                // this terminates the application.
                Environment.Exit(1);
            }

            if (File.Exists(findSession))
            {
                findSession = Path.GetDirectoryName(findSession) ?? string.Empty;
            }

            ResourceReportModel model = new();
            WorldResource world;
            string errorInformation;

            if (Directory.Exists(findSession))
            {
                if (!SelectWorldModel.LoadSession(findSession, out world, out errorInformation))
                {
                    SConsole.WriteLine(errorInformation);

                    File.WriteAllText(reportFile, model.CreateErrorReport(reportType, errorInformation));

                    Environment.Exit(3);
                }
            }
            else
            {
                if (!SelectWorldModel.FindSaveSession(SpaceEngineersConsts.BaseLocalPath.SavesPath, findSession, out world, out errorInformation))
                {
                    SConsole.WriteLine(errorInformation);
                    File.WriteAllText(reportFile, model.CreateErrorReport(reportType, errorInformation));

                    // this terminates the application.
                    Environment.Exit(3);
                }
            }

            baseModel.ActiveWorld = world;
            baseModel.ActiveWorld.LoadDefinitionsAndMods();
            if (!baseModel.ActiveWorld.LoadSector(out errorInformation, true))
            {
                File.WriteAllText(reportFile, model.CreateErrorReport(reportType, errorInformation));

                // this terminates the application.
                Environment.Exit(3);
            }
            baseModel.ParseSandBox();

            model.Load(baseModel.ActiveWorld.SaveName, baseModel.Structures);
            model.GenerateReport();
            if (VRage.Plugins.MyPlugins.Loaded)
            {
                VRage.Plugins.MyPlugins.Unload();
            }
            TempFileUtil.Dispose();

            File.WriteAllText(reportFile, model.CreateReport(reportType));

            // no errors returned.
            Environment.Exit(0);
        }

        #endregion

        private static string GetTextureToBase64(string fileName, int width, int height, bool ignoreAlpha = false)
        {
            using Stream stream = MyFileSystem.OpenRead(fileName);
            return TexUtil.GetTextureToBase64(stream, fileName, width, height, ignoreAlpha);
        }

        #region Helper Classes

        public class AsteroidContent
        {
            public string Name { get; set; }
            public Vector3D Position { get; set; }
            public long Empty { get; set; }
            public List<VoxelMaterialAssetModel> UntouchedOreList { get; set; }
        }

        public class OreContent
        {
            BaseModel baseModel = new();
            private string _name;
            public string Name
            {
                get => _name ?? string.Empty;
                set => baseModel.SetProperty(ref _name, value, FriendlyName = SpaceEngineersApi.GetResourceName(Name), nameof(FriendlyName));
            }

            public string FriendlyName { get; set; }
            public decimal Amount { get; set; }
            public double Mass { get; set; }
            public double Volume { get; set; }
            public string TextureFile { get; set; }
        }

        public class ShipContent
        {
            public string DisplayName { get; set; }
            public Vector3D Position { get; set; }
            public long EntityId { get; set; }
            public int BlockCount { get; set; }
            public int PCU { get; set; }
            public decimal Amount { get; set; }
            public double Mass { get; set; }
            public double Volume { get; set; }
            public TimeSpan Time { get; set; }
        }
    }
}

#endregion
