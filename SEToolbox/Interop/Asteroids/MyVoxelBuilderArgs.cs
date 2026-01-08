using VRageMath;

namespace SEToolbox.Interop.Asteroids
{
    public struct MyVoxelBuilderArgs(Vector3I size,
                                    Vector3I coordinatePoint,
                                    byte materialIndex,
                                    byte volume)
    {

        /// <summary>
        /// The size of the Voxel Storage.
        /// </summary>
        public Vector3I Size { get; } = size;

        /// <summary>
        /// The currently selected Voxel Coordinate in local space.
        /// </summary>
        public Vector3I CoordinatePoint { get; } = coordinatePoint;

        /// <summary>
        /// The Material to be applied. It may already be set with the existing material.
        /// </summary>
        public byte MaterialIndex { get; set; } = materialIndex;

        /// <summary>
        /// The Volume to be applied. It may already be set with the existing Volume.
        /// </summary>
        public byte Volume { get; set; } = volume;
    }

    public delegate void VoxelBuilderAction(ref MyVoxelBuilderArgs args);
}
