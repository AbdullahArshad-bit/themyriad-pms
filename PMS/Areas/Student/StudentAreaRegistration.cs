using System.Web.Compilation;
using System.Web.Mvc;
using System.Web.Routing;

namespace PMS.Areas.Student
{
    public class StudentAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Student";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            context.MapRoute(
                "Student_default",
                "Student/{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                new[] { "PMS.Areas.Student.Controllers" }
            );
        }
    }
}