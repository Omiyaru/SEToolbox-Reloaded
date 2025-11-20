using System;
using System.Collections.Generic;

using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using SEToolbox.Support;

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
            T value =default;
            var propertyName = parameters.FirstOrDefault() as string ?? parameters.LastOrDefault() as string ?? string.Empty;

            if (ReferenceEquals(value, field) || EqualityComparer<T>.Default.Equals(value, field))
                return;

            if (string.IsNullOrEmpty(propertyName) || parameters.Length < 1)
            {
                field = value;
                OnPropertyChanged(propertyName);
                return;
            }
            var actionToInvokeBefore = parameters.OfType<Action>().FirstOrDefault();
            var actionToInvokeAfter = parameters.OfType<Action>().LastOrDefault();
            var expressionToCompileBefore = parameters.OfType<Expression<Action>>().FirstOrDefault();
            var expressionToCompileAfter = parameters.OfType<Expression<Action>>().LastOrDefault();

            actionToInvokeBefore?.Invoke();
            expressionToCompileBefore?.Compile().Invoke();

            field = value;
            OnPropertyChanged(propertyName);

            actionToInvokeAfter?.Invoke();
            expressionToCompileAfter?.Compile().Invoke();
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

