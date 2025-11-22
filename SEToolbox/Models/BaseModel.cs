using System;
using System.Collections.Generic;

using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Sandbox.Engine.Multiplayer;
using SEToolbox.Support;
using VRage.Game.ModAPI.Ingame;

namespace SEToolbox.Models
{
    [Serializable]
    public class BaseModel : INotifyPropertyChanged
    {
        #region Methods

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event.
        /// Use the <see cref="nameof()"/> in conjunction with OnPropertyChanged.
        /// This will set the property name into a string during compile, which will be faster to execute then a runtime interpretation.
        /// </summary>
        /// <param name="propertyNames">The name of the property that changed.</param>
        protected void OnPropertyChanged(params string[] propertyNames)
        {
            PropertyChangedEventHandler handler = _propertyChanged;
            if (handler != null)
            {
                foreach (string propertyName in propertyNames)
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }


        public void SetProperty<T>(T field, T value, params string[] parameters) => SetProperty(ref field, value, parameters);
    
        public void SetProperty<T>(ref T field, T value, params object[] properetyNames)
        {
            var propertyName = properetyNames.OfType<string>().FirstOrDefault() ?? string.Empty;
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            if (properetyNames.Length > 0 && !string.IsNullOrEmpty(propertyName))
            {
                OnPropertyChanged(propertyName);
            }
        }

        public void SetProperty<T>(T field, params object[] parameters) => SetProperty(ref field, parameters);
        public void SetProperty<T>(ref T field, params object[] parameters)
        {
            T value = (T)parameters[0] ?? default;
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            if (parameters.Length > 1)
            {
                var propertyName = parameters.OfType<string>().FirstOrDefault() ?? string.Empty;
                var actionToInvoke = parameters.OfType<Action>().FirstOrDefault();
                bool? invokeBefore = Array.IndexOf(parameters, actionToInvoke) < Array.IndexOf(parameters, propertyName);
                var propertyNames = parameters.OfType<string>().ToArray();

                if (actionToInvoke != null)
                {
                    if (invokeBefore == true)
                        actionToInvoke?.Invoke();
                    field = value;
                    OnPropertyChanged(propertyName);
                    if (invokeBefore == false)
                        actionToInvoke?.Invoke();
                }
                if (invokeBefore == null && actionToInvoke != null && propertyName == null)
                {
                    field = value;
                    actionToInvoke?.Invoke();
                }
                else
                {
                    field = value;
                    OnPropertyChanged(propertyName);
                }
            }
        }


        #endregion

        #region INotifyPropertyChanged Members

        [NonSerialized]
        PropertyChangedEventHandler _propertyChanged;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add => _propertyChanged += value;
            remove { if (_propertyChanged != null) _propertyChanged -= value; }
        }

        #endregion
    }
}

