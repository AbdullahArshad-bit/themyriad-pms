
using PMS.Services.Services.UserManage;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace PMS.Areas.Student.Classes
{
    public class AllowUserFilter: AuthorizeAttribute
    {

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            var area = filterContext.RouteData.DataTokens["area"];
            if (area != null) {            
                IUserManageService userServvice = (IUserManageService)Services.ServiceRegistration.GlobalKernel.GetService(typeof(IUserManageService));
               var users= userServvice.GetUserById(PMS.Common.Globals.User.ID);

            if (!users.IsActive)
            {
                    System.Web.Security.FormsAuthentication.SignOut();
                    filterContext.Result = new RedirectResult("/Account/Login");
            }
            }


        }
    }
}