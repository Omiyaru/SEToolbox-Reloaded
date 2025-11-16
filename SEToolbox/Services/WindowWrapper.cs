using System;
using System.Windows;
using System.Windows.Forms;
using WindowInteropHelper = System.Windows.Interop.WindowInteropHelper;

namespace SEToolbox.Services
{
    /// <summary>
    /// WindowWrapper is an IWin32Window wrapper around a WPF window.
    /// </summary>
    /// <remarks>
    /// Construct a new wrapper taking a WPF window.
    /// </remarks>
    /// <param name="window">The WPF window to wrap.</param>
    class WindowWrapper(Window window) : IWin32Window
    {

        /// <summary>
        /// Gets the handle to the window represented by the implementer.
        /// </summary>
        /// <returns>A handle to the window represented by the implementer.</returns>
        public IntPtr Handle { get; private set; } = new WindowInteropHelper(window).Handle;
    }
}
