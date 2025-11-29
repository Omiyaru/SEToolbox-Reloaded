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

        #region events

        void AssociatedObject_Drop(object sender, DragEventArgs e)
        {
            // if the data type can be dropped.
            if (_dataType != null)
            {
                if (e.Data.GetDataPresent(_dataType))
                {
                    if (AllowDropToSource || (string)e.Data.GetData(typeof(string)) != ((FrameworkElement)sender).Uid)
                    {
                        // first find the UIElement that it was dropped over, then we determine if it's
                        // dropped above or under the UIElement, then insert at the correct index.
                        ItemsControl dropContainer = sender as ItemsControl;
                        // get the UIElement that was dropped over.
                        UIElement droppedOverItem = dropContainer.GetUIElement(e.GetPosition(dropContainer));
                        int dropIndex = -1; // the location where the item will be dropped.
                        if (droppedOverItem != null)
                        {
                            dropIndex = dropContainer.ItemContainerGenerator.IndexFromContainer(droppedOverItem) + 1;
                            // find if it was dropped above or below the index item so that we can insert
                            // the item in the correct place.
                            if (droppedOverItem.IsPositionAboveElement(e.GetPosition(droppedOverItem))) //if above
                            {
                                dropIndex--; //we insert at the index above it
                            }
                        }

                        // remove the data from each source.
                        foreach (object item in (IList)e.Data.GetData(_dataType))
                        {
                            IDragable source = item as IDragable;
                            source?.Remove(item);
                        }

                        // drop the data into destination.
                        IDropable target = AssociatedObject.DataContext as IDropable;
                        target.Drop(e.Data.GetData(_dataType), dropIndex);
                    }
                }
            }

            _insertAdornerManager?.Clear();

            e.Handled = true;
            return;
        }

        void AssociatedObject_DragLeave(object sender, DragEventArgs e)
        {
            _insertAdornerManager?.Clear();

            e.Handled = true;
        }

        void AssociatedObject_DragOver(object sender, DragEventArgs e)
        {
            if (_dataType != null)
            {
                if (e.Data.GetDataPresent(_dataType))
                {
                    if (!AllowDropToSource && (string)e.Data.GetData(typeof(string)) == ((FrameworkElement)sender).Uid)
                    {
                        e.Effects = DragDropEffects.None;
                        e.Handled = true;
                        return;
                    }

                    SetDragDropEffects(e);
                    if (_insertAdornerManager != null && ShowDropIndicator)
                    {
                        ItemsControl dropContainer = sender as ItemsControl;
                        UIElement droppedOverItem = dropContainer.GetUIElement(e.GetPosition(dropContainer));
                        if (droppedOverItem != null)
                        {
                            bool isAboveElement = droppedOverItem.IsPositionAboveElement(e.GetPosition(droppedOverItem));
                            _insertAdornerManager.UpdateDropIndicator(droppedOverItem, isAboveElement);
                        }
                        else
                        {
                            droppedOverItem = (UIElement)dropContainer.ItemContainerGenerator.ContainerFromIndex(dropContainer.Items.Count - 1);
                            _insertAdornerManager.UpdateDropIndicator(droppedOverItem, false);
                        }
                    }
                }
            }

            e.Handled = true;
        }

        void AssociatedObject_DragEnter(object sender, DragEventArgs e)
        {
            if (_dataType == null && AssociatedObject.DataContext is IDropable)
            {
                // if the DataContext implements IDropable, record the data type that can be dropped.
                if (DropType != null)
                    _dataType = typeof(List<>).MakeGenericType([DropType]);
                else
                    _dataType = typeof(List<>).MakeGenericType([((IDropable)AssociatedObject.DataContext).DataType]);
            }

            // initialize adorner manager with the adorner layer of the itemsControl.
            _insertAdornerManager = new ListBoxAdornerManager(AdornerLayer.GetAdornerLayer(sender as ItemsControl));

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
            if (e.Data.GetDataPresent(_dataType))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                // default to None.
                e.Effects = DragDropEffects.None;
            }
        }

        #endregion
    }
}
