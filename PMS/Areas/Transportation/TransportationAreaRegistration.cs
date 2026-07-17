using System.Web.Mvc;

namespace PMS.Areas.Transportation
{
    public class TransportationAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Transportation";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Transportation_default",
                "Transportation/{controller}/{action}/{id}",
                 new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                new[] { "PMS.Areas.Transportation.Controllers" }
            );
        }
    }
}