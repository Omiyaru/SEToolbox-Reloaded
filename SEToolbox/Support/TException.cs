using System;
using System.Collections.Concurrent;

using System.Diagnostics;
using System.Globalization;
using System.Reflection;


namespace SEToolbox.Support
{
    public class TException : Exception
    {
        public TException() : base() { }
        
        public TException(string message) : base(message) => RegisterListener<TException>(TExceptionListener<TException>);

        public TException(string message, params object[] args) : base(string.Format(CultureInfo.CurrentCulture, message, args)) =>
            RegisterListener<TException>(TExceptionListener<TException>);

        public TException(string message, Exception innerException) : base(message, innerException) =>
            RegisterListener<TException>(TExceptionListener<TException>);

        private static readonly ConcurrentDictionary<Type, Action<TException>> _listeners = new();

        public static void InitializeListeners()
        {
            // Initialize _listeners here
            _listeners.TryAdd(typeof(TException), (exception) =>
            {
                if (exception == null)
                    return;
                _listeners.TryGetValue(typeof(TException), out var listenerAction);
                listenerAction?.Invoke(exception);
                foreach (var listener in _listeners.Values)
                    listener?.Invoke(exception);

                RegisterListener<TException>(TExceptionListener<TException>);

                if (TExceptionListener<TException> != null)
                    ThrowDynamicException(exception);
            });
        }

        public static void RegisterListener<T>(Action<TException> listener) where T : Exception
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener), "Listener cannot be null.");

            _listeners.TryAdd(typeof(T), listener);
        }

        public static void TExceptionListener<T>(TException exception) where T : Exception
        {
            if (_listeners.TryGetValue(typeof(T), out var listener))
                listener?.Invoke(exception);
        }

        public static void ThrowDynamicException<T>(T exception = null) where T : Exception
        {
            if (exception == null)
            {
                var type = typeof(T);
                exception = (T)Activator.CreateInstance(type);

                var constructor = type.GetConstructor([typeof(string)]);
                if (constructor != null)
                {
                    var message = $"{type.Name} was thrown dynamically.\n\nPlease report this to the developer.";
                    exception = (T)constructor.Invoke([message]);

                    var exceptionTypeProperty = type.GetProperty(nameof(Exception), BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    if (exceptionTypeProperty != null)
                    {
                        var exceptionTypeEnum = (Exception)exceptionTypeProperty.GetValue(null);
                        if (exceptionTypeEnum != null)
                        {
                            exception = (T)Activator.CreateInstance(type, message, exceptionTypeEnum);
                        }
                    }
                    SConsole.WriteLine(exception as string);
                    Debug.WriteLine(exception);
                }
            }

            var dynamicMethod = typeof(T).GetMethod(nameof(ThrowDynamicException), BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException($"Could not find method. {typeof(T).Name}.{nameof(ThrowDynamicException)}");
            dynamicMethod.MakeGenericMethod(typeof(T)).Invoke(null, [exception]);
        }
    }
}









