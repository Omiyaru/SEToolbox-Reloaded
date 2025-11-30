using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace SEToolbox.Services
{
    public class CommandAction : TargetedTriggerAction<FrameworkElement>, ICommandSource
    {
        #region Dependency Properties

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(CommandAction), new PropertyMetadata(null, OnCommandChanged));
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register("CommandParameter", typeof(object), typeof(CommandAction), new PropertyMetadata());
        public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.Register("CommandTarget", typeof(IInputElement), typeof(CommandAction), new PropertyMetadata());
        public static readonly DependencyProperty SyncOwnerIsEnabledProperty = DependencyProperty.Register("SyncOwnerIsEnabled", typeof(bool), typeof(CommandAction), new PropertyMetadata());
        public static readonly DependencyProperty EventArgsProperty = DependencyProperty.Register("EventArgs", typeof(bool), typeof(CommandAction), new PropertyMetadata());

        #endregion

        #region Properties

        [Category("Command Properties")]
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
			set => SetValue(CommandProperty, value);
        }

        [Category("Command Properties")]
        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
			set => SetValue(CommandParameterProperty, value);
        }

        [Category("Command Properties")]
        public IInputElement CommandTarget
        {
            get => (IInputElement)GetValue(CommandTargetProperty);
			set => SetValue(CommandTargetProperty, value);
        }

        [Category("Command Properties")]
        public bool SyncOwnerIsEnabled
        {
            get => (bool)GetValue(SyncOwnerIsEnabledProperty);
			set => SetValue(SyncOwnerIsEnabledProperty, value);
        }

        [Category("Command Properties")]
        public bool EventArgs
        {
            get => (bool)GetValue(EventArgsProperty);
			set => SetValue(EventArgsProperty, value);
        }

        #endregion

        #region Event Declaration

        private EventHandler _canExecuteChanged;

        #endregion

        #region Event Handlers

        private void OnCanExecuteChanged(object sender, EventArgs e)
        {
            UpdateCanExecute();
        }

        #region Dependency Property Event Handlers

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CommandAction)d).OnCommandChanged((ICommand)e.OldValue, (ICommand)e.NewValue);
        }

        #endregion

        #endregion

        #region Overrides

        [DebuggerStepThrough]
        protected override void Invoke(object o)
        {
            if (Command is RoutedCommand routedCommand)
            {
                routedCommand.Execute(EventArgs ? o : CommandParameter, CommandTarget);
            }
            else
            {
                Command.Execute(EventArgs ? o : CommandParameter);
             
            }

        }
        

        #endregion

        #region Helper Functions

        private void OnCommandChanged(ICommand comOld, ICommand comNew)
        {
            if (comOld != null)
            {
                UnhookCommandCanExecuteChangedEventHandler(comOld);
            }
            if (comNew != null)
            {
                HookupCommandCanExecuteChangedEventHandler(comNew);
            }

        }

        private void HookupCommandCanExecuteChangedEventHandler(ICommand command)
        {
            _canExecuteChanged = new EventHandler(OnCanExecuteChanged);
            command.CanExecuteChanged += _canExecuteChanged;
            UpdateCanExecute();
        }

        private void UnhookCommandCanExecuteChangedEventHandler(ICommand command)
        {
            command.CanExecuteChanged -= _canExecuteChanged;
            UpdateCanExecute();
        }

        private void UpdateCanExecute()
        {
                bool canExecute = Command is RoutedCommand routedCommand
                    ? routedCommand.CanExecute(CommandParameter, CommandTarget)
                    : Command.CanExecute(CommandParameter);

                if (Target != null && SyncOwnerIsEnabled)
                    Target.IsEnabled = canExecute;
        }

        #endregion
    }
}
