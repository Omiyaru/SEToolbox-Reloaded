using System.Windows;

namespace SEToolbox.Views
{
    /// <summary>
    /// Interaction logic for WindowGenerateVoxelField.xaml
    /// </summary>
    public partial class WindowGenerateVoxelField : Window
    {
        public WindowGenerateVoxelField()
        {
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.IetfLanguageTag);
            InitializeComponent();
        }
    }
}
