using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Models;
using SEToolbox.Models.Asteroids;
using SEToolbox.Services;
using SEToolbox.Support;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using IDType = VRage.MyEntityIdentifier.ID_OBJECT_TYPE;

namespace SEToolbox.ViewModels
{
    public class StructureVoxelViewModel : StructureBaseViewModel<StructureVoxelModel>
    {
        #region Ctor

        public StructureVoxelViewModel(BaseViewModel parentViewModel, StructureVoxelModel dataModel)
            : base(parentViewModel, dataModel)
        {
            DataModel.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                // Will bubble property change events from the Model to the ViewModel.
                OnPropertyChanged(e.PropertyName);
            };
        }

        #endregion

        #region Command Properties

        public ICommand CopyDetailCommand
        {
            get => new DelegateCommand(CopyDetailExecuted, CopyDetailCanExecute);
        }

        public ICommand ReseedCommand
        {
            get => new DelegateCommand(ReseedExecuted, ReseedCanExecute);
        }

        public ICommand ReplaceSurfaceCommand
        {
            get => new DelegateCommand<string>(ReplaceSurfaceExecuted, ReplaceSurfaceCanExecute);
        }

        public ICommand ReplaceAllCommand
        {
            get => new DelegateCommand<string>(ReplaceAllExecuted, ReplaceAllCanExecute);
        }

        public ICommand ReplaceSelectedMenuCommand
        {
            get => new DelegateCommand<string>(new Func<string, bool>(ReplaceSelectedMenuCanExecute));
        }

        public ICommand ReplaceSelectedCommand
        {
            get => new DelegateCommand<string>(ReplaceSelectedExecuted, ReplaceSelectedCanExecute);
        }

        public ICommand SliceQuarterCommand
        {
            get => new DelegateCommand(SliceQuarterExecuted, SliceQuarterCanExecute);
        }

        public ICommand SliceHalfCommand
        {
            get => new DelegateCommand(SliceHalfExecuted, SliceHalfCanExecute);
        }

        public ICommand ExtractStationIntersectLooseCommand
        {
            get => new DelegateCommand(ExtractStationIntersectLooseExecuted, ExtractStationIntersectLooseCanExecute);
        }

        public ICommand ExtractStationIntersectTightCommand
        {
            get => new DelegateCommand(ExtractStationIntersectTightExecuted, ExtractStationIntersectTightCanExecute);
        }

        public ICommand RotateAsteroidYawPositiveCommand
        {
            get => new DelegateCommand(RotateAsteroidYawPositiveExecuted, RotateAsteroidYawPositiveCanExecute);
        }

        public ICommand RotateAsteroidYawNegativeCommand
        {
            get => new DelegateCommand(RotateAsteroidYawNegativeExecuted, RotateAsteroidYawNegativeCanExecute);
        }

        public ICommand RotateAsteroidPitchPositiveCommand
        {
            get => new DelegateCommand(RotateAsteroidPitchPositiveExecuted, RotateAsteroidPitchPositiveCanExecute);
        }

        public ICommand RotateAsteroidPitchNegativeCommand
        {
            get => new DelegateCommand(RotateAsteroidPitchNegativeExecuted, RotateAsteroidPitchNegativeCanExecute);
        }

        public ICommand RotateAsteroidRollPositiveCommand
        {
            get => new DelegateCommand(RotateAsteroidRollPositiveExecuted, RotateAsteroidRollPositiveCanExecute);
        }

        public ICommand RotateAsteroidRollNegativeCommand
        {
            get => new DelegateCommand(RotateAsteroidRollNegativeExecuted, RotateAsteroidRollNegativeCanExecute);
        }

        #endregion

        #region Properties

        protected new StructureVoxelModel DataModel
        {
            get => base.DataModel as StructureVoxelModel;
        }

        public string Name
        {
            get => DataModel.Name;
            set => DataModel.Name = value;
        }

        public BindableSize3DIModel Size
        {
            get => new(DataModel.Size);
            set => DataModel.Size = value.ToVector3I();
        }

        public BindableSize3DIModel ContentSize
        {
            get => new(DataModel.ContentSize);
        }

        public BindableVector3DModel Center
        {
            get => new(DataModel.Center);
            set => DataModel.Center = value.ToVector3D();
        }

        public long VoxCells
        {
            get => DataModel.VoxCells; 
            set => DataModel.VoxCells = value;
        }

        public double Volume
        {
            get => DataModel.Volume;
        }

        public List<VoxelMaterialAssetModel> MaterialAssets
        {
            get => DataModel.MaterialAssets;
            set => DataModel.MaterialAssets = value;
        }

        public VoxelMaterialAssetModel SelectedMaterialAsset
        {
            get => DataModel.SelectedMaterialAsset;
            set => DataModel.SelectedMaterialAsset = value;
        }

        public List<VoxelMaterialAssetModel> GameMaterialList
        {
            get => DataModel.GameMaterialList; 
            set => DataModel.GameMaterialList = value;
        }

        public List<VoxelMaterialAssetModel> EditMaterialList
        {
            get => DataModel.EditMaterialList;
            set => DataModel.EditMaterialList = value;
        }

        #endregion

        #region Methods

        public bool CopyDetailCanExecute()
        {
            return true;
        }

        public void CopyDetailExecuted()
        {
            StringBuilder ore = new();

            if (MaterialAssets != null)
            {
                foreach (VoxelMaterialAssetModel mat in MaterialAssets)
                {
                    ore.AppendFormat($"{mat.MaterialName}\t{mat.Volume:#,##0.00} m³\t{mat.Percent:P2}\r\n");
                }
            }

            string detail = string.Format(Properties.Resources.CtlVoxelDetail,
                Name,
                Size.Width, Size.Height, Size.Depth,
                ContentSize.Width, ContentSize.Height, ContentSize.Depth,
                Center.X, Center.Y, Center.Z,
                Volume,
                VoxCells,
                PlayerDistance,
                PositionAndOrientation.Value.Position.X, PositionAndOrientation.Value.Position.Y, PositionAndOrientation.Value.Position.Z,
                ore);

            try
            {
                Clipboard.Clear();
                Clipboard.SetText(detail);
            }
            catch
            {
                // Ignore exception which may be generated by a Remote desktop session where Clipboard access has not been granted.
            }
        }

        public bool ReseedCanExecute()
        {
            return DataModel.IsValid;
        }

        public void ReseedExecuted()
        {
            MainViewModel.IsBusy = true;

            string sourceFile = DataModel.SourceVoxelFilePath ?? DataModel.VoxelFilePath;

            MyVoxelMapBase asteroid = new();
            asteroid.Load(sourceFile);
            _ = asteroid.VoxCells;

            var materials = AsteroidSeedFillProperties.MaterialsData.Select(x => x.Value.Material);
            var material = materials.Where(m => m.MaterialIndex == 0).FirstOrDefault();
             var index = AsteroidSeedFillProperties.MaterialsData.First(x => x.Value.Material == material).Key;
            var rare = materials.Where(m => m.IsRare).ToList();
            var superRare = rare.Where(m => m.MinedRatio < 2).ToList();
            var nonRare = materials.Except(rare).ToList();
            var c = AsteroidSeedFillProperties.MaterialsData[index];
            var random = new Random();
            foreach (var m in nonRare)
            {
                
                int idx = random.Next(nonRare.Count) ;
                var newMaterial = nonRare[idx];
               AsteroidSeedFillProperties. SetMaterial(index, newMaterial, material.Radius, material.Veins);
            }

            foreach (var m in rare)
            {
                material = m;
                int idx = random.Next(rare.Count);
                var newMaterial = rare[idx];
                AsteroidSeedFillProperties.SetMaterial(index, newMaterial, material.Radius, material.Veins);
            }

            foreach (var m in superRare)
            {
                int idx = random.Next(superRare.Count);
                var newMaterial = superRare[idx];
                AsteroidSeedFillProperties.SetMaterial(index, newMaterial, material.Radius, material.Veins);
            };

            string tempFileName = TempFileUtil.NewFileName(MyVoxelMapBase.FileExtension.V2);
            asteroid.Save(tempFileName);

            DataModel.UpdateNewSource(asteroid, tempFileName);

            MainViewModel.IsModified = true;
            MainViewModel.IsBusy = false;

            DataModel.MaterialAssets = null;
            DataModel.InitializeAsync();
        }

        public bool ReplaceSurfaceCanExecute(string materialName)
        {
            return DataModel.IsValid;
        }

        public void ReplaceSurfaceExecuted(string materialName)
        {
            MainViewModel.IsBusy = true;

            string sourceFile = DataModel.SourceVoxelFilePath ?? DataModel.VoxelFilePath;

            MyVoxelMapBase asteroid = new();
            asteroid.Load(sourceFile);
            asteroid.ForceShellMaterial(materialName, 2);

            string tempFileName = TempFileUtil.NewFileName(MyVoxelMapBase.FileExtension.V2);
            asteroid.Save(tempFileName);

            DataModel.UpdateNewSource(asteroid, tempFileName);

            MainViewModel.IsModified = true;
            MainViewModel.IsBusy = false;

            DataModel.MaterialAssets = null;
            DataModel.InitializeAsync();
        }

        public bool ReplaceAllCanExecute(string materialName)
        {
            return DataModel.IsValid;
        }

        public void ReplaceAllExecuted(string materialName)
        {
            MainViewModel.IsBusy = true;

            string sourceFile = DataModel.SourceVoxelFilePath ?? DataModel.VoxelFilePath;

            MyVoxelMapBase asteroid = new();
            asteroid.Load(sourceFile);
            asteroid.ForceBaseMaterial(materialName, materialName);

            string tempFileName = TempFileUtil.NewFileName(MyVoxelMapBase.FileExtension.V2);
            asteroid.Save(tempFileName);

            DataModel.UpdateNewSource(asteroid, tempFileName);

            MainViewModel.IsModified = true;
            MainViewModel.IsBusy = false;

            DataModel.MaterialAssets = null;
            DataModel.InitializeAsync();
        }

        public bool ReplaceSelectedMenuCanExecute(string materialName)
        {
            return DataModel is { IsValid: true };
        }

        public bool ReplaceSelectedCanExecute(string materialName)
        {
            return DataModel is { IsValid: true };
        }

        public void ReplaceSelectedExecuted(string materialName)
        {
            MainViewModel.IsBusy = true;

            string sourceFile = DataModel.SourceVoxelFilePath ?? DataModel.VoxelFilePath;

            MyVoxelMapBase asteroid = new();
            asteroid.Load(sourceFile);

            if (string.IsNullOrEmpty(materialName))
            {
                asteroid.RemoveContent(SelectedMaterialAsset.MaterialName, null);
                DataModel.VoxCells = asteroid.VoxCells;
            }
            else
            {
                asteroid.ReplaceMaterial(SelectedMaterialAsset.MaterialName, materialName);
            }

            string tempFileName = TempFileUtil.NewFileName(MyVoxelMapBase.FileExtension.V2);
            asteroid.Save(tempFileName);

            DataModel.UpdateNewSource(asteroid, tempFileName);

            MainViewModel.IsModified = true;
            MainViewModel.IsBusy = false;

            DataModel.UpdateGeneralFromEntityBase();
            DataModel.MaterialAssets = null;
            DataModel.InitializeAsync();
        }

        public bool SliceQuarterCanExecute()
        {
            return DataModel.IsValid;
        }

        public void SliceQuarterExecuted()
        {
            MainViewModel.IsBusy = true;

            string sourceFile = DataModel.SourceVoxelFilePath ?? DataModel.VoxelFilePath;

            MyVoxelMapBase asteroid = new();
            asteroid.Load(sourceFile);
            asteroid.RefreshAssets();

            int height = asteroid.BoundingContent.Size.Y + 1;
            VRageMath.Vector3D contentCenter = asteroid.ContentCenter;
            VRageMath.Vector3I asteroidSize = asteroid.Size;
            VRageMath.Vector3I min = (VRageMath.Vector3I)VRageMath.Vector3D.Round(contentCenter, 0);

            // remove the Top half.
            asteroid.RemoveMaterial(min.X, asteroidSize.X, min.Y, asteroidSize.Y, 0, min.Z);

            string tempFileName = TempFileUtil.NewFileName(MyVoxelMapBase.FileExtension.V2);
            asteroid.Save(tempFileName);

            string newFileName = MainViewModel.CreateUniqueVoxelStorageName(DataModel.Name);
            MyPositionAndOrientation posOrient = DataModel.PositionAndOrientation ?? new MyPositionAndOrientation();
            posOrient.Position.y += height;

            // genreate a new Asteroid entry.
            MyObjectBuilder_VoxelMap newEntity = new()
            {
                EntityId = SpaceEngineersApi.GenerateEntityId(IDType.ASTEROID),
                PersistentFlags = MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                StorageName = Path.GetFileNameWithoutExtension(newFileName),
                PositionAndOrientation = new MyPositionAndOrientation
                {
                    Position = posOrient.Position,
                    Forward = posOrient.Forward,
                    Up = posOrient.Up
                }
            };

            var structure = MainViewModel.AddEntity(newEntity);
            ((StructureVoxelModel)structure).UpdateNewSource(asteroid, tempFileName); // Set the temporary file location of the Source Voxel, as it hasn't been written yet.

            MainViewModel.IsModified = true;
            MainViewModel.IsBusy = false;
        }

        public bool SliceHalfCanExecute()
        {
            return DataModel.IsValid;
        }

        public void SliceHalfExecuted()
        {
            MainViewModel.IsBusy = true;

            string sourceFile = DataModel.SourceVoxelFilePath ?? DataModel.VoxelFilePath;

            MyVoxelMapBase asteroid = new();
            asteroid.Load(sourceFile);
            asteroid.RefreshAssets();

            int height = asteroid.BoundingContent.Size.Y + 1;

            // remove the Top half.
            asteroid.RemoveMaterial(null, null, (int)Math.Round(asteroid.ContentCenter.Y, 0), asteroid.Size.Y, null, null);

            string tempFileName = TempFileUtil.NewFileName(MyVoxelMapBase.FileExtension.V2);
            asteroid.Save(tempFileName);

            string newFileName = MainViewModel.CreateUniqueVoxelStorageName(DataModel.Name);
            MyPositionAndOrientation posOrient = DataModel.PositionAndOrientation ?? new MyPositionAndOrientation();
            posOrient.Position.y += height;

            // genreate a new Asteroid entry.
            MyObjectBuilder_VoxelMap newEntity = new()
            {
                EntityId = SpaceEngineersApi.GenerateEntityId(IDType.ASTEROID),
                PersistentFlags = MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                StorageName = Path.GetFileNameWithoutExtension(newFileName),
                PositionAndOrientation = new MyPositionAndOrientation
                {
                    Position = posOrient.Position,
                    Forward = posOrient.Forward,
                    Up = posOrient.Up
                }
            };

            var structure = MainViewModel.AddEntity(newEntity);
            ((StructureVoxelModel)structure).UpdateNewSource(asteroid, tempFileName); // Set the temporary file location of the Source Voxel, as it hasn't been written yet.

            MainViewModel.IsModified = true;
            MainViewModel.IsBusy = false;
        }

        public bool ExtractStationIntersectLooseCanExecute()
        {
            return DataModel.IsValid;
        }

        public void ExtractStationIntersectLooseExecuted()
        {
            MainViewModel.IsBusy = true;
            bool modified = ExtractStationIntersect(false);
            if (modified)
            {
                DataModel.InitializeAsync();
                MainViewModel.IsModified = true;
            }
            MainViewModel.IsBusy = false;
        }

        public bool ExtractStationIntersectTightCanExecute()
        {
            return DataModel.IsValid;
        }

        public void ExtractStationIntersectTightExecuted()
        {
            MainViewModel.IsBusy = true;
            bool modified = ExtractStationIntersect(true);
            if (modified)
            {
                DataModel.InitializeAsync();
                MainViewModel.IsModified = true;
            }
            MainViewModel.IsBusy = false;
        }

        public bool RotateAsteroidYawPositiveCanExecute()
        {
            return DataModel.IsValid;
        }

        public void RotateAsteroidPitchPositiveExecuted()
        {
            MainViewModel.IsBusy = true;
            // +90 around X
            MaterialAssets = null;
            DataModel.RotateAsteroid(VRageMath.Quaternion.CreateFromYawPitchRoll(0, VRageMath.MathHelper.PiOver2, 0));
            DataModel.InitializeAsync();
            MainViewModel.IsModified = true;
            MainViewModel.IsBusy = false;
        }

        public bool RotateAsteroidPitchNegativeCanExecute()
        {
            return DataModel.IsValid;
        }

        public void RotateAsteroidPitchNegativeExecuted()
        {
            MainViewModel.IsBusy = true;
            // -90 around X
            DataModel.RotateAsteroid(VRageMath.Quaternion.CreateFromYawPitchRoll(0, -VRageMath.MathHelper.PiOver2, 0));
            DataModel.InitializeAsync();
            MainViewModel.IsModified = true;
            MainViewModel.IsBusy = false;
        }

        public bool RotateAsteroidRollPositiveCanExecute()
        {
            return DataModel.IsValid;
        }

        public void RotateAsteroidYawPositiveExecuted()
        {
            MainViewModel.IsBusy = true;
            // +90 around Y
            DataModel.RotateAsteroid(VRageMath.Quaternion.CreateFromYawPitchRoll(VRageMath.MathHelper.PiOver2, 0, 0));
            DataModel.InitializeAsync();
            MainViewModel.IsModified = true;
            MainViewModel.IsBusy = false;
        }

        public bool RotateAsteroidYawNegativeCanExecute()
        {
            return DataModel.IsValid;
        }

        public void RotateAsteroidYawNegativeExecuted()
        {
            MainViewModel.IsBusy = true;
            // -90 around Y
            DataModel.RotateAsteroid(VRageMath.Quaternion.CreateFromYawPitchRoll(-VRageMath.MathHelper.PiOver2, 0, 0));
            DataModel.InitializeAsync();
            MainViewModel.IsModified = true;
            MainViewModel.IsBusy = false;
        }

        public bool RotateAsteroidPitchPositiveCanExecute()
        {
            return DataModel.IsValid;
        }

        public void RotateAsteroidRollPositiveExecuted()
        {
            MainViewModel.IsBusy = true;
            // +90 around Z
            DataModel.RotateAsteroid(VRageMath.Quaternion.CreateFromYawPitchRoll(0, 0, VRageMath.MathHelper.PiOver2));
            DataModel.InitializeAsync();
            MainViewModel.IsModified = true;
            MainViewModel.IsBusy = false;
        }

        public bool RotateAsteroidRollNegativeCanExecute()
        {
            return DataModel.IsValid;
        }

        public void RotateAsteroidRollNegativeExecuted()
        {
            MainViewModel.IsBusy = true;
            // -90 around Z
            DataModel.RotateAsteroid(VRageMath.Quaternion.CreateFromYawPitchRoll(0, 0, -VRageMath.MathHelper.PiOver2));
            DataModel.InitializeAsync();
            MainViewModel.IsModified = true;
            MainViewModel.IsBusy = false;
        }

        private bool ExtractStationIntersect(bool tightIntersection)
        {
            return DataModel.ExtractStationIntersect(MainViewModel, tightIntersection);
        }

        #endregion
    }
}
