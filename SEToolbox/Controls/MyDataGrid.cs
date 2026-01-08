using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using SEToolbox.Support;
using System;

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
            

            if (cell is { IsReadOnly: false, IsEditing: false } and not null)
            {
                cell?.Focus();
                var parentDataGrid = cell.FindVisualParent<DataGrid>();
                _ = parentDataGrid?.SelectionUnit == DataGridSelectionUnit.FullRow ? cell.FindVisualParent<DataGridRow>()?.IsSelected = !cell.FindVisualParent<DataGridRow>().IsSelected
                                                                                    : cell.IsSelected = !cell.IsSelected;

            }
        }
    }
}

