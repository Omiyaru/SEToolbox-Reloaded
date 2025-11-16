using System.Windows;

namespace SEToolbox.Views
{
    /// <summary>
    /// Interaction logic for WindowGenerateFloatingObject.xaml
    /// </summary>
    public partial class WindowGenerateFloatingObject : Window
    {
        public WindowGenerateFloatingObject()
        {
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.IetfLanguageTag);
            InitializeComponent();
        }
    }
}
