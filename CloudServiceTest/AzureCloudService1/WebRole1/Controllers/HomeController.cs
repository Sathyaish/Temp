using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebRole1.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var roles = default(string);
            RoleEnvironment.Roles.Keys.ToList().ForEach((k) => roles += k);

            ViewBag.RoleId = RoleEnvironment.CurrentRoleInstance.Id;
            ViewBag.Roles = roles;

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}