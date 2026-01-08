using System;
using System.Windows;

namespace SEToolbox.Services
{
    public static class DialogCloser
    {
        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.RegisterAttached("DialogResult",
                                                typeof(bool?),
                                                typeof(DialogCloser),
                                                new PropertyMetadata(DialogResultChanged));

        private static void DialogResultChanged( DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            if (depObj is Window window )
            {
                try
                {
                    window.DialogResult = e.NewValue as bool?;
                }
                catch (InvalidOperationException)
                {
                    window.Close();
                }
                catch
                {
                    // Ignore non-modal error.
                }
            }
        }
        public static void SetDialogResult(Window target, bool? value)
        {
            target.SetValue(DialogResultProperty, value);
        }
    }
}
