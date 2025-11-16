using System.Windows;

namespace SEToolbox.Views
{
    /// <summary>
    /// Interaction logic for WindowProgressCancel.xaml
    /// </summary>
    public partial class WindowProgressCancel : Window
    {
        public WindowProgressCancel()
        {
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.IetfLanguageTag);
            InitializeComponent();
        }

        public WindowProgressCancel(object viewModel)
            : this()
        {
            DataContext = viewModel;
        }
    }
}
