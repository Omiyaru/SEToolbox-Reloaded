using System.Windows;

namespace SEToolbox.Views
{
    /// <summary>
    /// Interaction logic for WindowBlueprintDialog.xaml
    /// </summary>
    public partial class WindowBlueprintDialog : Window
    {
        public WindowBlueprintDialog()
        {
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.IetfLanguageTag);
            InitializeComponent();
        }
    }
}
