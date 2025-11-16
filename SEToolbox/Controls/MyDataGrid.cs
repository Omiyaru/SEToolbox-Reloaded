using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using SEToolbox.Support;

namespace SEToolbox.Controls
{
    /// <summary>
    /// Provides one click editing on data cells.
    /// </summary>
    public class MyDataGrid : DataGrid
    {
 
        public MyDataGrid()
        {
            // Fix the event handler to respond correctly when clicking on dropdown list items.
            PreviewMouseLeftButtonDown += MDG_PreviewMouseLeftButtonDown;
        }

        void MDG_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var cell = dataGrid.GetHitControl<DataGridCell>(e);

            if (cell == null)
                return;

            if (!cell.IsEditing && !cell.IsReadOnly)
            {
                var content = cell.Content;
                if (content is ComboBox)
                {
                    var comboBox = content as ComboBox;
                    if (comboBox.IsDropDownOpen)
                        return;
                }
                cell.Focus();
            }
            var row = cell.FindVisualParent<DataGridRow>();
            if (row != null)
            {
                if (!row.IsSelected)
                {
                    row.IsSelected = true;
                }
            }
            if (!cell.IsSelected)
            {
                cell.IsSelected = true;
            }
        }
    }
}

