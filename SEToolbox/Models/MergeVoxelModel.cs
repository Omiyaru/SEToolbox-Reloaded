using SEToolbox.Interfaces;
using SEToolbox.Support;

namespace SEToolbox.Models
{
    public class MergeVoxelModel : BaseModel
    {
        #region Fields

        private IStructureBase _selectionLeft;
        private IStructureBase _selectionRight;
        private string _sourceFile;
        private bool _isValidMerge;
        private VoxelMergeType _voxelMergeType;
        private bool _isBusy;
        private string _mergeFileName;
        private bool _removeOriginalAsteroids;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value, nameof(IsBusy), () =>
                    {
                        if (_isBusy)
                        {
                            System.Windows.Forms.Application.DoEvents();
                        }
                    });
        }

        public IStructureBase SelectionLeft
        {
            get => _selectionLeft;
            set => SetProperty(ref _selectionLeft, value, nameof(SelectionLeft));
        }

        public IStructureBase SelectionRight
        {
            get => _selectionRight;
            set => SetProperty(ref _selectionRight, value, nameof(SelectionRight));
        }

        /// <summary>
        /// Indicates if the Entity created at the end of processing is valid.
        /// </summary>
        public bool IsValidMerge
        {
            get => _isValidMerge;
            set => SetProperty(ref _isValidMerge, value, nameof(IsValidMerge));
        }

        public string SourceFile
        {
            get => _sourceFile;
            set => SetProperty(ref _sourceFile, value, nameof(SourceFile));
        }

        public VoxelMergeType VoxelMergeType
        {
            get => _voxelMergeType;
            set => SetProperty(ref _voxelMergeType, value, nameof(VoxelMergeType));
        }

        public string MergeFileName
        {
            get => _mergeFileName;
            set => SetProperty(ref _mergeFileName, value, nameof(MergeFileName));
        }

        public bool RemoveOriginalAsteroids
        {
            get => _removeOriginalAsteroids;
            set => SetProperty(ref _removeOriginalAsteroids, value, nameof(RemoveOriginalAsteroids));
        }

        #endregion

        #region Methods

        public void Load(IStructureBase selection1, IStructureBase selection2)
        {
            SelectionLeft = selection1;
            SelectionRight = selection2;

            var modelLeft = (StructureVoxelModel)SelectionLeft;
            var modelRight = (StructureVoxelModel)SelectionRight;

            IsValidMerge = modelLeft.WorldAabb.Intersects(modelRight.WorldAabb);
        }

        #endregion
    }
}