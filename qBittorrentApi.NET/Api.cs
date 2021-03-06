﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace qBittorrentApi.NET
{
    public class Api
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _httpClientHandler;
        private readonly ServerCredential _serverCredential;

        public Api(ServerCredential argServerCredential)
        {
            _serverCredential = argServerCredential;
            _httpClientHandler = new HttpClientHandler();
            _httpClient = new HttpClient(_httpClientHandler) {BaseAddress = _serverCredential.Uri};
        }

        private bool IsAuthenticated()
        {
            return _httpClientHandler.CookieContainer.GetCookies(_httpClient.BaseAddress)["SID"] != null;
        }

        private async Task CheckAuthentification()
        {
            if (!IsAuthenticated())
            {
                if (!await Login())
                {
                    throw new SecurityException();
                }
            }
        }

        private async Task<bool> Login()
        {
            HttpContent bodyContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", _serverCredential.Username),
                new KeyValuePair<string, string>("password", _serverCredential.Password)
            });

            var uri = new Uri("/login", UriKind.Relative);

            await _httpClient.PostAsync(uri, bodyContent);

            return IsAuthenticated();
        }

        public async Task<int> GetApiVersion()
        {
            return int.Parse(await _httpClient.GetStringAsync(new Uri("/version/api", UriKind.Relative)));
        }

        public async Task<int> GetApiMinVersion()
        {
            return int.Parse(await _httpClient.GetStringAsync(new Uri("/version/api_min", UriKind.Relative)));
        }

        public async Task<Version> GetQBittorrentVersion()
        {
            var versionStr = await _httpClient.GetStringAsync(new Uri("/version/qbittorrent", UriKind.Relative));
            return Version.Parse(versionStr);
        }

        public async Task<IList<Torrent>> GetTorrents(Filter filter = Filter.All, string category = null)
        {
            await CheckAuthentification();

            var keyValuePairs = new KeyValuePair<string, string>[2];
            keyValuePairs.SetValue(new KeyValuePair<string, string>("filter", filter.ToString().ToLower()), 0);


            if (category != null)
            {
                keyValuePairs.SetValue(new KeyValuePair<string, string>("category", category), 1);
            }

            HttpContent content = new FormUrlEncodedContent(keyValuePairs);

            var uri = new Uri("/query/torrents?" + await content.ReadAsStringAsync(), UriKind.Relative);
            var response = await _httpClient.GetAsync(uri);
            var jsonStr = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IList<Torrent>>(jsonStr);
        }

        public async Task<GeneralProperties> GetGeneralProperties(string hash)
        {
            var jsonStr =
                await _httpClient.GetStringAsync(new Uri("/query/propertiesGeneral/" + hash, UriKind.Relative));
            return JsonConvert.DeserializeObject<GeneralProperties>(jsonStr);
        }

        public async Task<IList<TrackersProperties>> GetTrackersProperties(string hash)
        {
            var jsonStr =
                await _httpClient.GetStringAsync(new Uri("/query/propertiesTrackers/" + hash, UriKind.Relative));
            return JsonConvert.DeserializeObject<IList<TrackersProperties>>(jsonStr);
        }

        public async Task<IList<FilesProperties>> GetFilesProperties(string hash)
        {
            var jsonStr =
                await _httpClient.GetStringAsync(new Uri("/query/propertiesFiles/" + hash, UriKind.Relative));
            return JsonConvert.DeserializeObject<IList<FilesProperties>>(jsonStr);
        }

        public async Task<bool> DownloadFromUrls(IList<Uri> uris)
        {
            await CheckAuthentification();

            var stringBuilder = new StringBuilder();
            foreach (var uri in uris)
            {
                stringBuilder.Append(uri);
                stringBuilder.Append('\n');
            }

            HttpContent bodyContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("urls", stringBuilder.ToString())
            });

            var uriDownload = new Uri("/command/download", UriKind.Relative);
            var httpResponseMessage = await _httpClient.PostAsync(uriDownload, bodyContent);

            return httpResponseMessage.IsSuccessStatusCode;
        }

        public async Task<bool> Upload(byte[][] torrents)
        {
            await CheckAuthentification();

            using (var content = new MultipartFormDataContent("Upload----" + DateTime.Now))
            {
                foreach (var torrent in torrents)
                {
                    var guid = Guid.NewGuid().ToString();
                    content.Add(new ByteArrayContent(torrent), guid, guid);
                }

                var uriUpload = new Uri("/command/upload", UriKind.Relative);
                var message = await _httpClient.PostAsync(uriUpload, content);
                return message.IsSuccessStatusCode;
            }
        }

        public async Task<bool> Delete(IList<string> hashes)
        {
            await CheckAuthentification();

            var stringBuilder = new StringBuilder();
            foreach (var hash in hashes)
            {
                stringBuilder.Append(hash);
                stringBuilder.Append('|');
            }

            HttpContent bodyContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("hashes", stringBuilder.ToString())
            });

            var uriDownload = new Uri("/command/delete", UriKind.Relative);
            var httpResponseMessage = await _httpClient.PostAsync(uriDownload, bodyContent);

            return httpResponseMessage.IsSuccessStatusCode;
        }

        public async Task<bool> DeletePermanently(IList<string> hashes)
        {
            await CheckAuthentification();

            var stringBuilder = new StringBuilder();
            foreach (var hash in hashes)
            {
                stringBuilder.Append(hash);
                stringBuilder.Append('|');
            }

            HttpContent bodyContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("hashes", stringBuilder.ToString())
            });

            var uriDownload = new Uri("/command/deletePerm", UriKind.Relative);
            var httpResponseMessage = await _httpClient.PostAsync(uriDownload, bodyContent);

            return httpResponseMessage.IsSuccessStatusCode;
        }

        public async Task<bool> SetCategory(IList<string> hashes, string category)
        {
            await CheckAuthentification();

            var stringBuilder = new StringBuilder();

            foreach (var hash in hashes)
            {
                stringBuilder.Append(hash);
                stringBuilder.Append('|');
            }

            HttpContent bodyContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("hashes", stringBuilder.ToString()),
                new KeyValuePair<string, string>("category", category)
            });

            var uriSetCategory = new Uri("/command/setCategory", UriKind.Relative);
            var httpResponseMessage = await _httpClient.PostAsync(uriSetCategory, bodyContent);

            return httpResponseMessage.IsSuccessStatusCode;
        }

        public async Task<bool> Recheck(string hash)
        {
            await CheckAuthentification();

            HttpContent bodyContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("hash", hash),
            });

            var uriRecheck = new Uri("/command/recheck", UriKind.Relative);
            var httpResponseMessage = await _httpClient.PostAsync(uriRecheck, bodyContent);

            return httpResponseMessage.IsSuccessStatusCode;
        }

        public async Task<bool> AddTrackers(string hash, IList<Uri> trackers)
        {
            await CheckAuthentification();

            var stringBuilder = new StringBuilder();

            foreach (var tracker in trackers)
            {
                stringBuilder.Append(tracker);
                stringBuilder.Append('\n');
            }

            HttpContent bodyContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("hash", hash),
                new KeyValuePair<string, string>("urls", stringBuilder.ToString())
            });

            var uriAddTrackers = new Uri("/command/addTrackers", UriKind.Relative);
            var httpResponseMessage = await _httpClient.PostAsync(uriAddTrackers, bodyContent);

            return httpResponseMessage.IsSuccessStatusCode;
        }

        public async Task<bool> Resume(string hash)
        {
            await CheckAuthentification();

            HttpContent bodyContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("hash", hash),
            });

            var uriResume = new Uri("/command/resume", UriKind.Relative);
            var httpResponseMessage = await _httpClient.PostAsync(uriResume, bodyContent);

            return httpResponseMessage.IsSuccessStatusCode;
        }

        public async Task<bool> Shutdown()
        {
            await CheckAuthentification();

            var uriShutdown = new Uri("/command/shutdown", UriKind.Relative);
            var httpResponseMessage = await _httpClient.GetAsync(uriShutdown);

            return httpResponseMessage.IsSuccessStatusCode;
        }

        public async Task<TransferInfo> GetTransferInfo()
        {
            await CheckAuthentification();

            var uriTransferInfo = new Uri("/query/transferInfo", UriKind.Relative);
            var jsonStr = await _httpClient.GetStringAsync(uriTransferInfo);

            return JsonConvert.DeserializeObject<TransferInfo>(jsonStr);
        }
    }
}