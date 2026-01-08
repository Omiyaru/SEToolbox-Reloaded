using System;
using System.Security.Permissions;
using System.Windows.Threading;

namespace SEToolbox.Services
{
    /// <summary>
    ///
    /// </summary>
    public static class DispatcherHelper
    {
        /// <summary>
        /// Simulate Application.DoEvents function of <see cref=" System.Windows.Forms.Application"/> class.
        /// </summary>
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static void DoEvents()
        {
            DispatcherFrame frame = new();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrames), frame);

            try
            {
                Dispatcher.PushFrame(frame);
            }
            catch (InvalidOperationException)
            {
            }
        }
        /// <summary>
        ///
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private static object ExitFrames(object frame)
        {
            var dispatcherFrame = frame as DispatcherFrame;
            dispatcherFrame.Continue = false;

            return null;
        }
    }
}
