using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Email2Calendar.Services;
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

        //
        // PUT: /Home/           
        [HttpPost]
        public ActionResult Index(string emailAddress) {

            var e2c = new Email2Provider(emailAddress);
            if (e2c.Resolve()) {
                string json = Newtonsoft.Json.Serialization
            }
            else {
                RedirectToAction("Failure", "Home", new { address = emailAddress, reason = e2c.FailureReason });                
            }


            return View();
        }

        public ActionResult Result(string address, string provider ) {
            return View();
            
        }

        public ActionResult Failure(string address, string reason)
        {
            return View();

        }
    }
}
