using System.Windows.Data;
using System.ComponentModel;
using SEToolbox.Models;
namespace SEToolbox.Controls
{
    public class SortableGridViewColumn : System.Windows.Controls.GridViewColumn
    {
        #region Fields

        private BindingBase _sortBinding;
        private BaseModel _model;

        #endregion

        #region SortBinding

        public BindingBase SortBinding
        {
            get => _sortBinding;
            set => _model.SetProperty(ref _sortBinding, ()=> OnDisplayMemberBindingChanged());
        }
        
   

        private void OnDisplayMemberBindingChanged()
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(DisplayMemberBinding)));
        }

        #endregion
    }
}
