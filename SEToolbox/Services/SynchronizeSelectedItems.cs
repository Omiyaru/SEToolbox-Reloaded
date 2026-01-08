// Prism 4.1
// http://www.microsoft.com/en-us/download/details.aspx?displaylang=en&id=28950
//
//===================================================================================
// Microsoft patterns & practices
// Composite Application Guidance for Windows Presentation Foundation and Silverlight
//===================================================================================
// Copyright (c) Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===================================================================================
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
//===================================================================================

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;


namespace SEToolbox.Services
{
    /// <summary>
    /// Custom behavior that synchronizes the list in <see cref="ListBox.SelectedItems"/> with a collection.
    /// </summary>
    /// <remarks>
    /// This behavior uses a weak event handler to listen for changes on the synchronized collection.
    /// </remarks>
    public class SynchronizeSelectedItems : Behavior<ListBox>
    {
        public static readonly DependencyProperty SelectionsProperty =
            DependencyProperty.Register(
                "Selections",
                typeof(IList),
                typeof(SynchronizeSelectedItems),
                new PropertyMetadata(null, OnSelectionsPropertyChanged));

        private bool _updating;
        private WeakEventHandler<SynchronizeSelectedItems, object, NotifyCollectionChangedEventArgs> _currentWeakHandler;

        public IList Selections
        {
            get => (IList)GetValue(SelectionsProperty);
			set => SetValue(SelectionsProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.SelectionChanged += OnSelectedItemsChanged;
            UpdateSelectedItems();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SelectionChanged += OnSelectedItemsChanged;

            base.OnDetaching();
        }

        private static void OnSelectionsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (SynchronizeSelectedItems)d;

            if (behavior != null)
            {
                if (behavior._currentWeakHandler != null)
                {
                    behavior._currentWeakHandler.Detach();
                    behavior._currentWeakHandler = null;
                }

                if (e.NewValue != null)
                {
                    if (e.NewValue is INotifyCollectionChanged notifyCollectionChanged)
                    {
                        behavior._currentWeakHandler =
                            new WeakEventHandler<SynchronizeSelectedItems, object, NotifyCollectionChangedEventArgs>(
                                behavior,
                                (instance, sender, args) => instance.OnSelectionsCollectionChanged(sender, args),
                                (listener) => notifyCollectionChanged.CollectionChanged -= listener.OnEvent);
                        notifyCollectionChanged.CollectionChanged += behavior._currentWeakHandler.OnEvent;
                    }

                    behavior.UpdateSelectedItems();
                }
            }
        }

        private void OnSelectedItemsChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelections(e);
        }

        private void UpdateSelections(SelectionChangedEventArgs e)
        {
            ExecuteIfNotUpdating(
                () =>
                {
                    if (Selections != null)
                    {
                        foreach (object item in e.AddedItems)
                        {
                            Selections.Add(item);
                        }

                        foreach (object item in e.RemovedItems)
                        {
                            Selections.Remove(item);
                        }
                    }
                });
        }

        private void OnSelectionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateSelectedItems();
        }

        private void UpdateSelectedItems()
        {
            ExecuteIfNotUpdating(
                () =>
                {
                    if (AssociatedObject != null)
                    {
                        AssociatedObject.SelectedItems.Clear();
                        foreach (object item in Selections ?? new object[0])
                        {
                            AssociatedObject.SelectedItems.Add(item);
                        }
                    }
                });
        }

        private void ExecuteIfNotUpdating(Action execute)
        {
            if (!_updating)
            {
                try
                {
                    _updating = true;
                    execute();
                }
                finally
                {
                    _updating = false;
                }
            }
        }
    }
}
