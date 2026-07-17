using PMS.Common;
using System;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Routing;

namespace PMS.Filters
{
    public class ExceptionHandlerFilter : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            var ex = filterContext.Exception;
            //var enttityValidataionErro = ex.
            Common.Classes.ErrorLogger.WriteToExceptionLog(Convert.ToString(ex.Message), Convert.ToString(ex.StackTrace) + "/n" + Convert.ToString(ex.InnerException), Convert.ToString(ex.HResult));

            if (filterContext.ExceptionHandled)
                return;

            if (ex is HttpAntiForgeryException)
            {
                filterContext.Result = BuildAntiForgeryRedirect(filterContext);
                filterContext.ExceptionHandled = true;
                filterContext.HttpContext.Response.Clear();
                filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
                return;
            }

            base.OnException(filterContext);
        }

        private static ActionResult BuildAntiForgeryRedirect(ExceptionContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            var urlHelper = new UrlHelper(filterContext.RequestContext);
            var referrerPath = request.UrlReferrer?.PathAndQuery;
            var hasLocalReferrer = !string.IsNullOrWhiteSpace(referrerPath) && urlHelper.IsLocalUrl(referrerPath);

            if (request.IsAuthenticated)
            {
                if (hasLocalReferrer)
                    return new RedirectResult(referrerPath);

                if (Globals.User?.IsStudent == true)
                {
                    return new RedirectToRouteResult(new RouteValueDictionary(new
                    {
                        area = "Student",
                        controller = "Home",
                        action = "Index"
                    }));
                }

                return new RedirectToRouteResult(new RouteValueDictionary(new
                {
                    area = "",
                    controller = "Home",
                    action = "Index"
                }));
            }

            return new RedirectToRouteResult(new RouteValueDictionary(new
            {
                area = "",
                controller = "Account",
                action = "Login",
                ReturnUrl = hasLocalReferrer ? referrerPath : null,
                reason = "session-expired"
            }));
        }
    }

}