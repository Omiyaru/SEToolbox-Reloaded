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
            //double width = AdornedElement.DesiredSize.Width;
            //double height = AdornedElement.DesiredSize.Height;

            Rect adornedElementRect = new(AdornedElement.DesiredSize);

            SolidColorBrush renderBrush = new(Colors.Red);
            renderBrush.Opacity = 0.5;
            Pen renderPen = new Pen(new SolidColorBrush(Colors.White), 1.5);
            double renderRadius = 5.0;

            if (IsAboveElement)
            {
                drawingContext.DrawEllipse(renderBrush, renderPen, adornedElementRect.TopLeft, renderRadius, renderRadius);
                drawingContext.DrawEllipse(renderBrush, renderPen, adornedElementRect.TopRight, renderRadius, renderRadius);
            }
            else
            {
                drawingContext.DrawEllipse(renderBrush, renderPen, adornedElementRect.BottomLeft, renderRadius, renderRadius);
                drawingContext.DrawEllipse(renderBrush, renderPen, adornedElementRect.BottomRight, renderRadius, renderRadius);
            }
        }
    }
}
