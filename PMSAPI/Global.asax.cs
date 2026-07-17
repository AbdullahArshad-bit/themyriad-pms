using System;
using System.Web;
using System.Web.Http;

namespace PMSAPI
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            var path = HttpContext.Current.Request.Url.AbsolutePath.TrimEnd('/');
            if (string.IsNullOrEmpty(path))
            {
                HttpContext.Current.Response.Redirect("~/swagger", true);
            }
        }
    }
}
