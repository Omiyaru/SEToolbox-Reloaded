using System;

using SEToolbox.Interop;

namespace SEToolbox.Models
{
    [Serializable]
    public class CubeAssetModel : BaseModel
    {
        #region Fields

        private string _name;

        private double _mass;

        private double _volume;

        private long _count;

        private TimeSpan _time;

        private int _pcu;

        private string _textureFile;

        #endregion

        #region Properties

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, nameof(Name), FriendlyName == SpaceEngineersApi.GetResourceName(Name), nameof(FriendlyName));
        }

        public string FriendlyName { get; set; }

        public double Mass
        {
            get => _mass;
            set => SetProperty(ref _mass, nameof(Mass));
        }

        public double Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, nameof(Volume));
        }

        public long Count
        {
            get => _count;
            set => SetProperty(ref _count, nameof(Count));
        }

        public TimeSpan Time
        {
            get => _time;
            set => SetProperty(ref _time, nameof(Time));
        }

        public int PCU
        {
            get => _pcu;
            set => SetProperty(ref _pcu, nameof(PCU));
        }


        public string TextureFile
        {
            get => _textureFile;
            set => SetProperty(ref _textureFile, nameof(TextureFile));
        }

        #endregion
    }
}
