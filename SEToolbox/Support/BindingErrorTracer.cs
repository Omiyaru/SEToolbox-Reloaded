using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Markup;
using System.Windows.Input;
using System.ComponentModel;
using System.IO;



namespace SEToolbox.Support
{
    public class BindingErrorTraceListener : DefaultTraceListener
    {   
        
        private static  BindingErrorTraceListener Listener = new();
        
        public static void SetTrace()
        {
            SetTrace(SourceLevels.Error, TraceOptions.None);
        }

        public static void SetTrace(SourceLevels level, TraceOptions options)
        {
            if (Listener == null)
            {               
                Listener = new BindingErrorTraceListener();
                PresentationTraceSources.DataBindingSource.Listeners.Add(Listener);
            }
            Listener.TraceOutputOptions = options;
            PresentationTraceSources.DataBindingSource.Switch.Level = level;
            Console.WriteLine("Binding Error Trace Set");
            Debug.WriteLine("Binding Error Trace Set");
        }

        public static void CloseTrace()
        {
            if (Listener == null)
            {
                return;
            }
            Listener.Flush();
            Listener.Close();
            PresentationTraceSources.DataBindingSource.Listeners.Remove(Listener);
            Listener = null;
        }

        private readonly StringBuilder _Message = new();
     
        public BindingErrorTraceListener()
        {
            _Message = new StringBuilder();
        }

        public override void Write(string message)
        {
            _Message.Append(message);
        }

        public override void WriteLine(string message)
        {
            int length = _Message.Length;
            if (length > 10000)
            {
                string bindingError = _Message.ToString();
                _Message.Clear();
                Application.Current.Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        GetBindingError(new object(), new RoutedEventArgs());
                        //MessageBox.Show(bindingError, "Binding Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Debug.WriteLine(bindingError, "Binding Error");
                        Console.WriteLine(bindingError, "Binding Error");
                    }), DispatcherPriority.Normal, null, bindingError, "Binding Error");
                throw new InvalidOperationException("Too many binding errors logged. See binding error log.");
            }
            else
            {
                _Message.AppendLine(message);
            }
        }
    
       
        public static readonly List<Type> WPFFeatures =
        [
            typeof(FrameworkPropertyMetadata),
            typeof(FrameworkElementFactory),
            typeof(FrameworkContentElement),
            typeof(HeaderedContentControl),
            typeof(BindingExpressionBase),
            typeof(DataTemplateSelector),
            //typeof(HeaderedItemsControl),
            typeof(UIElementCollection),
            typeof(InheritanceBehavior),
            typeof(XamlParseException),
            typeof(ResourceDictionary),
            typeof(DependencyProperty),
            typeof(RoutedEventHandler),
            
            typeof(BindableAttribute),
            typeof(BindingExpression),
            typeof(FrameworkTemplate),
            typeof(FrameworkElement),
            typeof(PropertyMetadata),
            typeof(DependencyObject),
            typeof(DispatcherObject),
            typeof(ControlTemplate),
            typeof(RoutedEventArgs),
            typeof(MarkupExtension),
            typeof(ContentControl),
            typeof(ICommandSource),
            typeof(ContentElement),
            typeof(ItemsControl),
            typeof(IInputElement),
            typeof(DataTemplate),
            typeof(BindingExpression),
            typeof(ItemsControl),
            typeof(EventHandler),
            //typeof(MultiBinding),
            //typeof(MultiTrigger),
            typeof(RoutedEvent),
            typeof(BindingBase),
            typeof(TriggerBase),
            typeof(UIElement3D),
            typeof(UIElement),
            typeof(Attribute),
            typeof(IAddChild),
            typeof(EventArgs),
            typeof(Binding),
            typeof(System.Windows.Controls.Control),
            //typeof(Trigger),
            typeof(Style),

        ];

        public void GetBindingError(object sender, RoutedEventArgs e)
        {
            if (sender is not DependencyObject dependencyObject)
            {
                return;
            }

            var dataContext = GetDataContext(dependencyObject);
            if (dataContext == DependencyProperty.UnsetValue)
            {
                return;
            }

            if (dataContext == null)
            {
                Debug.WriteLine($"Binding Error: {dataContext}" + " " + Environment.NewLine + new StackTrace());
            }

            var bindingExpression = BindingOperations.GetBindingExpression(dependencyObject, (DependencyProperty)dataContext);
            if (bindingExpression?.ValidationErrors.Count > 0)
            {
                foreach (var error in bindingExpression.ValidationErrors)
                {
                    var errorFeature = WPFFeatures.FirstOrDefault(fn => error.ErrorContent.ToString().Contains(fn.Name));
                    LogValidationError(error.ErrorContent as string);
                    Log.WriteLine($"Binding Error: {error.ErrorContent} {errorFeature?.Name} {dataContext} " + (error.ErrorContent as string));
                }
            }
        }
        


        private object GetDataContext(DependencyObject dependencyObject)
        {   
            return dependencyObject is FrameworkElement frameworkElement
                ? frameworkElement.GetValue(FrameworkElement.DataContextProperty)
                : DependencyProperty.UnsetValue;
        }

        private void LogValidationError(string validationError)
        {
            var errorFeature = WPFFeatures.FirstOrDefault(fn => validationError.Contains(fn.Name));
            var errorMessage = errorFeature != null
                ? $"WPF Error: {errorFeature.Name}"
                : validationError;

            Debug.WriteLine(errorMessage + Environment.NewLine + new StackTrace());
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BindingErrors.log");
            var content = $"Binding Error: {errorMessage} {DateTime.Now}{Environment.NewLine}{new StackTrace()}{Environment.NewLine}";
            File.AppendAllText(path, content);
        }

    }
}

