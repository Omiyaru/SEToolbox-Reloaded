using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace SEToolbox.Services
{
    internal class ListBoxDropAdorner : Adorner
    {
        private readonly AdornerLayer adornerLayer;

        public bool IsAboveElement { get; set; }

        public ListBoxDropAdorner(UIElement adornedElement, AdornerLayer adornerLayer)
            : base(adornedElement)
        {
            this.adornerLayer = adornerLayer;
            adornerLayer.Add(this);
        }

        /// <summary>
        /// Update UI
        /// </summary>
        internal void Update()
        {
            adornerLayer.Update(AdornedElement);
            Visibility = Visibility.Visible;
        }

        public void Remove()
        {
            Visibility = Visibility.Collapsed;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var adornedElementRect = new Rect(AdornedElement.DesiredSize);
            var renderPen = new Pen(Brushes.White, 1.5);
            var renderBrush = new SolidColorBrush(Colors.Red) { Opacity = 0.5 };
            var renderRadius = 5.0;

            var points = new[] { adornedElementRect.TopLeft, adornedElementRect.TopRight };
            if (!IsAboveElement)
            {
                points = [adornedElementRect.BottomLeft, adornedElementRect.BottomRight];
            }

            drawingContext.DrawEllipse(renderBrush, renderPen, points[0], renderRadius, renderRadius);
            drawingContext.DrawEllipse(renderBrush, renderPen, points[1], renderRadius, renderRadius);
        }
    }
}
