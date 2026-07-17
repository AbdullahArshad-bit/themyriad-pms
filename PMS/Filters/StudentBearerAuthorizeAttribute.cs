using PMS.Classes;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace PMS.Filters
{
    public class StudentBearerAuthorizeAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var token = StudentAuthTokenHelper.GetBearerToken(actionContext.Request);
            if (string.IsNullOrWhiteSpace(token)
                || !StudentAuthTokenHelper.TryValidateToken(token, out _, out _, out _))
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }
    }
}
