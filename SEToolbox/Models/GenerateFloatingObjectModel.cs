using VRage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Sandbox.Definitions;
using SEToolbox.Interop;
using SEToolbox.Support;
using VRage.Game;
using SETypes = SEToolbox.Interop.SpaceEngineersTypes;


namespace SEToolbox.Models
{
    public class GenerateFloatingObjectModel : BaseModel
    {
        public const int UniqueUnits = 1;

        #region Fields

        private MyPositionAndOrientation _characterPosition;
        private ObservableCollection<ComponentItemModel> _stockItemList;
        private ComponentItemModel _stockItem;

        private bool _isValidItemToImport;

        private double? _volume;
        private double? _mass;
        private int? _units;
        private decimal? _decimalUnits;
        private bool _isDecimal;
        private bool _isInt;
        private bool _isUnique;
        private int _multiplier;
        private float _maxFloatingObjects;

        #endregion

        #region Ctor

        public GenerateFloatingObjectModel()
        {
            _stockItemList = [];
            Multiplier = 1;
        }

        #endregion

        #region Properties

        public MyPositionAndOrientation CharacterPosition
        {
            get => _characterPosition;
            // unable to check for equivalence and mostly unnecessary
            set => SetProperty(ref _characterPosition, value, nameof(CharacterPosition));
        }

        public ObservableCollection<ComponentItemModel> StockItemList
        {
            get => _stockItemList;
            set => SetProperty(ref _stockItemList, value, nameof(StockItemList));
        }

        public ComponentItemModel StockItem
        {
            get => _stockItem;
            set => SetProperty(ref _stockItem, value, nameof(StockItem));
        }

        public bool IsValidItemToImport
        {
            get => _isValidItemToImport;
            set => SetProperty(ref _isValidItemToImport, value, nameof(IsValidItemToImport));
        }

        public double? Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value, nameof(Volume));
        }

        public double? Mass
        {
            get => _mass;
            set => SetProperty(ref _mass, value, nameof(Mass));
        }

        public int? Units
        {
            get => _units;
            set => SetProperty(ref _units, value, nameof(Units), () => SetMassVolume());
                   

        }

        public decimal? DecimalUnits
        {
            get => _decimalUnits;
            set => SetProperty(ref _decimalUnits, value, nameof(DecimalUnits), ()=>{ SetMassVolume()});
        }

        public bool IsDecimal
        {
            get => _isDecimal;
            set => SetProperty(ref _isDecimal, value, nameof(IsDecimal));
        }

        public bool IsInt
        {
            get => _isInt;
            set => SetProperty(ref _isInt, value, nameof(IsInt));
        }

        public bool IsUnique
        {
            get => _isUnique;
            set => SetProperty(ref _isUnique, value, nameof(IsUnique));
        }

        /// <summary>
        /// Generates this many individual items.
        /// </summary>
        public int Multiplier
        {
            get => _multiplier;
            set => SetProperty(ref _multiplier, value, nameof(Multiplier));
        }

        /// <summary>
        /// The maximum number of floating objects as defined in the World.
        /// </summary>
        public float MaxFloatingObjects
        {
            get => _maxFloatingObjects;
            set => SetProperty(ref _maxFloatingObjects, value, nameof(MaxFloatingObjects));
        }

        #endregion

        #region Methods

        public void Load(MyPositionAndOrientation characterPosition, float maxFloatingObjects)
        {
            MaxFloatingObjects = maxFloatingObjects;
            CharacterPosition = characterPosition;
            StockItemList.Clear();
            List<ComponentItemModel> list = [];
            string contentPath = ToolboxUpdater.GetApplicationContentPath();

            foreach (MyComponentDefinition componentDefinition in SpaceEngineersResources.ComponentDefinitions)
            {
                MyBlueprintDefinitionBase bp = SpaceEngineersApi.GetBlueprint(componentDefinition.Id.TypeId, componentDefinition.Id.SubtypeName);
                list.Add(new ComponentItemModel
                {
                    Name = componentDefinition.DisplayNameText,
                    TypeId = componentDefinition.Id.TypeId,
                    SubtypeId = componentDefinition.Id.SubtypeName,
                    Mass = componentDefinition.Mass,
                    TextureFile = componentDefinition.Icons == null ? null : SpaceEngineersCore.GetDataPathOrDefault(componentDefinition.Icons.First(), Path.Combine(contentPath, componentDefinition.Icons.First())),
                    Volume = componentDefinition.Volume * SpaceEngineersConsts.VolumeMultiplyer,
                    Accessible = componentDefinition.Public,
                    Time = bp != null ? TimeSpan.FromSeconds(bp.BaseProductionTimeInSeconds) : (TimeSpan?)null,
                });
            }

            foreach (MyPhysicalItemDefinition physicalItemDefinition in SpaceEngineersResources.PhysicalItemDefinitions)
            {
                if (physicalItemDefinition.Id.TypeId == typeof(MyObjectBuilder_TreeObject) ||
                    physicalItemDefinition.Id.SubtypeName == "CubePlacerItem" ||
                    physicalItemDefinition.Id.SubtypeName == "WallPlacerItem")
                    continue;

                MyBlueprintDefinitionBase bp = SpaceEngineersApi.GetBlueprint(physicalItemDefinition.Id.TypeId, physicalItemDefinition.Id.SubtypeName);
                list.Add(new ComponentItemModel
                {
                    Name = physicalItemDefinition.DisplayNameText,
                    TypeId = physicalItemDefinition.Id.TypeId,
                    SubtypeId = physicalItemDefinition.Id.SubtypeName,
                    Mass = physicalItemDefinition.Mass,
                    Volume = physicalItemDefinition.Volume * SpaceEngineersConsts.VolumeMultiplyer,
                    TextureFile = physicalItemDefinition.Icons == null ? null : SpaceEngineersCore.GetDataPathOrDefault(physicalItemDefinition.Icons.First(), Path.Combine(contentPath, physicalItemDefinition.Icons.First())),
                    Accessible = physicalItemDefinition.Public,
                    Time = bp != null ? TimeSpan.FromSeconds(bp.BaseProductionTimeInSeconds) : (TimeSpan?)null,
                });
            }

            foreach (ComponentItemModel item in list.OrderBy(i => i.FriendlyName))
            {
                StockItemList.Add(item);
            }

            list.Clear();

            foreach (MyCubeBlockDefinition cubeDefinition in SpaceEngineersResources.CubeBlockDefinitions)
            {
                list.Add(new ComponentItemModel
                {
                    Name = cubeDefinition.DisplayNameText,
                    TypeId = cubeDefinition.Id.TypeId,
                    SubtypeId = cubeDefinition.Id.SubtypeName,
                    CubeSize = cubeDefinition.CubeSize,
                    TextureFile = cubeDefinition.Icons == null ? null : Path.Combine(contentPath, cubeDefinition.Icons.First()),
                    Accessible = !string.IsNullOrEmpty(cubeDefinition.Model),
                });
            }

            foreach (ComponentItemModel item in list.OrderBy(i => i.FriendlyName))
            {
                StockItemList.Add(item);
            }
        }

        private void SetMassVolume()
        {
            if (StockItem == null)
            {
                Mass = null;
                Volume = null;
            }
            else
            {
                IsUnique = StockItem.TypeId != SETypes.MOBTypeIds.Ore &&
                           StockItem.TypeId != SETypes.MOBTypeIds.Ingot &&
                           StockItem.TypeId != SETypes.MOBTypeIds.Component &&
                           StockItem.TypeId != SETypes.MOBTypeIds.AmmoMagazine &&
                           StockItem.TypeId != SETypes.MOBTypeIds.PhysicalGunObject;

                IsInt = !IsUnique;

                IsDecimal = StockItem.TypeId == SETypes.MOBTypeIds.Ore ||
                            StockItem.TypeId == SETypes.MOBTypeIds.Ingot ||
                            StockItem.TypeId == SETypes.MOBTypeIds.AmmoMagazine;

                if (IsUnique)
                {
                    Mass = UniqueUnits * StockItem.Mass;
                    Volume = UniqueUnits * StockItem.Volume;
                }
                else if (IsInt)
                {
                    Mass = Units.HasValue ? Units.Value * StockItem.Mass : null;
                    Volume = Units.HasValue ? Units.Value * StockItem.Volume : null;
                }
                else
                {
                    Mass = DecimalUnits.HasValue ? (double)DecimalUnits * StockItem.Mass : null;
                    Volume = DecimalUnits.HasValue ? (double)DecimalUnits * StockItem.Volume : null;
                }
            }
        }

        #endregion
    }
}
