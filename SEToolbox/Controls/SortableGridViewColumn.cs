using System.Windows.Data;
using System.ComponentModel;
using SEToolbox.Models;
namespace SEToolbox.Controls
{
    public class SortableGridViewColumn : System.Windows.Controls.GridViewColumn
    {
        #region Fields

        private BindingBase _sortBinding;
        private BaseModel _model = new();

        #endregion

        #region SortBinding

        public BindingBase SortBinding
        {
            get => _sortBinding;
            set => _model.SetValue(ref _sortBinding, value, () =>
                   OnDisplayMemberBindingChanged());
        }
        

        private void OnDisplayMemberBindingChanged()
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(DisplayMemberBinding)));
        }

        #endregion
    }
}
