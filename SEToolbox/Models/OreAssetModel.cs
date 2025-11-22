using SEToolbox.Interop;

using System;

using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace SEToolbox.Models
{
    [Serializable]
    public class OreAssetModel : BaseModel
    {
        private string _name;

        private decimal _amount;

        private double _mass;

        private double _volume;

        private TimeSpan _time;

        private string _textureFile;

        public static object MaterialColors { get; internal set; }

        #region Properties

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value, () => FriendlyName = SpaceEngineersApi.GetResourceName(Name), 
            nameof(Name), nameof(FriendlyName));
            
        }

        public string FriendlyName { get; set; }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value, nameof(Amount));
        }

        public double Mass
        {
            get => _mass;
            set => SetProperty(ref _mass, value, nameof(Mass));
        }

        public double Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value, nameof(Volume));
        }

        public TimeSpan Time
        {
            get => _time;
            set => SetProperty(ref _time, value, nameof(Time));
        }

        public string TextureFile
        {
            get => _textureFile;
            set => SetProperty(ref _textureFile, value, nameof(TextureFile));
        }

        #endregion
    }
}
