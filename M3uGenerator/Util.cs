using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace M3uGenerator
{
    public static class Util
    {
        public readonly static string[] AudioExtensions = {
            ".dsf", ".dff", ".iso", ".dxd", ".ape",
            ".flac", ".wav", ".aiff", ".aif", ".dts",
            ".mp3", ".wma", ".aac", ".ogg", ".alac",
            ".mp2", ".m4a", ".ac3" };

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

        public static void Generate(string m3uPath, IEnumerable<string> fileNames)
        {
            var folder = Path.GetDirectoryName(m3uPath);
            var sb = new StringBuilder();
            foreach (var file in fileNames)
                sb.AppendLine(GetRelativePath(file, folder));
            File.WriteAllText(m3uPath, sb.ToString());
        }
    }
}
