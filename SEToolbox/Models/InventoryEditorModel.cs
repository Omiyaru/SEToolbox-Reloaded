using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Sandbox.Definitions;
using SEToolbox.Interop;
using SEToolbox.Support;
using VRage.Game;
using MOBTypeIds = SEToolbox.Interop.SpaceEngineersTypes.MOBTypeIds;

namespace SEToolbox.Models
{
    [Serializable]
    public class InventoryEditorModel : BaseModel
    {
        #region Fields

        // Fields are marked as NonSerialized, as they aren't required during the drag-drop operation.

        [NonSerialized]
        private string _name;

        [NonSerialized]
        private bool _isValid;

        [NonSerialized]
        private ObservableCollection<InventoryModel> _items;

        [NonSerialized]
        private InventoryModel _selectedRow;

        [NonSerialized]
        private double _totalVolume;

        [NonSerialized]
        private double _totalMass;

        [NonSerialized]
        private float _maxVolume;

        [NonSerialized]
        private readonly MyObjectBuilder_Inventory _inventory;

        // not required for Cube inventories.
        [NonSerialized]
        private readonly MyObjectBuilder_Character _character;

        [NonSerialized]
        private MyObjectBuilder_InventoryItem _item;
        #endregion

        #region Ctor

        public InventoryEditorModel(bool isValid)
        {
            IsValid = isValid;
		}


        public InventoryModel GetInventoryModel(MyObjectBuilder_InventoryItem item, string name, string description)
        {
            _name = string.Empty;
            _items = [];

            _item = new MyObjectBuilder_InventoryItem();

            _selectedRow = new InventoryModel(_item, name, description);
            _isValid = true;

            _inventory.Items.Add(_item);
              return new InventoryModel(item, name, description);
        }

        public InventoryEditorModel(MyObjectBuilder_Inventory inventory, float maxVolume, MyObjectBuilder_Character character = null)
        {

            _name = string.Empty;
            _item = new MyObjectBuilder_InventoryItem();
            _items = [];
            _selectedRow = null;
            _inventory = inventory;
            _maxVolume = maxVolume;
            _character = character;
            UpdateGeneralFromEntityBase();

                if (inventory.Items.Any())
                {
                    MyObjectBuilder_InventoryItem firstItem = inventory.Items.FirstOrDefault();
                    if (firstItem != null)
                    {
                       _selectedRow = new InventoryModel(firstItem, "Item Name", "Item Description");
                        _isValid = true;
                    }

            // Cube.InventorySize.X * Cube.InventorySize.Y * Cube.InventorySize.Z * 1000 * Sandbox.InventorySizeMultiplier;
            // or Cube.InventoryMaxVolume * 1000 * Sandbox.InventorySizeMultiplier;
            //Character.Inventory = 0.4 * 1000 * Sandbox.InventorySizeMultiplier;
            }
        }

        #endregion

        #region Properties

        [XmlIgnore]
        public string Name
        {
            get => _name;

            set => SetProperty(ref _name, value, nameof(Name));
        }

        [XmlIgnore]
        public bool IsValid
        {
            get => _isValid;
            set => SetProperty(ref _isValid, value, nameof(IsValid));
        }

        [XmlIgnore]
        public ObservableCollection<InventoryModel> Items
        {
            get => _items;

            set => SetProperty(ref _items, value, nameof(Items));
        }

        [XmlIgnore]
        public InventoryModel SelectedRow
        {
            get => _selectedRow;
            set => SetProperty(ref _selectedRow, value, nameof(SelectedRow));
        }

        [XmlIgnore]
        public double TotalVolume
        {
            get => _totalVolume;

            set => SetProperty(ref _totalVolume, value, nameof(TotalVolume));
        }

        [XmlIgnore]
        public double TotalMass
        {
            get => _totalMass;

            set => SetProperty(ref _totalMass, value, nameof(TotalMass));
        }

        [XmlIgnore]
        public float MaxVolume
        {
            get => _maxVolume;

            set => SetProperty(ref _maxVolume, value, nameof(MaxVolume));
        }

        #endregion

        #region Methods

        private void UpdateGeneralFromEntityBase()
        {
            ObservableCollection<InventoryModel> list = [];
            string contentPath = ToolboxUpdater.GetApplicationContentPath();
            TotalVolume = 0;
            TotalMass = 0;

            if (_inventory != null)
            {
                foreach (MyObjectBuilder_InventoryItem item in _inventory.Items)
                {
                    list.Add(CreateItem(item, contentPath));
                }
            }

            Items = list;
        }

        private InventoryModel CreateItem(MyObjectBuilder_InventoryItem item, string contentPath)
        {
            MyPhysicalItemDefinition definition = MyDefinitionManager.Static.GetDefinition(item.PhysicalContent.TypeId, item.PhysicalContent.SubtypeName) as MyPhysicalItemDefinition;

            string name;
            string textureFile;
            double massMultiplyer;
            double volumeMultiplyer;

            if (definition == null)
            {
                name = item.PhysicalContent.SubtypeName + " " + item.PhysicalContent.TypeId;
                massMultiplyer = 1;
                volumeMultiplyer = 1;
                textureFile = null;
            }
            else
            {
                name = definition.DisplayNameText;
                massMultiplyer = definition.Mass;
                volumeMultiplyer = definition.Volume * SpaceEngineersConsts.VolumeMultiplyer;
                textureFile = (definition.Icons == null || definition.Icons.First() == null) ? null : SpaceEngineersCore.GetDataPathOrDefault(definition.Icons.First(), Path.Combine(contentPath, definition.Icons.First()));
            }

            InventoryModel newItem = new(item, name, item.PhysicalContent.SubtypeName)
            {
                Name = name,
                Amount = (decimal)item.Amount,
                SubtypeId = item.PhysicalContent.SubtypeName,
                TypeId = item.PhysicalContent.TypeId,
                MassMultiplier = massMultiplyer,
                VolumeMultiplier = volumeMultiplyer,
                TextureFile = textureFile,
                IsUnique = item.PhysicalContent.TypeId == MOBTypeIds.PhysicalGunObject || item.PhysicalContent.TypeId == MOBTypeIds.OxygenContainerObject,
                IsInteger = item.PhysicalContent.TypeId == MOBTypeIds.Component || item.PhysicalContent.TypeId == MOBTypeIds.AmmoMagazine,
                IsDecimal = item.PhysicalContent.TypeId == MOBTypeIds.Ore || item.PhysicalContent.TypeId == MOBTypeIds.Ingot,
                Exists = definition != null, // item no longer exists in Space Engineers definitions.
            };

            TotalVolume += newItem.Volume;
            TotalMass += newItem.Mass;

            return newItem;
        }

        internal void Additem(MyObjectBuilder_InventoryItem item)
        {
            string contentPath = ToolboxUpdater.GetApplicationContentPath();
            item.ItemId = _inventory.nextItemId++;
            _inventory.Items.Add(item);
            Items.Add(CreateItem(item, contentPath));
        }

        internal void RemoveItem(int index)
        {
            var invItem = _inventory.Items[index];

            // Remove HandWeapon if item is HandWeapon.
            if (_character != null && _character.HandWeapon != null && invItem.PhysicalContent.TypeId == MOBTypeIds.PhysicalGunObject)
            {
                if (((MyObjectBuilder_PhysicalGunObject)invItem.PhysicalContent).GunEntity?.EntityId == _character.HandWeapon.EntityId)
                {
                    _character.HandWeapon = null;
                }
            }

            TotalVolume -= Items[index].Volume;
            TotalMass -= Items[index].Mass;
            Items.RemoveAt(index);
            _inventory.Items.RemoveAt(index);
            _inventory.nextItemId--;

            // Re-index ItemId.
            for (uint i = 0; i < _inventory.Items.Count; i++)
            {
                _inventory.Items[(int)i].ItemId = i;
            }
        }

        #endregion
    }
}
