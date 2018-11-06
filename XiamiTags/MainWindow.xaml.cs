using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

namespace XiamiTags
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        const string Format = @"%album% // %albumartist% // %year% // %genre% // %discnumber% // %track% // %title% // %artist% // %comment%";

        Tag[] Tags = null;

        string CoverUrl = null;

        public MainWindow()
            => InitializeComponent();

        private void OutputTags()
        {
            if (Tags == null) return;
            textBox.Clear();

            textBox.AppendText($"Album: {Tags[0].Album}\r\n");
            textBox.AppendText($"Artist: {Tags[0].AlbumArtist}\r\n");
            textBox.AppendText($"Year: {Tags[0].Year}\r\nGrene: {Tags[0].Grene}\r\n");

            var disc = "";
            foreach(var tag in Tags)
            {
                var d = tag.DiscNumber.Split('/')[0];
                if(d != disc)
                {
                    disc = d;
                    textBox.AppendText($"\r\n[Disc {d}]\r\n");
                }

                textBox.AppendText($"[{tag.Track}] {tag.Title}");
                if (tag.Artist != tag.AlbumArtist) textBox.AppendText($" ({tag.Artist})");
                if (!string.IsNullOrEmpty(tag.Comment)) textBox.AppendText($" //{tag.Comment}");
                textBox.AppendText("\r\n");
            }
            textBox.ScrollToLine(0);
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            if(Clipboard.ContainsText())
            {
                var html = Clipboard.GetText();
                Tags = TagBuilder.LoadFrom(html);
                if(Tags == null)
                {
                    textBox.Text = "导入信息失败";
                    return;
                }
                btnExport.IsEnabled = btnCopy.IsEnabled = btnCover.IsEnabled = true;
                CoverUrl = TagBuilder.ParsedCoverUrl;
                OutputTags();
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (Tags == null) return;
            var dialog = new SaveFileDialog { Filter = "文本文件|*.txt" };
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TagBuilder.Save(Tags, dialog.FileName);
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Format);
        }

        private void btnCover_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { FileName = System.IO.Path.GetFileName(CoverUrl) };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                new System.Net.WebClient().DownloadFileAsync(new Uri(CoverUrl), dialog.FileName);
        }
    }
}
