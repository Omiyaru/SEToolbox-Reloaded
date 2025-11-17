using SEToolbox.Models;
using VRage.Game;

namespace SEToolbox.ViewModels
{
    public class StructureFloatingObjectViewModel : StructureBaseViewModel<StructureFloatingObjectModel>
    {
        #region Ctor

        public StructureFloatingObjectViewModel(BaseViewModel parentViewModel, StructureFloatingObjectModel dataModel)
            : base(parentViewModel, dataModel)
        {
            // Will bubble property change events from the Model to the ViewModel.
            DataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Properties

        protected new StructureFloatingObjectModel DataModel
        {
            get => base.DataModel as StructureFloatingObjectModel;
        }

        public MyObjectBuilder_InventoryItem Item
        {
            get => DataModel.Item;

            set => DataModel.Item = value;
        }

        public string SubTypeName
        {
            get  => DataModel.Item.PhysicalContent.SubtypeName; 
        }

        public double? Volume
        {
            get => DataModel.Volume;

            set => DataModel.Volume = value;
        }

        public decimal? Units
        {
            get => DataModel.Units;

            set => DataModel.Units = value;
        }

        #endregion
    }
}
