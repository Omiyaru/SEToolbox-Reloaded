using SEToolbox.Models;

namespace SEToolbox.ViewModels
{
    public class StructureInventoryBagViewModel : StructureBaseViewModel<StructureInventoryBagModel>
    {
        #region Ctor

        public StructureInventoryBagViewModel(BaseViewModel parentViewModel, StructureInventoryBagModel dataModel)
            : base(parentViewModel, dataModel)
        {
            // Will bubble property change events from the Model to the ViewModel.
            DataModel.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
        }

        #endregion

        #region Properties

        protected new StructureInventoryBagModel DataModel
        {
            get => base.DataModel as StructureInventoryBagModel;
        }

        #endregion
    }
}
