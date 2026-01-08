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
            set => SetProperty(FloatingObject.Item, value, nameof(Item));
        }

        [XmlIgnore]
        public double? Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value, nameof(Volume));
        }

        [XmlIgnore]
        public decimal? Units
        {
            get => _units;
            set => SetProperty(ref _units, value, nameof(Units));
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

            MyPhysicalItemDefinition pd = (MyPhysicalItemDefinition)MyDefinitionManager.Static.GetDefinition(FloatingObject.Item.PhysicalContent.TypeId, FloatingObject.Item.PhysicalContent.SubtypeName);
            string friendlyName = pd != null ? SpaceEngineersApi.GetResourceName(pd.DisplayNameText) : FloatingObject.Item.PhysicalContent.SubtypeName;
            string desc = string.Empty ?? null;
           if (FloatingObject.Item.PhysicalContent is MyObjectBuilder_Ore || FloatingObject.Item.PhysicalContent is MyObjectBuilder_Ingot)
            {
                desc = desc != null ? $"{Mass:#,##0.00} {Res.GlobalSIMassKilogram}" : null;
            }
                DisplayName = friendlyName;
                Description = string.Format($"x {FloatingObject.Item.Amount}");
                Units = (decimal)FloatingObject.Item.Amount;
                Volume = pd == null ? 0 : pd.Volume * SpaceEngineersConsts.VolumeMultiplier * (double)FloatingObject.Item.Amount;
                Mass = pd == null ? 0 : pd.Mass * (double)FloatingObject.Item.Amount;
                Description = desc;
            }
        }

        #endregion
    }

