using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;

namespace OAuthYouTube.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var authorizationServerUrl = "https://accounts.google.com/o/oauth2/auth?";

            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = ConfigurationManager.AppSettings["client_id"],
                ["redirect_uri"] = "https://localhost:44385/Home/RedirectUri",
                ["response_type"] = "code",
                ["scope"] = "https://www.googleapis.com/auth/youtube https://www.googleapis.com/auth/youtube.force-ssl https://www.googleapis.com/auth/youtube.readonly https://www.googleapis.com/auth/youtubepartner",
                ["state"] = "abcd"
            };
    
            var url = MakeUrlWithQuery(authorizationServerUrl, parameters);
            
            return View((object)url);
        }

        private string MakeUrlWithQuery(string baseUrl, 
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            if (string.IsNullOrEmpty(baseUrl)) baseUrl = string.Empty;

            if (parameters == null || parameters.Count() == 0) return baseUrl;

            return parameters.Aggregate(baseUrl,
                (accumulated, kvp) => string.Format($"{accumulated}{kvp.Key}={kvp.Value}&"));
        }

        public async Task<ActionResult> RedirectUri()
        {
            if (Request["code"] != null)
            {
                var code = Request["code"];

                var accessToken = await GetAccessTokenAsync(code);

                var results = GetLikedVideosAsync(accessToken);

                return Content("Success!");
            }

            var error = Request["error"]?.Replace('_', ' ') ?? string.Empty;

            return Content("Error: " + error + "!");
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
                    ["redirect_uri"] = "https://localhost:44385/Home/RedirectUri",
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
            catch(AggregateException agg)
            {
                Debugger.Break();
                
                foreach (var e in agg.Flatten().InnerExceptions)
                    Debug.Print(e.Message);

                return null;
            }
            catch(Exception ex)
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