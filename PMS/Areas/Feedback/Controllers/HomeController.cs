using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Areas.Feedback.Controllers
{
    public class HomeController : Controller
    {
        // GET: Feedback/Home
        public ActionResult Index()
        {
            return View();
        }
    }
}