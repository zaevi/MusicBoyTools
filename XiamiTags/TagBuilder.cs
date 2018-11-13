using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XiamiTags
{
    class Album
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Year { get; set; }
        public string Grene { get; set; }

        public string CoverUrl { get; set; }
        public string CoverPreviewUrl => CoverUrl + "@4e_1c_100Q_128w_128h";

        public List<Track> Tracks { get; } = new List<Track>();
    }

    class Track
    {
        public Album Album { get; set; }

        public string TrackNumber { get; set; }
        public string DiscNumber { get; set; }

        public string Title { get; set; }
        public string Artist { get; set; }
        public string Comment { get; set; }

        public override string ToString()
        {
            var tags = new[] { Album.Title, Album.Artist, Album.Year, Album.Grene, DiscNumber, TrackNumber, Title, Artist, Comment };
            return string.Join(" // ", tags);
        }
    }

    class TagParser
    {
        public static Album LoadFrom(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return Load(doc.DocumentNode);
        }

        public static Album Load(HtmlNode rootNode)
        {
            var body = rootNode.SelectSingleNode("//*[@id=\"track_list\"]/tbody");
            if (body == null) return null;
            
            var album = ParseAlbum(rootNode);

            var trNodes = rootNode.SelectNodes("//*[@id=\"track_list\"]/tbody/tr");

            var discs = trNodes.Where(t => t.Elements("td").Count() == 1).Count();
            var disc = 0;

            foreach(var trNode in trNodes)
            {
                if (trNode.Elements("td").Count() == 1)
                {
                    disc++;
                }
                else
                {
                    var track = new Track() { Album = album, DiscNumber = $"{disc}/{discs}" };

                    track.TrackNumber = trNode.SelectSingleNode("td[2]").InnerText.Trim().PadLeft(2, '0');
                    var titles = trNode.SelectSingleNode("td[3]").ChildNodes;
                    track.Title = titles[1].InnerText.Trim('\r', '\t', ' ', '\n');
                    track.Artist = titles[2].InnerText.Trim('\r', '\t', ' ', '\n');
                    if (string.IsNullOrWhiteSpace(track.Artist)) track.Artist = album.Artist;
                    if (titles.Count > 3)
                        track.Comment = titles[3].InnerText.Trim('\r', '\t', ' ', '\n');

                    album.Tracks.Add(track);
                }
            }

            album.CoverUrl = rootNode.SelectSingleNode("//*[@id=\"cover_lightbox\"]")?.GetAttributeValue("href", null);
            if (album.CoverUrl.StartsWith("//")) album.CoverUrl = "https:" + album.CoverUrl;

            return album;
        }

        static Album ParseAlbum(HtmlNode rootNode)
        {
            var album = new Album();

            var node = rootNode.SelectSingleNode("//*[@id=\"title\"]/h1").FirstChild;
            album.Title = node.InnerText;

            var nodes = rootNode.SelectSingleNode("//*[@id=\"album_info\"]/table").Elements("tr");
            foreach(var tr in nodes)
            {
                var td = tr.Elements("td").ToArray();
                var key = td[0].InnerText;
                var value = td[1].InnerText.Trim('\t','\n','\r');
                if (key.StartsWith("艺人")) album.Artist = value;
                else if (key.StartsWith("专辑风格")) album.Grene = value;
                else if (key.StartsWith("发行时间")) album.Year = value.Substring(0, 4);
            }

            return album;
        }
    }
}
