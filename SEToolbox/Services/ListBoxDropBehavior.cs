// Originally sourced from:
// http://www.codeproject.com/Articles/420545/WPF-Drag-and-Drop-MVVM-using-Behavior
// http://www.dotnetlead.com/wpf-drag-and-drop/application

using SEToolbox.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Xaml.Behaviors;

// Modified to work with MultiSelect, and passing of bound data, and numerous other fixes.

namespace SEToolbox.Services
{
    /// <summary>
    /// For enabling Drop on ItemsControl
    /// </summary>
    public class ListBoxDropBehavior : Behavior<ItemsControl>
    {
        #region Fields

        private Type _dataType; // the type of the data that can be dropped into this control.
        private ListBoxAdornerManager _insertAdornerManager;

        #endregion

        #region Properties

        public static readonly DependencyProperty AllowDropToSourceProperty = DependencyProperty.Register("AllowDropToSource", typeof(bool), typeof(ListBoxDropBehavior), new PropertyMetadata(true));
        public bool AllowDropToSource
        {
            get => (bool)GetValue(AllowDropToSourceProperty);
            set => SetValue(AllowDropToSourceProperty, value);
        }

        public static readonly DependencyProperty ShowDropIndicatorProperty = DependencyProperty.Register("ShowDropIndicator", typeof(bool), typeof(ListBoxDropBehavior), new PropertyMetadata(true));
        public bool ShowDropIndicator
        {
            get => (bool)GetValue(ShowDropIndicatorProperty);
            set => SetValue(ShowDropIndicatorProperty, value);
        }

        public static readonly DependencyProperty DropTypeProperty = DependencyProperty.Register("DropType", typeof(Type), typeof(ListBoxDropBehavior));
        /// <summary>
        /// Specify the base Type of the data expected, when the ListBoxItemDragBehavior has set the DragSourceBinding property.
        /// </summary>
        public Type DropType
        {
            get => (Type)GetValue(DropTypeProperty);
            set => SetValue(DropTypeProperty, value);
        }

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Uid = Guid.NewGuid().ToString();
            AssociatedObject.AllowDrop = true;
            AssociatedObject.DragEnter += new DragEventHandler(AssociatedObject_DragEnter);
            AssociatedObject.DragOver += new DragEventHandler(AssociatedObject_DragOver);
            AssociatedObject.DragLeave += new DragEventHandler(AssociatedObject_DragLeave);
            AssociatedObject.Drop += new DragEventHandler(AssociatedObject_Drop);
        }

        #region Event Handlers

        void AssociatedObject_Drop(object sender, DragEventArgs e)
        {
            if (_dataType is not null && e.Data.GetDataPresent(_dataType))
            {
                var dropContainer = sender as ItemsControl;
                var droppedOverItem = dropContainer.GetUIElement(e.GetPosition(dropContainer));
                var dropIndex = dropContainer.ItemContainerGenerator.IndexFromContainer(droppedOverItem) + (droppedOverItem.IsPositionAboveElement(e.GetPosition(droppedOverItem)) ? dropContainer.Items.Count - 1 : -1);
                var data = e.Data.GetData(_dataType) as IEnumerable;
                foreach (var item in data)
                {
                    (item as IDragable)?.Remove(item);
                }
                var target = AssociatedObject.DataContext as IDropable;
                target.Drop(data, dropIndex);
            }
            _insertAdornerManager?.Clear();
            e.Handled = true;
        }

        void AssociatedObject_DragLeave(object sender, DragEventArgs e)
        {
            _insertAdornerManager?.Clear();
            e.Handled = true;
        }

        void AssociatedObject_DragOver(object sender, DragEventArgs e)
        {
            var stringEquals = string.Equals((string)e.Data.GetData(typeof(string)), ((FrameworkElement)sender).Uid);
            if (_dataType != null && e.Data.GetDataPresent(_dataType) && !stringEquals)
            {
                if (!AllowDropToSource)
                {
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                    return;
                }
                SetDragDropEffects(e);

                if (ShowDropIndicator)
                {
                    ItemsControl dropContainer = sender as ItemsControl;
                    UIElement droppedOverItem = dropContainer.GetUIElement(e.GetPosition(dropContainer));
                    bool isAboveElement = droppedOverItem.IsPositionAboveElement(e.GetPosition(droppedOverItem));
                    _insertAdornerManager?.UpdateDropIndicator(droppedOverItem, isAboveElement);

                       droppedOverItem = (UIElement)dropContainer.ItemContainerGenerator.ContainerFromIndex(dropContainer.Items.Count - 1);
                        _insertAdornerManager.UpdateDropIndicator(droppedOverItem, false);

                }
            }

            e.Handled = true;
        }

        void AssociatedObject_DragEnter(object sender, DragEventArgs e)
        {

            IDropable dataContext = AssociatedObject.DataContext as IDropable;
            _dataType ??= DropType != null ? typeof(List<>).MakeGenericType([DropType]) : dataContext.DataType?.MakeGenericType([dataContext.DataType]);
			// initialize adorner manager with the adorner layer of the itemsControl.
            _insertAdornerManager ??= new ListBoxAdornerManager(AdornerLayer.GetAdornerLayer(sender as ItemsControl));

            e.Handled = true;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Provides feedback on if the data can be dropped.
        /// </summary>
        /// <param name="e"></param>
        private void SetDragDropEffects(DragEventArgs e)
        {
            // if the data type can be dropped.
            e.Effects = e.Data.GetDataPresent(_dataType) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        #endregion
    }
}
