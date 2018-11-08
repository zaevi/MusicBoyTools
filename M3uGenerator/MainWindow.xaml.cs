using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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
            dataGrid.PreviewKeyDown += DataGrid_PreviewKeyDown;
            dataGrid.ItemsSource = FileList;
            Loaded += MainWindow_Loaded;
            dataGrid.Sorting += DataGrid_Sorting;
        }

        private void DataGrid_Sorting(object sender, System.Windows.Controls.DataGridSortingEventArgs e)
        {
            var property = typeof(Tag).GetProperty(e.Column.SortMemberPath);
            IEnumerable<Tag> list;
            ListSortDirection sortDirection;
            if (e.Column.SortDirection == ListSortDirection.Ascending)
            {
                list = FileList.OrderByDescending(t => property.GetValue(t));
                sortDirection = ListSortDirection.Descending;
            }
            else
            {
                list = FileList.OrderBy(t => property.GetValue(t));
                sortDirection = ListSortDirection.Ascending;
            }
            FileList = new ObservableCollection<Tag>(list);
            dataGrid.ItemsSource = FileList;
            e.Column.SortDirection = sortDirection;
            e.Handled = true;

            Action focusAction = () => dataGrid.CurrentCell.GetDataGridCell()?.Focus();
            dataGrid.Dispatcher.BeginInvoke(DispatcherPriority.Background, focusAction);
        }

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftAlt) && (Keyboard.IsKeyDown(Key.Up) || Keyboard.IsKeyDown(Key.Down)))
            {
                foreach (var column in dataGrid.Columns)
                    column.SortDirection = null;
                var idx = FileList.IndexOf(dataGrid.SelectedItem as Tag);
                if (Keyboard.IsKeyDown(Key.Up) && idx > 0)
                    FileList.Move(idx - 1, idx);
                else if (Keyboard.IsKeyDown(Key.Down) && idx < dataGrid.Items.Count - 1)
                    FileList.Move(idx + 1, idx);
                e.Handled = true;
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
            var fileName = FileList.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Album))?.Album ?? "playlist";
            var dialog = new Forms.SaveFileDialog()
            {
                FileName = fileName + ".m3u", AddExtension = true, Filter = "M3u文件|*.m3u",
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
