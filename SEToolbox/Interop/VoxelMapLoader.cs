
using SEToolbox.Support;

using System;
using System.Linq;
using System.Reflection;
using VRage.Game.ModAPI;

namespace SEToolbox.Interop
{

    public static class VoxelMapLoader
    {
        static VoxelMapLoader()
        {
            // This is a refined approach to get only the necessary types for loading the voxel map.
            Type voxelMapType = typeof(IMyVoxelMap);
            Type cubeDefinitionType = typeof(Sandbox.Definitions.MyCubeDefinition);
            Type storageType = typeof(Sandbox.Engine.Voxels.MyOctreeStorage);
            Type modApiStorageType = typeof(VRage.ModAPI.IMyStorage);

            Assembly assembly = cubeDefinitionType.Assembly;
            Type[] exportedTypes = assembly.GetExportedTypes();
            try
            {
                // Get all exported types from the assembly
                // Filter types that are assignable to IMyStorage without loading unnecessary assemblies
                Type[] assignableTypes = [..assembly.GetTypes()
                    .Where(type => voxelMapType.IsAssignableFrom(type)
                                   || cubeDefinitionType.IsAssignableFrom(type)
                                   || storageType.IsAssignableFrom(type)
                                   || modApiStorageType.IsAssignableFrom(type))];
                //Todo identify any other types as needed

                // Count of relevant types
                int count = assignableTypes.Count();
                
            }

            catch (ReflectionTypeLoadException ex)
            {
               
                foreach (Exception loaderException in ex.LoaderExceptions)
                {
                    SConsole.WriteLine(loaderException.Message);
                }
            }
            catch (Exception ex)
            {
                // The types required to load the current asteroid files are in the Sandbox.Game.dll.
                // Trying to iterate through the types in the Sandbox.Game assembly, will practically cause it to load every other assembly in the game.
                SConsole.WriteLine(ex.Message);
            }
        }

        public static void Load(string fileName) 
        {
            throw new NotImplementedException();//todo: Implement loading the voxel map from the specifiedfileName.
        }
    }
}
