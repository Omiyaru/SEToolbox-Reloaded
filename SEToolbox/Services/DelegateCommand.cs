using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using SEToolbox.Models;

namespace SEToolbox.Services
{
    /// <summary>
    ///     This class allows delegating the commanding logic to methods passed as parameters,
    ///     and enables a View to bind commands to objects that are not part of the element tree.
    /// </summary>
    public class DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod, bool isAutomaticRequeryDisabled) : ICommand
    {
        #region Constructors

        /// <summary>
        ///     Constructor
        /// </summary>
        public DelegateCommand(Action executeMethod)
            : this(executeMethod, null, false)
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod)
            : this(executeMethod, canExecuteMethod, false)
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        public DelegateCommand(Func<bool> canExecuteMethod)
            : this(null, canExecuteMethod, false)
        {
        }

        #endregion
        private readonly BaseModel baseModel = new();
        #region Operators
        public static implicit operator DelegateCommand(Action executeMethod) => new(executeMethod, null, false);

        public static implicit operator DelegateCommand(Func<bool> canExecuteMethod) => new(null, canExecuteMethod, false);

        public static DelegateCommand operator +(DelegateCommand left, DelegateCommand right)
        {
            // Define the logic for adding two DelegateCommand objects here
            return new DelegateCommand(() =>
            {
                bool leftCanExecute = left.CanExecute();
                bool rightCanExecute = right.CanExecute();
                _ = (leftCanExecute, rightCanExecute) switch
                {
                    (true, true) => true,
                    (true, false) => true,
                    (false, true) => true,
                    _ => false
                };
                left.Execute();
                right.Execute();
            });
        }

        public static DelegateCommand operator -(DelegateCommand left, DelegateCommand right)
        {
            // Define the logic for removing two DelegateCommand objects here
            return new DelegateCommand(() =>
            {
                bool leftCanExecute = left.CanExecute();
                bool rightCanExecute = right.CanExecute();
                _ = (leftCanExecute, rightCanExecute) switch
                {
                    (true, true) => true,
                    (true, false) => true,
                    (false, true) => true,
                    _ => false
                };
                left.Execute();
                right.Execute();
            });
        }

        #endregion
        #region Public Methods

        /// <summary>
        ///     Method to determine if the command can be executed
        /// </summary>
        public bool CanExecute()
        {
            return _canExecuteMethod?.Invoke() ?? true;
        }

        /// <summary>
        ///     Execution of the command
        /// </summary>
        public void Execute()
        {
            _executeMethod?.Invoke();
        }


        /// <summary>
        ///     Property to enable or disable CommandManager's automatic requery on this command
        /// </summary>
        public bool IsAutomaticRequeryDisabled
        {
            get => _isAutomaticRequeryDisabled;
            set => baseModel.SetProperty(ref _isAutomaticRequeryDisabled, value, () =>
            {
                var handlers = _canExecuteChangedHandlers;
                CommandManagerHelper.SetHandlersForRequerySuggested(handlers, value);

            }, nameof(IsAutomaticRequeryDisabled));
        }

        /// <summary>
        ///     Raises the CanExecuteChaged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        /// <summary>
        ///     Protected virtual method to raise CanExecuteChanged event
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            CommandManagerHelper.CallWeakReferenceHandlers(_canExecuteChangedHandlers);
        }

        #endregion

        #region ICommand Members

        /// <summary>
        ///     ICommand.CanExecuteChanged implementation
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (!_isAutomaticRequeryDisabled)
                {
                    CommandManager.RequerySuggested += value;
                }
                CommandManagerHelper.AddWeakReferenceHandler(ref _canExecuteChangedHandlers, value, 2);
            }
            remove
            {
                if (!_isAutomaticRequeryDisabled)
                {
                    CommandManager.RequerySuggested -= value;
                }
                CommandManagerHelper.RemoveWeakReferenceHandler(_canExecuteChangedHandlers, value);
            }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        void ICommand.Execute(object parameter)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Execute();
        }

        #endregion

        #region Data

        private readonly Action _executeMethod = executeMethod;
        private readonly Func<bool> _canExecuteMethod = canExecuteMethod;
        private bool _isAutomaticRequeryDisabled = isAutomaticRequeryDisabled;
        private List<WeakReference> _canExecuteChangedHandlers;

        #endregion
    }

    /// <summary>
    ///     This class allows delegating the commanding logic to methods passed as parameters,
    ///     and enables a View to bind commands to objects that are not part of the element tree.
    /// </summary>
    /// <typeparam name="T">Type of the parameter passed to the delegates</typeparam>

    public class DelegateCommand<T>(Action<T> executeMethod, Func<T, bool> canExecuteMethod, bool isAutomaticRequeryDisabled) : ICommand
    {
        private readonly BaseModel baseModel = new();
        #region Constructors

        /// <summary>
        ///     Constructor
        /// </summary>
        public DelegateCommand(Action<T> executeMethod)
            : this(executeMethod, null, false)
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        public DelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
            : this(executeMethod, canExecuteMethod, false)
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        public DelegateCommand(Func<T, bool> canExecuteMethod)
            : this(null, canExecuteMethod, false)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Method to determine if the command can be executed
        /// </summary>
        public bool CanExecute(T parameter)
        {
            return _canExecuteMethod?.Invoke(parameter) ?? true;
        }

        /// <summary>
        ///     Execution of the command
        /// </summary>
        public void Execute(T parameter)
        {
            _executeMethod?.Invoke(parameter);
        }

        /// <summary>
        ///     Raises the CanExecuteChaged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        /// <summary>
        ///     Protected virtual method to raise CanExecuteChanged event
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            CommandManagerHelper.CallWeakReferenceHandlers(_canExecuteChangedHandlers); 

        }

        /// <summary>
        ///     Property to enable or disable CommandManager's automatic requery on this command
        /// </summary>
        public bool IsAutomaticRequeryDisabled
        {
            get => _isAutomaticRequeryDisabled;
            set => baseModel.SetProperty(ref _isAutomaticRequeryDisabled, value, () =>
                {
                    CommandManagerHelper.SetHandlersForRequerySuggested(_canExecuteChangedHandlers, value);
                }, nameof(IsAutomaticRequeryDisabled));
        }

        #endregion

        #region ICommand Members

        /// <summary>
        ///     ICommand.CanExecuteChanged implementation
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add {Delegate action = _isAutomaticRequeryDisabled ? () => CommandManagerHelper.AddWeakReferenceHandler(ref _canExecuteChangedHandlers, value) : () => CommandManager.RequerySuggested += value;}
            remove {Delegate action = _isAutomaticRequeryDisabled ? () => CommandManagerHelper.RemoveWeakReferenceHandler(_canExecuteChangedHandlers, value) : () => CommandManager.RequerySuggested -= value;}
        }

        bool ICommand.CanExecute(object parameter)
        {
            return parameter switch
            {
                null when typeof(T).IsValueType => _canExecuteMethod == null,
                _ => CanExecute((T)parameter),
            };
        }

        void ICommand.Execute(object parameter)
        {
            Execute((T)parameter);
        }

        #endregion

        #region Data

        private readonly Action<T> _executeMethod = executeMethod;
        private readonly Func<T, bool> _canExecuteMethod = canExecuteMethod;
        private bool _isAutomaticRequeryDisabled = isAutomaticRequeryDisabled;
        private List<WeakReference> _canExecuteChangedHandlers;

        #endregion
    }


    /// <summary>
    ///     This class contains methods for the CommandManager that help avoid memory leaks by
    ///     using weak references.
    /// </summary>
    internal class CommandManagerHelper
    {
    internal static void CallWeakReferenceHandlers(List<WeakReference> handlers)
    {
        if (handlers == null)
        {
            return;
        }

        var handlersToCall = handlers.Where(h => h.Target is EventHandler)
                                     .Select(h => (EventHandler)h.Target)
                                     .ToList();

        handlersToCall.ForEach(h => h(null, EventArgs.Empty));

        handlers.RemoveAll(h => !handlersToCall.Contains(h.Target));
    }

    internal static void AddHandlersToRequerySuggested(List<WeakReference> handlers)
    {
        var activeHandlers = handlers?.Where(h => h.Target is EventHandler)
                                      .Select(h => (EventHandler)h.Target)
                                      .ToList();
        activeHandlers.ForEach(h => CommandManager.RequerySuggested += h);
    }


    internal static void RemoveHandlersFromRequerySuggested(List<WeakReference> handlers)
    {
        var activeHandlers = handlers?.Where(h => h.Target is EventHandler)
                                      .Select(h => (EventHandler)h.Target)
                                      .ToList();

        activeHandlers.ForEach(h => CommandManager.RequerySuggested -= h);
    }


    internal static void SetHandlersForRequerySuggested(List<WeakReference> handlers, bool value)
    {
        Action action = value switch

        {
            true => () => AddHandlersToRequerySuggested(handlers),
            false => () => RemoveHandlersFromRequerySuggested(handlers),
        };
        action();
    }

    internal static void AddWeakReferenceHandler(ref List<WeakReference> handlers, EventHandler handler)
    {
         AddWeakReferenceHandler(ref handlers, handler, -1);

    }

    internal static void AddWeakReferenceHandler(ref List<WeakReference> handlers, EventHandler handler, int defaultListSize)
    {
        handlers ??= defaultListSize > 0 ? new List<WeakReference>(defaultListSize) : [];
        handlers.Add(new WeakReference(handler));
    }

    internal static void RemoveWeakReferenceHandler(List<WeakReference> handlers, EventHandler handler)
    {
        if (handlers != null)
        {
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                WeakReference reference = handlers[i];

                switch (reference.Target)
                {
                    case EventHandler existingHandler when ReferenceEquals(existingHandler, handler):
                    case null:
                        // Clean up old handlers that have been collected
                        // in addition to the handler that is to be removed.
                        handlers.RemoveAt(i);
                        break;
                    default:
                        break;

                }

            }

        }
    }
}
}
