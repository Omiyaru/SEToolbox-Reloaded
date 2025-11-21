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

        public void SetProperty<T>(ref T field, params object[] parameters) => SetProperty(field, parameters);

        public void SetProperty<T>(T field, params object[] parameters)
        {   
            T value = default;
            var propertyName = parameters.FirstOrDefault() as string ?? parameters.LastOrDefault() as string ?? string.Empty;
            if (ReferenceEquals(value, field) || EqualityComparer<T>.Default.Equals(value, field))
                return;
            if (parameters.Contains(propertyName) || !string.IsNullOrEmpty(propertyName) || parameters.Length < 1 && field != null)
            {
                field = value;
                OnPropertyChanged(propertyName);
                return;
            }
        
            var actionToInvoke = parameters.OfType<Action>().FirstOrDefault() ?? parameters.OfType<Action>().LastOrDefault();
            var expressionToCompile = parameters.OfType<Expression<Action>>().FirstOrDefault() ?? parameters.OfType<Expression<Action>>().LastOrDefault();
            bool? invokeBefore = actionToInvoke != null || expressionToCompile != null ? true : false;

            if (invokeBefore == true)
            {
                actionToInvoke?.Invoke();
                expressionToCompile?.Compile().Invoke();
            }
            field = value;
            OnPropertyChanged(propertyName);
            if (invokeBefore == false)
            {
                actionToInvoke?.Invoke();
                expressionToCompile?.Compile().Invoke();
            }
      
            if (string.IsNullOrEmpty(propertyName) && !parameters.Contains(propertyName))
            {
                field = value;
                actionToInvoke?.Invoke();
                expressionToCompile?.Compile().Invoke();
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

