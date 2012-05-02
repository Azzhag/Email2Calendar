using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Email2Calendar.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace Email2Calendar.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetProvider(string address) {
            var e2c = new Email2Provider(address);
            e2c.Resolve();
            return Json(e2c, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AddFeedback(string emailAddress, string provider, string realProvider) {
            return Json(null);
        }
    }
}
