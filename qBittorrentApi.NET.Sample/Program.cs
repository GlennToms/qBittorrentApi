using System;

namespace qBittorrentApi.NET.Sample
{
    internal static class Program
    {
        private static void Main()
        {
            var client = new Api(new ServerCredential(new Uri("http://Server:Port"), "Username", "Password"));

            var torrents = client.GetTorrents();
            foreach (var torrent in torrents.Result)
            {
                Console.WriteLine(torrent.Name);
            }
            Console.ReadKey();
        }
    }
}
