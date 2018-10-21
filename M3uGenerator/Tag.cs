using System.Collections.Generic;
using System.Linq;
using TagLib;

namespace M3uGenerator
{
    public class Tag
    {
        public bool Selected { get; set; } = true;

        public string Album { get; }

        public uint Disc { get; }

        public uint Track { get; }

        public string DiscTrack => $"{Disc}#{Track:00}";

        public string Title { get; }

        public string Artist { get; }

        public string Path;
        public string FileName => System.IO.Path.GetFileName(Path);

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
            try
            {
                using (var file = File.Create(path))
                {
                    var tag = new Tag(file.Tag);
                    tag.Path = path;

                    return tag;
                }
            }
            catch
            {
                return null;
            }
        }
    }

    public static class TagExtensions
    {
        public static IEnumerable<Tag> Sorted(this IEnumerable<Tag> list)
            => list.OrderBy(t => t.Album).ThenBy(t => t.Disc).ThenBy(t => t.Track);
    }
}
