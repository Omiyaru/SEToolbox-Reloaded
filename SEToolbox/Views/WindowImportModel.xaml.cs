using System.Windows;

namespace SEToolbox.Views
{
    /// <summary>
    /// Interaction logic for WindowImportModel.xaml
    /// </summary>
    public partial class WindowImportModel : Window
    {
        public WindowImportModel()
        {
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.IetfLanguageTag);
            InitializeComponent();
        }
    }
}