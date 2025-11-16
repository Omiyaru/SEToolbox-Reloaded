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


namespace SEToolbox.Services
{
    /// <summary>
    /// Weak event handler implementation.
    /// </summary>
    /// <typeparam name="TInstance">The type of the object with the actual handler.</typeparam>
    /// <typeparam name="TSender">Type of the event sender.</typeparam>
    /// <typeparam name="TEventArgs">Type of the event arguments.</typeparam>
    /// <remarks>
    /// Initializes a new instance of the WeakEventHandler{TInstance, TSender, TEventArgs} class.
    /// </remarks>
    /// <param name="instance">The object with the actual handler, to which a weak reference will be held.</param>
    /// <param name="handlerAction">An action to invoke the actual handler.</param>
    /// <param name="detachAction">An action to detach the weak handler from the event.</param>
    public class WeakEventHandler<TInstance, TSender, TEventArgs>(
        TInstance instance,
        Action<TInstance, TSender, TEventArgs> handlerAction,
        Action<WeakEventHandler<TInstance, TSender, TEventArgs>> detachAction)
        where TInstance : class
    {
        private readonly WeakReference _instanceReference = new(instance);
        private readonly Action<TInstance, TSender, TEventArgs> _handlerAction = handlerAction;
        private Action<WeakEventHandler<TInstance, TSender, TEventArgs>> _detachAction = detachAction;
    
        /// <summary>
        /// Removes the weak event handler from the event.
        /// </summary>
        public void Detach()
        {
            if (_detachAction != null)
            {
                _detachAction(this);
                _detachAction = null;
            }
        }

        /// <summary>
        /// Invokes the handler action.
        /// </summary>
        /// <remarks>
        /// This method must be added as the handler to the event.
        /// </remarks>
        /// <param name="sender">The event source object.</param>
        /// <param name="e">The event arguments.</param>
        public void OnEvent(TSender sender, TEventArgs e)
        {
            if (_instanceReference.Target is TInstance instance)
            {
                _handlerAction.Invoke((TInstance)_instanceReference.Target, sender, e);
            }
            else
            {
                Detach();
            }
        }
    }
}
