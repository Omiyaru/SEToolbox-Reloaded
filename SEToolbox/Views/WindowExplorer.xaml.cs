using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Shell;

using System.Windows.Markup;
using System.Threading;


namespace SEToolbox.Views
{
    /// <summary>
    /// Interaction logic for WindowExplorer.xaml
    /// </summary>
    public partial class WindowExplorer : Window
    {
        // public ListBox ControlMultiple { get; set; }
        // public ListView SortableListView { get; set; }
        // public TabControl TabControl { get; set; }

        public WindowExplorer()
        {
            Language = XmlLanguage.GetLanguage(Thread.CurrentThread.CurrentCulture.IetfLanguageTag);
            
            InitializeComponent();
            InitializeTaskbar();
        }

        private void InitializeTaskbar()
        {
            try
            {
                object taskbarList = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("56FDF344-FD6D-11d0-958A-006097C9A090")));
                taskbarList = null;

                TaskbarItemInfo taskbar = new();
                BindingOperations.SetBinding(taskbar, TaskbarItemInfo.ProgressStateProperty, new Binding("ProgressState"));
                BindingOperations.SetBinding(taskbar, TaskbarItemInfo.ProgressValueProperty, new Binding("ProgressValue"));
                taskbar.ProgressState = TaskbarItemProgressState.Normal;
                taskbar.ProgressValue = 0;
                TaskbarItemInfo = taskbar;
            }
            catch
            {
                // This is to replace the Xaml code below that implments TaskbarInfoItem, and instead do it through code, so that it can catch a little known error.
                //    System.OutOfMemoryException: Retrieving the COM class factory for component with CLSID {56FDF344-FD6D-11D0-958A-006097C9A090} failed
                //    due to the following error: 8007000e Not enough storage is available to complete this operation. (Exception from HRESULT: 0x8007000E (E_OUTOFMEMORY)).
                // The cause of this error is unknown, but there have been 4 reported cases of this issue during the life of SEToolbox so far.
                // In this case, the progress bar has been coded to gracefully degrade if this issue occurs.

                // <Window.TaskbarItemInfo>
                //     <TaskbarItemInfo ProgressState="{Binding ProgressState}" ProgressValue="{Binding ProgressValue}" />
                // </Window.TaskbarItemInfo>
            }

        }

        public WindowExplorer(object viewModel)
            : this()
        {
            DataContext = viewModel;
        }
    }
}
