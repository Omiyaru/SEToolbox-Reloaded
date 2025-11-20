using System.Collections.ObjectModel;
using SEToolbox.Interfaces;
using VRageMath;

namespace SEToolbox.Models
{
    public class GroupMoveModel : BaseModel
    {
        #region Fields

        private ObservableCollection<GroupMoveItemModel> _selections;
        private Vector3 _playerPosition;

        private float _globalOffsetPositionX;
        private float _globalOffsetPositionY;
        private float _globalOffsetPositionZ;
        private bool _isGlobalOffsetPosition;

        private float _singlePositionX;
        private float _singlePositionY;
        private float _singlePositionZ;
        private bool _isSinglePosition;

        private bool _isBusy;
        internal bool _isRelativePosition;
        private Vector3D _centerPosition;

        #endregion

        #region Ctor

        public GroupMoveModel()
        {
            GlobalOffsetPositionX = 0f;
            GlobalOffsetPositionY = 0f;
            GlobalOffsetPositionZ = 0f;
        }

        #endregion

        #region Properties

        public ObservableCollection<GroupMoveItemModel> Selections
        {
            get => _selections;
            set => SetProperty(ref _selections, nameof(Selections));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set 
            {
                 SetProperty(ref _isBusy, nameof(IsBusy));
           
                    if (_isBusy)
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }
             
            }
        }

        public float GlobalOffsetPositionX
        {
            get => _globalOffsetPositionX;
            set => SetProperty(ref _globalOffsetPositionX, nameof(GlobalOffsetPositionX));
        }

        public float GlobalOffsetPositionY
        {
            get => _globalOffsetPositionY;
            set => SetProperty(ref _globalOffsetPositionY, nameof(GlobalOffsetPositionY));
        }

        public float GlobalOffsetPositionZ
        {
            get => _globalOffsetPositionZ;
            set => SetProperty(ref _globalOffsetPositionZ, nameof(GlobalOffsetPositionZ));
        }

        public bool IsGlobalOffsetPosition
        {
            get => _isGlobalOffsetPosition;
            set => SetProperty(ref _isGlobalOffsetPosition, nameof(IsGlobalOffsetPosition));
        }

        public float SinglePositionX
        {
            get => _singlePositionX;
            set => SetProperty(ref _singlePositionX, nameof(SinglePositionX));
        }

        public float SinglePositionY
        {
            get => _singlePositionY;
            set => SetProperty(ref _singlePositionY, nameof(SinglePositionY));
        }

        public float SinglePositionZ
        {
            get => _singlePositionZ;
            set => SetProperty(ref _singlePositionZ, nameof(SinglePositionZ));
        }

        public bool IsSinglePosition
        {
            get => _isSinglePosition;
            set => SetProperty(ref _isSinglePosition, nameof(IsSinglePosition));
        }

        public Vector3D CenterPosition
        {
            get => _centerPosition;
            set => SetProperty(ref _centerPosition, nameof(CenterPosition));
        }

        public bool IsRelativePosition
        {
            get => _isRelativePosition;
            set => SetProperty(ref _isRelativePosition, nameof(IsRelativePosition));
        }
       
        #endregion

        #region Methods
        public void Load(ObservableCollection<IStructureViewBase> selections, Vector3D playerPosition, bool IsRelativePosition = false, Vector3D centerPosition = default)
        {
            Selections = [];
            _playerPosition = playerPosition;
            IsGlobalOffsetPosition = true;

            foreach (IStructureViewBase selection in selections)
            {
                Selections.Add(new GroupMoveItemModel
                {
                    Item = selection,
                    PositionX = selection.DataModel.PositionX,
                    PositionY = selection.DataModel.PositionY,
                    PositionZ = selection.DataModel.PositionZ,
                    PlayerDistance = selection.DataModel.PlayerDistance
                });
            }

            if (IsRelativePosition)
            {
                // Calculate the center of the group
                Vector3D Center = CalculateGroupCenter(centerPosition);

                // Move the group itself to a new position
                foreach (GroupMoveItemModel item in Selections)
                {
                    if (item == null)
                        continue;

                    item.PositionX = item.PositionX + centerPosition.X - Center.X;
                    item.PositionY = item.PositionY + centerPosition.Y - Center.Y;
                    item.PositionZ = item.PositionZ + centerPosition.Z - Center.Z;
                }
            }
        }
        
        public Vector3D CalculateGroupCenter( Vector3D centerPosition)
        {
            if (Selections == null || Selections.Count == 0)
                return centerPosition;

            Vector3D center = Vector3D.Zero;
            foreach (GroupMoveItemModel item in Selections)
            {
                if (item == null)
                    continue;

                center += new Vector3D(item.PositionX, item.PositionY, item.PositionZ);
            }
            center /= Selections.Count;
            return center;
        }

        #endregion

        #region Helpers

        public void CalcOffsetDistances()
        {
            foreach (GroupMoveItemModel selection in Selections)
            {
                if (IsGlobalOffsetPosition)
                {
                    // Apply a Global Offset to all objects.
                    selection.PositionX = selection.Item.DataModel.PositionX + GlobalOffsetPositionX;
                    selection.PositionY = selection.Item.DataModel.PositionY + GlobalOffsetPositionY;
                    selection.PositionZ = selection.Item.DataModel.PositionZ + GlobalOffsetPositionZ;
                }

                if (IsSinglePosition)
                {
                    // Apply a Single Position to all objects.
                    selection.PositionX = SinglePositionX;
                    selection.PositionY = SinglePositionY;
                    selection.PositionZ = SinglePositionZ;
                }

                if (IsRelativePosition)
                {
                    foreach (GroupMoveItemModel item in Selections)
                    {
                        if (selection.Item.DataModel != null)
                        {
                            item.PositionX = selection.Item.DataModel.PositionX = CenterPosition.X;
                            item.PositionY = selection.Item.DataModel.PositionY = CenterPosition.Y;
                            selection.PositionZ = selection.Item.DataModel.PositionX = CenterPosition.Z;
                        }

                        selection.PlayerDistance = (_playerPosition - new Vector3D(selection.PositionX, selection.PositionY, selection.PositionZ)).Length();

                    }
                }
            }
        }

        public void ApplyNewPositions()
        {
            foreach (GroupMoveItemModel selection in Selections)
            {
                selection.Item.DataModel.PositionX = selection.PositionX;
                selection.Item.DataModel.PositionY = selection.PositionY;
                selection.Item.DataModel.PositionZ = selection.PositionZ;
            }
        }

        #endregion
    }
}
