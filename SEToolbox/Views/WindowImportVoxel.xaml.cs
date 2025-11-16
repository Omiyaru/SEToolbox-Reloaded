using System.Windows;

namespace SEToolbox.Views
{
    /// <summary>
    /// Interaction logic for WindowImportVoxel.xaml
    /// </summary>
    public partial class WindowImportVoxel : Window
    {
        public WindowImportVoxel()
        {
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.IetfLanguageTag);
            InitializeComponent();
        }
    }
}
