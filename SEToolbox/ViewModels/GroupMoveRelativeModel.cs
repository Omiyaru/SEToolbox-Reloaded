// using SEToolbox.Interfaces;
// using SEToolbox.Models;
// using System;
// using System.Collections.ObjectModel;
// using System.Windows.Input;

// using VRageMath;





// namespace SEToolbox.Models
// {
//     public class GroupMoveByRelativeCenter
//     {
//         #region Fields
//         private ObservableCollection<GroupMoveItemModel> Group { get; set; }

//         private Vector3 _playerPosition;

//         public bool GroupMove { get; private set; }

//         private ObservableCollection<GroupMoveItemModel> group;
//         private GroupMoveToNewPositionModel _dataModel;
//         #endregion

//         #region Ctor

//         public GroupMoveByRelative( ObservableCollection<GroupMoveItemModel> group, GroupMoveToNewPositionModel dataModel)
//         {
//             GlobalOffsetPositionX = 0f;
//             GlobalOffsetPositionY = 0f;
//             GlobalOffsetPositionZ = 0f;
//         }

//         #endregion

//         #region Properties
//         public ObservableCollection<GroupMoveItemModel> Selections
//         {
//             get => _dataModel.Selections;

//             set
//             {
//                 if (value != _dataModel.Selections)
//                 {
//                     _dataModel.Selections = value;
//                     OnPropertyChanged(nameof(Selections));
//                 }
//             }
//         }

//         public Vector3D PlayerPosition
//         {
//             get => _playerPosition;
//             set => _playerPosition = value;
//         }

//         /// <summary>
//         /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
//         /// </summary>
//         public bool IsBusy
//         {
//             get;

//             set
//             {
//                 if (value != field)
//                 {
//                     field = value;
//                     OnPropertyChanged(nameof(IsBusy));
//                     if (field)
//                     {
//                         System.Windows.Forms.Application.DoEvents();
//                     }
//                 }
//             }
//         }


//         #endregion
//         #region Methods
//         public GroupMoveToNewPosition(ObservableCollection<GroupMoveItemModel> group) => this.group = Group;

//         public void MoveRelative(float offsetX, float offsetY, float offsetZ)
//         {
//             // Calculate the center of the group
//             Vector3D center = CalculateGroupCenter();

//             // Move each object in the group by the relative movement
//             foreach (GroupMoveItemModel selection in Selections)
//             {

//                 selection.OffsetPositionX += offsetX;
//                 selection.OffsetPositionY += offsetY;
//                 selection.OffsetPositionZ += offsetZ;

//             }

//             // Update the center of the group
//             center.X += offsetX;
//             center.Y += offsetY;
//             center.Z += offsetZ;
//         }


//         public Vector3D CenterPosition
//         {
//             get => _centerPosition;
//             set
//             {
//                 if (value != _centerPosition)
//                 {
//                     _centerPosition = value;
//                     OnPropertyChanged(nameof(CenterPosition));
//                 }
//             }
//         }

//         public float GlobalOffsetPositionX { get; private set; }
//         public float GlobalOffsetPositionY { get; private set; }
//         public float GlobalOffsetPositionZ { get; }

//         private Vector3D CalculateGroupCenter()
//         {
//             Vector3D center = Vector3D.Zero;
//             foreach (GroupMoveItemModel item in Group)
//             {
//                 center += new Vector3D(item.PositionX, item.PositionY, item.PositionZ);
//             }
//             center /= group.Count;
//             return center;
//         }

//         #endregion

//         #region Methods

//         public void Load(ObservableCollection<IStructureViewBase> selections, Vector3D position, Vector3D playerPosition)

//         {
//             Selections = [];
//             _playerPosition = playerPosition;
//             GroupMove = true;
//             CenterPosition = CalculateGroupCenter(position);

//             foreach (IStructureViewBase selection in selections)
//             {
//                 Selections.Add(new GroupMoveItemModel
//                 {
//                     Item = selection,
//                     PositionX = selection.DataModel.PositionX,
//                     PositionY = selection.DataModel.PositionY,
//                     PositionZ = selection.DataModel.PositionZ,
//                     PlayerLocation = new Vector3D(selection.PositionX, selection.PositionY, selection.PositionZ),
//                 });
//             }
//         }
//         #endregion

//         #region Helpers


//         private Vector3D CalculateGroupCenter(Vector3D position)
//         {
//             Vector3D center = Vector3D.Zero;
//             foreach (IStructureViewBase item in Selections)
//             {
//                 center += new Vector3D(item.PositionX, item.PositionY, item.PositionZ);
//             }
//             item.PlayerDistance = (_playerPosition - new Vector3D(item.PositionX, selection.PositionY, selection.PositionZ)).Length();
//             center /= Selections.Count;
//             return center;
//         }

//         public void ApplyNewPositions()
//         {
//             foreach (IStructureViewBase selection in Selections)
//             {

//                 selection.PositionX += GlobalOffsetPositionX;
//                 selection.PositionY += GlobalOffsetPositionY;
//                 selection.PositionZ += GlobalOffsetPositionZ;
//             }
//         }

//         #endregion
//     }
// }
