using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace SEToolbox.Services
{
    // Used on sub-controls of an expander to bubble the mouse wheel scroll event up
    public sealed class BubbleScrollBehavior : Behavior<UIElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
            base.OnDetaching();
        }

        void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            RoutedEvent routedEvent = UIElement.MouseWheelEvent;
            MouseWheelEventArgs e2 = new(e.MouseDevice, e.Timestamp, e.Delta);
            e2.RoutedEvent = routedEvent;
            AssociatedObject.RaiseEvent(e2);
        }
    }
}