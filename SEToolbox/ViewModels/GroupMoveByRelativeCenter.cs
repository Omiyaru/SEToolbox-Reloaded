//using SEToolbox.Interfaces;
//using SEToolbox.Models;

//using System.Collections.ObjectModel;
//using System.Windows.Input;

//using VRageMath;

//namespace SEToolbox.Models
//{
//    public class GroupMoveByRelativeCenter
//    {
//        private object _dataModel;
//        private GroupMoveByRelativeCenter.Group group;
//        #region Fields
//        private ObservableCollection<GroupMoveItemModel> Group { get; set; }

//         #endregion

//        #region Ctor

//        public GroupMoveByRelativeCenterModel()
//        {
//            GlobalOffsetPositionX = 0f;
//            GlobalOffsetPositionY = 0f;
//            GlobalOffsetPositionZ = 0f;
//        }

//        #endregion

//        #region Properties
//        public ObservableCollection<GroupMoveItemModel> Selections
//        {
//            get => _dataModel.Selections;

//            set
//            {
//                if (value != _dataModel.Selections)
//                {
//                    _dataModel.Selections = value;
//                    OnPropertyChanged(nameof(Selections));
//                }
//            }
//        }

//        public GroupMoveByRelativeCenter(ObservableCollection<GroupMoveItemModel> group)
//        {
//            this.group = Group;
//        }

//        public void MoveByRelativeCenter(float offsetX, float offsetY, float offsetZ)
//        {
//            // Calculate the center of the group
//            Vector3D center = CalculateGroupCenter();

//            // Move each object in the group by the relative movement
//            foreach (GroupMoveItemModel selection in Selections)
//            {

//                selection.OffsetPositionX += offsetX;
//                selection.OffsetPositionY += offsetY;
//                selection.OffsetPositionZ += offsetZ;

//            }

//            // Update the center of the group
//            center.X += offsetX;
//            center.Y += offsetY;
//            center.Z += offsetZ;
//        }

//        public ObservableCollection<GroupMoveItemModel> Selections
//        {
//            get => _dataModel.Selections;

//            set
//            {
//                if (value != _dataModel.Selections)
//                {
//                    _dataModel.Selections = value;
//                    OnPropertyChanged(nameof(Selections));
//                }
//            }
//        }
//        public Vector3D CenterPosition
//        {
//            get => _centerPosition;
//            set
//            {
//                if (value != _centerPosition)
//                {
//                    _centerPosition = value;
//                    OnPropertyChanged(nameof(CenterPosition));
//                }
//            }
//        }

//        public float GlobalOffsetPositionX { get; }
//        public float GlobalOffsetPositionY { get; }
//        public float GlobalOffsetPositionZ { get; }

//

//        #endregion

//        #region Methods

//        public void Load(ObservableCollection<GroupMoveItemModel> selections, Vector3D position, Vector3D playerPosition)
//        {
//            public void Load(ObservableCollection<IStructureViewBase> selections, )
//        {
//            Selections = [];
//            _playerPosition = playerPosition;

//            CenterPosition = CalculateGroupCenter(position);
//        }

//        public void ApplyNewPositions()
//        {
//            foreach (GroupMoveItemModel selection in Selections)
//            {
//                selection.PositionX += GlobalOffsetPositionX;
//                selection.PositionY += GlobalOffsetPositionY;
//                selection.PositionZ += GlobalOffsetPositionZ;
//            }
//        }

//        private Vector3D CalculateGroupCenter(Vector3D position)
//        {
//            Vector3D center = Vector3D.Zero;
//            foreach (GroupMoveItemModel item in Selections)
//            {
//                center += new Vector3D(item.PositionX, item.PositionY, item.PositionZ);
//            }
//            center /= Selections.Count;
//            return center;
//        }

//        private class Group
//        {
//        }

//        #endregion
//    }
//}
//}