using VRageMath;

namespace SEToolbox.Models
{
    public class GenerateVoxelDetailModel : BaseModel
    {
        #region Fields

        private string _name;
        private string _sourceFileName;
        private string _voxelFileName;
        private Vector3I _size;

        #endregion

        #region Properties

        public string Name
        {
            get => _name;

            set => SetProperty(ref _name, value, nameof(Name));
        }

        public string SourceFileName
        {
            get => _sourceFileName;

            set => SetProperty(ref _sourceFileName, value, nameof(SourceFileName));
        }

        public string VoxelFileName
        {
            get => _voxelFileName;

            set => SetProperty(ref _voxelFileName, value, nameof(VoxelFileName));
        }

        public Vector3I Size
        {
            get => _size;

            set => SetProperty(ref _size, value, nameof(Size));
        }

        public int SizeX
        {
            get => _size.X; 
        }

        public int SizeY
        {
            get => _size.Y; 
        }

        public int SizeZ
        {
            get => _size.Z; 
        }

        public long FileSize { get; set; }

        #endregion

        // To allow text searching in ComboBox.
        public override string ToString()
        {
            return _name;
        }

        internal GenerateVoxelDetailModel Clone()
        {
            return new GenerateVoxelDetailModel();
        }
    }
}
