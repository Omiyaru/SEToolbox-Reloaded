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
        private int _secondPercent;
        private MaterialSelectionModel _thirdMaterial;
        private int _thirdPercent;
        private MaterialSelectionModel _fourthMaterial;
        private int _fourthPercent;
        private MaterialSelectionModel _fifthMaterial;
        private int _fifthPercent;
        private MaterialSelectionModel _sixthMaterial;
        private int _sixthPercent;
        private MaterialSelectionModel _seventhMaterial;
        private int _seventhPercent;
        public ObservableCollection<AsteroidByteFillProperties> _voxelCollection;
        //private ObservableCollection<MaterialSelectionModel> _materialsCollection;
        
        public ObservableCollection<AsteroidByteFillProperties> VoxelCollection
        {
            get => _voxelCollection;
            set => SetProperty( ref _voxelCollection, nameof(VoxelCollection));
        }

        #endregion

        #region Properties

        public AsteroidByteFillProperties SelectedRow
        {
            get => _voxelCollection[Index];
            
            set => SetProperty(_voxelCollection[Index], nameof(SelectedRow));
        }


        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, nameof(Index));
        }

        public int TotalPercent
        {
            get => _totalPercent;
            set => SetProperty(ref _totalPercent, nameof(TotalPercent));
        }

        public GenerateVoxelDetailModel VoxelFile
        {
            get => _voxelFile;
            set => SetProperty(ref _voxelFile, nameof(VoxelFile));
        }

        public MaterialSelectionModel MainMaterial
        {
            get => _mainMaterial;
            set => SetProperty(ref _mainMaterial, nameof(MainMaterial));
        }

        public MaterialSelectionModel SecondMaterial
        {
            get => _secondMaterial;
            set => SetProperty(ref _secondMaterial, nameof(SecondMaterial));
        }

        public int SecondPercent
        {
            get => _secondPercent;
            set => SetProperty(ref _secondPercent, nameof(SecondPercent), () => UpdateTotal());
        }

        public MaterialSelectionModel ThirdMaterial
        {
            get => _thirdMaterial;
            set => SetProperty(ref _thirdMaterial, nameof(ThirdMaterial));
        }

        public int ThirdPercent
        {
            get => _thirdPercent;
            set => SetProperty(ref _thirdPercent, nameof(ThirdPercent), () => UpdateTotal());
        }

        public MaterialSelectionModel FourthMaterial
        {
            get => _fourthMaterial;
            set => SetProperty(ref _fourthMaterial, nameof(FourthMaterial));
        }

        public int FourthPercent
        {
            get => _fourthPercent;
            set => SetProperty(ref _fourthPercent, nameof(FourthPercent), () => UpdateTotal());
        }

        public MaterialSelectionModel FifthMaterial
        {
            get => _fifthMaterial;
            set => SetProperty(ref _fifthMaterial, nameof(FifthMaterial));
        }

        public int FifthPercent
        {
            get => _fifthPercent;
            set => SetProperty(ref _fifthPercent, nameof(FifthPercent), () => UpdateTotal());
        }

        public MaterialSelectionModel SixthMaterial
        {
            get => _sixthMaterial;
            set => SetProperty(ref _sixthMaterial, nameof(SixthMaterial));
        }

        public int SixthPercent
        {
            get => _sixthPercent;
            set => SetProperty(ref _sixthPercent, nameof(SixthPercent), () => UpdateTotal());
        }

        public MaterialSelectionModel SeventhMaterial
        {
            get => _seventhMaterial;
            set => SetProperty(ref _seventhMaterial, nameof(SeventhMaterial));
        }

        public int SeventhPercent
        {
            get => _seventhPercent;
            set => SetProperty(ref _seventhPercent, nameof(SeventhPercent), () => UpdateTotal());
        }

        #endregion

        public IMyVoxelFillProperties Clone()
        {
            return new AsteroidByteFillProperties
            {
                Index = Index,
                TotalPercent = TotalPercent,
                VoxelFile = VoxelFile,
                MainMaterial = MainMaterial,
                SecondMaterial = SecondMaterial,
                SecondPercent = SecondPercent,
                ThirdMaterial = ThirdMaterial,
                ThirdPercent = ThirdPercent,
                FourthMaterial = FourthMaterial,
                FourthPercent = FourthPercent,
                FifthMaterial = FifthMaterial,
                FifthPercent = FifthPercent,
                SixthMaterial = SixthMaterial,
                SixthPercent = SixthPercent,
                SeventhMaterial = SeventhMaterial,
                SeventhPercent = SeventhPercent,
            };
        }

        private void UpdateTotal()
        {
            TotalPercent = SecondPercent + ThirdPercent + FourthPercent + FifthPercent + SixthPercent + SeventhPercent;
        }
    }
}
