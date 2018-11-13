using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

namespace XiamiTags
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        const string Format = @"%album% // %albumartist% // %year% // %genre% // %discnumber% // %track% // %title% // %artist% // %comment%";

        Album Album = null;

        public MainWindow()
        { 
            InitializeComponent();
        }

        private void Refresh()
        {
            if (Album == null) return;
            imageBox.Source = new BitmapImage(new Uri(Album.CoverPreviewUrl));
            albumText.DataContext = Album;
            listView.Items.Clear();

            var disc = "";
            foreach(var track in Album.Tracks)
            {
                if(disc != track.DiscNumber)
                {
                    disc = track.DiscNumber;
                    listView.Items.Add(new ListViewItem { Content = $"Disc {disc}", IsEnabled = false });
                }

                var sb = new StringBuilder();
                sb.Append($"{track.TrackNumber} - {track.Title}");
                if (track.Artist != track.Album.Artist) sb.Append($" ({track.Artist})");
                if (!string.IsNullOrEmpty(track.Comment)) sb.Append($" //{track.Comment}");
                var item = new ListViewItem { Content = sb.ToString(), DataContext = track };
                item.MouseDoubleClick += trackItem_Click;
                listView.Items.Add(item);
            }

        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            if(Clipboard.ContainsText())
            {
                var html = Clipboard.GetText();
                Album = TagParser.LoadFrom(html);
                if(Album == null)
                {
                    return;
                }
                albumInfo.Visibility = Visibility.Visible;
                btnExport.IsEnabled = btnCopy.IsEnabled = btnCover.IsEnabled = true;
                Refresh();
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (Album == null) return;
            var dialog = new SaveFileDialog { Filter = "文本文件|*.txt" };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                using (var file = new StreamWriter(dialog.FileName, false, Encoding.Unicode))
                    foreach (var track in Album.Tracks)
                        file.WriteLine(track);
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Format);
        }

        private void trackItem_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText((sender as ListViewItem)?.DataContext.ToString());
        }

        private void btnCover_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { FileName = System.IO.Path.GetFileName(Album.CoverUrl) };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                new System.Net.WebClient().DownloadFileAsync(new Uri(Album.CoverUrl), dialog.FileName);
        }
    }
}
