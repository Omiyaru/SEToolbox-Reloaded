using VRageMath;

using SEToolbox.Interfaces;

namespace SEToolbox.Models
{
    public class GroupMoveItemModel : BaseModel
    {
        #region Fields

        private IStructureViewBase _item;
        private double _positionX;
        private double _positionY;
        private double _positionZ;
        private double _playerDistance;
        //private Vector3D _playerLocation;

        #endregion

        #region Properties

        public IStructureViewBase Item
        {
            get => _item;
            set => SetProperty(ref _item, nameof(Item));
        }
        
        public Vector3D Position
        {
            get => new(x: _positionX, y: _positionY, z: _positionZ);
        
            set => SetProperty(ref _positionX, value.X, nameof(PositionX),
                               	   _positionY, value.Y, nameof(PositionY),
                                   _positionZ, value.Z, nameof(PositionZ),
                                   nameof(Position));
        } 
        public double PositionX
        {
            get => _positionX;
            set => SetProperty(ref _positionX, nameof(PositionX));
        }

        public double PositionY
        {
            get => _positionY;
            set => SetProperty(ref _positionY, nameof(PositionY));
        }

        public double PositionZ
        {
            get => _positionZ;
            set => SetProperty(ref _positionZ, nameof(PositionZ));
        }

        public double PlayerDistance
        {
            get => _playerDistance;
            set => SetProperty(ref _playerDistance, nameof(PlayerDistance));
        }
        
        public Vector3D PlayerPosition
        {
            get => new( _positionX, _positionY, _positionZ);
            set => SetProperty(ref _positionX, value.X, nameof(PositionX),
                                   _positionY, value.Y, nameof(PositionY),
                                   _positionZ, value.Z, nameof(PositionZ),
                                nameof(Position));
        }


        // public Vector3D PlayerLocation
        // {
        //     get => _playerLocation;
        //     internal set
        //     {
        //         if (value != _playerLocation)
        //         {
        //             _playerLocation = value;
        //             OnPropertyChanged(nameof(PlayerLocation));
        //         }
        //     }


            #endregion
        }
    }
