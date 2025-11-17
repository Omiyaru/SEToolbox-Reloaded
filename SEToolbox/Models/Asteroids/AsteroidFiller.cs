using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using SEToolbox.Interop.Asteroids;

namespace SEToolbox.Models.Asteroids
{

    // TODO: Rewrite how the fill interface is displayed to allow custom fill methods.
    // Otherwise, it will have to remain generic.
    // TODO: possibly allow users to create their own fill methods using new methods aand parameters

    public class AsteroidFiller : BaseModel
    {
        #region Fields

        private int _index;
        private GenerateVoxelDetailModel _voxelFile;
        private IMyVoxelFiller _fillMethod;
        private List<GenerateVoxelDetailModel> _voxelFileList;

        #endregion

        public AsteroidFiller()
        {
            FillMethod = new AsteroidByteFiller();
        }

        #region Properties

        public int Index
        {
            get => _index;

            set => SetProperty( ref _index, value, nameof(Index));
        }

        public GenerateVoxelDetailModel VoxelFile
        {
            get => _voxelFile;

            set => SetProperty( ref _voxelFile, value, nameof(VoxelFile));
        }

        public IMyVoxelFiller FillMethod
        {
            get => _fillMethod;

            set => SetProperty( ref _fillMethod, value, nameof(FillMethod));
        }

        public List<GenerateVoxelDetailModel> VoxelFileList
        {
            get => _voxelFileList;

            set => SetProperty( ref _voxelFileList, value, nameof(VoxelFileList));
        }

        //private ObservableCollection<MaterialSelectionModel> _materialsCollection;

        #endregion


        // GenerateVoxelFieldViewModel.cs has  SelectedRow and VoxelCollection, dataModel.BaseMaterial,MaterialsCollection, VoxelFileList,
        // GenerateVoxelFieldModel.cs Has BaseMaterial

        public static void CreateFillProperties(AsteroidByteFillProperties SelectedRow, ObservableCollection<AsteroidByteFillProperties> VoxelCollection)
        {
            var newFillProperties = (AsteroidByteFillProperties)SelectedRow.Clone();
            VoxelCollection.Insert(VoxelCollection.IndexOf(SelectedRow) + 1, newFillProperties);
        }

        public void RandomizeFillProperties( AsteroidByteFillProperties SelectedRow, ObservableCollection<AsteroidByteFillProperties> VoxelCollection, GenerateVoxelFieldModel dataModel, ObservableCollection<MaterialSelectionModel> MaterialsCollection, ObservableCollection<GenerateVoxelDetailModel> VoxelFileList)
        {
            var filler = new AsteroidByteFiller();
            var randomModel = filler.CreateRandom(VoxelCollection.Count + 1, dataModel.BaseMaterial, MaterialsCollection, VoxelFileList) as AsteroidByteFillProperties;
            if (SelectedRow != null)
            {
                VoxelCollection[VoxelCollection.IndexOf(SelectedRow)] = randomModel;
            }
            else
            {
                VoxelCollection.Add(randomModel);
            }
        }

        public void FillAsteroid(MyVoxelMapBase asteroid, IMyVoxelFillProperties fillProperties)
        {
            _fillMethod.FillAsteroid(asteroid, fillProperties);
        }
    }
}
