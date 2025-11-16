    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Forms;
    using System.Windows.Input;
    using Microsoft.Xaml.Behaviors;
    using SEToolbox.Interop;
using SEToolbox.Models;
using SEToolbox.Support;
    using Binding = System.Windows.Data.Binding;
    using DataObject = System.Windows.DataObject;
    using DragDropEffects = System.Windows.DragDropEffects;
    using ListBox = System.Windows.Controls.ListBox;
    using ListViewItem = System.Windows.Controls.ListViewItem;
    using MouseEventArgs = System.Windows.Input.MouseEventArgs;
    using MouseEventHandler = System.Windows.Input.MouseEventHandler;

namespace SEToolbox.Services
{
    /// <summary>
    /// Multi Select Item Drag.
    /// </summary>
    public class ListBoxItemDragBehavior : Behavior<ListBoxItem>
    {
        #region Fields

        private bool _isMouseClicked = false;
        private bool _wasDragging = false;
        private BindingBase _dragMemberBinding;

        #endregion

        #region Properties

        public BindingBase DragSourceBinding
        {
            get => _dragMemberBinding;
            set
            {
                if (_dragMemberBinding != value)
                {
                    _dragMemberBinding = value;
                }
            }
        }

        #endregion

        #region Methods

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseLeave += new MouseEventHandler(AssociatedObject_MouseLeave);
            AssociatedObject.PreviewMouseLeftButtonDown += AssociatedObject_PreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseLeftButtonUp += AssociatedObject_PreviewMouseLeftButtonUp;
        }

        #endregion

        #region events

        void AssociatedObject_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isMouseClicked = true;

            var item = AssociatedObject.GetHitControl<ListViewItem>(e);
            if (item is {IsEnabled: true, IsSelected: true})
               if  ((NativeMethods.GetKeyState(Keys.ShiftKey) & KeyStates.Down) != KeyStates.Down
                && (NativeMethods.GetKeyState(Keys.ControlKey) & KeyStates.Down) != KeyStates.Down)
            {
                e.Handled = true;
            }
        }
        void AssociatedObject_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = AssociatedObject.GetHitControl<ListViewItem>((MouseEventArgs)e);
           if (item is {IsEnabled: true, IsSelected: true} && _isMouseClicked && !_wasDragging
            && (NativeMethods.GetKeyState(Keys.ShiftKey) & KeyStates.Down) != KeyStates.Down
            && (NativeMethods.GetKeyState(Keys.ControlKey) & KeyStates.Down) != KeyStates.Down)
                {
                    ListBox parent = ItemsControl.ItemsControlFromItemContainer(AssociatedObject) as ListBox;
                    parent.SelectedItems.Clear();
                    item.IsSelected = true;
                    item.Focus();
                }

            _isMouseClicked = false;
            _wasDragging = false;
        }

        void AssociatedObject_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isMouseClicked)
            {
                // set the item's DataContext as the data to be transferred.
                if (AssociatedObject.DataContext is IDragable dragObject)
                {
                    _wasDragging = true;
                    DataObject data = new();

                    ListBox parent = ItemsControl.ItemsControlFromItemContainer(AssociatedObject) as ListBox;
                    IList list = null;
                    if (DragSourceBinding == null)
                    {
                        // Pass the raw ItemSource as the drag object.
                        list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(dragObject.DataType));
                        if (!AssociatedObject.IsSelected)
                        {
                            list.Add(AssociatedObject.DataContext);
                        }

                        foreach (object item in parent.SelectedItems)
                        {
                            list.Add(item);
                        }
                    }
                    else
                    {
                        // Pass the Binding object under the ItemSource as the drag object.
                        string propertyName = ((Binding)DragSourceBinding).Path.Path;
                        PropertyDescriptor pd = TypeDescriptor.GetProperties(AssociatedObject.DataContext).Find(propertyName, false);

                        list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(pd.PropertyType));
                        if (!AssociatedObject.IsSelected)
                        {
                            list.Add(pd.GetValue(AssociatedObject.DataContext));
                        }

                        foreach (object item in parent.SelectedItems)
                        {
                            list.Add(pd.GetValue(item));
                        }
                    }

                    data.SetData(list.GetType(), list);

                    // Send the ListBox that initiated the drag, so we can determine if the drag and drop are different or not.
                    data.SetData(typeof(string), parent.Uid);

                    data.SetData(dragObject.DataType, AssociatedObject.DataContext);
                    DragDrop.DoDragDrop(parent, data, DragDropEffects.Copy);
                    DragDrop.DoDragDrop(AssociatedObject, data, DragDropEffects.Move);
                }
            }

            _isMouseClicked = false;
        }
        #endregion
    }
}
