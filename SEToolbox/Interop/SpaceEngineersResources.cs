using Sandbox.Definitions;
using System.Collections.Generic;
using System.Linq;
using VRage.Collections;
using VRage.Game;
using VRage.ObjectBuilders;
using static VRage.Game.MyObjectBuilder_Checkpoint;
using MOBSerializerKeen = VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen;
using SEConsts = SEToolbox.Interop.SpaceEngineersConsts;
namespace SEToolbox.Interop
{
    /// <summary>
    /// Encapsulates the game definitions, either stock or loaded for a specific save game world.
    /// </summary>
    public class SpaceEngineersResources
    {
        public Dictionary<string, byte> MaterialIndex { get; private set; }

        /// <summary>
        /// Loads Stock definitions from default path, useful for tests.
        /// </summary>
        public void LoadDefinitions()
        {
            // Call PreloadDefinitions(), to load DefinitionsToPreload.sbc file first.
            // otherwise LoadData() may throw an InvalidOperationException due to a modified collection.
            MyDefinitionManager.Static.PreloadDefinitions();
            MyDefinitionManager.Static.LoadData([]);
            MaterialIndex = [];
        }

        public void LoadDefinitionsAndMods(string userModsPath, List<ModItem> mods)
        {
            // Call PreloadDefinitions(), to load DefinitionsToPreload.sbc file first.
            // otherwise LoadData() may throw an InvalidOperationException due to a modified collection.
          	List<ModItem> userMods =[];
            userModsPath = SEConsts.BaseLocalPath.ModsPath;
            if (!string.IsNullOrEmpty(userModsPath)) 
            {
                 SpaceEngineersWorkshop.GetLocalModsBlocking(userModsPath, mods);
                foreach (var mod in userMods)
                {
                    if (!mods.Contains(mod))
                    {
                        mods.Add(mod);
                    }
                }
                
                MyDefinitionManager.Static.PreloadDefinitions();
                MyDefinitionManager.Static.LoadData(mods);
                MaterialIndex = [];
            } 
          
        }

      
        private static readonly object MatindexLock = new();

        public static byte GetMaterialIndex(string materialName)
        {
            lock (MatindexLock)
            {
                return MyDefinitionManager.Static.GetVoxelMaterialDefinition(materialName).Index;
            }
        }
        
        public static IEnumerable<MyPlanetGeneratorDefinition> GetPlanetGeneratorDefinitions
        {
            get => [.. MyDefinitionManager.Static.GetPlanetsGeneratorsDefinitions()];
        }


        public static DictionaryReader<string, MyAsteroidGeneratorDefinition> AsteroidDefinitions
        {
            get => MyDefinitionManager.Static.GetAsteroidGeneratorDefinitions();
        }

        public static IList<MyBlueprintDefinitionBase> BlueprintDefinitions
        {
            get => [.. MyDefinitionManager.Static.GetBlueprintDefinitions()];
        }

        public static IEnumerable<MyCubeBlockDefinition> CubeBlockDefinitions
        {
            get => MyDefinitionManager.Static.GetAllDefinitions().Where(e => e is MyCubeBlockDefinition).Cast<MyCubeBlockDefinition>();
        }

        public static IList<MyComponentDefinition> ComponentDefinitions
        {
            get => [.. MyDefinitionManager.Static.GetPhysicalItemDefinitions().Where(e => e is MyComponentDefinition).Cast<MyComponentDefinition>()];
        }

        public static IList<MyPhysicalItemDefinition> PhysicalItemDefinitions
        {
            get => [.. MyDefinitionManager.Static.GetPhysicalItemDefinitions().Where(predicate: e => e is not MyComponentDefinition)];
        }

        public static IList<MyAmmoMagazineDefinition> AmmoMagazineDefinitions
        {
            get => [.. MyDefinitionManager.Static.GetAllDefinitions().Where(e => e is MyAmmoMagazineDefinition).Cast<MyAmmoMagazineDefinition>()];
        }

        public static IList<MyVoxelMaterialDefinition> VoxelMaterialDefinitions
        {
            get => [.. MyDefinitionManager.Static.GetVoxelMaterialDefinitions()];
        }

        public static IList<MyVoxelMapStorageDefinition> VoxelMapStorageDefinitions
        {
            get => [.. MyDefinitionManager.Static.GetVoxelMapStorageDefinitions()];
        }

        public static IList<MyCharacterDefinition> CharacterDefinitions
        {
            get => [.. MyDefinitionManager.Static.Characters];
        }

        public static string GetMaterialName(byte materialIndex, byte defaultMaterialIndex)
        {
            if (materialIndex <= MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Count)
            {
                return MyDefinitionManager.Static.GetVoxelMaterialDefinition(materialIndex).Id.SubtypeName;
            }
            if (defaultMaterialIndex <= MyDefinitionManager.Static.GetVoxelMaterialDefinitions().Count)
            {
                return MyDefinitionManager.Static.GetVoxelMaterialDefinition(defaultMaterialIndex).Id.SubtypeName;
            }
            return null;
        }
        //return _definitions.VoxelMaterials[materialIndex].Id.SubtypeName;
        //return _definitions.VoxelMaterials[defaultMaterialIndex].Id.SubtypeName;


        public static string GetMaterialName(byte materialIndex)
        {
            return MyDefinitionManager.Static.GetVoxelMaterialDefinition(materialIndex).Id.SubtypeName;
            // return _definitions.VoxelMaterials[materialIndex].Id.SubtypeName;
        }

        public static string GetDefaultMaterialName()
        {
            return MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition().Id.SubtypeName;
        }

        public static byte GetDefaultMaterialIndex()
        {
            return MyDefinitionManager.Static.GetDefaultVoxelMaterialDefinition().Index;
        }

        public static T CreateNewObject<T>()
            where T : MyObjectBuilder_Base
        {
            return (T)MOBSerializerKeen.CreateNewObject(typeof(T));
        }

        public static T CreateNewObject<T>(MyObjectBuilderType typeId, string subtypeId)
           where T : MyObjectBuilder_Base
        {
            return (T)MOBSerializerKeen.CreateNewObject(typeId, subtypeId);
        }


        // private static readonly Dictionary<Type, MyObjectBuilderType> _types = new();


        // public static MyObjectBuilderType CreateType<T>(MyObjectBuilderType typeId, string subtypeId) where T : MyObjectBuilder_Base
        // {
        //     var type = typeof(T);
        //     if (!_types.TryGetValue(type, out var result))
        //     {
        //         _types[type] = result = (MyObjectBuilderType)(object)MOBSerializerKeen.CreateNewObject(typeId, subtypeId);
        //     }
        //     return result;

    }
}

