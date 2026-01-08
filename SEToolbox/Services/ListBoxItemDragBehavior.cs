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
using System.Linq;
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
        private BaseModel _baseModel = new();
        #endregion

        #region Properties

        public BindingBase DragSourceBinding
        {
            get => _dragMemberBinding;
            set => _baseModel.SetProperty(ref _dragMemberBinding, value);
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
            if (item is { IsEnabled: true, IsSelected: true } &&
                !NativeMethods.GetKeyState(Keys.ShiftKey).HasFlag(KeyStates.Down) &&
                !NativeMethods.GetKeyState(Keys.ControlKey).HasFlag(KeyStates.Down))
            {
                e.Handled = true;
            }
        }
        void AssociatedObject_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ListViewItem item = AssociatedObject.GetHitControl<ListViewItem>(e);
            if (item is { IsEnabled: true, IsSelected: true } && _isMouseClicked && 
                !NativeMethods.GetKeyState(Keys.ShiftKey).HasFlag(KeyStates.Down) &&
                !NativeMethods.GetKeyState(Keys.ControlKey).HasFlag(KeyStates.Down) && _wasDragging)
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
            // set the item's DataContext as the data to be transferred.
            if (_isMouseClicked && AssociatedObject.DataContext is IDragable dragObject && _wasDragging == false)
            {
                _wasDragging = true;
                DataObject data = new();

                var propertyName = ((Binding)DragSourceBinding).Path.Path;
                var propType = dragObject.GetType().GetProperty(DragSourceBinding.ToString());

                var propDesc = TypeDescriptor.GetProperties(AssociatedObject.DataContext).Find(propertyName, false);
                var parent = ItemsControl.ItemsControlFromItemContainer(AssociatedObject) as ListBox;
                parent.SelectedItems.Cast<object>();
                IList list = null;
                Action AssociatedAction = AssociatedObject.IsSelected switch
                {
                    true => () => list.Add(propDesc.GetValue(AssociatedObject.DataContext)),
                    false => () => parent.SelectedItems.Cast<object>().ToArray().ForEach(item => list.Add(item)),
                };
                Action action = (DragSourceBinding, AssociatedAction) switch
                {
                    (null, _) => () => list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(dragObject.DataType), AssociatedAction),
                    (_, _) => () =>  list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(propDesc.PropertyType), AssociatedAction),    
                };


                action();
                data.SetData(list.GetType(), list);

                // Send the ListBox that initiated the drag, so we can determine if the drag and drop are different or not.
                data.SetData(typeof(string), parent.Uid);

                data.SetData(dragObject.DataType, AssociatedObject.DataContext);
                DragDrop.DoDragDrop(parent, data, DragDropEffects.Copy);
                DragDrop.DoDragDrop(AssociatedObject, data, DragDropEffects.Move);
            }

            _isMouseClicked = false;
        }
        #endregion
    }
}
