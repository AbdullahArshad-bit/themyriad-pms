using PMS.Common.Filters;
using PMS.Filters;
using System.Web.Mvc;

namespace PMS
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new AuthorizeUser());
            filters.Add(new ExceptionHandlerFilter());
        }
    }
}
