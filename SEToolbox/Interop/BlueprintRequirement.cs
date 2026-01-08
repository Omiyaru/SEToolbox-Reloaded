using VRage.ObjectBuilders;

namespace SEToolbox.Interop
{
    public class BlueprintRequirement
    {
        public decimal Amount { get; set; }

        public SerializableDefinitionId Id { get; set; }

        public string SubtypeName { get; set; }

        public string TypeId { get; set; }
    }
}
