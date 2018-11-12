using System;
using System.IO;
using System.Text;
using System.Windows;
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
            => InitializeComponent();

        private void OutputTags()
        {
            if (Album == null) return;
            textBox.Clear();

            textBox.AppendText($"Album: {Album.Title}\r\n");
            textBox.AppendText($"Artist: {Album.Artist}\r\n");
            textBox.AppendText($"Year: {Album.Year}\r\nGrene: {Album.Grene}\r\n");

            var disc = "";
            foreach(var track in Album.Tracks)
            {
                var d = track.DiscNumber.Split('/')[0];
                if(d != disc)
                {
                    disc = d;
                    textBox.AppendText($"\r\n[Disc {d}]\r\n");
                }

                textBox.AppendText($"[{track.TrackNumber}] {track.Title}");
                if (track.Artist != track.Album.Artist) textBox.AppendText($" ({track.Artist})");
                if (!string.IsNullOrEmpty(track.Comment)) textBox.AppendText($" //{track.Comment}");
                textBox.AppendText("\r\n");
            }
            textBox.ScrollToLine(0);
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            if(Clipboard.ContainsText())
            {
                var html = Clipboard.GetText();
                Album = TagParser.LoadFrom(html);
                if(Album == null)
                {
                    textBox.Text = "导入信息失败";
                    return;
                }
                btnExport.IsEnabled = btnCopy.IsEnabled = btnCover.IsEnabled = true;
                OutputTags();
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

        private void btnCover_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { FileName = System.IO.Path.GetFileName(Album.CoverUrl) };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                new System.Net.WebClient().DownloadFileAsync(new Uri(Album.CoverUrl), dialog.FileName);
        }
    }
}
