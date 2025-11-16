using System.Windows;

namespace SEToolbox.Views
{
    /// <summary>
    /// Interaction logic for WindowFindApplication.xaml
    /// </summary>
    public partial class WindowFindApplication : Window
    {
        public WindowFindApplication()
        {
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.IetfLanguageTag);
            InitializeComponent();
        }

        public WindowFindApplication(object viewModel)
            : this()
        {
            DataContext = viewModel;
        }
    }
}
