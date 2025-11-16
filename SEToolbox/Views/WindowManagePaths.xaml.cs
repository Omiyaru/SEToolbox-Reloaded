using System.Collections.ObjectModel;
using System.Windows;

using System.Windows.Controls;
namespace SEToolbox.Views
{
    public partial class WindowManagePaths : Window
    {
        public ObservableCollection<string> Paths { get; private set; }
        public System.Collections.Generic.List<string> UpdatedPaths { get; private set; }
        //public object PathsListBox { get; }
        public TextBox PathTextBox { get; private set; }

        public WindowManagePaths(System.Collections.Generic.List<string> currentPaths)
        {
            InitializeComponent();
            Paths = new ObservableCollection<string>(currentPaths);
            ((ListBox)PathsListBox).ItemsSource = Paths;
             NewPathTextBox = new TextBox();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select folder to add",
                ShowNewFolderButton = true
            };
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PathTextBox.Text = dlg.SelectedPath;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string path = PathTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(path) && !Paths.Contains(path))
            {
                Paths.Add(path);
                NewPathTextBox.Clear();
            }
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
           var selected = (PathsListBox as ListBox).SelectedItems;
            if (selected.Count == 0)
                return;

            var toRemove = new System.Collections.Generic.List<string>();
            foreach (var item in selected)
                toRemove.Add(item as string);

            foreach (var path in toRemove)
                Paths.Remove(path);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            UpdatedPaths = [.. Paths];
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
