using System;
using System.Runtime.Serialization;

using SEToolbox.Interop;
using VRage.ObjectBuilders;

namespace SEToolbox.Models
{
    [Serializable]
    public class StructureUnknownModel(MyObjectBuilder_EntityBase entityBase) : StructureBaseModel(entityBase)
    {
        #region Ctor

        #endregion

        #region Methods

        [OnSerializing]
        private void OnSerializingMethod(StreamingContext context)
        {
            SerializedEntity = SpaceEngineersApi.Serialize<MyObjectBuilder_EntityBase>(EntityBase);
        }

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            EntityBase = SpaceEngineersApi.Deserialize<MyObjectBuilder_EntityBase>(SerializedEntity);
        }

        public override void UpdateGeneralFromEntityBase()
        {
            ClassType = ClassType.Unknown;
            DisplayName = EntityBase.TypeId.ToString();
        }

        #endregion
    }
}
