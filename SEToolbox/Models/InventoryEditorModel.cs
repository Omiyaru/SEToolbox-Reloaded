using System;
using System.Collections.Generic;
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

            UpdateItems(inventory);
            // pointers to fixed bufffers may only be used for in an unsafe context, such as in a constructor.   
            //   Cube.InventorySize.X * CUbe.InventorySize.Y * CUbe.InventorySize.Z * 1000 * Sandbox.InventorySizeMultiplier
            //  || Cube.InventoryMaxVolume * 1000 * Sandbox.InventorySizeMultiplier;
            // Character.Inventory = 0.4 * 1000 * Sandbox.InventorySizeMultiplier;

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
           List<InventoryModel> list = [];
            string contentPath = ToolboxUpdater.GetApplicationContentPath();
            TotalVolume = 0;
            TotalMass = 0;
            list.Clear();

            list.AddRange(_inventory?.Items.Select(item => CreateItem(item, contentPath)) ?? Enumerable.Empty<InventoryModel>());

            Items = new ObservableCollection<InventoryModel>(list);
        }

        public void UpdateItems(MyObjectBuilder_Inventory inventory)
        {

            if (inventory.Items.Any())
            {
                var items = inventory.Items.Select(item => new InventoryModel(item, "Item Name", "Item Description")).ToList();
                _selectedRow = items.FirstOrDefault();
                _isValid = true;
                _items = new ObservableCollection<InventoryModel>(items);
            }
        }

        private InventoryModel CreateItem(MyObjectBuilder_InventoryItem item, string contentPath)
        {
            MyPhysicalItemDefinition definition = MyDefinitionManager.Static.GetDefinition(item.PhysicalContent.TypeId, item.PhysicalContent.SubtypeName) as MyPhysicalItemDefinition;

            string name;
            string textureFile;
            double massMultiplier;
            double volumeMultiplier;
            switch (definition)
            {
                case null:
                name = item.PhysicalContent.SubtypeName + " " + item.PhysicalContent.TypeId;
                massMultiplier = 1;
                volumeMultiplier = 1;
                textureFile = null;
                break;
                case MyPhysicalItemDefinition def:
                name = def.DisplayNameText;
                massMultiplier = def.Mass;
                volumeMultiplier = def.Volume * SpaceEngineersConsts.VolumeMultiplier;
                textureFile = (def.Icons == null || definition.Icons.First() == null) ? null : SpaceEngineersCore.GetDataPathOrDefault(def.Icons.First(), Path.Combine(contentPath, def.Icons.First()));
                break;
            }
        

            InventoryModel newItem = new(item, name, item.PhysicalContent.SubtypeName)
            {
                Name = name,
                Amount = (decimal)item.Amount,
                SubtypeName = item.PhysicalContent.SubtypeName,
                TypeId = item.PhysicalContent.TypeId,
                MassMultiplier = massMultiplier,
                VolumeMultiplier = volumeMultiplier,
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

        internal void AddItem(MyObjectBuilder_InventoryItem item)
        {
            string contentPath = ToolboxUpdater.GetApplicationContentPath();
            item.ItemId = _inventory.nextItemId++;
            _inventory.Items.Add(item);
            Items.Add(CreateItem(item, contentPath));
        }

        internal void RemoveItem(int index)
        {
            var invItem = _inventory.Items[index];
            var gunObject = (MyObjectBuilder_PhysicalGunObject)invItem.PhysicalContent;
            // Remove HandWeapon if item is HandWeapon.
            if (invItem.PhysicalContent.TypeId == MOBTypeIds.PhysicalGunObject &&
                 gunObject.GunEntity?.EntityId == _character?.HandWeapon?.EntityId)
            {
                _character.HandWeapon = null;
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
