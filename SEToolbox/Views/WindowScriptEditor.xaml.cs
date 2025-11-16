// using System;
// using System.Collections.Generic;
// using System.Collections.Specialized;
// using System.IO;
// using System.Linq;
// using System.Windows;
// using System.Windows.Controls;
// using Microsoft.Win32;

// namespace SEToolbox.Views
//  {
//     public partial class WindowScriptEditor : Window
//     {
//        private List<FileInfo> _allScripts;
//        private List<string> _scriptDirectories;

//        public WindowScriptEditor()
//        {
//            InitializeComponent();
//            LoadSavedPaths();
//            LoadScriptList();
//        }

//        private void LoadSavedPaths()
//        {
//            _scriptDirectories = Properties.Settings.Default.ScriptSearchPaths?.Cast<string>().ToList()
//                                 ?? new List<string>();

//            if (_scriptDirectories.Count == 0)
//            {
//                _scriptDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SpaceEngineers\\Mods"));
//                _scriptDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SpaceEngineers\\Saves"));
//                _scriptDirectories.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SpaceEngineers\\ScriptStorage"));
//            }
//        }

//         // private void SavePaths()
//         // {
//         //   .Default.ScriptSearchPaths = new StringCollection();
//         //   Default.ScriptSearchPaths.AddRange(_scriptDirectories.ToArray());
//         //    .Default.Save();
//         // }

//         private void LoadScriptList()
//         {
//            _allScripts = _scriptDirectories
//                .Where(Directory.Exists)
//                .SelectMany(d => Directory.GetFiles(d, "*.cs", SearchOption.AllDirectories))
//                .Select(f => new FileInfo(f))
//                .OrderBy(f => f.Name)
//                .ToList();

//            ScriptListBox.ItemsSource = _allScripts;
//            ScriptPreviewBox.Clear();
//            OpenInEditorButton.IsEnabled = false;
//         }

//         private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
//         {
//            string query = SearchBox.Text.Trim().ToLower();
//            var filtered = _allScripts.Where(f => f.Name.ToLower().Contains(query)).ToList();
//            ScriptListBox.ItemsSource = filtered;
//         }

//         private void ScriptListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//         {
//            if (ScriptListBox.SelectedItem is FileInfo selectedFile)
//            {
//                ScriptPreviewBox.Text = File.ReadAllText(selectedFile.FullName);
//                OpenInEditorButton.IsEnabled = true;
//            }
//            else
//            {
//                ScriptPreviewBox.Clear();
//                OpenInEditorButton.IsEnabled = false;
//            }
//         }

//         private void OpenInEditorButton_Click(object sender, RoutedEventArgs e)
//         {
//            if (ScriptListBox.SelectedItem is FileInfo selectedFile && selectedFile.Exists)
//            {
//                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
//                {
//                    FileName = selectedFile.FullName,
//                    UseShellExecute = true
//                });
//            }
//         }

//         private void ReloadScripts_Click(object sender, RoutedEventArgs e)
//         {
//            LoadScriptList();
//         }

//         private void ManagePaths_Click(object sender, RoutedEventArgs e)
//         {
//            {
//                var dlg = new WindowManagePaths(_scriptDirectories);
//                if (dlg.ShowDialog() == true)
//                {
//                    _scriptDirectories = dlg.UpdatedPaths;
//                    SavePaths();
//                    LoadScriptList();
//                }
//            }

//            var toRemove = MessageBox.Show("Do you want to remove any script paths?", "Manage Paths", MessageBoxButton.YesNo);
//            if (toRemove == MessageBoxResult.Yes)
//            {
//                var removeDialog = new WindowRemovePaths(_scriptDirectories);
//                if (removeDialog.ShowDialog())
//                {
//                    _scriptDirectories = removeDialog.UpdatedPaths;
//                    SavePaths();
//                    LoadScriptList();
//                }
//            }
//        }
//    }
// }