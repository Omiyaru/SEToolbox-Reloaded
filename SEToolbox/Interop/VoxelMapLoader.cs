
using SEToolbox.Support;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            // Filter types that are assignable to IMyStorage without loading unnecessary assemblies
                          
                //Todo identify any other types as needed       
                List<Type> types =
                [
                    typeof(IMyVoxelMap),
                    typeof(Sandbox.Definitions.MyCubeDefinition),
                    typeof(VRage.Game.Voxels.IMyStorage), 
                    //typeof(Sandbox.Engine.Voxels.MyStorageBase),  
                    typeof(VRage.Game.Components.MyEntityStorageComponent),
                    typeof(Sandbox.Engine.Voxels.MyOctreeStorage),
                    typeof(VRage.ModAPI.IMyStorage)
                ];

            Type cubeDefinitions = typeof(Sandbox.Definitions.MyCubeDefinition);
            Assembly assembly = cubeDefinitions.Assembly;

            // Get all exported types from the assembly
            Type[] exportedTypes = assembly.GetExportedTypes();
            
            try
            {
                Type[] assignableTypes = [.. assembly.GetTypes().Where(type => types.Any(t => t.IsAssignableFrom(type)))];

                // Count of relevant types
                int count = assignableTypes.Count();

            }
            catch (Exception)
            {
                // The types required to load the current asteroid files are in the Sandbox.Game.dll.
                // Trying to iterate through the types in the Sandbox.Game assembly, will practically cause it to load every other assembly in the game.
                //Log.WriteLine(ex.Message);
            }
        }

        public static void Load(string fileName)
        {
            throw new NotImplementedException();//todo: Implement loading the voxel map from the specifiedfileName.
        }
    }
}
