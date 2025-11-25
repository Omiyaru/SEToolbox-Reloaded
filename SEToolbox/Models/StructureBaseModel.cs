using System;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Xml.Serialization;

using Sandbox.Common.ObjectBuilders;
using SEToolbox.Interfaces;
using SEToolbox.Interop;
using VRageMath;
using VRage.ObjectBuilders;
using VRage;
using VRage.Game;
using VRage.Game.ObjectBuilders;

namespace SEToolbox.Models
{
    [Serializable]
    public class StructureBaseModel : BaseModel, IStructureBase
    {
        #region Fields

        // Fields are marked as NonSerialized, as they aren't required during the drag-drop operation.

        [NonSerialized]
        private MyObjectBuilder_EntityBase _entityBase;

        [NonSerialized]
        private ClassType _classType;

        [NonSerialized]
        private string _name;

        [NonSerialized]
        private string _description;

        [NonSerialized]
        private Vector3D _center;

        [NonSerialized]
        private BoundingBoxD _worldAabb;

        [NonSerialized]
        private double _playerDistance;

        [NonSerialized]
        private double _mass;

        [NonSerialized]
        private int _blockCount;

        [NonSerialized]
        private double _linearVelocity;

        [NonSerialized]
        private bool _isBusy;

        [NonSerialized]
        internal bool _isValid;

        private string _serializedEntity;

        [NonSerialized]
        internal Dispatcher _dispatcher;

        private Vector3D _playerPosition;

        private string _sourceVoxelFilePath;

        #endregion

        #region Ctor

        public StructureBaseModel()
        {
        }

        public StructureBaseModel(MyObjectBuilder_EntityBase entityBase)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            EntityBase = entityBase;
        }

        #endregion

        #region Properties

        [XmlIgnore]
        public virtual MyObjectBuilder_EntityBase EntityBase
        {
            get => _entityBase;

            set => SetProperty(ref _entityBase, value, () =>
            UpdateGeneralFromEntityBase(),
            nameof(EntityBase));

        }

        [XmlIgnore]
        public long EntityId
        {
            get => _entityBase.EntityId;
            set => SetProperty(ref _entityBase.EntityId, value, nameof(EntityId));

        }

        [XmlIgnore]
        public MyPositionAndOrientation? PositionAndOrientation
        {
            get => _entityBase.PositionAndOrientation;

            set => SetProperty(ref _entityBase.PositionAndOrientation,value, nameof(PositionAndOrientation));
        }

        [XmlIgnore]
        public double PositionX
        {
            get => _entityBase.PositionAndOrientation.Value.Position.X;
            set => SetProperty(_entityBase.PositionAndOrientation.Value.Position.X, value, () =>
            {
                    var pos = _entityBase.PositionAndOrientation.Value;
                    pos.Position.X = value;
                    _entityBase.PositionAndOrientation = pos;
                  
            }, nameof(PositionX));
        }

        [XmlIgnore]
        public double PositionY
        {
            get => _entityBase.PositionAndOrientation.Value.Position.Y;
            set => SetProperty(_entityBase.PositionAndOrientation.Value.Position.Y, value, () =>
              {
                      var pos = _entityBase.PositionAndOrientation.Value;
                      pos.Position.Y = value;
                      _entityBase.PositionAndOrientation = pos;
              }, nameof(PositionY));
        }

        [XmlIgnore]
        public double PositionZ
        {
            get => _entityBase.PositionAndOrientation.Value.Position.Z;
            set => SetProperty(_entityBase.PositionAndOrientation.Value.Position.Z, value, () =>
              {
                      var pos = _entityBase.PositionAndOrientation.Value;
                      pos.Position.Z = value;
                      _entityBase.PositionAndOrientation = pos;
              }, nameof(PositionZ));
        }

        public SerializableVector3D Position
        {
            get => new Vector3D(
                    _entityBase.PositionAndOrientation.Value.Position.Z,
                    _entityBase.PositionAndOrientation.Value.Position.Y,
                    _entityBase.PositionAndOrientation.Value.Position.X);
            set => SetProperty(_entityBase.PositionAndOrientation.Value.Position, value, () =>
              {
                  if (_entityBase.PositionAndOrientation.HasValue
                      && value.X != _entityBase.PositionAndOrientation.Value.Position.X
                      && value.Y != _entityBase.PositionAndOrientation.Value.Position.Y
                      && value.Z != _entityBase.PositionAndOrientation.Value.Position.Z)
                  {
                      MyPositionAndOrientation pos = _entityBase.PositionAndOrientation.Value;
                      pos.Position.X = value.X;
                      pos.Position.Y = value.Y;
                      pos.Position.Z = value.Z;

                      _entityBase.PositionAndOrientation = pos;
                  }
              }, nameof(Position));
        }

        [XmlIgnore]
        public ClassType ClassType
        {
            get => _classType;
            set => SetProperty(ref _classType, value, nameof(ClassType));
        }

        [XmlIgnore]
        public virtual string DisplayName
        {
            get => _name;
            set => SetProperty(ref _name, value, nameof(DisplayName));

        }

        [XmlIgnore]
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value, nameof(Description));

        }

        [XmlIgnore]
        public double PlayerDistance
        {
            get => _playerDistance;
            set => SetProperty(ref _playerDistance, value, nameof(PlayerDistance));

        }

        [XmlIgnore]
        public double Mass
        {
            get => _mass;
            set => SetProperty(ref _mass, value, nameof(Mass));
        }

        [XmlIgnore]
        public virtual int BlockCount
        {
            get => _blockCount;
            set => SetProperty(ref _blockCount, value, nameof(BlockCount));
        }

        [XmlIgnore]
        public virtual double LinearVelocity
        {
            get => _linearVelocity;
            set => SetProperty(ref _linearVelocity, value, nameof(LinearVelocity));
        }

        /// <summary>
        /// Center of the object in space.
        /// </summary>
        [XmlIgnore]
        public Vector3D Center
        {
            get => _center;
            set => SetProperty(ref _center, value, nameof(Center));
        }

        /// <summary>
        /// Bounding box.
        /// </summary>
        [XmlIgnore]
        public BoundingBoxD WorldAabb
        {
            get => _worldAabb;
            set => SetProperty(ref _worldAabb, value, nameof(WorldAabb));
        }

        public string SerializedEntity
        {
            get => _serializedEntity;
            set => SetProperty(ref _serializedEntity, value, nameof(SerializedEntity));
        }

        [XmlIgnore]
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

        [XmlIgnore]
        public bool IsValid
        {
            get => _isValid;

            set => SetProperty(ref _isValid, value, nameof(IsValid));
        }

        public double PlayerLocationX
        {
            get => _playerPosition.X;

            set => SetProperty(ref _playerPosition.X, value, nameof(PlayerLocationX), nameof(PlayerPosition));

        }

        public double PlayerLocationY
        {
            get => _playerPosition.Y;

            set => SetProperty(ref _playerPosition.Y, value, nameof(PlayerLocationY), nameof(PlayerPosition));

        }

        public double PlayerLocationZ
        {
            get => _playerPosition.Z;
            set => SetProperty(ref _playerPosition.Z, value, nameof(PlayerLocationZ), nameof(PlayerPosition));

        }

        public Vector3D PlayerPosition
        {
            get => _playerPosition;
            set => SetProperty(ref _playerPosition, value, nameof(PlayerPosition));
        }

        public Vector3D PlayerLocation
        {
            get => _playerPosition;
            set => SetProperty(ref _playerPosition, value,
                                nameof(PlayerLocation),
                                nameof(PlayerLocationX),
                                nameof(PlayerLocationY),
                                nameof(PlayerLocationZ),
                                nameof(PlayerPosition)
                                );
        }

        public string SourceVoxelFilePath
        {
            get => _sourceVoxelFilePath;
            set => SetProperty(ref _sourceVoxelFilePath, value, nameof(SourceVoxelFilePath));
        }

        Vector3D IStructureBase.Position
        {
            get => Position;
            set => Position = value;
        }

        #endregion

        #region Methods


        public virtual void UpdateGeneralFromEntityBase()
        {
            ClassType = ClassType.Unknown;
        }

        public static IStructureBase Create(MyObjectBuilder_EntityBase entityBase, string savefilePath)
        {
            return entityBase switch
            {
                MyObjectBuilder_Planet _ => new StructurePlanetModel(entityBase, savefilePath),
                MyObjectBuilder_VoxelMap _ => new StructureVoxelModel(entityBase, savefilePath),
                MyObjectBuilder_Character _ => new StructureCharacterModel(entityBase),
                MyObjectBuilder_CubeGrid _ => new StructureCubeGridModel(entityBase),
                MyObjectBuilder_FloatingObject _ => new StructureFloatingObjectModel(entityBase),
                MyObjectBuilder_Meteor _ => new StructureMeteorModel(entityBase),
                MyObjectBuilder_InventoryBagEntity _ => new StructureInventoryBagModel(entityBase),
               _ => new StructureUnknownModel(entityBase) ?? throw new NotImplementedException($"A new object has not been catered for in the StructureBase, of type '{entityBase.GetType()}'.")
            };
        }

        public virtual void InitializeAsync()
        {
            // to be overridden.
        }

        public virtual void CancelAsync()
        {
            // to be overridden.
        }

        public virtual void RecalcPosition(Vector3D playerPosition)
        {
            PlayerDistance = (playerPosition - PositionAndOrientation.Value.Position).Length();
        }

        #endregion
    }
}
