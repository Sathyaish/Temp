using KYC.ViewModels;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using KYC.Contracts;

namespace KYC.Controllers
{
    public class HomeController : Controller
    {
        private string _redirectUri = "https://localhost:44397/Home/RedirectUri";

        public ActionResult Index()
        {
            var authorizationServerUrl = "https://accounts.google.com/o/oauth2/auth";

            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = ConfigurationManager.AppSettings["client_id"],
                ["redirect_uri"] = _redirectUri,
                ["response_type"] = "code",
                ["scope"] = "https://www.googleapis.com/auth/youtube https://www.googleapis.com/auth/youtube.force-ssl https://www.googleapis.com/auth/youtube.readonly https://www.googleapis.com/auth/youtubepartner",
                ["state"] = "abcd"
            };

            var url = MakeUrlWithQuery(authorizationServerUrl, parameters);

            var viewModel = new IndexViewModel { Url = url };
            return View(viewModel);
        }

        private string MakeUrlWithQuery(string baseUrl,
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = string.Empty;
            }
            else
            {
                baseUrl = baseUrl.Trim();

                if (baseUrl.ElementAt(baseUrl.Length - 1) != '?')
                {
                    if (baseUrl.IndexOf('?') < 0)
                        baseUrl = string.Concat(baseUrl, "?");
                }
            }
            
            if (parameters == null || parameters.Count() == 0) return baseUrl;

            return parameters.Aggregate(baseUrl,
                (accumulated, kvp) => string.Format($"{accumulated}{kvp.Key}={kvp.Value}&"));
        }

        public async Task<ActionResult> RedirectUri()
        {
            var viewModel = new IndexViewModel { IsPostback = true };

            if (Request["code"] != null)
            {
                var code = Request["code"];

                var accessToken = await GetAccessTokenAsync(code);

                if (string.IsNullOrEmpty(accessToken))
                {
                    viewModel.Failed = true;
                    viewModel.ErrorMessage = "You either did enter your correct password into the YouTube sign in screen or you did not approve sharing your YouTube data with us.";
                    return View("~/Views/Home/Index.cshtml", viewModel);
                }

                var subscriptions = await GetSubscriptionsAsync(accessToken);

                if (subscriptions == null || subscriptions.Count() == 0)
                {
                    return View("~/Views/Home/Index.cshtml", viewModel);
                }

                //var tasks = new List<Task<Subscription>>();
                //foreach(var subscription in subscriptions)
                //{
                //    var task = GetSubscriptionUploads(accessToken, subscription);
                //    tasks.Add(task);
                //}

                //await Task.WhenAll(tasks);

                //tasks.ForEach(t => viewModel.AddSubscription(t.Result));

                viewModel.Subscriptions = subscriptions;
                return View("~/Views/Home/Index.cshtml", viewModel);
            }

            var error = Request["error"]?.Replace('_', ' ') ?? string.Empty;

            return View("~/Views/Home/Index.cshtml", new IndexViewModel { ErrorMessage = error, Failed = true });
        }

        private async Task<Subscription> GetSubscriptionUploads(string accessToken, Subscription subscriber)
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

                foreach(var item in result.items)
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
                var fullUrl = MakeUrlWithQuery(baseUrl, parameters);

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

        private async Task<string> GetAccessTokenAsync(string code)
        {
            try
            {
                var tokenUrl = "https://accounts.google.com/o/oauth2/token";

                var parameters = new Dictionary<string, string>
                {
                    ["client_id"] = ConfigurationManager.AppSettings["client_id"],
                    ["client_secret"] = ConfigurationManager.AppSettings["client_secret"],
                    ["redirect_uri"] = _redirectUri,
                    ["code"] = code,
                    ["grant_type"] = "authorization_code"
                };

                var client = new HttpClient();
                var data = MakeUrlWithQuery(string.Empty, parameters);
                var httpContent = new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded");
                var response = await client.PostAsync(tokenUrl, httpContent);
                var json = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(json);

                var accessToken = result.access_token;

                return accessToken;
            }
            catch (AggregateException agg)
            {
                Debugger.Break();

                foreach (var e in agg.Flatten().InnerExceptions)
                    Debug.Print(e.Message);

                return null;
            }
            catch (Exception ex)
            {
                Debugger.Break();

                Debug.Print(ex.Message);

                return null;
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
                var fullUrl = MakeUrlWithQuery(baseUrl, parameters);

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