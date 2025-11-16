using System.Windows;

namespace SEToolbox.Views
{
    /// <summary>
    /// Interaction logic for WindowFrameworkBuild.xaml
    /// </summary>
    public partial class WindowFrameworkBuild : Window
    {
        public WindowFrameworkBuild()
        {
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.IetfLanguageTag);
            InitializeComponent();
        }
    }
}