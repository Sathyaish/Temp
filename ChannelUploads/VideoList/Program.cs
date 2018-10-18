using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace VideoList
{
    public class Program
    {
        // VideoList.exe YouTubeChannelURL
        static void Main(string[] args)
        {
            try
            {
                var len = args?.Length;

                if (len == null || len.Value == 0)
                {
                    PrintHelp();
                    Console.ReadKey();
                    return;
                }

                var channelUrl = args[0];

                var channelName = ExtractChannelNameFromUrl(channelUrl);

                var playlistId = GetUploadsPlaylistIdFromChannelNameAsync(channelName).Result;

                var videos = GetVideosInPlaylist(playlistId);

                if (videos == null || videos.Count() == 0)
                {
                    Console.WriteLine($"No uploads by {channelName}.");
                    return;
                }

                var i = 0;
                var pad = videos.Count().ToString().Length;

                foreach (var video in videos)
                    Console.WriteLine($"{++i,3}) {video}");

                Console.WriteLine("\n");
            }
            catch (AggregateException agg)
            {
                foreach (var e in agg.Flatten().InnerExceptions)
                    Console.WriteLine(e.Message);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static async Task<string> GetUploadsPlaylistIdFromChannelNameAsync(string channelName)
        {
            var parameters = new Dictionary<string, string>
            {
                ["key"] = ConfigurationManager.AppSettings["APIKey"],
                ["part"] = "contentDetails",
                ["forUsername"] = channelName, 
                ["fields"] = "items/contentDetails/relatedPlaylists/uploads",
            };

            var baseUrl = "https://www.googleapis.com/youtube/v3/channels?";
            var fullUrl = MakeUrlWithQuery(baseUrl, parameters);

            var result = await new HttpClient().GetStringAsync(fullUrl);

            if (result != null)
            {
                dynamic obj = JsonConvert.DeserializeObject(result);

                return (string)obj?.items?[0]?.contentDetails?.relatedPlaylists.uploads;
            }

            return null;
        }

        private static string ExtractChannelNameFromUrl(string channelUrl)
        {
            if (string.IsNullOrEmpty(channelUrl))
                throw new ArgumentNullException(nameof(channelUrl));

            var match = Regex.Match(channelUrl, 
                @"[Hh][Tt]{2}[Pp][Ss]?\:\/{2}[Ww]{3}\.[Yy][Oo][Uu][Tt][Uu][Bb][Ee]\.[Cc][Oo][Mm]\/[Uu][Ss][Ee][Rr]\/(?<channelName>.+).*");

            return match?.Groups?["channelName"]?.Value;
        }

        private static IEnumerable<string> GetVideosInPlaylist(string playlistId)
        {
            string nextPageToken = null;
            var videos = new List<string>();
            
            do
            {
                var result = GetVideosInPlaylistAsync(playlistId, nextPageToken).Result;

                if (result.pageInfo.totalResults == 0) return null;

                nextPageToken = result.nextPageToken;

                foreach (var item in result?.items)
                    videos.Add((string)item.snippet.title);

            } while (nextPageToken != null);

            return videos;
        }

        private static async Task<dynamic> GetVideosInPlaylistAsync(string playlistId, string nextPageToken)
        {
            var parameters = new Dictionary<string, string>
            {
                ["key"] = ConfigurationManager.AppSettings["APIKey"],
                ["playlistId"] = playlistId,
                ["part"] = "snippet",
                ["fields"] = "nextPageToken, pageInfo, items/snippet(title)",
                ["maxResults"] = "50"
            };

            if (!string.IsNullOrEmpty(nextPageToken))
                parameters.Add("pageToken", nextPageToken);

            var baseUrl = "https://www.googleapis.com/youtube/v3/playlistItems?";
            var fullUrl = MakeUrlWithQuery(baseUrl, parameters);

            var result = await new HttpClient().GetStringAsync(fullUrl);

            if (result != null)
            {
                return JsonConvert.DeserializeObject(result);
            }

            return default(dynamic);
        }

        private static string MakeUrlWithQuery(string baseUrl,
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));

            if (parameters == null || parameters.Count() == 0)
                return baseUrl;

            return parameters.Aggregate(baseUrl,
                (accumulated, kvp) => string.Format($"{accumulated}{kvp.Key}={kvp.Value}&"));
        }

        private static void PrintHelp()
        {
            Console.WriteLine("This program lists the names of videos uploaded by a channel.");
            Console.WriteLine("USAGE: VideoList.exe {YouTubeChannelUrl}");
        }
    }
}