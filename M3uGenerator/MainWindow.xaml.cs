using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace M3uGenerator
{

    public partial class MainWindow : Window
    {
        private M3u _currentM3u = null;
        public M3u CurrentM3u { get => _currentM3u; set {
                _currentM3u = value;
                DataContext = CurrentM3u;
            } }

        public MainWindow()
        {
            InitializeComponent();
            dataGrid.PreviewKeyDown += DataGrid_PreviewKeyDown;
            Loaded += MainWindow_Loaded;
            dataGrid.Sorting += DataGrid_Sorting;
            Closing += MainWindow_Closing;
            InitCommands();

            var menu = new ContextMenu();
            dataGrid.ContextMenu = menu;
            var binding = new Binding("Visibility") { Converter = new VisibilityConverter(), Mode = BindingMode.TwoWay };
            foreach (var column in dataGrid.Columns)
            {
                var item = new MenuItem() { Header = column.Header, IsCheckable = true, DataContext=column };
                item.SetBinding(MenuItem.IsCheckedProperty, binding);
                menu.Items.Add(item);
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!CheckAndShowSaveMessage()) e.Cancel = true;
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
            ApplicationCommands.New.Execute(null, null);
        }

        private void AddFolder(IEnumerable<string> fileNames)
        {
            foreach(var fileName in fileNames)
            {
                if (!fileName.IsAudio()) continue;
                var tag = M3uGenerator.Tag.ReadFrom(fileName);
                if (tag != null) CurrentM3u.FileList.Add(tag);
            }
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                if (e.Data.GetData(DataFormats.FileDrop) is string[] fileNames)
                {
                    if (fileNames.Length == 1)
                    {
                        if (fileNames[0].EndsWith(".m3u", StringComparison.OrdinalIgnoreCase)
                            && CurrentM3u.FilePath == null && CurrentM3u.FileList.Count == 0)
                        {
                            e.Effects = DragDropEffects.Copy;
                            return;
                        }
                    }
                    e.Effects = DragDropEffects.Move;
                }
        }

        private void Window_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] fileNames)
            {
                if(fileNames.Length == 1)
                {
                    if (fileNames[0].EndsWith(".m3u", StringComparison.OrdinalIgnoreCase)
                        && CurrentM3u.FilePath == null && CurrentM3u.FileList.Count == 0)
                    {
                        ApplicationCommands.Open.Execute(fileNames[0], null);
                        return;
                    }
                    else if(Directory.Exists(fileNames[0]))
                    {
                        AddFolder(Directory.GetFiles(fileNames[0]));
                        return;
                    }
                }
                AddFolder(fileNames);
                return;
            }
        }

        void InitCommands()
        {
            CommandBindings.AddRange(new[] {
                new CommandBinding(ApplicationCommands.New, (s, e) =>
                {
                    // New
                    if(!CheckAndShowSaveMessage()) return;
                    CurrentM3u = new M3u();
                    CurrentM3u.Changed = false;
                }),
                new CommandBinding(ApplicationCommands.Open, (s, e) =>
                {
                    // Open
                    if(!CheckAndShowSaveMessage()) return;
                    if(!(e.Parameter is string fileName))
                    {
                        var dialog = new Forms.OpenFileDialog()
                        {
                            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonMusic),
                            Filter = "M3u文件|*.m3u",
                        };
                        if(dialog.ShowDialog() != Forms.DialogResult.OK) return;
                        fileName = dialog.FileName;
                    }
                    try
                    {
                        var m3u = M3u.Load(fileName);
                        CurrentM3u = m3u;
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show($"{ex.GetType().Name}\r\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }),
                new CommandBinding(ApplicationCommands.Save, (s, e) =>
                {
                    // Save
                    string filePath = null;
                    if(CurrentM3u.FileName == null)
                    {
                        var fileName = CurrentM3u.FileList.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Album))?.Album ?? "playlist";
                        var dialog = new Forms.SaveFileDialog()
                        {
                            FileName = fileName + ".m3u", AddExtension = true, Filter = "M3u文件|*.m3u",
                            DefaultExt = "M3u文件|*.m3u"
                        };
                        if(dialog.ShowDialog() != Forms.DialogResult.OK) return;
                        filePath = dialog.FileName;
                    }
                    try
                    {
                        CurrentM3u.Save(filePath);
                        CurrentM3u.Changed = false;
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show($"{ex.GetType().Name}\r\n{ex.Message}", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }),
                new CommandBinding(ApplicationCommands.SaveAs, (s, e) =>
                {
                    // Save As
                    var dialog = new Forms.SaveFileDialog()
                    {
                        FileName = CurrentM3u.FileName, InitialDirectory = Path.GetDirectoryName(CurrentM3u.FilePath),
                        AddExtension = true, Filter = "M3u文件|*.m3u", DefaultExt = "M3u文件|*.m3u"
                    };
                    if(dialog.ShowDialog() != Forms.DialogResult.OK) return;
                    try
                    {
                        CurrentM3u.Save(dialog.FileName);
                        CurrentM3u.Changed = false;
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show($"{ex.GetType().Name}\r\n{ex.Message}", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }, (s, e) => e.CanExecute = CurrentM3u.CanSaveAs),
            });
        }

        private bool CheckAndShowSaveMessage()
        {
            if (CurrentM3u == null || CurrentM3u.Changed) return true;
            var result = MessageBox.Show("是否要保存当前文件?", "", MessageBoxButton.YesNoCancel);
            if (result == MessageBoxResult.Cancel)
                return false;
            else if(result == MessageBoxResult.Yes)
            {
                ApplicationCommands.Save.Execute(null, null);
                return !CurrentM3u.Changed;
            }
            else
            {
                return true;
            }
        }
    }
}
