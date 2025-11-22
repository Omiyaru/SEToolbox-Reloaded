using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Sandbox.Definitions;
using SEToolbox.Interop;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.Models
{
    [Serializable]
    public class StructureCharacterModel(MyObjectBuilder_EntityBase entityBase) : StructureBaseModel(entityBase)
    {
        #region Fields

        // Fields are marked as NonSerialized, as they aren't required during the drag-drop operation.

        [NonSerialized]
        private bool _isPlayer;

        [NonSerialized]
        private bool _isPilot;

        [NonSerialized]
        private InventoryEditorModel _inventory;

        #endregion
        #region Ctor

        #endregion

        #region Properties

        [XmlIgnore]
        public MyObjectBuilder_Character Character
        {
            get
            {
                return EntityBase as MyObjectBuilder_Character;
            }
        }

        [XmlIgnore]
        public SerializableVector3 Color
        {
            get => Character.ColorMaskHSV;
            set => SetProperty(Character?.ColorMaskHSV, value, nameof(Color), () => UpdateGeneralFromEntityBase());

        }

        [XmlIgnore]
        public bool Light
        {
            get => Character.LightEnabled;
            set => SetProperty(Character.LightEnabled, value, nameof(Light));
        }

        [XmlIgnore]
        public bool JetPack
        {
            get => Character.JetpackEnabled;
            set => SetProperty(Character.JetpackEnabled, value, nameof(JetPack));
        }

        [XmlIgnore]
        public bool Dampeners
        {
            get => Character.DampenersEnabled;
            set => SetProperty(Character.DampenersEnabled, value, nameof(Dampeners));
        }

        [XmlIgnore]
        public float BatteryCapacity // Character.Battery.CurrentCapacity ?? 0;
        {
            get => Character.Battery.CurrentCapacity;
            set => SetProperty(Character.Battery.CurrentCapacity, value, nameof(BatteryCapacity));
        }

        [XmlIgnore]
        public float? Health
        {
            get => Character.Health;
            set => SetProperty(Character.Health, value, nameof(Health));
        }

        //[XmlIgnore]


        //public bool IsDead
        //{
        //    get =>  Character.IsDead;
        //}


        [XmlIgnore]
        public float OxygenLevel
        {
            get
            {
                if (Character?.StoredGases == null)
                    return 0;
                // doesn't matter if Oxygen is not there, as it will still be 0.
                MyObjectBuilder_Character.StoredGas gas = Character.StoredGases.FirstOrDefault(e => e.Id.SubtypeName == "Oxygen");
                return gas.FillLevel;
            }

            set
            {
                if (Character != null && ReplaceGasValue("Oxygen", value))
                    OnPropertyChanged(nameof(OxygenLevel));
            }
        }

        [XmlIgnore]
        public float HydrogenLevel
        {
            get
            {

                if (Character?.StoredGases == null)
                    return 0;
                // doesn't matter if Hydrogen is not there, as it will still be 0.
                MyObjectBuilder_Character.StoredGas gas = Character.StoredGases.FirstOrDefault(e => e.Id.SubtypeName == "Hydrogen");
                return gas.FillLevel;
            }

            set
            {
                if (Character != null && ReplaceGasValue("Hydrogen", value))
                    OnPropertyChanged(nameof(HydrogenLevel));
            }
        }

        [XmlIgnore]
        public bool IsPlayer
        {
            get => _isPlayer;
            set => SetProperty(ref _isPlayer, value, nameof(IsPlayer));
        }

        [XmlIgnore]
        public override double LinearVelocity
        {
            get => Character.LinearVelocity.ToVector3().LinearVector();
        }


        [XmlIgnore]
        public bool IsPilot
        {
            get => _isPilot;
            set => SetProperty(ref _isPilot, value, nameof(IsPilot));
           
        }

        [XmlIgnore]
        public InventoryEditorModel Inventory
        {
            get => _inventory;
            set => SetProperty(ref _inventory, value, nameof(Inventory));
        }

        #endregion

        #region Methods

        [OnSerializing]
        private void OnSerializingMethod(StreamingContext context)
        {
            SerializedEntity = SpaceEngineersApi.Serialize<MyObjectBuilder_Character>(Character);
        }

        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            EntityBase = SpaceEngineersApi.Deserialize<MyObjectBuilder_Character>(SerializedEntity);
        }

        public override void UpdateGeneralFromEntityBase()
        {
            ClassType = ClassType.Character;
            string dead = Character.MovementState == MyCharacterMovementEnum.Died ? $" | {Res.ClsCharacterDead}" : "";

            if (string.IsNullOrEmpty(Character.DisplayName))
            {
                Description = Res.ClsCharacterNPC;
                DisplayName = (Character.CharacterModel ?? "Unknown Model") + dead;
                Mass = SpaceEngineersConsts.PlayerMass; // no idea what an npc body weighs.
            }
            else
            {
                Description = Res.ClsCharacterPlayer;
                DisplayName = Character.DisplayName + dead;
                Mass = SpaceEngineersConsts.PlayerMass;
            }

            if (Inventory == null)
            {
                System.Collections.ObjectModel.ObservableCollection<InventoryEditorModel> inventories = Character.ComponentContainer.GetInventory();
                if (inventories?.Count > 0)
                {
                    Inventory = inventories[0];
                    Mass += Inventory.TotalMass;
                }
                else
                {
                    Inventory = null;
                }
            }
        }

        public void ResetVelocity()
        {
            Character.LinearVelocity = new VRageMath.Vector3(0, 0, 0);
            OnPropertyChanged(nameof(LinearVelocity));
        }

        public void ReverseVelocity()
        {
            Character.LinearVelocity = new VRageMath.Vector3(Character.LinearVelocity.X * -1, 
                                                             Character.LinearVelocity.Y * -1,
                                                             Character.LinearVelocity.Z * -1);
            OnPropertyChanged(nameof(LinearVelocity));
        }

        private bool ReplaceGasValue(string gasName, float value)
        {
            if (Character.StoredGases == null)
                Character.StoredGases = [];

            // Find the existing gas value.
            for (int i = 0; i < Character.StoredGases.Count; i++)
            {
                MyObjectBuilder_Character.StoredGas gas = Character.StoredGases[i];
                if (gas.Id.SubtypeName == gasName)
                {
                    if (value != gas.FillLevel)
                    {
                        gas.FillLevel = value;
                        Character.StoredGases[i] = gas;
                        return true;
                    }
                }
            }

            // If it doesn't exist for old save games, add it in.
            MyObjectBuilder_Character.StoredGas newGas = new()
            {
                // This could cause an exception if the gas names are ever changed, even in casing.
                Id = MyDefinitionManager.Static.GetGasDefinitions().FirstOrDefault(e => e.Id.SubtypeName == gasName).Id,
                FillLevel = value
            };
            Character.StoredGases.Add(newGas);
            return true;
        }
        #endregion
    }
}
