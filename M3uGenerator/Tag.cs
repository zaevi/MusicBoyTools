using System.Collections.Generic;
using System.IO;
using System.Linq;
using TagLib;
using System;

namespace M3uGenerator
{
    public class Tag
    {
        public string Album { get; }

        public uint Disc { get; }

        public uint Track { get; }

        public string DiscTrack => $"{Disc}#{Track:00}";

        public string Title { get; }

        public string Artist { get; }

        private string path;
        private FileInfo fileInfo;

        public string Path { get => path; set { path = value; fileInfo = new FileInfo(path); } }

        public string FileName => System.IO.Path.GetFileName(Path);

        public DateTime CreationTime => fileInfo.CreationTime;

        public DateTime LastWriteTime => fileInfo.LastWriteTime;

        public Tag() { }

        public Tag(TagLib.Tag tag)
        {
            Album = tag.Album;
            Disc = tag.Disc;
            Track = tag.Track;
            Title = tag.Title;
            Artist = tag.Performers.FirstOrDefault();
        }

        public static Tag ReadFrom(string path)
        {
            if (!System.IO.File.Exists(path)) return null;
            try
            {
                using (var file = TagLib.File.Create(path))
                {
                    return new Tag(file.Tag) { Path = path };
                }
            }
            catch(UnsupportedFormatException)
            {
                if (path.EndsWith(".m3u", StringComparison.OrdinalIgnoreCase))
                    return new Tag { Path = path };
                return null;
            }
        }
    }

    public static class TagExtensions
    {

    }
}
