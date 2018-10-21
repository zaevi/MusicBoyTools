using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Forms = System.Windows.Forms;

namespace M3uGenerator
{

    public partial class MainWindow : Window
    {
        ObservableCollection<Tag> FileList = new ObservableCollection<Tag>();

        string CurrentFolder = null;

        public MainWindow()
        {
            InitializeComponent();
            dataGrid.KeyDown += DataGrid_PreviewKeyDown;
            dataGrid.ItemsSource = FileList;
            Loaded += MainWindow_Loaded;
        }

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftAlt) && (Keyboard.IsKeyDown(Key.Up) || Keyboard.IsKeyDown(Key.Down)))
            {
                foreach (var column in dataGrid.Columns)
                    column.SortDirection = null;
                e.Handled = true;
                var idx = dataGrid.SelectedIndex;
                Tag tag;
                if (Keyboard.IsKeyDown(Key.Up) && idx > 0)
                    tag = dataGrid.Items[idx - 1] as Tag;
                else if (Keyboard.IsKeyDown(Key.Down) && idx < dataGrid.Items.Count - 1)
                    tag = dataGrid.Items[idx + 1] as Tag;
                else
                    return;
                FileList.Remove(tag);
                FileList.Insert(idx, tag);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void LoadFolder(string path)
        {
            if (!Directory.Exists(path)) return;

            CurrentFolder = path;

            var list = new List<Tag>();

            foreach(var f in Directory.EnumerateFiles(path))
            {
                if (!f.IsAudio()) continue;
                var tag = M3uGenerator.Tag.ReadFrom(f);
                if (tag != null) list.Add(tag);
            }
            FileList = new ObservableCollection<Tag>(list.Sorted());
            dataGrid.ItemsSource = FileList;
        }

        private void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Forms.SaveFileDialog()
            {
                FileName = "playlist.m3u", AddExtension = true, Filter = "M3u文件|*.m3u",
                DefaultExt = "M3u文件|*.m3u", InitialDirectory=CurrentFolder
            };
            if(dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                Util.Generate(dialog.FileName, FileList.Select(t => t.Path));
            }
        }

        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Forms.FolderBrowserDialog()
            {
                ShowNewFolderButton = false,
                Description = "选择包含音乐文件的目录"
            };
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
                LoadFolder(dialog.SelectedPath);
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                if (e.Data.GetData(DataFormats.FileDrop) is string[] fileNames)
                    if (fileNames.Length == 1 && Directory.Exists(fileNames[0]))
                    {
                        e.Effects = DragDropEffects.All;
                        return;
                    }
            e.Effects = DragDropEffects.None;
        }

        private void Window_PreviewDrop(object sender, DragEventArgs e)
        {
            if(e.Data.GetData(DataFormats.FileDrop) is string[] fileNames)
            {
                if (fileNames.Length == 1 && Directory.Exists(fileNames[0]))
                {
                    LoadFolder(fileNames[0]);
                }
            }
        }
    }
}
