using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Models;
using SEToolbox.Models.Asteroids;
using SEToolbox.Services;
using SEToolbox.Support;
using VRageMath;
using IDType = VRage.MyEntityIdentifier.ID_OBJECT_TYPE;
using VRage.ObjectBuilders;
using VRage;
using VRage.Game;

namespace SEToolbox.ViewModels
{
    public class GenerateVoxelFieldViewModel : BaseViewModel
    {
        #region Fields

        private readonly GenerateVoxelFieldModel _dataModel;
        private AsteroidByteFillProperties _selectedRow;

        private bool? _closeResult;
        private bool _isBusy;

        #endregion

        #region Constructors

        public GenerateVoxelFieldViewModel(BaseViewModel parentViewModel, GenerateVoxelFieldModel dataModel)
            : base(parentViewModel)
        {
            _dataModel = dataModel;
            // Will bubble property change events from the Model to the ViewModel.
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Command Properties

        public ICommand ClearRowsCommand
        {
           get => new DelegateCommand(ClearRowsExecuted, ClearRowsCanExecute); 
        }

        public ICommand AddRandomRowCommand
        {
           get => new DelegateCommand(AddRandomRowExecuted, AddRandomRowCanExecute); 
        }

        public ICommand AddRowCommand
        {
           get => new DelegateCommand(AddRowExecuted, AddRowCanExecute); 
        }

        public ICommand DeleteRowCommand
        {
           get => new DelegateCommand(DeleteRowExecuted, DeleteRowCanExecute); 
        }

        public ICommand CreateCommand
        {
           get => new DelegateCommand(CreateExecuted, CreateCanExecute); 
        }

        public ICommand CancelCommand
        {
           get => new DelegateCommand(CancelExecuted, CancelCanExecute);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the DialogResult of the View.  If True or False is passed, this initiates the Close().
        /// </summary>
        public bool? CloseResult
        {
            get => _closeResult;
            set => SetProperty(ref _closeResult, value, nameof(CloseResult));
        }

        public AsteroidByteFillProperties SelectedRow
        {
            get => _selectedRow;
            set => SetProperty( ref _selectedRow, value, nameof(SelectedRow));
         
        }

        public ObservableCollection<AsteroidByteFillProperties> VoxelCollection
        {
        get => _dataModel.VoxelCollection;
        set => _dataModel.VoxelCollection = value;
        }

        public int MinimumRange
        {
        get => _dataModel.MinimumRange;
        set => _dataModel.MinimumRange = value;
        }

        public int MaximumRange
        {
        get => _dataModel.MaximumRange;
        set => _dataModel.MaximumRange = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;

            set
            {
                    SetProperty(ref _isBusy, value, nameof(IsBusy));
                    if (_isBusy)
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }
                }
            }
        

        public ObservableCollection<GenerateVoxelDetailModel> VoxelFileList
        {
           get => _dataModel.VoxelFileList; 
        }

        public ObservableCollection<MaterialSelectionModel> MaterialsCollection
        {
           get => _dataModel.MaterialsCollection; 
        }

        public List<int> PercentList
        {
            get => _dataModel.PercentList;
        }

        public double CenterPositionX
        {
        get => _dataModel.CenterPositionX;
        set => _dataModel.CenterPositionX = value;
        }

        public double CenterPositionY
        {
        get => _dataModel.CenterPositionY;
        set => _dataModel.CenterPositionY = value;
        }

        public double CenterPositionZ
        {
        get => _dataModel.CenterPositionZ;
        set => _dataModel.CenterPositionZ = value;
        }

        public AsteroidFillType.AsteroidFills AsteroidFillType
        {
        get => _dataModel.AsteroidFillType;
        set => _dataModel.AsteroidFillType = value;
        }

        #endregion

        #region Command Methods

        public bool ClearRowsCanExecute()
        {
            return VoxelCollection.Count > 0;
        }

        public void ClearRowsExecuted()
        {
            VoxelCollection.Clear();
            MinimumRange = 400;
            MaximumRange = 800;
        }

        public bool AddRandomRowCanExecute()
        {
            return true;
        }

        public void AddRandomRowExecuted()
        {
            AsteroidByteFiller filler = new();
            AsteroidByteFillProperties randomModel = (AsteroidByteFillProperties)filler.CreateRandom(VoxelCollection.Count + 1, _dataModel.BaseMaterial, MaterialsCollection, VoxelFileList);

            if (SelectedRow != null)
            {
                VoxelCollection.Insert(VoxelCollection.IndexOf(SelectedRow) + 1, randomModel);
            }
            else
            {
                VoxelCollection.Add(randomModel);
            }

            _dataModel.RenumberCollection();
        }

        public bool AddRowCanExecute()
        {
            return true;
        }

        public void AddRowExecuted()
        {
            if (SelectedRow != null)
            {
                VoxelCollection.Insert(VoxelCollection.IndexOf(SelectedRow) + 1, (AsteroidByteFillProperties)SelectedRow.Clone());
            }
            else
            {
                VoxelCollection.Add(_dataModel.NewDefaultVoxel(VoxelCollection.Count + 1));
            }

            _dataModel.RenumberCollection();
        }

        public bool DeleteRowCanExecute()
        {
            return SelectedRow != null;
        }

        public void DeleteRowExecuted()
        {
            int index = VoxelCollection.IndexOf(SelectedRow);
            VoxelCollection.Remove(SelectedRow);
            _dataModel.RenumberCollection();

            while (index >= VoxelCollection.Count)
            {
                index--;
            }
            if (index >= 0)
            {
                SelectedRow = VoxelCollection[index];
            }
        }

        public bool CreateCanExecute()
        {
            bool valid = VoxelCollection.Count > 0;
            return VoxelCollection.Aggregate(valid, (current, t) => current);
        }

        public void CreateExecuted()
        {
            CloseResult = true;
        }

        public bool CancelCanExecute()
        {
            return true;
        }

        public void CancelExecuted()
        {
            CloseResult = false;
        }

        #endregion

        #region Methods

        public void BuildEntities(out string[] sourceVoxelFiles, out MyObjectBuilder_EntityBase[] sourceEntities)
        {
            List<MyObjectBuilder_EntityBase> entities = [];
            List<string> sourceFiles = [];

            MainViewModel.ResetProgress(0, VoxelCollection.Count);

            foreach (AsteroidByteFillProperties voxelDesign in VoxelCollection)
            {
                MainViewModel.Progress++;

                if (string.IsNullOrEmpty(voxelDesign.VoxelFile.SourceFileName) || ! MyVoxelMapBase.IsVoxelMapFile(voxelDesign.VoxelFile.SourceFileName))
                    continue;

                using  MyVoxelMapBase asteroid = new();
                string tempSourceFileName = null;
                AsteroidFillType asteroidFillType = new( 1,string.Empty);
                    int id = asteroidFillType.Id;

                        switch (asteroidFillType.Id)
                {
                    case 0: //AsteroidFillType.None:
                        asteroid.Load(voxelDesign.VoxelFile.SourceFileName);
                        tempSourceFileName = voxelDesign.VoxelFile.SourceFileName;
                        break;

                    case 1: // AsteroidFillType.ByteFiller
                        asteroid.Load(voxelDesign.VoxelFile.SourceFileName);
                        AsteroidByteFiller filler = new();
                        filler.FillAsteroid(asteroid, voxelDesign);
                        tempSourceFileName = TempFileUtil.NewFileName( MyVoxelMapBase.FileExtension.V2);
                        asteroid.Save(tempSourceFileName);
                        break;
                    case 2:  //AsteroidFillType.Custom
                        asteroid.Load(voxelDesign.VoxelFile.SourceFileName);
                        tempSourceFileName = voxelDesign.VoxelFile.SourceFileName;

                        break;

                    default:
                        throw new InvalidOperationException("Unsupported AsteroidFillType.");
                }



                // automatically number all files, and check for duplicate fileNames.
                string fileName = MainViewModel.CreateUniqueVoxelStorageName(voxelDesign.VoxelFile.Name +  MyVoxelMapBase.FileExtension.V2, [.. entities]);

                double radius = RandomUtil.GetDouble(MinimumRange, MaximumRange);
                double longitude = RandomUtil.GetDouble(0, 2 * Math.PI);
                double latitude = RandomUtil.GetDouble(-Math.PI / 2, (Math.PI / 2) + double.Epsilon);

                // Test data. Place asteroids items into a circle.
                //radius = 500;
                //longitude = Math.PI * 2 * ((double)voxelDesign.Index / VoxelCollection.Count);
                //latitude = 0;

                double x = radius * Math.Cos(latitude) * Math.Cos(longitude);
                double z = radius * Math.Cos(latitude) * Math.Sin(longitude);
                double y = radius * Math.Sin(latitude);

                Vector3D center = new(CenterPositionX, CenterPositionY, CenterPositionZ);
                Vector3D position = center + new Vector3D(x, y, z) - asteroid.ContentCenter;

                MyObjectBuilder_VoxelMap entity = new(position, fileName)
                {
                    EntityId = SpaceEngineersApi.GenerateEntityId(IDType.ASTEROID),
                    PersistentFlags = MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                    StorageName = Path.GetFileNameWithoutExtension(fileName),
                    PositionAndOrientation = new MyPositionAndOrientation
                    {
                        Position = position,
                        Forward = Vector3.Forward, // Asteroids currently don't have any orientation.
                        Up = Vector3.Up
                    }
                };

                entities.Add(entity);
                sourceFiles.Add(tempSourceFileName);
            }

            sourceVoxelFiles = [.. sourceFiles];
            sourceEntities = [.. entities];
        }

        internal AsteroidByteFillProperties Clone()
        {
            return (AsteroidByteFillProperties)SelectedRow.Clone();
        }

        #endregion
    }
}
