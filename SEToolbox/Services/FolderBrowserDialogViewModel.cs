using SEToolbox.Interfaces;

namespace SEToolbox.Services
{
    /// <summary>
    /// ViewModel of the FolderBrowserDialog initializes a new instance of the <see cref="FolderBrowserDialogViewModel"/> class.
    /// </summary>
    public class FolderBrowserDialogViewModel() : IFolderBrowserDialog
    {

        public string Description { get; set; } = string.Empty;

        public string SelectedPath { get; set; } = string.Empty;

        public bool ShowNewFolderButton { get; set; } = true;
    }
}
