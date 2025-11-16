
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI.Weapons;
using VRage.Game.ObjectBuilders; 

using VRage.Game;
using VRage.Game.ModAPI;
using MOBTypes = VRage.ObjectBuilders.MyObjectBuilderType;

namespace SEToolbox.Interop
{
    /// <summary>
    /// Some hopefully generic items.
    /// </summary>
    public class SpaceEngineersTypes
    {

        public MOBTypes Types { get; private set; }

        public  static class MOBTypeIds
        {
            public static readonly MOBTypes Component = new(typeof(MyObjectBuilder_Component));
            public static readonly MOBTypes AmmoMagazine = new(typeof(MyObjectBuilder_AmmoMagazine));
            public static readonly MOBTypes PhysicalGunObject = new(typeof(MyObjectBuilder_PhysicalGunObject));
            public static readonly MOBTypes OxygenContainerObject = new(typeof(MyObjectBuilder_OxygenContainerObject));
            public static readonly MOBTypes GasContainerObject = new(typeof(MyObjectBuilder_GasContainerObject));
            public static readonly MOBTypes Ore = new(typeof(MyObjectBuilder_Ore));
            public static readonly MOBTypes Ingot = new(typeof(MyObjectBuilder_Ingot));
            public static readonly MOBTypes VoxelMaterialDefinition = new(typeof(MyObjectBuilder_VoxelMaterialDefinition));
            public static readonly MOBTypes LandingGear = new(typeof(MyObjectBuilder_LandingGear));
            public static readonly MOBTypes MedicalRoom = new(typeof(MyObjectBuilder_MedicalRoom));
            public static readonly MOBTypes Cockpit = new(typeof(MyObjectBuilder_Cockpit));
            public static readonly MOBTypes Thrust = new(typeof(MyObjectBuilder_Thrust));
            public static readonly MOBTypes CubeBlock = new(typeof(MyObjectBuilder_CubeBlock));
        }           
            /// <summary>
            /// future use??
        public static class MOBTypeWeaponIds
        {
            public static readonly MOBTypes HandheldTool = new(typeof(IMyHandheldGunObject<MyToolBase>));
            public static readonly MOBTypes HandheldGun = new(typeof(IMyHandheldGunObject<MyGunBase>));
            public static readonly MOBTypes HandheldDevice = new(typeof(IMyHandheldGunObject<MyDeviceBase>));
            public static readonly MOBTypes AutomaticRifle = new(typeof(MyObjectBuilder_AutomaticRifle));
            public static readonly MOBTypes ObjectAutomaticRifleGun = new(typeof(IMyAutomaticRifleGun));
            public static readonly MOBTypes PhysicalGunObjectBase = new(typeof(IMyGunObject<MyGunBase>));
            public static readonly MOBTypes MissileGunObject = new(typeof(IMyMissileGunObject));
            public static readonly MOBTypes PhysicalGunObject = new(typeof(MyObjectBuilder_PhysicalGunObject));

        }
        //FOLIAGE, HARVESTABLE??


    /// <summary>
        /// The base path of the save files, minus the userid.
        /// </summary>
        public static readonly UserDataPath BaseLocalPath;

        public static readonly UserDataPath BaseDedicatedServerHostPath;

        public static readonly UserDataPath BaseDedicatedServerServicePath;
      
    }
}
