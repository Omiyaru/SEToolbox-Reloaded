using VRage.Voxels;
using VRageMath;

namespace SEToolbox.Interop.Asteroids
{
    class MyVoxelTaskWorker(Vector3I baseCoords, MyStorageData voxelCache)
    {
        public Vector3I BaseCoords { get; set; } = baseCoords;
        public MyStorageData VoxelCache { get; set; } = voxelCache;
    }
}
