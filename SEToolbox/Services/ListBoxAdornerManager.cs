using System.Windows;
using System.Windows.Documents;

namespace SEToolbox.Services
{
    internal class ListBoxAdornerManager
    {
        private readonly AdornerLayer adornerLayer;
        private ListBoxDropAdorner adorner;

        private bool shouldCreateNewAdorner = false;

        internal ListBoxAdornerManager(AdornerLayer layer)
        {
            adornerLayer = layer;
        }

        internal void UpdateDropIndicator(UIElement adornedElement, bool isAboveElement)
        {
        	//exit if nothing changed
            if (!shouldCreateNewAdorner && adorner?.AdornedElement == adornedElement && adorner.IsAboveElement == isAboveElement)
            {
                return;
            }
            Clear();
            //draw new adorner
            adorner = new ListBoxDropAdorner(adornedElement, adornerLayer)
            {
                IsAboveElement = isAboveElement
            };
            adorner.Update();
            shouldCreateNewAdorner = false;
        }

        /// <summary>
        /// Remove the adorner
        /// </summary>
        internal void Clear()
        {
            adorner?.Remove();
            shouldCreateNewAdorner = adorner == null;
        }
    }
}
