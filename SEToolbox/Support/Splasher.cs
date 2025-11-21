using System.Windows;

namespace SEToolbox.Support
{
    /// <summary>
    /// Helper to manage the splash screen window.
    /// </summary>
    public static class Splasher
    {
        /// <summary>
        ///
        /// </summary>
        private static Window _splash;

        /// <summary>
        /// Gets or sets the splash screen window.
        /// </summary>
        public static Window Splash
        {
            get => _splash;
            set => _splash = value;
        }
        /// <summary>
        /// Displays the splash screen if it is set.
        /// </summary>
        public static void ShowSplash()
        {
            _splash?.Show();
            System.Windows.Forms.Application.DoEvents();
         }
    
        /// <summary>
        /// Closes the splash screen if it is set.
        /// </summary>
        public static void CloseSplash()
        {
            _splash?.Close();
        }
    }
}