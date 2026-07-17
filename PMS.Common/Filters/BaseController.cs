using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Common.Filters
{
    public class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var layout = Request.QueryString["layout"];
            ViewBag.layout = layout;

            if (TempData["error"] != null)
                ViewBag.error = TempData["error"];
            if (TempData["success"] != null)
                ViewBag.success = TempData["success"];

            base.OnActionExecuting(filterContext);
        }
    }
}
