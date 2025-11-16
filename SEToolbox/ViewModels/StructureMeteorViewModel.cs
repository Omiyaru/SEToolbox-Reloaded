using System.ComponentModel;
using System.Windows.Input;
using SEToolbox.Models;
using SEToolbox.Services;
using VRage.Game;

namespace SEToolbox.ViewModels
{
    public class StructureMeteorViewModel : StructureBaseViewModel<StructureMeteorModel>
    {
        #region Ctor

        public StructureMeteorViewModel(BaseViewModel parentViewModel, StructureMeteorModel dataModel)
            : base(parentViewModel, dataModel)
        {
            DataModel.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                // Will bubble property change events from the Model to the ViewModel.
                OnPropertyChanged(e.PropertyName);
            };
        }

        #endregion

        #region Command Properties

        public ICommand ResetVelocityCommand
        {
            get => new DelegateCommand(ResetVelocityExecuted, ResetVelocityCanExecute); 
        }

        public ICommand ReverseVelocityCommand
        {
            get => new DelegateCommand(ReverseVelocityExecuted, ReverseVelocityCanExecute); 
        }

        public ICommand MaxVelocityAtPlayerCommand
        {
            get => new DelegateCommand(MaxVelocityAtPlayerExecuted, MaxVelocityAtPlayerCanExecute);
        }

        #endregion

        #region Properties

        protected new StructureMeteorModel DataModel
        {
            get => base.DataModel as StructureMeteorModel;
        }

        public MyObjectBuilder_InventoryItem Item
        {
            get => DataModel.Item;
            set => DataModel.Item = value;
        }

        public string SubTypeName
        {
            get => DataModel.Item.PhysicalContent.SubtypeName; 
        }

        public double? Volume
        {
            get => DataModel.Volume;
            set => DataModel.Volume = value;
        }

        public override double LinearVelocity
        {
            get => DataModel.LinearVelocity; 
        }

        public float Integrity
        {
            get => DataModel.Integrity;
        }

        #endregion

        #region Methods
        
         public bool ResetVelocityCanExecute()
        {
            return DataModel.LinearVelocity != 0f || DataModel.AngularVelocity != 0f;
        }

        public void ResetVelocityExecuted()
        {
            DataModel.ResetVelocity();
            MainViewModel.IsModified = true;
        }

        public bool ReverseVelocityCanExecute()
        {
            return DataModel.LinearVelocity != 0f || DataModel.AngularVelocity != 0f;
        }

        public void ReverseVelocityExecuted()
        {
            DataModel.ReverseVelocity();
            MainViewModel.IsModified = true;
        }

        public bool MaxVelocityAtPlayerCanExecute()
        {
            return MainViewModel.ThePlayerCharacter != null;
        }

        public void MaxVelocityAtPlayerExecuted()
        {
            var position = MainViewModel.ThePlayerCharacter.PositionAndOrientation.Value.Position;
            DataModel.MaxVelocityAtPlayer(position);
            MainViewModel.IsModified = true;
        }

        #endregion
    }
}
