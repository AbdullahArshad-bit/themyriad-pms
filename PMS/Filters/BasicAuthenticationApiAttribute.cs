using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace PMS.Filters
{
    public class BasicAuthenticationApiAttribute : AuthorizationFilterAttribute
    {
        private static string AuthInfo = "themyriad:Bits%L@hore";
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            //base.OnAuthorization(actionContext);
            if (actionContext.Request.Headers.Authorization == null)
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
                return;
            }

            string token = null;
            var authHeader = actionContext.Request.Headers.Authorization;

            // Check if the scheme is "Basic" and get the parameter
            if (authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader.Parameter;
            }
            else
            {
                // Handle case where Swagger might send the token without "Basic " prefix
                // Try to get the token from the Authorization header value directly
                var authValue = actionContext.Request.Headers.GetValues("Authorization")?.FirstOrDefault();
                if (!string.IsNullOrEmpty(authValue))
                {
                    // If it starts with "Basic ", extract the token part
                    if (authValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                    {
                        token = authValue.Substring(6); // Remove "Basic " prefix
                    }
                    else
                    {
                        // Assume the entire value is the token
                        token = authValue;
                    }
                }
            }

            // Validate token is not null or empty
            if (string.IsNullOrEmpty(token))
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
                return;
            }

            try
            {
                string decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(token));

                if (AuthInfo.Equals(decodedToken))
                {
                    //Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity(""), null);
                }
                else
                {
                    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
                }
            }
            catch (FormatException)
            {
                // Invalid base64 string
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
            catch (ArgumentNullException)
            {
                // Token is null (shouldn't happen due to check above, but just in case)
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }
    }
}