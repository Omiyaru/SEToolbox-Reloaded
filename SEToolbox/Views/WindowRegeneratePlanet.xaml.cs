using System.Windows;

namespace SEToolbox.Views
{
    /// <summary>
    /// Interaction logic for WindowRegeneratePlanet.xaml
    /// </summary>
    public partial class WindowRegeneratePlanet : Window
    {
        public WindowRegeneratePlanet()
        {
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.IetfLanguageTag);
            InitializeComponent();
        }
    }
}
