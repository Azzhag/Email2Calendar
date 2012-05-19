using System;
using System.Reflection;
using System.Web.Mvc;
using Email2Calendar.Services;
using Newtonsoft.Json;
using log4net;

namespace Email2Calendar.Controllers
{
    public class HomeController : Controller
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //
        // GET: /Home/

        public ActionResult Index() {
            return View();
        }

        public JsonResult GetProvider(string address) {
            var e2c = new Email2Provider(address);
            e2c.Resolve();
            Log.Info(String.Format("{0}, {1}", JsonConvert.SerializeObject(address), JsonConvert.SerializeObject(e2c)));
            return Json(e2c, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AddFeedback(string emailAddress, string provider, string realProvider) {
            Log.Info(String.Format("{0}, {1}, {2}", JsonConvert.SerializeObject(emailAddress), 
                JsonConvert.SerializeObject(provider), JsonConvert.SerializeObject(realProvider))); 
            return Json(true, JsonRequestBehavior.AllowGet);
        }
    }
}
