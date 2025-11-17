using SEToolbox.Interfaces;
using SEToolbox.Services;

namespace SEToolbox.ViewModels
{

    public class DataBaseViewModel(BaseViewModel parentViewModel, IStructureBase dataModel) : BaseViewModel(parentViewModel), IDragable
    {
        #region Fields

        private IStructureBase _dataModel = dataModel;

        #endregion
        #region Ctor

        #endregion

        #region Properties

        public IStructureBase DataModel
        {
            get => _dataModel;

            set => SetProperty(ref _dataModel, value, nameof(DataModel));
        }
        #endregion

        #region IDragable Interface

        //[XmlIgnore]
        System.Type IDragable.DataType
        {
            get => typeof(DataBaseViewModel);
        }

        void IDragable.Remove(object i)
        {

        }

        #endregion
    }
}