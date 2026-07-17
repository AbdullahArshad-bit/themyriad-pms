using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Intuit.Ipp.OAuth2PlatformClient;
using System.Configuration;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Configuration;
using PMS.Controllers;
using PMS.Repository.UnitOfWork;
using PMS.EF;

namespace PMS.Controllers
{
    public class QBAHelperController : Controller
    {
        private readonly UnitOfWork<PMSEntities> uow;
        public QBAHelperController(UnitOfWork<PMSEntities> _uow)
        {
            uow = _uow;
        }

        // GET: QBAHelper
        public async Task<ActionResult> Index()
        {
            //Sync the state info and update if it is not the same
            var state = Request.QueryString["state"];
            if (state.Equals(HomeController.auth2Client.CSRFToken, StringComparison.Ordinal))
            {
                ViewBag.State = state + " (valid)";
            }
            else
            {
                ViewBag.State = state + " (invalid)";
            }
            
           string code = Request.QueryString["code"] ?? "none";
           
            string realmId = ConfigurationManager.AppSettings["realmId"];
            await GetAuthTokensAsync(code, realmId);

            ViewBag.Error = Request.QueryString["error"] ?? "none";

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Exchange Auth code with Auth Access and Refresh tokens and add them to Claim list
        /// </summary>
        private async Task GetAuthTokensAsync(string code, string realmId)
        {
            if (realmId != null)
            {
                Session["realmId"] = realmId;
            }

            Request.GetOwinContext().Authentication.SignOut("TempState");
            var tokenResponse = await HomeController.auth2Client.GetBearerTokenAsync(code);
            var client = uow.GenericRepository<ClientIntegration>().Table.Where(x => x.Client_Name == "QuickBooks").FirstOrDefault();
            client.Refresh_Token = tokenResponse.RefreshToken;
            client.Access_Token = tokenResponse.AccessToken;
            client.Refresh_Token_Expiry = DateTime.Now.AddSeconds(tokenResponse.RefreshTokenExpiresIn).ToString();
            client.Access_Token_Expiry = DateTime.Now.AddSeconds(tokenResponse.AccessTokenExpiresIn).ToString();
            uow.GenericRepository<ClientIntegration>().Update(client);
            uow.SaveChanges();

            var claims = new List<Claim>();

            if (Session["realmId"] != null)
            {
                claims.Add(new Claim("realmId", Session["realmId"].ToString()));
            }

            if (!string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                claims.Add(new Claim("access_token", tokenResponse.AccessToken));
                claims.Add(new Claim("access_token_expires_at", (DateTime.Now.AddSeconds(tokenResponse.AccessTokenExpiresIn)).ToString()));
            }

            if (!string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
            {
                claims.Add(new Claim("refresh_token", tokenResponse.RefreshToken));
                claims.Add(new Claim("refresh_token_expires_at", (DateTime.Now.AddSeconds(tokenResponse.RefreshTokenExpiresIn)).ToString()));
            }

            var id = new ClaimsIdentity(claims, "Cookies");
            Request.GetOwinContext().Authentication.SignIn(id);
        }
    }
}