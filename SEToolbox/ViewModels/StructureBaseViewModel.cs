using System.Collections.Generic;

using SEToolbox.Interfaces;
using SEToolbox.Interop;
using VRage;


namespace SEToolbox.ViewModels
{
    public abstract class StructureBaseViewModel<TModel>(BaseViewModel parentViewModel,
                                                         TModel dataModel) : DataBaseViewModel(parentViewModel, dataModel),
                                                        IStructureViewBase where TModel : IStructureBase
    {
        #region Fields

        private bool _isSelected;

        #endregion
        #region Ctor

        #endregion

        #region Properties

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, nameof(IsSelected));
        }
        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => DataModel.IsBusy;
            set => DataModel.IsBusy = value;
        }

        public long EntityId
        {
            get => DataModel.EntityId;
            set => SetProperty( DataModel.EntityId, nameof(EntityId));
        }

        public MyPositionAndOrientation? PositionAndOrientation
        {
            get => DataModel.PositionAndOrientation;
            set => SetProperty( DataModel.PositionAndOrientation, nameof(PositionAndOrientation));
        }

        public ClassType ClassType
        {
            get => DataModel.ClassType;
            set => SetProperty( DataModel.ClassType, nameof(ClassType));
        }

        public string DisplayName
        {
            get => DataModel.DisplayName;
            set
            {
                DataModel.DisplayName = value;
                MainViewModel.IsModified = true;
            }
        }

        public string Description
        {
            get => DataModel.Description;
            set => DataModel.Description = value;
        }

        public double PlayerDistance
        {
            get => DataModel.PlayerDistance;
            set => DataModel.PlayerDistance = value;
        }

        public double Mass
        {
            get => DataModel.Mass;
            set => DataModel.Mass = value;
        }

        public int BlockCount
        {
            get => DataModel.BlockCount;
            set => DataModel.BlockCount = value;
        }

        public virtual double LinearVelocity
        {
            get => DataModel.LinearVelocity;
            set => DataModel.LinearVelocity = value;
        }

        public double PositionX
        {
            get => DataModel.PositionX;
            set
            {
                DataModel.PositionX = value;
                MainViewModel.IsModified = true;
                MainViewModel.CalcDistances();
            }
        }

        public double PositionY
        {
            get => DataModel.PositionY;
            set
            {
                DataModel.PositionY = value;
                MainViewModel.IsModified = true;
                MainViewModel.CalcDistances();
            }
        }

        public double PositionZ
        {
            get => DataModel.PositionZ;
            set
            {
                DataModel.PositionZ = value;
                MainViewModel.IsModified = true;
                MainViewModel.CalcDistances();
            }
        }

        #endregion
    }
}
