using PMS.Common;
using PMS.EF;
using PMS.Filters;
using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace PMS
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs args)
        {
            try
            {
                if (Context.User != null)
                {
                    //IEnumerable<Role> roles = new UsersService.UsersClient().GetUserRoles(
                    //                                        Context.User.Identity.Name);


                    //string[] rolesArray = new string[roles.Count()];
                    //for (int i = 0; i < roles.Count(); i++)
                    //{
                    //    rolesArray[i] = roles.ElementAt(i).RoleName;
                    //}



                    UnitOfWork<PMSEntities> uow = new UnitOfWork<PMSEntities>();

                    var db = uow.Context;


                    if (PMS.Common.Globals.User.ID == 1)
                    {
                        var ro = db.SubMenus.Where(x => x.IsEnable == true)
                            .Select(x => x.RoleName).ToArray();



                        GenericPrincipal gp1 = new GenericPrincipal(Context.User.Identity, ro);
                        Context.User = gp1;
                        return;
                    }



                    var roles = (from rr in db.RoleRights
                                 join ur in db.UserRoles
                                 on rr.RoleId equals ur.RoleId
                                 join um in db.UserMasters
                                 on ur.UserMasterId equals um.ID
                                 where um.ID == Globals.User.ID
                                 select rr.SubMenu.RoleName).ToArray();

                    //roles.AddRange(db.SubMenus.Where(x =>
                    //x.IsEnable == true
                    //&& x.ShouldDisplay == false)
                    //.Select(y => new AppUserRoles
                    //{
                    //    ControllerName = y.ControllerName,
                    //    ActionName = y.ActionName
                    //}).ToList());




                    GenericPrincipal gp = new GenericPrincipal(Context.User.Identity, roles);
                    Context.User = gp;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
