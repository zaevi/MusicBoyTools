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
        public M3u CurrentM3u = null;

        public MainWindow()
        {
            InitializeComponent();
            dataGrid.PreviewKeyDown += DataGrid_PreviewKeyDown;
            Loaded += MainWindow_Loaded;
            dataGrid.Sorting += DataGrid_Sorting;
            InitCommands();
        }

        private void DataGrid_Sorting(object sender, System.Windows.Controls.DataGridSortingEventArgs e)
        {
            var property = typeof(Tag).GetProperty(e.Column.SortMemberPath);
            IEnumerable<Tag> list;
            ListSortDirection sortDirection;
            if (e.Column.SortDirection == ListSortDirection.Ascending)
            {
                list = CurrentM3u.FileList.OrderByDescending(t => property.GetValue(t));
                sortDirection = ListSortDirection.Descending;
            }
            else
            {
                list = CurrentM3u.FileList.OrderBy(t => property.GetValue(t));
                sortDirection = ListSortDirection.Ascending;
            }
            CurrentM3u.FileList = new ObservableCollection<Tag>(list);
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
                var idx = CurrentM3u.FileList.IndexOf(dataGrid.SelectedItem as Tag);
                if (Keyboard.IsKeyDown(Key.Up) && idx > 0)
                    CurrentM3u.FileList.Move(idx - 1, idx);
                else if (Keyboard.IsKeyDown(Key.Down) && idx < dataGrid.Items.Count - 1)
                    CurrentM3u.FileList.Move(idx + 1, idx);
                e.Handled = true;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentM3u = new M3u();
            DataContext = CurrentM3u;
            CurrentM3u.Changed = false;
        }

        private void LoadFolder(string path)
        {
            if (!Directory.Exists(path)) return;

            var list = new List<Tag>();

            foreach(var f in Directory.EnumerateFiles(path))
            {
                if (!f.IsAudio()) continue;
                var tag = M3uGenerator.Tag.ReadFrom(f);
                if (tag != null) list.Add(tag);
            }

            CurrentM3u = new M3u();
            CurrentM3u.FileList = new ObservableCollection<Tag>(list);
            DataContext = CurrentM3u;
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

        void InitCommands()
        {
            CommandBindings.AddRange(new[] {
                new CommandBinding(ApplicationCommands.New, (s, e) =>
                {
                    // New
                    CurrentM3u = new M3u();
                    DataContext = CurrentM3u;
                    CurrentM3u.Changed = false;
                }),
                new CommandBinding(ApplicationCommands.Open, (s, e) =>
                {
                    // Open
                }),
                new CommandBinding(ApplicationCommands.Save, (s, e) =>
                {
                    // Save
                    var fileName = CurrentM3u.FileList.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Album))?.Album ?? "playlist";
                    var dialog = new Forms.SaveFileDialog()
                    {
                        FileName = fileName + ".m3u", AddExtension = true, Filter = "M3u文件|*.m3u",
                        DefaultExt = "M3u文件|*.m3u"
                    };
                    if(dialog.ShowDialog() == Forms.DialogResult.OK)
                    {
                        Util.Generate(dialog.FileName, CurrentM3u.FileList.Select(t => t.Path)); //TODO
                    }
                }),
                new CommandBinding(ApplicationCommands.SaveAs, (s, e) =>
                {
                    // Save As
                }, (s, e) => e.CanExecute = CurrentM3u.CanSaveAs),
            });
        }
    }
}
