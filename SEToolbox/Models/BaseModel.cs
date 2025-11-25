using System;
using System.Collections.Generic;

using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;


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

        public void SetProperty<T>(T field, T value, params string[] propertyNames) => SetProperty(ref field, value, propertyNames);

        public void SetProperty<T>(ref T field, T value, params string[] propertyNames)
        {
            var propertyName = propertyNames.FirstOrDefault() ?? string.Empty;
            if (!EqualityComparer<T>.Default.Equals(field, value))

                field = value;
            if (propertyNames.Length > 0 && !string.IsNullOrEmpty(propertyName))
            {
                OnPropertyChanged(propertyName);
            }
           
        }

        public void SetProperty<T>(T field, T value, params object[] parameters) => SetProperty(ref field, value, parameters);

        public void SetProperty<T>(ref T field, T value) => SetProperty(ref field, value, string.Empty);
        
        public void SetProperty<T>(ref T field, T value, params object[] parameters)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            var actionToInvoke = parameters.OfType<Action>().FirstOrDefault();
            var propertyName = parameters.OfType<string>().FirstOrDefault();
            var propertyNames = parameters.OfType<string>().ToArray();
            bool changeProperty = Array.IndexOf(parameters, propertyName) < Array.IndexOf(parameters, actionToInvoke);
            bool invokeBeforePropertySet = Array.IndexOf(parameters, actionToInvoke) < Array.IndexOf(parameters, propertyName) || changeProperty;
            if (actionToInvoke != null && propertyNames != null)
            {
                if (invokeBeforePropertySet)
                    actionToInvoke?.Invoke();
                field = value;
                OnPropertyChanged(propertyName);
                if (!invokeBeforePropertySet)
                    actionToInvoke?.Invoke();
            }
            else if (actionToInvoke != null && propertyNames == null)
            {
                if (invokeBeforePropertySet)
                    actionToInvoke?.Invoke();
                field = value;
                if (!invokeBeforePropertySet)
                    actionToInvoke?.Invoke();
            }
            else if (actionToInvoke == null && propertyNames != null)
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
         
        }

        public void SetValue<T>(ref T field, T value) => field = value;

        public void SetValue<T>(ref T field, T value, params object[] parameters)
        {
            
          if (parameters.Length > 1)
            {
                var actionToInvoke = parameters.OfType<Action>().FirstOrDefault() ?? parameters.OfType<Expression<Action>>().FirstOrDefault()?.Compile();
                bool? invokeBefore = Array.IndexOf(parameters, actionToInvoke) < Array.IndexOf(parameters, field);
                var propertyNames = parameters.OfType<string>().ToArray();

                if (actionToInvoke != null)
                {
                    if (invokeBefore == true)   
                    actionToInvoke?.Invoke();
                    field = value;
                    if (invokeBefore == false)
                        actionToInvoke?.Invoke();
                }
                if (actionToInvoke != null && propertyNames == null)
                {
                    field = value;
                    actionToInvoke?.Invoke();
                }
                else
                {
                    field = value;
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

