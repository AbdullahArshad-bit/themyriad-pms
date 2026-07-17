using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Areas.Transportation.Controllers
{
    public class HomeController : Controller
    {
        // GET: Transportation/Home
        public ActionResult Index()
        {
            return View();
        }
    }
}