using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Markup;
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

        public void SetProperty<T>(T field, T value, params string[] propertyNames) => SetProperty(ref field, value, propertyNames);

        public void SetProperty<T>(ref T field, T value, params string[] propertyNames)
        {
            var propertyName = propertyNames.FirstOrDefault() ?? string.Empty;
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
            }
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
            {
                return;
            }

            var actionToInvoke = parameters.OfType<Action>().FirstOrDefault();
            var propertyName = parameters.OfType<string>().FirstOrDefault();
            var propertyNames = parameters.OfType<string>().ToArray();
            var actionIndex = Array.IndexOf(parameters, actionToInvoke);
            var propertyNameIndex = Array.IndexOf(parameters, propertyName);
            bool? invokeBefore = actionIndex < propertyNameIndex;

            var ati = actionToInvoke;
            var pn = propertyName;
            var values = value as IEnumerable<T>;
            var enumerable = field is IEnumerable<T> && value is IEnumerable<T> && propertyNames?.Length > 0;
            var dict = (field, value) as IDictionary<T, T>;
            var valuesList = values?.ToList() ?? new List<T>();
            var internalAction = actionToInvoke;
            var ib = invokeBefore;
            var opc = OnPropertyChanged;
            var lst = ib is not null ? ib is true ? dict.Keys.ToList() : dict.Values.ToList() : valuesList?.Count > 0 || enumerable ? valuesList : null;
            Action<T, T, Action> action = (ib, ati) switch
            {
               (not null, not null) => (f, v, a) => { a = ib is true ? a = () => { ati?.Invoke(); f = v; }
                                                                     : a = () => { f = v; ati?.Invoke(); };  },

                (not null,  null) when ib is not null &&  pn is not null => (f, v, a) => a = ib is true ? a = () => { f = v; opc(pn); }
                                                                                                        : a = () => { f = v; opc(pn); },
                (_ or null, not null) when lst is not null => (f, v, a) => 
                { 
                   lst.ForEach(i => Conditional.Coalesced(ib is true, a = () => { ati?.Invoke(); f = v; opc(pn); }, a = () => { f = v; opc(pn); ati?.Invoke(); }));
                },
                (null, null) when lst is not null => (f, v, _) => { lst.ForEach(i => f = v); },
                (null, null) when pn is not null => (f, v, _) => { f = v; opc(pn); },
                (_, null) when pn is null => (_, _, _) => throw new Exception(" Property name cannot be null."),
                 _ or (_, null) or (null, _) or (null, null) => (f, v, _) => { f = v; opc(pn); },  
            };
            action(field, value, internalAction);
        }
        public void SetValue<T>(T field, T value, params object[] parameters) => SetValue(field, value, parameters);

        public void SetValue<T>(ref T field, T value, params object[] parameters)
        {
            if (parameters.Length > 1)
            {
                var actionToInvoke = parameters.OfType<Action>().FirstOrDefault();
                var actionIndex = Array.IndexOf(parameters, actionToInvoke);
                var values = value as IEnumerable<T>;
                var pairIndex = Array.IndexOf(parameters, field);
                bool? invokeBefore = actionIndex < pairIndex;
                var enumerable = field is IEnumerable<T> && value is IEnumerable<T>;
                var dict = (field, value) as IDictionary<T, T>;
                var valuesList = values?.ToList() ?? new List<T>();
                var ati = actionToInvoke;
                var ib = invokeBefore;
                var lst = ib is not null ? ib is true ? dict.Keys.ToList() : dict.Values.ToList() : valuesList?.Count > 0 || enumerable ? valuesList : null;

                Action<T, T, Action> action = (ib, ati) switch
                {
                    (not null, not null) => (f, v, a) => { a = ib is true ? a = () => { ati?.Invoke(); f = v; }
                                                                          : a = () => { f = v; ati?.Invoke(); }; },
                    _ when lst is not null => (f, v, a) => { lst.ForEach(_ => a = ib is true ? a = () => { ati?.Invoke(); f = v; }
                                                                                             : a = () => { f = v; ati?.Invoke(); }); 
                    },
                    (_, null) when lst is not null => (f, v, _) => { lst.ForEach(i => f = v); },
                    (_, null) when ati is null => (f, v, _) => { f = v; },
                    (_, null) or (null, _) or (null, null) => (f, v, _) => { f = v; },
                };

                action(field, value, actionToInvoke);
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
            remove => _ = _propertyChanged != null ? _propertyChanged -= value : null;

        }
    }
        #endregion
}


