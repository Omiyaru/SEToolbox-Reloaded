using System.Drawing;

using SEToolbox.Interop;
using VRage;

namespace SEToolbox.Models
{
    public class ImportImageModel : BaseModel
    {
        #region Fields

        private string _fileName;
        private bool _isValidImage;

        private Size _originalImageSize;
        private BindableSizeModel _newImageSize;
        private BindablePoint3DModel _position;
        private BindableVector3DModel _forward;
        private BindableVector3DModel _up;
        private ImportImageClassType _classType;
        private ImportArmorType _armorType;
        private MyPositionAndOrientation _characterPosition;
        private int _alphaLevel;
        private System.Windows.Media.Color _keyColor;
        private bool _isAlphaLevel;
        private bool _isKeyColor;

        #endregion

        public ImportImageModel()
        {
            AlphaLevel = 127;
            KeyColor = System.Windows.Media.Color.FromArgb(0, 255, 0, 255);
            IsAlphaLevel = true;
        }

        #region Properties

        public string FileName
        {
            get => _fileName; 

            set => SetProperty(ref _fileName, value, nameof(FileName));
        }

        public bool IsValidImage
        {
            get =>  _isValidImage; 

            set => SetProperty(ref _isValidImage, value, nameof(IsValidImage));
        }

        public Size OriginalImageSize
        {
            get =>  _originalImageSize;

            set => SetProperty(ref _originalImageSize, value, nameof(OriginalImageSize));
        }

        public BindableSizeModel NewImageSize
        {
            get => _newImageSize; 

            set
            {
                if (value != _newImageSize)
                {
                    _newImageSize = value;
                    OnPropertyChanged(nameof(NewImageSize));
                }
            }
        }

        public BindablePoint3DModel Position
        {
            get =>  _position; 

            set => SetProperty(ref _position, value, nameof(Position));
        }

        public BindableVector3DModel Forward
        {
            get =>  _forward; 

            set => SetProperty(ref _forward, value, nameof(Forward));
        }

        public BindableVector3DModel Up
        {
            get =>  _up; 

            set => SetProperty(ref _up, value, nameof(Up));
        }

        public ImportImageClassType ClassType
        {
            get =>  _classType; 

            set => SetProperty(ref _classType, value, nameof(ClassType));
        }

        public ImportArmorType ArmorType
        {
            get =>  _armorType; 

            set
            {
                if (value != _armorType)
                {
                    _armorType = value;
                    OnPropertyChanged(nameof(ArmorType));
                }
            }
        }

        public MyPositionAndOrientation CharacterPosition
        {
            get =>  _characterPosition;

                //if (value != characterPosition) // Unable to check for equivilence, without long statement. And, mostly uncessary.
            set => SetProperty(ref _characterPosition, value, nameof(CharacterPosition));
        }

        public int AlphaLevel
        {
            get =>  _alphaLevel; 

            set => SetProperty(ref _alphaLevel, value, nameof(AlphaLevel));
        }

        public System.Windows.Media.Color KeyColor
        {
            get =>  _keyColor; 

            set => SetProperty(ref _keyColor, value, nameof(KeyColor));
        }

        public bool IsAlphaLevel
        {
            get =>  _isAlphaLevel; 

            set => SetProperty(ref _isAlphaLevel, value, nameof(IsAlphaLevel));
        }

        public bool IsKeyColor
        {
            get =>  _isKeyColor; 

            set => SetProperty(ref _isKeyColor, value, nameof(IsKeyColor));
        }

        #endregion

        #region Methods

        public void Load(MyPositionAndOrientation characterPosition)
        {
            CharacterPosition = characterPosition;
        }

        #endregion

        #region Helpers

        #endregion
    }
}
