using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using PMS.Repository.UnitOfWork;
using PMS.EF;
using PMS.Common.Classes;
using System.Web.Routing;

namespace PMS.Common.Filters
{
    public class AuthorizeUser : AuthorizeAttribute
    {

        public override void OnAuthorization(System.Web.Mvc.AuthorizationContext filterContext)
        {
            bool skipAuthorization = filterContext.ActionDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true)
                         || filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true);
            var area = filterContext.RouteData.DataTokens["area"];
            if (skipAuthorization)
                return;

            if (/*HttpContext.Current.Session["User"] == null ||*/ !HttpContext.Current.Request.IsAuthenticated)
            {
                if (filterContext.HttpContext.Request.IsAjaxRequest())
                {
                    filterContext.HttpContext.Response.StatusCode = 302; //Found Redirection to another page. Here- login page. Check Layout ajaxError() script.
                    filterContext.HttpContext.Response.End();
                }
                else
                {
                    // For deep-link unauthenticated requests, preserve ReturnUrl unless it's unsafe (e.g., LogOut)
                    var raw = filterContext.HttpContext.Request.RawUrl ?? string.Empty;
                    if (!raw.ToLower().StartsWith("/account/logout"))
                    {
                        filterContext.Result = new System.Web.Mvc.RedirectResult(
                            System.Web.Security.FormsAuthentication.LoginUrl + "?ReturnUrl=" +
                            filterContext.HttpContext.Server.UrlEncode(raw));
                    }
                    else
                    {
                        filterContext.Result = new System.Web.Mvc.RedirectResult(System.Web.Security.FormsAuthentication.LoginUrl);
                    }
                }
            }
            else
            {

                if (PMS.Common.Globals.User.IsStudent == true)
                {
                    if (filterContext.HttpContext.Request.RawUrl.Equals("/"))
                    {
                        filterContext.Result = new System.Web.Mvc.RedirectResult("/Student");
                    }

                }
                else if (area != null && area == "Student")
                {
                    filterContext.Result = new System.Web.Mvc.RedirectResult("/");
                }

            }

            base.OnAuthorization(filterContext);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (HttpContext.Current.Request.IsAuthenticated)
            {
                // Returns HTTP 401 by default - see HttpUnauthorizedResult.cs.
                filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary
                {
        { "action", "Unauthorized" },
        { "controller", "Home" },
                    //{ "parameterName", "YourParameterValue" }
                });
            }
        }
    }
}
