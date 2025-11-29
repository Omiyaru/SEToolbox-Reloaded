using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using SEToolbox.Support;
using Binding = System.Windows.Data.Binding;
using Primitives = System.Windows.Controls.Primitives;

namespace SEToolbox.Controls
{
    public class SortableListView : ListView
    {
        private const int MaxSortableColumns = 1;
        #region Fields
        private List<SortColumn> _sortList = [];
        #endregion

        #region Dependency Properties

        #region ColumnHeaderArrowUpTemplate
        public static readonly DependencyProperty ColumnHeaderArrowUpTemplateProperty =
            DependencyProperty.Register(nameof(ColumnHeaderArrowUpTemplate), typeof(DataTemplate), typeof(SortableListView));

        public DataTemplate ColumnHeaderArrowUpTemplate
        {
            get => (DataTemplate)GetValue(ColumnHeaderArrowUpTemplateProperty);
            set => SetValue(ColumnHeaderArrowUpTemplateProperty, value);
        }
        #endregion

        #region ColumnHeaderArrowDownTemplate

        public static readonly DependencyProperty ColumnHeaderArrowDownTemplateProperty =
            DependencyProperty.Register(nameof(ColumnHeaderArrowDownTemplate), typeof(DataTemplate), typeof(SortableListView));

        public DataTemplate ColumnHeaderArrowDownTemplate
        {
            get => (DataTemplate)GetValue(ColumnHeaderArrowDownTemplateProperty);
            set => SetValue(ColumnHeaderArrowDownTemplateProperty, value);
        }
        #endregion
        
        #endregion

        public static readonly DependencyProperty DefaultSortColumnProperty =
            DependencyProperty.Register(nameof(DefaultSortColumn), typeof(string), typeof(SortableListView));

        public string DefaultSortColumn
        {
            get => (string)GetValue(DefaultSortColumnProperty);
            set => SetValue(DefaultSortColumnProperty, value);
        }

        public static readonly RoutedEvent MouseDoubleClickItemEvent =
            EventManager.RegisterRoutedEvent(nameof(MouseDoubleClickItem), RoutingStrategy.Direct, typeof(MouseButtonEventHandler), typeof(SortableListView));

        public static event MouseButtonEventHandler MouseDoubleClickItem;

        
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // add the event handler to the GridViewColumnHeader. This strongly ties this ListView to a GridView.
            AddHandler(Primitives.ButtonBase.ClickEvent, new RoutedEventHandler(GridViewColumnHeaderClickedHandler));
            AddHandler(MouseDoubleClickEvent, new RoutedEventHandler(MouseDoubleClickedHandler));
        }


        private static void MouseDoubleClickedHandler(object sender, RoutedEventArgs e)
        {
            var listView = ((ListView)sender).GetHitControl<ListViewItem>((MouseEventArgs)e);
            if (listView != null)
            {
                MouseDoubleClickItem?.Invoke(sender, e as MouseButtonEventArgs);
            }
        }


        #region  Methods
        // protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        // {
        //     base.OnItemsSourceChanged(oldValue, newValue);
        //     if (ItemsSource != null)
        //     {
        //         ICollectionView dataView = CollectionViewSource.GetDefaultView(ItemsSource);
        //         if (dataView.SortDescriptions.Count == 0 && _sortList.Any())
        //         {
        //             foreach (SortColumn sortColumn in _sortList)
        //             {
        //                 dataView.SortDescriptions.Add(new SortDescription(sortColumn.SortPath, sortColumn.SortDirection));
        //             }
        //             dataView.Refresh();
        //         }
        //     }
        // }

        private static bool IsMatchingColumn(SortableGridViewColumn column, string sortColumn)
        {
            return column switch
            {
                { SortBinding: Binding binding } => binding.Path.Path == sortColumn,
                { DisplayMemberBinding: Binding displayBinding } => displayBinding.Path.Path == sortColumn,
                _ => column.Header.ToString() == sortColumn
            };
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            SortByDefault();
        }

        private void SortByDefault()
        {

            if (DefaultSortColumn == null || View is not GridView gridView)
                return;

            GridViewColumn selectedColumn = FindColumnToSort(gridView.Columns);

            if (selectedColumn != null)
            {
                _sortList.Clear();
                _sortList.Add(new SortColumn(DefaultSortColumn, ListSortDirection.Ascending, selectedColumn));
                Sort(this, _sortList);

                if (ColumnHeaderArrowUpTemplate != null)
                    selectedColumn.HeaderTemplate = ColumnHeaderArrowUpTemplate;
            }
        }

        GridViewColumn FindColumnToSort(IList<GridViewColumn> columns)
        {
            var column = columns.FirstOrDefault(c =>
                c is SortableGridViewColumn sortableColumn && IsMatchingColumn(sortableColumn, DefaultSortColumn) ||
                c.DisplayMemberBinding is Binding binding && binding.Path.Path == DefaultSortColumn ||
                c.Header.ToString() == DefaultSortColumn);

            return column;
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            var headerClicked = sender as GridViewColumnHeader;
            var listView = headerClicked.FindVisualParent<ListView>();
            var headerPaths = GetHeaderPaths(headerClicked);

            if (e.OriginalSource is not GridViewColumnHeader || headerClicked.Role == GridViewColumnHeaderRole.Padding || headerClicked.Column is null || headerPaths.Count == 0)
            {
                return;
            }

            var oldItem = _sortList.FirstOrDefault(i => i.SortPath == headerPaths[0] || ReferenceEquals(i.Column, headerClicked.Column));

            ListSortDirection direction;
            if (oldItem == null)
            {
                direction = ListSortDirection.Ascending;
            }
            else if (oldItem.Column.Equals(headerClicked.Column))
            {
                direction = oldItem.SortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            else
            {
                _sortList.RemoveAll(i => i.SortPath == headerPaths[0] || ReferenceEquals(i.Column, headerClicked.Column));

                if (headerClicked.Column != null)
                {
                    headerClicked.Column.HeaderTemplate = null;
                }

                Sort(listView, _sortList);
                return;
            }

            UpdateHeaderTemplate(headerClicked, direction);
            UpdateSortList(headerClicked, direction, headerPaths);
            Sort(listView, _sortList);
        }

        private static List<string> GetHeaderPaths(GridViewColumnHeader headerClicked)
        {
            if (headerClicked.Column is not SortableGridViewColumn sortableColumn)
            {
                return
                [
                    headerClicked.Column.DisplayMemberBinding is Binding displayBinding
                        ? displayBinding.Path.Path
                        : headerClicked.Column.Header as string
                ];
            }

            var headerPaths = new List<string>();
            if (sortableColumn.SortBinding is MultiBinding multiBinding)
            {
                foreach (var binding in multiBinding.Bindings.OfType<Binding>())
                {
                    headerPaths.Add(binding.Path.Path);
                }
            }
            else if (sortableColumn.SortBinding is Binding binding)
            {
                headerPaths.Add(binding.Path.Path);
            }

            return headerPaths;
        }

        private void UpdateSortList(GridViewColumnHeader headerClicked, ListSortDirection direction, List<string> headerPaths)
        {
            _sortList.RemoveAll(sortColumn => sortColumn.Column == null || headerClicked.Column.Equals(sortColumn.Column));

            while (_sortList.Count > MaxSortableColumns)
            {
                _sortList[_sortList.Count - 1].Column.HeaderTemplate = null;
                _sortList.RemoveAt(_sortList.Count - 1);
            }

            _sortList.ForEach(sortColumn => sortColumn.Column.HeaderTemplate = null);
            foreach (string colPath in headerPaths)
            {
                _sortList.Insert(0, new SortColumn(colPath, direction, headerClicked.Column));
            }

            _sortList = [.. _sortList.GroupBy(sortColumn => sortColumn.Column).Select(g => g.First())];
        }

        private static void Sort(ItemsControl listView, List<SortColumn> sortList)
        {
            if (listView.ItemsSource is not ICollectionView dataView)
            {
                return;
            }
            dataView.SortDescriptions.Clear();
            foreach (SortColumn sortColumn in sortList)
            {
                dataView.SortDescriptions.Add(new SortDescription(sortColumn.SortPath, sortColumn.SortDirection));
            }
            if (dataView.SortDescriptions.Count > 0)
            {
                dataView.Refresh();
            }
        }

        private void UpdateHeaderTemplate(GridViewColumnHeader headerClicked, ListSortDirection direction)
        {
            headerClicked.Column.HeaderTemplate = direction == ListSortDirection.Ascending ? ColumnHeaderArrowUpTemplate : ColumnHeaderArrowDownTemplate;
        }

        public class SortColumn(string sortPath, ListSortDirection direction, GridViewColumn gridViewColumn)
        {
            public string SortPath { get; set; } = sortPath;
            public ListSortDirection SortDirection { get; set; } = direction;
            public GridViewColumn Column { get; set; } = gridViewColumn;
        }
    }
}
#endregion