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
            PreviewMouseLeftButtonDown += MDG_PreviewMouseLeftButtonDown;
        }

        void MDG_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var cell = dataGrid.GetHitControl<DataGridCell>(e);

            if (cell is { IsReadOnly: false, IsEditing: false })
            {
                cell?.Focus();
                dataGrid = cell?.FindVisualParent<DataGrid>();
                var row = cell?.FindVisualParent<DataGridRow>();

                if (dataGrid?.SelectionUnit != DataGridSelectionUnit.FullRow)
                {
                    cell?.IsSelected = !cell.IsSelected;
                }
                else
                {
                    row?.IsSelected = !row.IsSelected;
                }
            }
        }
    }
}

