
using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Models;
using SEToolbox.Services;
using SEToolbox.Support;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using VRage;
using VRage.Game;
using VRage.Library.Collections;
using VRage.ObjectBuilders;
using VRage.Voxels;
using VRageMath;
using IDType = VRage.MyEntityIdentifier.ID_OBJECT_TYPE;
using PRange = SEToolbox.Support.PRange;

namespace SEToolbox.ViewModels
{
    public class MergeVoxelViewModel : BaseViewModel
    {
        #region Fields

        private readonly MergeVoxelModel _dataModel;
        private bool? _closeResult;

        #endregion

        #region Constructors

        public MergeVoxelViewModel(BaseViewModel parentViewModel, MergeVoxelModel dataModel)
            : base(parentViewModel)
        {
            _dataModel = dataModel;

            // Will bubble property change events from the Model to the ViewModel.
            _dataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);

            MergeFileName = "merge";
        }

        #endregion

        #region Command Properties

        public ICommand ApplyCommand
        {
            get => new DelegateCommand(ApplyExecuted, ApplyCanExecute);
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

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => _dataModel.IsBusy;
            set => _dataModel.IsBusy = value;
        }

        public bool IsValidMerge
        {
            get => _dataModel.IsValidMerge;
            set => _dataModel.IsValidMerge = value;
        }

        public MyObjectBuilder_VoxelMap NewEntity { get; set; }

        public StructureVoxelModel SelectionLeft
        {
            get => (StructureVoxelModel)_dataModel.SelectionLeft;
            set => _dataModel.SelectionLeft = value;
        }

        public StructureVoxelModel SelectionRight
        {
            get => (StructureVoxelModel)_dataModel.SelectionRight;
            set => _dataModel.SelectionRight = value;
        }

        public string SourceFile
        {
            get => _dataModel.SourceFile;
            set => _dataModel.SourceFile = value;
        }

        public VoxelMergeType VoxelMergeType
        {
            get => _dataModel.VoxelMergeType;
            set => _dataModel.VoxelMergeType = value;
        }

        public string MergeFileName
        {
            get => _dataModel.MergeFileName;
            set => _dataModel.MergeFileName = value;
        }

        public bool RemoveOriginalAsteroids
        {
            get => _dataModel.RemoveOriginalAsteroids;
            set => _dataModel.RemoveOriginalAsteroids = value;
        }

        #endregion

        #region Methods

        public bool ApplyCanExecute()
        {
            return IsValidMerge;
        }

        public void ApplyExecuted()
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

        public MyObjectBuilder_EntityBase BuildEntity()
        {
            // Realign both asteroids to a common grid, so voxels can be lined up.
            Vector3I roundedPosLeft = SelectionLeft.WorldAabb.Min.RoundToVector3I();
            Vector3D offsetPosLeft = SelectionLeft.WorldAabb.Min - (Vector3D)roundedPosLeft;
            Vector3I roundedPosRight = (SelectionRight.WorldAabb.Min - offsetPosLeft).RoundToVector3I();
            Vector3D offsetPosRight = SelectionRight.WorldAabb.Min - (Vector3D)roundedPosRight;

            // calculate smallest allowable size for contents of both.
            const int paddCells = 3;

            // Force a calculation of the ContentBounds.
            SelectionLeft.LoadDetailsSync();
            SelectionRight.LoadDetailsSync();

            Vector3D minLeft = SelectionLeft.WorldAabb.Min + SelectionLeft.InflatedContentBounds.Min - offsetPosLeft;
            Vector3D minRight = SelectionRight.WorldAabb.Min + SelectionRight.InflatedContentBounds.Min - offsetPosRight;
            Vector3D min = Vector3D.Zero;
            Vector3D posOffset = Vector3D.Zero;
            Vector3I asteroidSize = Vector3I.Zero;

            switch (VoxelMergeType)
            {
                case VoxelMergeType.UnionVolumeLeftToRight:
                case VoxelMergeType.UnionVolumeRightToLeft:
                    min = Vector3D.Min(minLeft, minRight) - paddCells;
                    Vector3D max = Vector3D.Max(
                        SelectionLeft.WorldAabb.Min + SelectionLeft.InflatedContentBounds.Max - offsetPosLeft,
                        SelectionRight.WorldAabb.Min + SelectionRight.InflatedContentBounds.Max - offsetPosRight) + paddCells;
                    posOffset = GetPosOffset(minLeft, minRight, offsetPosLeft, offsetPosRight);
                    asteroidSize = MyVoxelBuilder.CalcRequiredSize((max - min).RoundToVector3I());
                    break;
                case VoxelMergeType.UnionMaterialLeftToRight:
                case VoxelMergeType.SubtractVolumeLeftFromRight:
                    min = SelectionRight.WorldAabb.Min - offsetPosRight;
                    posOffset = GetPosOffset(minLeft, minRight, offsetPosLeft, offsetPosRight);
                    asteroidSize = SelectionRight.Size;
                    break;
                case VoxelMergeType.UnionMaterialRightToLeft:
                case VoxelMergeType.SubtractVolumeRightFromLeft:
                    min = SelectionLeft.WorldAabb.Min - offsetPosLeft;
                    posOffset = GetPosOffset(minLeft, minRight, offsetPosLeft, offsetPosRight);
                    asteroidSize = SelectionLeft.Size;
                    break;
            }

            // Prepare new asteroid.
            MyVoxelMapBase newAsteroid = new();
            newAsteroid.Create(asteroidSize, SpaceEngineersResources.GetDefaultMaterialIndex());
            MergeFileName = string.IsNullOrEmpty(MergeFileName) ? "merge" : MergeFileName;
            string fileName = MainViewModel.CreateUniqueVoxelStorageName(MergeFileName);

            // Merge operations.
            PerformMerge(ref newAsteroid, min, minLeft, minRight);


            // Generate Entity
            var tempFileName = TempFileUtil.NewFileName(MyVoxelMapBase.FileExtension.V2);
            newAsteroid.Save(tempFileName);
            SourceFile = tempFileName;

            Vector3D position = min + posOffset;
            MyObjectBuilder_VoxelMap entity = new(position, fileName)
            {
                EntityId = SpaceEngineersApi.GenerateEntityId(IDType.ASTEROID),
                PersistentFlags = MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                StorageName = Path.GetFileNameWithoutExtension(fileName),
                PositionAndOrientation = new MyPositionAndOrientation
                {
                    Position = position,
                    Forward = Vector3.Forward,
                    Up = Vector3.Up
                }
            };

            return entity;
        }

        private static Vector3D GetPosOffset(Vector3D minLeft, Vector3D minRight, Vector3D offsetPosLeft, Vector3D offsetPosRight)
        {
            return new Vector3D(
                minLeft.X < minRight.X ? offsetPosLeft.X : offsetPosRight.X,
                minLeft.Y < minRight.Y ? offsetPosLeft.Y : offsetPosRight.Y,
                minLeft.Z < minRight.Z ? offsetPosLeft.Z : offsetPosRight.Z
            );
        }

        private void PerformMerge(ref MyVoxelMapBase newAsteroid, Vector3D min, Vector3D minLeft, Vector3D minRight)
        {
            switch (VoxelMergeType)
            {
                case VoxelMergeType.UnionVolumeLeftToRight:
                case VoxelMergeType.UnionVolumeRightToLeft:
                    MergeAsteroidVolumeInto(ref newAsteroid, min,
                        VoxelMergeType == VoxelMergeType.UnionVolumeRightToLeft ? SelectionRight : SelectionLeft,
                        VoxelMergeType == VoxelMergeType.UnionVolumeLeftToRight ? SelectionLeft : SelectionRight,
                        VoxelMergeType == VoxelMergeType.UnionVolumeRightToLeft ? minRight : minLeft,
                        VoxelMergeType == VoxelMergeType.UnionVolumeLeftToRight ? minLeft : minRight
                    );
                    break;
                case VoxelMergeType.UnionMaterialLeftToRight:
                case VoxelMergeType.UnionMaterialRightToLeft:
                    MergeAsteroidMaterialFrom(ref newAsteroid, min,
                        VoxelMergeType == VoxelMergeType.UnionMaterialRightToLeft ? SelectionRight : SelectionLeft,
                        VoxelMergeType == VoxelMergeType.UnionMaterialLeftToRight ? SelectionLeft : SelectionRight,
                        VoxelMergeType == VoxelMergeType.UnionMaterialRightToLeft ? minRight : minLeft,
                        VoxelMergeType == VoxelMergeType.UnionMaterialLeftToRight ? minLeft : minRight
                    );
                    break;
                case VoxelMergeType.SubtractVolumeLeftFromRight:
                case VoxelMergeType.SubtractVolumeRightFromLeft:
                    SubtractAsteroidVolumeFrom(
                        ref newAsteroid,
                        min,
                        VoxelMergeType == VoxelMergeType.SubtractVolumeRightFromLeft ? SelectionRight : SelectionLeft,
                        VoxelMergeType == VoxelMergeType.SubtractVolumeLeftFromRight ? SelectionLeft : SelectionRight,
                        VoxelMergeType == VoxelMergeType.SubtractVolumeRightFromLeft ? minRight : minLeft,
                        VoxelMergeType == VoxelMergeType.SubtractVolumeLeftFromRight ? minLeft : minRight
                    );
                    break;
            }
        }


        #region MergeAsteroidVolumeInto

        private void MergeAsteroidVolumeInto(ref MyVoxelMapBase newAsteroid, Vector3D min, StructureVoxelModel modelPrimary,
            StructureVoxelModel modelSecondary, Vector3D minPrimary, Vector3D minSecondary)
        {
            string fileNameSecondary = modelSecondary.SourceVoxelFilePath ?? modelSecondary.VoxelFilePath;
            string fileNamePrimary = modelPrimary.SourceVoxelFilePath ?? modelPrimary.VoxelFilePath;

            Vector3I newBlock;
            Vector3I cacheSize;

            using MyVoxelMapBase asteroid = new();
            asteroid.Load(fileNameSecondary);

            BoundingBoxI content = modelSecondary.InflatedContentBounds;

            Vector3I block = content.Min;

            PRange.ProcessRange(block, (content.Max - content.Min + 1) / 64);
            MyStorageData cache = new();

            cacheSize = new Vector3I(MathHelper.Min(content.Max.X, block.X + 63) - block.X + 1,
                MathHelper.Min(content.Max.Y, block.Y + 63) - block.Y + 1,
                MathHelper.Min(content.Max.Z, block.Z + 63) - block.Z + 1);

            cache.Resize(cacheSize);

            asteroid.Storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, block, block + cacheSize - 1);

            newBlock = (minSecondary - min + (Vector3D)(block - content.Min)).RoundToVector3I();

            newAsteroid.Storage.WriteRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, newBlock, newBlock + cacheSize - 1);

            asteroid.Load(fileNamePrimary);
            content = modelPrimary.InflatedContentBounds;

            PRange.ProcessRange(block, (content.Max - content.Min + 1) / 64);

            cacheSize = new Vector3I(MathHelper.Min(content.Max.X, block.X + 63) - block.X + 1,
                MathHelper.Min(content.Max.Y, block.Y + 63) - block.Y + 1,
                MathHelper.Min(content.Max.Z, block.Z + 63) - block.Z + 1);

            cache.Resize(cacheSize);
            asteroid.Storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, block, block + cacheSize - 1);

            newBlock = (minPrimary - min + (Vector3D)(block - content.Min)).RoundToVector3I();

            MyStorageData newCache = new();
            newCache.Resize(cacheSize);

            newAsteroid.Storage.ReadRange(newCache, MyStorageDataTypeFlags.ContentAndMaterial, 0, newBlock, newBlock + cacheSize - 1);

            Vector3I p = Vector3I.Zero;
            PRange.ProcessRange(p, cacheSize);

            byte volume = cache.Content(ref p);
            byte material = cache.Material(ref p);

            if (volume > 0)
            {
                byte existingVolume = newCache.Content(ref p);

                if (volume > existingVolume)
                    newCache.Content(ref p, volume);

                // Overwrites secondary material with primary.
                newCache.Material(ref p, material);
            }
            else
            {
                // try to find cover material.
                Vector3I[] points = CreateTestPoints(p, cacheSize - 1);

                for (int i = 0; i < points.Length; i++)
                {
                    byte testVolume = cache.Content(ref points[i]);

                    if (testVolume > 0)
                    {
                        material = cache.Material(ref points[i]);
                        newCache.Material(ref p, material);
                        break;
                    }
                }


                newAsteroid.Storage.WriteRange(newCache, MyStorageDataTypeFlags.ContentAndMaterial, newBlock, newBlock + cacheSize - 1);
            }
        }

        #endregion

        #region SubtractAsteroidVolumeFrom

        private static void SubtractAsteroidVolumeFrom(ref MyVoxelMapBase newAsteroid, Vector3D min, StructureVoxelModel modelPrimary,
            StructureVoxelModel modelSecondary, Vector3D minPrimary, Vector3D minSecondary)
        {
            string fileNameSecondary = modelSecondary.SourceVoxelFilePath ?? modelSecondary.VoxelFilePath;
            string fileNamePrimary = modelPrimary.SourceVoxelFilePath ?? modelPrimary.VoxelFilePath;


            Vector3I newBlock;
            Vector3I cacheSize;

            using MyVoxelMapBase asteroid = new();
            asteroid.Load(fileNamePrimary);

            BoundingBoxI content = modelPrimary.InflatedContentBounds;
            Vector3I block = content.Min;

            PRange.ProcessRange(block, (content.Max - content.Min + 1) / 64);
            MyStorageData cache = new();

            cacheSize = new Vector3I(MathHelper.Min(content.Max.X, block.X + 63) - block.X + 1,
                MathHelper.Min(content.Max.Y, block.Y + 63) - block.Y + 1,
                MathHelper.Min(content.Max.Z, block.Z + 63) - block.Z + 1);

            cache.Resize(cacheSize);

            asteroid.Storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, block, block + cacheSize - 1);

            newBlock = (minPrimary - min + (Vector3D)(block - content.Min)).RoundToVector3I();

            newAsteroid.Storage.WriteRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, newBlock, newBlock + cacheSize - 1);


            asteroid.Load(fileNameSecondary);

            content = modelSecondary.InflatedContentBounds;

            PRange.ProcessRange(block, (content.Max - content.Min + 1) / 64);


            cacheSize = new Vector3I(MathHelper.Min(content.Max.X, block.X + 63) - block.X + 1,
                MathHelper.Min(content.Max.Y, block.Y + 63) - block.Y + 1,
                MathHelper.Min(content.Max.Z, block.Z + 63) - block.Z + 1);

            cache.Resize(cacheSize);

            asteroid.Storage.ReadRange(cache, MyStorageDataTypeFlags.Content, 0, block, block + cacheSize - 1);

            newBlock = (minSecondary - min + (Vector3D)(block - content.Min)).RoundToVector3I();

            MyStorageData newCache = new();
            newCache.Resize(cacheSize);

            newAsteroid.Storage.ReadRange(newCache, MyStorageDataTypeFlags.ContentAndMaterial, 0, newBlock, newBlock + cacheSize - 1);

            Vector3I p = Vector3I.Zero;
            PRange.ProcessRange(p, cacheSize);

            byte volume = cache.Content(ref p);

            if (volume > 0)
            {
                byte existingVolume = newCache.Content(ref p);

                if (existingVolume - volume < 0)
                    volume = 0;
                else
                    volume = (byte)(existingVolume - volume);

                newCache.Content(ref p, volume);
            }

            newAsteroid.Storage.WriteRange(newCache, MyStorageDataTypeFlags.ContentAndMaterial, newBlock, newBlock + cacheSize - 1);
        }

        #endregion

        #region MergeAsteroidMaterialFrom

        private static void MergeAsteroidMaterialFrom(ref MyVoxelMapBase newAsteroid, Vector3 min, StructureVoxelModel modelPrimary,
                                                          StructureVoxelModel modelSecondary, Vector3 minPrimary, Vector3 minSecondary)
        {
            string fileNameSecondary = modelSecondary.SourceVoxelFilePath ?? modelSecondary.VoxelFilePath;
            string fileNamePrimary = modelPrimary.SourceVoxelFilePath ?? modelPrimary.VoxelFilePath;

            Vector3I newBlock;
            Vector3I cacheSize;

            using MyVoxelMapBase asteroid = new();
            asteroid.Load(fileNamePrimary);

            BoundingBoxI content = modelPrimary.InflatedContentBounds;

            Vector3I block = content.Min;

            PRange.ProcessRange(block, (content.Max - content.Min + 1) / 64);
            MyStorageData cache = new();

            cacheSize = new Vector3I(MathHelper.Min(content.Max.X, block.X + 63) - block.X + 1,
                MathHelper.Min(content.Max.Y, block.Y + 63) - block.Y + 1,
                MathHelper.Min(content.Max.Z, block.Z + 63) - block.Z + 1);

            cache.Resize(cacheSize);

            asteroid.Storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, block, block + cacheSize - 1);

            newBlock = (minPrimary - min + (Vector3D)(block - content.Min)).RoundToVector3I();

            newAsteroid.Storage.WriteRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, newBlock, newBlock + cacheSize - 1);

            asteroid.Load(fileNameSecondary);

            content = modelSecondary.InflatedContentBounds;

            PRange.ProcessRange(block, (content.Max - content.Min + 1) / 64);
            cache = new();
            cacheSize = new Vector3I(MathHelper.Min(content.Max.X, block.X + 63) - block.X + 1,
                MathHelper.Min(content.Max.Y, block.Y + 63) - block.Y + 1,
                MathHelper.Min(content.Max.Z, block.Z + 63) - block.Z + 1);

            cache.Resize(cacheSize);

            asteroid.Storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, block, block + cacheSize - 1);

            newBlock = (minSecondary - min + (Vector3D)(block - content.Min)).RoundToVector3I();

            MyStorageData newCache = new();
            newCache.Resize(cacheSize);

            newAsteroid.Storage.ReadRange(newCache, MyStorageDataTypeFlags.ContentAndMaterial, 0, newBlock, newBlock + cacheSize - 1);


            Vector3I p = Vector3I.Zero;
            PRange.ProcessRange(p, cacheSize);
            byte volume = cache.Content(ref p);
            byte material = cache.Material(ref p);

            if (volume > 0)
            {
                newCache.Material(ref p, material);
            }

            newAsteroid.Storage.WriteRange(newCache, MyStorageDataTypeFlags.ContentAndMaterial, newBlock, newBlock + cacheSize - 1);
        }

        #endregion

        #region CreateTestPoints
        /// <summary>
        /// Creates a list of points around the specified point (within a 3x3x3 grid), but only when they are within the bounds.
        /// </summary>
        private static Vector3I[] CreateTestPoints(Vector3I point, Vector3I max)
        {
            List<Vector3I> points = [];
            Vector3I newPoint;
            // Define the possible directions for movement
            Vector3I[] directions =
            [
                new(-1, 0, 0), new(1, 0, 0),
                new(0, -1, 0), new(0, 1, 0),
                new(0, 0, -1), new(0, 0, 1)
            ];

            // Add single step points
            foreach (Vector3I direction in directions)
            {
                newPoint = point + direction;
                if (IsWithinBounds(newPoint, max))
                {
                    points.Add(newPoint);
                }
            }

            // Add points for the 3x3x3 grid
            int x = 0, y = 0, z = 0;
            PRange.ProcessRange(point, x, y, z, -1, 3);

            newPoint = new(point.X + x, point.Y + y, point.Z + z);
            if (IsWithinBounds(newPoint, max))
            {
                points.Add(newPoint);
            }
            return [.. points];
        }
        #endregion

        #region IsWithinBounds
        
        private static bool IsWithinBounds(Vector3I point, Vector3I max)
        {
            return point.X >= 0 && point.X <= max.X &&
                   point.Y >= 0 && point.Y <= max.Y &&
                   point.Z >= 0 && point.Z <= max.Z;
        }
    }
}
    
        #endregion