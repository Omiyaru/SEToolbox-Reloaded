using System.Windows;

namespace SEToolbox.Views
{
    /// <summary>
    /// Interaction logic for WindowSelectCube.xaml
    /// </summary>
    public partial class WindowSelectCube : Window
    {
        public WindowSelectCube()
        {
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.IetfLanguageTag);
            InitializeComponent();
        }
    }
}
