using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SEToolbox.Models.Asteroids
{
    public class AsteroidByteFillProperties : BaseModel, IMyVoxelFillProperties
    {
        #region Fields

        private int _index;
        private int _totalPercent;
        private GenerateVoxelDetailModel _voxelFile;
        private MaterialSelectionModel _mainMaterial;
        private MaterialSelectionModel _secondMaterial;
        private MaterialSelectionModel _thirdMaterial;
        private MaterialSelectionModel _fourthMaterial;
        private MaterialSelectionModel _fifthMaterial;
        private MaterialSelectionModel _sixthMaterial;
        private MaterialSelectionModel _seventhMaterial;
        
        public ObservableCollection<AsteroidByteFillProperties> _voxelCollection;
        private ObservableCollection<MaterialSelectionModel> _materialsCollection;
        private int[] _percentages = new int[6];
        public ObservableCollection<AsteroidByteFillProperties> VoxelCollection
        {
            get => _voxelCollection;
            set => SetProperty(ref _voxelCollection, value, nameof(VoxelCollection));
        }

        #endregion

        #region Properties

        public AsteroidByteFillProperties SelectedRow
        {
            get => _voxelCollection[Index];
            set => SetProperty(_voxelCollection[Index], value, nameof(SelectedRow));
        }

        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value, nameof(Index));
        }

        public int TotalPercent
        {
            get => _totalPercent;
            set => SetProperty(ref _totalPercent, value, nameof(TotalPercent));
        }

        public GenerateVoxelDetailModel VoxelFile
        {
            get => _voxelFile;
            set => SetProperty(ref _voxelFile, value, nameof(VoxelFile));
        }

        public ObservableCollection<MaterialSelectionModel> MaterialsCollection
        {
            get => _materialsCollection;
            set => SetProperty(ref _materialsCollection, value, nameof(MaterialsCollection));
        }
     
        public MaterialSelectionModel MainMaterial
        {
            get => _mainMaterial;
            set => SetProperty(ref _mainMaterial, value, nameof(MainMaterial));
        }

        public MaterialSelectionModel SecondMaterial
        {
            get => _secondMaterial;
            set => SetProperty(ref _secondMaterial, value, nameof(SecondMaterial));
        }

        public int SecondPercent
        {
            get => _percentages[1];
            set => SetProperty(ref _percentages[1], value, nameof(SecondPercent), () => 
                   UpdateTotal());
        }

        public MaterialSelectionModel ThirdMaterial
        {
            get => _thirdMaterial;
            set => SetProperty(ref _thirdMaterial, value, nameof(ThirdMaterial));
        }

        public int ThirdPercent
        {
            get => _percentages[2];
            set => SetProperty(ref _percentages[2], value, nameof(ThirdPercent), () =>
                   UpdateTotal());
        }

        public MaterialSelectionModel FourthMaterial
        {
            get => _fourthMaterial;
            set => SetProperty(ref _fourthMaterial, value, nameof(FourthMaterial));
        }

        public int FourthPercent
        {
            get => _percentages[3];
            set => SetProperty(ref _percentages[3], value, nameof(FourthPercent), () => 
                   UpdateTotal());
        }

        public MaterialSelectionModel FifthMaterial
        {
            get => _fifthMaterial;
            set => SetProperty(ref _fifthMaterial, value, nameof(FifthMaterial));
        }

        public int FifthPercent
        {
            get => _percentages[4];
            set => SetProperty(ref _percentages[4], value, nameof(FifthPercent), () => 
                   UpdateTotal());
        }

        public MaterialSelectionModel SixthMaterial
        {
            get => _sixthMaterial;
            set => SetProperty(ref _sixthMaterial, value, nameof(SixthMaterial));
        }

        public int SixthPercent
        {
            get => _percentages[5];
            set => SetProperty(ref _percentages[5], value, nameof(SixthPercent), () =>
                   UpdateTotal());
        }

        public MaterialSelectionModel SeventhMaterial
        {
            get => _seventhMaterial;
            set => SetProperty(ref _seventhMaterial, value, nameof(SeventhMaterial));
        }

        public int SeventhPercent
        {
            get => _percentages[6];
            set => SetProperty(ref _percentages[6], value, nameof(SeventhPercent), () => 
                   UpdateTotal());
        }

        #endregion

        public IMyVoxelFillProperties Clone()
        {
            AsteroidByteFillProperties clone = (AsteroidByteFillProperties)MemberwiseClone();
            clone.Index = Index;
            clone.TotalPercent = TotalPercent;
            clone.VoxelFile = VoxelFile.Clone();
            clone.MainMaterial = MainMaterial.Clone();
            clone.SecondMaterial = SecondMaterial.Clone();
            clone.SecondPercent = SecondPercent;
            clone.ThirdMaterial = ThirdMaterial.Clone();
            clone.ThirdPercent = ThirdPercent;
            clone.FourthMaterial = FourthMaterial.Clone();
            clone.FourthPercent = FourthPercent;
            clone.FifthMaterial = FifthMaterial.Clone();
            clone.FifthPercent = FifthPercent;
            clone.SixthMaterial = SixthMaterial.Clone();
            clone.SixthPercent = SixthPercent;
            clone.SeventhMaterial = SeventhMaterial.Clone();
            clone.SeventhPercent = SeventhPercent;
            return clone;
        }

        private void UpdateTotal()
        {
            TotalPercent = SecondPercent + ThirdPercent + FourthPercent + FifthPercent + SixthPercent + SeventhPercent;
        }
    }
}
