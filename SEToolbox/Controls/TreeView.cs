
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using SysTree = System.Windows.Controls.TreeView;

namespace SEToolbox.Controls
{
    public class TreeView : SysTree
    {
        public Action<object, RoutedEventArgs> ItemExpanded { get; private set; }

        public TreeView()
        {
            ItemExpanded += TreeViewItem_Expanded;
        }

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            ExpandSubtree(SelectedItem, true);
        }

        public void ExpandSubtree(object item, bool expaned = false)
        {
            if (item != null && expaned == false)
            {
                if (ItemContainerGenerator.ContainerFromItem(item) is ItemsControl itemsControl)
                {
                    foreach (object subItem in itemsControl.Items)
                    {
                        ExpandSubtree(subItem, true);
                    }
                }
            }
        }

        internal void CollapseSubtree(object item, bool expanded = false)
        {
            if (item != null && expanded == true)
            {
                if (ItemContainerGenerator.ContainerFromItem(item) is ItemsControl itemsControl)
                {
                    foreach (var subItem in itemsControl.Items)
                    {
                        CollapseSubtree(subItem, false);
                    }
                }
            }
        }

        public class TreeViewCollection(TreeView treeView) : ObservableCollection<object>
        {
            private readonly TreeView _treeView = treeView;
            private readonly HashSet<object> _items = [];

            protected override void ClearItems()
            {
                foreach (var item in this)
                {
                    _items.Remove(item);
                    _treeView.ExpandSubtree(item);
                }

                base.ClearItems();
            }

            protected override void InsertItem(int index, object item)
            {
                if (item == null)
                    throw new ArgumentNullException("item");

                _items.Add(item);
                _treeView.ExpandSubtree(item);
                base.InsertItem(index, item);
            }

            protected override void RemoveItem(int index)
            {
                var item = this[index];
                _items.Remove(item);
                _treeView.CollapseSubtree(item);
                base.RemoveItem(index);
            }

            protected override void SetItem(int index, object item)
            {
                if (item == null)
                    throw new ArgumentNullException("item");

                var oldItem = this[index];
                _items.Remove(oldItem);
                _items.Add(item);
                _treeView.CollapseSubtree(oldItem);
                _treeView.ExpandSubtree(item);
                base.SetItem(index, item);
            }
        }
    }
}

