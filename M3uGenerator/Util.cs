using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace M3uGenerator
{
    public static class Util
    {
        public readonly static string[] AudioExtensions = {
            ".dsf", ".dff", ".iso", ".dxd", ".ape",
            ".flac", ".wav", ".aiff", ".aif", ".dts",
            ".mp3", ".wma", ".aac", ".ogg", ".alac",
            ".mp2", ".m4a", ".ac3", ".m3u" };

        public static bool IsAudio(this string path)
            => AudioExtensions.Contains(Path.GetExtension(path).ToLower());

        public static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                folder += Path.DirectorySeparatorChar;
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public static DataGridCell GetDataGridCell(this DataGridCellInfo cellInfo)
            => cellInfo.Column?.GetCellContent(cellInfo.Item)?.Parent as DataGridCell;
    }

    public class VisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (bool.TryParse(value.ToString(), out var result))
            {
                return result ? Visibility.Visible : Visibility.Hidden;
            }
            return null;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility == Visibility.Visible;
            return false;
        }
    }

}
