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
using System.Windows.Forms;



namespace SEToolbox.Support
{
    public class BindingErrorTraceListener : DefaultTraceListener
    {

        private static BindingErrorTraceListener Listener = new();
        private bool includeXaml;

        public static void SetTrace()
        {
            SetTrace(SourceLevels.Error, TraceOptions.Callstack);
        }

        public static void SetTrace(SourceLevels level, TraceOptions options, bool includeXaml = false)
        {

            Listener ??= new BindingErrorTraceListener() { includeXaml = includeXaml };
            PresentationTraceSources.DataBindingSource.Listeners.Add(Listener);

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
                _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        GetBindingError(new object(), new RoutedEventArgs());
                        //System.Windows.MessageBox.Show(bindingError, "Binding Error", MessageBoxButton.OK, MessageBoxImage.Error);
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


        public static readonly IReadOnlyList<Type> WPFFeatures = new List<Type>
        {
            typeof(FrameworkPropertyMetadata),
            typeof(FrameworkElementFactory),
            typeof(FrameworkContentElement),
            typeof(HeaderedContentControl),
            typeof(BindingExpressionBase),
            typeof(DataTemplateSelector),
            typeof(UIElementCollection),
            typeof(InheritanceBehavior),
            typeof(XamlParseException),
            typeof(ResourceDictionary),
            typeof(DependencyProperty),
            typeof(RoutedEventHandler),
            typeof(RoutedEvent),
            typeof(RoutedEventArgs),
            typeof(PresentationSource),
            typeof(BindableAttribute),
            typeof(BindingExpression),
            typeof(FrameworkTemplate),
            typeof(FrameworkElement),
            typeof(PropertyMetadata),
            typeof(ButtonBase),
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
            typeof(RoutedEvent),
            typeof(BindingBase),
            typeof(TriggerBase),
            typeof(UIElement3D),
            typeof(UIElement),
            typeof(Attribute),
            typeof(IAddChild),
            typeof(EventArgs),
            typeof(IComponent),
            typeof(IContainer),
            typeof(INotifyPropertyChanged),
            typeof(INotifyPropertyChanging),
            typeof(IProvideValueTarget),
            typeof(ResourceDictionary),
            typeof(Window),
            typeof(Style),
            typeof(DependencyProperty),
            typeof(DependencyObject),
            typeof(DispatcherObject),
            typeof(FrameworkElement),
            typeof(FrameworkContentElement),
            typeof(ContentControl),
            typeof(ContentElement),
        }.AsReadOnly();

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
                    Debug.WriteLine($"Binding Error: {error.ErrorContent} {errorFeature?.Name} {dataContext} " + (error.ErrorContent as string));
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
              Window root = null;  
                var xaml = "";
            if (includeXaml && System.Windows.Application.Current != null && root != null && root.Content is FrameworkElement frameworkElement) 
            {   
                root = System.Windows.Application.Current.MainWindow;
                xaml = XamlWriter.Save(root.Content);
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BindingErrors.log");
                var content = $"Binding Error: {errorMessage} {DateTime.Now}{Environment.NewLine}{new StackTrace()}{Environment.NewLine}{xaml}{Environment.NewLine}";
                File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BindingErrors.log"),
                    $"Binding Error: {errorMessage} {DateTime.Now}{Environment.NewLine}{new StackTrace()}{Environment.NewLine}{xaml}{Environment.NewLine}");
            }
            else
            {
                Debug.WriteLine(errorMessage + Environment.NewLine + new StackTrace());
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BindingErrors.log");
                var content = $"Binding Error: {errorMessage} {DateTime.Now}{Environment.NewLine}{new StackTrace()}{Environment.NewLine}";
                File.AppendAllText(path, content);
            }
        }
    }
}

