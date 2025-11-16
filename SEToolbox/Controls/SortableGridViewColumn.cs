using System.Windows.Data;
using System.ComponentModel;
namespace SEToolbox.Controls
{
    public class SortableGridViewColumn : System.Windows.Controls.GridViewColumn
    {
        #region Fields

        private BindingBase _sortBinding;

        #endregion

        #region SortBinding

        public BindingBase SortBinding
        {
            get => _sortBinding;
            set
            {
                if (_sortBinding != value)
                {
                    _sortBinding = value;
                    OnDisplayMemberBindingChanged();
                }
            }
        }
   

        private void OnDisplayMemberBindingChanged()
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(DisplayMemberBinding)));
        }

        #endregion
    }
}
