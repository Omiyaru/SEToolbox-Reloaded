using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Sandbox.Definitions;
using SEToolbox.Interop;
using VRage.Game;
using VRage.ObjectBuilders;
using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.Models
{
    [Serializable]
    public class StructureFloatingObjectModel(MyObjectBuilder_EntityBase entityBase) : StructureBaseModel(entityBase)
    {
        #region Fields

        // Fields are marked as NonSerialized, as they aren't required during the drag-drop operation.

        [NonSerialized]
        private double? _volume;

        [NonSerialized]
        private decimal? _units;

        #endregion
        #region Ctor

        #endregion

        #region Properties

        [XmlIgnore]
        public MyObjectBuilder_FloatingObject FloatingObject
        {
            get
            {
                return EntityBase as MyObjectBuilder_FloatingObject;
            }
        }

        [XmlIgnore]
        public MyObjectBuilder_InventoryItem Item
        {
            get => FloatingObject.Item;
            set => SetProperty(FloatingObject.Item, nameof(Item));
        }

        [XmlIgnore]
        public double? Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, nameof(Volume));
        }

        [XmlIgnore]
        public decimal? Units
        {
            get => _units;
            set => SetProperty(ref _units, nameof(Units));
        }

        #endregion

        #region Methods

        [OnSerializing]
        private void OnSerializingMethod(StreamingContext context)
        {
            SerializedEntity = SpaceEngineersApi.Serialize<MyObjectBuilder_FloatingObject>(FloatingObject);
        }

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            EntityBase = SpaceEngineersApi.Deserialize<MyObjectBuilder_FloatingObject>(SerializedEntity);
        }

        public override void UpdateGeneralFromEntityBase()
        {
            ClassType = ClassType.FloatingObject;

            MyPhysicalItemDefinition cd = (MyPhysicalItemDefinition)MyDefinitionManager.Static.GetDefinition(FloatingObject.Item.PhysicalContent.TypeId, FloatingObject.Item.PhysicalContent.SubtypeName);
            string friendlyName = cd != null ? SpaceEngineersApi.GetResourceName(cd.DisplayNameText) : FloatingObject.Item.PhysicalContent.SubtypeName;

            if (FloatingObject.Item.PhysicalContent is MyObjectBuilder_Ore)
            {
                DisplayName = friendlyName;
                Units = (decimal)FloatingObject.Item.Amount;
                Volume = cd == null ? 0 : cd.Volume * SpaceEngineersConsts.VolumeMultiplyer * (double)FloatingObject.Item.Amount;
                Mass = cd == null ? 0 : cd.Mass * (double)FloatingObject.Item.Amount;
                Description = string.Format($"{Mass:#,##0.00} {Res.GlobalSIMassKilogram}");
            }
            else if (FloatingObject.Item.PhysicalContent is MyObjectBuilder_Ingot)
            {
                DisplayName = friendlyName;
                Units = (decimal)FloatingObject.Item.Amount;
                Volume = cd == null ? 0 : cd.Volume * SpaceEngineersConsts.VolumeMultiplyer * (double)FloatingObject.Item.Amount;
                Mass = cd == null ? 0 : cd.Mass * (double)FloatingObject.Item.Amount;
                Description = string.Format($"{Mass:#,##0.00} {Res.GlobalSIMassKilogram}");
            }
            else
            {
                DisplayName = friendlyName;
                Description = string.Format($"x {FloatingObject.Item.Amount}");
                Units = (decimal)FloatingObject.Item.Amount;
                Volume = cd == null ? 0 : cd.Volume * SpaceEngineersConsts.VolumeMultiplyer * (double)FloatingObject.Item.Amount;
                Mass = cd == null ? 0 : cd.Mass * (double)FloatingObject.Item.Amount;
            }
        }

        #endregion
    }
}
