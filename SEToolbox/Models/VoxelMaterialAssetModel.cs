using System;


namespace SEToolbox.Models
{
    [Serializable]
    public class VoxelMaterialAssetModel : BaseModel
    {
        private string _materialName;

        private string _displayName;

        private double _volume;

        private double _percent;

        private string _textureFile;

        #region Properties

        /// <summary>
        /// 'Name' which represents the name used in the Voxel Material, and .vox file.
        /// </summary>
        public string MaterialName
        {
            get => _materialName;
            set => SetProperty(ref _materialName, nameof(MaterialName));
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, nameof(DisplayName));
        }

        public double Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, nameof(Volume));
        }

        public double Percent
        {
            get => _percent;
            set => SetProperty(ref _percent, nameof(Percent));
        }

        public string TextureFile
        {
            get => _textureFile;
            set => SetProperty(ref _textureFile, nameof(TextureFile));
        }

        #endregion
    }
}
