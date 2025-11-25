
using SEToolbox.Interop;

namespace SEToolbox.Models
{
    public class MaterialSelectionModel : BaseModel
    {
        private string _displayName;
        private string _value;
        private bool _isRare;
        private float _minedRatio;
        private byte _radius;
        private int _veins;
        private string _name;


        #region Properties

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value, nameof(DisplayName));
        }

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value, nameof(Value));
        }

        public byte? MaterialIndex => _value == null ? null :
                 SpaceEngineersResources.GetMaterialIndex(_value);

        public bool IsRare
        {
            get => _isRare;
            set => SetProperty(ref _isRare, value, nameof(IsRare));
        }

        public float MinedRatio
        {
            get => _minedRatio;
            set => SetValue(ref _minedRatio, value, nameof(MinedRatio));
        }

        public string Name
        {
            get => _name;
            set => SetValue(ref _name, value, nameof(Name));
        }
        
        public byte Radius
        {
            get => _radius;
            set => SetValue(ref _radius, value, nameof(Radius));
        }
        
        public int Veins
        {
            get => _veins;
            set => SetValue(ref _veins, value, nameof(Veins));
        }
        
       
        internal MaterialSelectionModel Clone()
        {
            return (MaterialSelectionModel)MemberwiseClone();
        }

        #endregion
    }
}