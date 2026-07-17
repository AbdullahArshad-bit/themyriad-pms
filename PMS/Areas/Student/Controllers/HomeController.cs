using PMS.Areas.Student.Classes;
using PMS.Common.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace PMS.Areas.Student.Controllers
{
    
    public class HomeController : Controller
    {
        // GET: Student/Home
        [AuthorizeUser]
        [AllowUserFilter]
        public ActionResult Index()
        {
            return View();
        }
       
    }
}