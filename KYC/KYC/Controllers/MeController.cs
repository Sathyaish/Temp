using KYC.BaseServices;
using KYC.Contracts;
using KYC.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace KYC.Controllers
{
    public class MeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            var accessToken = Session["access_token"] as string;

            if (string.IsNullOrEmpty(accessToken))
            {
                Trace.WriteLine("Couldn't find an access token in the session state.");
                var homeIndexViewModel = new ViewModelBase { Failed = true, ErrorMessage = "An error occured. Please try again." };
                return View("~/Views/Home/Index.cshtml", homeIndexViewModel);
            }

            MeIndexViewModel viewModel = Session["MeIndexViewModel"] as MeIndexViewModel;

            if (viewModel == null)
            {
                viewModel = new MeIndexViewModel();
            }

            if (viewModel.Channel == null)
            {
                viewModel.Channel = await GetChannelAsync(accessToken);

                if (viewModel.Channel == null)
                {
                    var homeIndexViewModel = new ViewModelBase();
                    viewModel.Failed = true;
                    viewModel.ErrorMessage = "There was an error getting your channel information from YouTube. Please contact the system administrator.";
                    return View("~/Views/Home/Index.cshtml", homeIndexViewModel);
                }
            }

            Session["MeIndexViewModel"] = viewModel;
            return View(viewModel);
        }

        public async Task<ActionResult> Uploads()
        {
            throw new NotImplementedException();
        }

        public async Task<ActionResult> Subscriptions()
        {
            var accessToken = Session["access_token"] as string;

            if (string.IsNullOrEmpty(accessToken))
            {
                Trace.WriteLine("Couldn't find an access token in the session state.");
                var homeIndexViewModel = new ViewModelBase { Failed = true, ErrorMessage = "An error occured. Please try again." };
                return View("~/Views/Home/Index.cshtml", homeIndexViewModel);
            }

            MeIndexViewModel viewModel = Session["MeIndexViewModel"] as MeIndexViewModel;

            if (viewModel == null)
            {
                viewModel = new MeIndexViewModel();
            }

            if (viewModel.Channel == null)
            {
                viewModel.Channel = await GetChannelAsync(accessToken);

                if (viewModel.Channel == null)
                {
                    var homeIndexViewModel = new ViewModelBase();
                    viewModel.Failed = true;
                    viewModel.ErrorMessage = "There was an error getting your channel information from YouTube. Please contact the system administrator.";
                    return View("~/Views/Home/Index.cshtml", homeIndexViewModel);
                }
            }

            if (viewModel.Subscriptions == null)
            {
                viewModel.Subscriptions = await GetSubscriptionsAsync(accessToken);

                if (viewModel.Subscriptions == null)
                {
                    var homeIndexViewModel = new ViewModelBase();
                    viewModel.Failed = true;
                    viewModel.ErrorMessage = "There was an error getting your subscription information from YouTube. Please contact the system administrator.";
                    return View("~/Views/Home/Index.cshtml", homeIndexViewModel);
                }
            }

            Session["MeIndexViewModel"] = viewModel;
            return View(viewModel);
        }

        public async Task<ActionResult> Subscribers()
        {
            throw new NotImplementedException();
        }

        private async Task<IEnumerable<Subscription>> GetSubscriptionsAsync(string accessToken)
        {
            string nextPageToken = null;
            List<Subscription> subscriptions = null;
            var eagerCount = 0;

            do
            {
                var result = await GetSubscriptionsBatchAsync(accessToken, nextPageToken);

                eagerCount = result.pageInfo.totalResults;
                nextPageToken = result.nextPageToken;

                if (eagerCount == 0) return null;

                if (subscriptions == null) subscriptions = new List<Subscription>();

                foreach (var item in result.items)
                {
                    var subscription = new Subscription();
                    subscription.Id = item.id;
                    subscription.Title = item.snippet.title;
                    subscription.Description = item.snippet.description;
                    subscription.ChannelId = item.snippet.resourceId.channelId;
                    subscription.DefaultThumbnailUrl = item.snippet.thumbnails["default"].url;
                    subscription.UploadCount = item.contentDetails.totalItemCount;
                    subscription.RecentUploadCount = item.contentDetails.newItemCount;

                    subscriptions.Add(subscription);
                }

            } while (nextPageToken != null);

            return subscriptions;
        }

        private async Task<dynamic> GetSubscriptionsBatchAsync(string accessToken,
            string nextPageToken)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    ["access_token"] = accessToken,
                    ["mine"] = "true",
                    ["part"] = "snippet, contentDetails",
                    ["fields"] = "nextPageToken, pageInfo, items(id, snippet(title, description, resourceId/channelId, thumbnails/default), contentDetails)",
                    ["maxResults"] = "50"
                };

                if (!string.IsNullOrEmpty(nextPageToken))
                    parameters.Add("pageToken", nextPageToken);

                var baseUrl = "https://www.googleapis.com/youtube/v3/subscriptions?";
                var fullUrl = StringHelpers.MakeUrlWithQuery(baseUrl, parameters);

                var result = await new HttpClient().GetStringAsync(fullUrl);

                if (result != null)
                {
                    return JsonConvert.DeserializeObject(result);
                }

                return default(dynamic);
            }
            catch (Exception ex)
            {
                Debugger.Break();

                Debug.Print(ex.Message);

                return default(dynamic);
            }
        }

        private async Task<Channel> GetChannelAsync(string accessToken)
        {
            try
            {
                Channel channel;

                var parameters = new Dictionary<string, string>
                {
                    ["access_token"] = accessToken,
                    ["mine"] = "true",
                    ["part"] = "snippet, contentDetails, statistics",
                    ["fields"] = "etag, items(id, snippet(title, description, publishedAt, thumbnails(high)), contentDetails(relatedPlaylists/uploads), statistics(viewCount, subscriberCount, videoCount))",
                    ["maxResults"] = "1"
                };

                var baseUrl = "https://www.googleapis.com/youtube/v3/channels?";
                var fullUrl = StringHelpers.MakeUrlWithQuery(baseUrl, parameters);

                var responseString = await new HttpClient().GetStringAsync(fullUrl);

                if (!string.IsNullOrEmpty(responseString))
                {
                    dynamic obj = JsonConvert.DeserializeObject(responseString);

                    channel = new Channel();

                    channel.ETag = obj.etag;
                    channel.Id = obj?.items?[0]?.id;
                    channel.Title = obj?.items?[0]?.snippet?.title;
                    channel.Description = obj?.items?[0]?.snippet?.description;
                    channel.MemberSince = obj?.items?[0]?.snippet?.publishedAt;
                    channel.HighResolutionThumbnailUrl = obj?.items?[0]?.snippet?.thumbnails.high.url;
                    channel.UploadsPlaylistId = obj?.items?[0]?.contentDetails.relatedPlaylists.uploads;
                    channel.ViewCount = obj?.items?[0]?.statistics.viewCount;
                    channel.SubscriberCount = obj?.items?[0]?.statistics.subscriberCount;
                    channel.VideoCount = obj?.items?[0]?.statistics.videoCount;

                    channel.SubscriptionCount = await GetChannelSubscriptionCountAsync(accessToken);

                    return channel;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debugger.Break();

                Debug.Print(ex.Message);

                return null;
            }
        }

        private async Task<int> GetChannelSubscriptionCountAsync(string accessToken, 
            string channelId = null)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    ["access_token"] = accessToken,
                    ["mine"] = "true",
                    ["part"] = "id",
                    ["fields"] = "pageInfo/totalResults"
                };

                var baseUrl = "https://www.googleapis.com/youtube/v3/subscriptions?";
                var fullUrl = StringHelpers.MakeUrlWithQuery(baseUrl, parameters);

                var responseString = await new HttpClient().GetStringAsync(fullUrl);

                if (!string.IsNullOrEmpty(responseString))
                {
                    dynamic obj = JsonConvert.DeserializeObject(responseString);

                    return obj?.pageInfo?.totalResults ?? 0;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Debugger.Break();

                Debug.Print(ex.Message);

                return 0;
            }
        }

        private async Task<dynamic> GetLikedVideosAsync(string accessToken)
        {
            string nextPageToken = null;
            List<dynamic> results = null;

            do
            {
                var result = await GetBatchOfLikedVideosAsync(accessToken, nextPageToken);

                if (result.pageInfo.totalResults == 0) return null;

                results.Add(result);

                nextPageToken = result.nextPageToken;

            } while (nextPageToken != null);

            return results;
        }

        private async Task<dynamic> GetBatchOfLikedVideosAsync(string accessToken, string nextPageToken)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    ["access_token"] = accessToken,
                    ["myRating"] = "like",
                    ["part"] = "snippet",
                    ["fields"] = "nextPageToken, pageInfo, items/snippet(title, description, channelTitle, thumbnails/medium)",
                    ["maxResults"] = "50"
                };

                if (!string.IsNullOrEmpty(nextPageToken))
                    parameters.Add("pageToken", nextPageToken);

                var baseUrl = "https://www.googleapis.com/youtube/v3/videos?";
                var fullUrl = StringHelpers.MakeUrlWithQuery(baseUrl, parameters);

                var result = await new HttpClient().GetStringAsync(fullUrl);

                if (result != null)
                {
                    return JsonConvert.DeserializeObject(result);
                }

                return default(dynamic);
            }
            catch (Exception ex)
            {
                Debugger.Break();

                Debug.Print(ex.Message);

                return default(dynamic);
            }
        }
    }
}