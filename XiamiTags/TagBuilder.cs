using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XiamiTags
{
    class Tag
    {
        /*
         * %album% // %albumartist% // %year% // %genre% // %discnumber% // %track% // %title% // %artist% // %comment%
         */

        public string Album { get; set; }
        public string AlbumArtist { get; set; }
        public string Year { get; set; }
        public string Grene { get; set; }

        public string DiscNumber { get; set; }

        public string Track { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Comment { get; set; }

        public void Update(Tag t)
        {
            Album = t.Album;
            AlbumArtist = t.AlbumArtist;
            Year = t.Year;
            Grene = t.Grene;
            DiscNumber = t.DiscNumber;
            Artist = Artist ?? AlbumArtist;
        }

        public override string ToString()
        {
            var tags = new[] { Album, AlbumArtist, Year, Grene, DiscNumber, Track, Title, Artist, Comment};
            return string.Join(" // ", tags);
        }
    }

    class TagBuilder
    {
        public static string ParsedCoverUrl = null;

        public static Tag[] LoadFrom(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return Load(doc.DocumentNode);
        }

        public static Tag[] Load(HtmlNode rootNode)
        {
            var body = rootNode.SelectSingleNode("//*[@id=\"track_list\"]/tbody");
            if (body == null) return null;
            
            var tags = new List<Tag>();

            var album = ParseAlbum(rootNode);

            var trNodes = rootNode.SelectNodes("//*[@id=\"track_list\"]/tbody/tr");

            var discs = trNodes.Where(t => t.Elements("td").Count() == 1).Count();
            var disc = 1;

            foreach(var tr in trNodes)
            {
                if (tr.Elements("td").Count() == 1)
                {
                    album.DiscNumber = $"{disc++}/{discs}";
                }
                else
                {
                    var tag = ParseTrack(tr);
                    tag.Update(album);
                    tags.Add(tag);
                }
            }

            ParsedCoverUrl = rootNode.SelectSingleNode("//*[@id=\"cover_lightbox\"]")?.GetAttributeValue("href", null);
            if (ParsedCoverUrl.StartsWith("//")) ParsedCoverUrl = "https:" + ParsedCoverUrl;

            return tags.ToArray();
        }

        static Tag ParseTrack(HtmlNode trNode)
        {
            // 5: [1]title [2]artist [3]comment
            var tag = new Tag();
            tag.Track = trNode.SelectSingleNode("td[2]").InnerText.Trim().PadLeft(2, '0');
            var titles = trNode.SelectSingleNode("td[3]").ChildNodes;
            tag.Title = titles[1].InnerText.Trim('\r', '\t', ' ', '\n');
            tag.Artist = titles[2].InnerText.Trim('\r', '\t', ' ', '\n');
            if (tag.Artist.Length == 0) tag.Artist = null;
            if(titles.Count > 3)
                tag.Comment = titles[3].InnerText.Trim('\r', '\t', ' ', '\n');
            return tag;
        }

        static Tag ParseAlbum(HtmlNode rootNode)
        {
            var album = new Tag();

            var node = rootNode.SelectSingleNode("//*[@id=\"title\"]/h1").FirstChild;
            album.Album = node.InnerText;

            var nodes = rootNode.SelectSingleNode("//*[@id=\"album_info\"]/table").Elements("tr");
            foreach(var tr in nodes)
            {
                var td = tr.Elements("td").ToArray();
                var key = td[0].InnerText;
                var value = td[1].InnerText.Trim('\t','\n','\r');
                if (key.StartsWith("艺人")) album.AlbumArtist = value;
                else if (key.StartsWith("专辑风格")) album.Grene = value;
                else if (key.StartsWith("发行时间")) album.Year = value.Substring(0, 4);
            }

            return album;
        }

        public static void Save(Tag[] tags, string path)
        {
            using (var file = new StreamWriter(path, false, Encoding.Unicode))
                foreach(var tag in tags)
                    file.WriteLine(tag);
        }
    }
}
