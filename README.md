# qBittorrentApi.Net
C-Sharp API for qBitTorrent

qBitTorrent can be found here http://www.qbittorrent.org/

Code cloned from https://github.com/rbarbe/qBittorrentApi

## Setup
You'll need the qBitTorrent url, username and password.

###Dependencies

+ JSON.net

### Setup Client

```c#
const string Username = "USERNAME";
const string Password = "PASSWORD";
var client = new Api(new ServerCredential(new Uri("http://Server:Port"), Username, Password));
```

### List All Torrents
```c#
var torrents = await client.GetTorrents();
foreach (var torrent in torrents)
{
    Console.WriteLine(torrent.Name);
}
```
