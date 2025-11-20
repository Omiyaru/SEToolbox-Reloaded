using System;
using System.ComponentModel;

using SEToolbox.Interop;
using VRage.Game;
using VRage.ObjectBuilders;

namespace SEToolbox.Models
{
    [Serializable]
    public class InventoryModel : BaseModel, IDataErrorInfo
    {
        #region Fields

        [NonSerialized]
        private readonly MyObjectBuilder_InventoryItem _item;

        private string _name;
        private decimal _amount;
        private double _mass;
        private double _massMultiplier;
        private double _volume;
        private double _volumeMultiplier;

        #endregion

        public InventoryModel(MyObjectBuilder_InventoryItem item)
        {
            _item = item;
        }

        #region Properties

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, nameof(Name),  FriendlyName == SpaceEngineersApi.GetResourceName(Name), nameof(FriendlyName));
        }

        public MyObjectBuilderType TypeId { get; set; }
        
        public string SubtypeId { get; set; }

        public decimal Amount
        {
            get => _amount;
            set
            {
                SetProperty(ref _amount, nameof(Amount), () => 
                UpdateMassVolume());
            }
        }

        public double Mass
        {
            get => _mass;
            private set => SetProperty(ref _mass, nameof(Mass));
        }

        public double MassMultiplier
        {
            get => _massMultiplier;
            set  => SetProperty(ref _massMultiplier, nameof(MassMultiplier), () => UpdateMassVolume());
        }

        public double Volume
        {
            get => _volume;
            private set => SetProperty(ref _volume, nameof(Volume));
        }

        public double VolumeMultiplier
        {
            get => _volumeMultiplier;
            set => SetProperty(ref _volumeMultiplier, nameof(VolumeMultiplier), () => UpdateMassVolume());
        }

        public string TextureFile { get; set; }
        
        public MyCubeSize? CubeSize { get; set; }
        
        public bool Exists { get; set; }
        
        public string FriendlyName { get; set; }
        
        public bool IsUnique { get; set; }
        
        public bool IsDecimal { get; set; }
        
        public bool IsInteger { get; set; }

        public override string ToString()
        {
            return FriendlyName;
        }

        #endregion

        private void UpdateMassVolume()
        {
            Mass = MassMultiplier * (double)Amount;
            Volume = VolumeMultiplier * (double)Amount;
            _item.Amount = Amount.ToFixedPoint();
        }

        #region Volume Change Notification

        public event Action VolumeChanged;

        protected virtual void OnVolumeChanged()
        {
            VolumeChanged.Invoke();
        }

        #endregion

        #region IDataErrorInfo Implementation

        public string Error
        {
            get => null;
        }

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(Amount):
                        if (IsUnique && Amount != 1)
                            return "The Amount must be 1 for Unique items";

                        if (IsInteger && (Amount % 1 != 0))
                            return "The Amount must not contain decimal places";

                        break;
                }

                return string.Empty;
            }
        }

        //  TODO: need to bubble volume change up to InventoryEditor for updating TotalVolume, and up to MainViewModel.IsModified = true;

        #endregion


        public InventoryModel(MyObjectBuilder_InventoryItem item, string name, string subtypeId)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrEmpty(subtypeId))
                throw new ArgumentNullException(nameof(subtypeId));

            _item = item ?? throw new ArgumentNullException(nameof(item));
            _name = name;
            SubtypeId = subtypeId;
            FriendlyName = SpaceEngineersApi.GetResourceName(Name.ToString());
            TextureFile = string.Empty;
            VolumeChanged = () => { };
        }
    }
}