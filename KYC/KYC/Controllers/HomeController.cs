using KYC.BaseServices;
using KYC.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace KYC.Controllers
{
    public class HomeController : Controller
    {
        private string _redirectUri = "http://myfirstwebapponappservice.azurewebsites.net/Home/RedirectUri";

        public ActionResult Index()
        {
            if (Session["access_token"] != null && !string.IsNullOrEmpty((string)Session["access_token"]))
            {
                return RedirectToAction("Index", "Me");
            }

            var authorizationServerUrl = "https://accounts.google.com/o/oauth2/auth";

            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = ConfigurationManager.AppSettings["client_id"],
                ["redirect_uri"] = _redirectUri,
                ["response_type"] = "code",
                ["scope"] = "https://www.googleapis.com/auth/youtube https://www.googleapis.com/auth/youtube.force-ssl https://www.googleapis.com/auth/youtube.readonly https://www.googleapis.com/auth/youtubepartner https://www.googleapis.com/auth/youtubepartner-channel-audit",
                ["state"] = "abcd"
            };

            var url = StringHelpers.MakeUrlWithQuery(authorizationServerUrl, parameters);

            var viewModel = new HomeIndexViewModel { Url = url };
            return View(viewModel);
        }

        public async Task<ActionResult> RedirectUri()
        {
            var viewModel = new HomeIndexViewModel { IsPostback = true };

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

                Session["access_token"] = accessToken;

                return RedirectToAction("Index", "Me");
            }

            var error = Request["error"]?.Replace('_', ' ') ?? string.Empty;
            return View("~/Views/Home/Index.cshtml", new HomeIndexViewModel { ErrorMessage = error, Failed = true });
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
                var data = StringHelpers.MakeUrlWithQuery(string.Empty, parameters);
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
    }
}