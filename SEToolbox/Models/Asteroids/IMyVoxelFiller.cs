using SEToolbox.Interop.Asteroids;
using System.Collections.Generic;

namespace SEToolbox.Models.Asteroids
{
    public interface IMyVoxelFiller
    {
       void FillAsteroid(MyVoxelMapBase asteroid, IMyVoxelFillProperties fillProperties);

        IMyVoxelFillProperties CreateRandom(int index, MaterialSelectionModel defaultMaterial, IEnumerable<MaterialSelectionModel> materialsCollection, IEnumerable<GenerateVoxelDetailModel> voxelCollection);
    }
}
