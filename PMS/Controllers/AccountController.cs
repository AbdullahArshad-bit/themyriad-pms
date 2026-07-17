using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.ContractViewModels;
using PMS.Services.Services.Account;
using PMS.Services.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.Security;
using PMS.Common;
using PMS.EF;
using PMS.Services.Services.Setup;
using PMS.Services.Services.StudentPortal.Devices;
using PMS.Classes;

namespace PMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService accountService;
        private readonly IContractManageService contractManageService;
        private readonly ISetupService setupService;
        private readonly IStudentDeviceService studentDeviceService;

        public AccountController(
            IAccountService _accountService,
            IContractManageService _contractManageService,
            ISetupService _setupService,
            IStudentDeviceService _studentDeviceService)
        {
            accountService = _accountService;
            contractManageService = _contractManageService;
            setupService = _setupService;
            studentDeviceService = _studentDeviceService;
        }

        [AllowAnonymous]
        public ActionResult Accept_Contract_Details(int id)
        {
           StudentConractsVM  model = contractManageService.GetStudentContractById(id);
            ViewBag.JustSigned = TempData["JustSigned"] ?? false;
            var key = Request.QueryString["contractkey"];
            if (model != null && key == model.contractkery)
            {
                if (model.IsSigned == true && model.IsCancel == false)
                {
                    return RedirectToAction("ContractAccepted");
                }
                else if (model.IsCancel == true)
                {
                    return RedirectToAction("ContractCancelled", new { id = id });
                }
                else
                {
                    ViewBag.ShouldClose = false;

                }
            }
            else
            {
                return Redirect("https://themyriad.com");
            }
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Accept_Contract_Details(StudentConractsVM studentConracts)
        {
            var signdocument = contractManageService.SignContractDocument(studentConracts.Id, studentConracts.Signature, "By User Online");
            
            if (signdocument == true)
            {
                TempData["JustSigned"] = true;
                return RedirectToAction("ContractAccepted");

            }

            else
            {
                return RedirectToAction("Accept_Contract_Details");
            }
        }

        [AllowAnonymous]
        public ActionResult Accept_documentation_Details(int id)
        {
            PreCheckInDocumentationVM model = contractManageService.GetPreCheckInDocumentationById(id);

            var key = Request.QueryString["documentationkey"];
            if (model != null && key == model.DocumentationKey)
            {
                if (model.IsSigned == true)
                {
                    return RedirectToAction("DocumentAccepted");
                }
                else
                {
                    ViewBag.ShouldClose = false;

                }
            }
            else
            {
                return Redirect("https://themyriad.com");
            }
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Accept_documentation_Details(PreCheckInDocumentationVM preCheckInDocumentation)
        {
            var signprecheckindocument = contractManageService.SignPrecheckinDocument(preCheckInDocumentation.Id, preCheckInDocumentation.StudentSignature, "By User Online");

            if (signprecheckindocument == true)
            {
                return RedirectToAction("DocumentAccepted");
            }

            else
            {
                return RedirectToAction("Accept_documentation_Details");
            }

        }
        [AllowAnonymous]
        public ActionResult DocumentAccepted()
        {
            return View();
        }
        [AllowAnonymous]
        public ActionResult ContractAccepted()
        {
            return View();
        }
        [AllowAnonymous]
        public ActionResult ContractCancelled(int? id)
        {
            string cancellationReason = "";
            if (id.HasValue)
            {
                var contract = contractManageService.GetStudentContractById(id.Value);
                if (contract != null)
                {
                    cancellationReason = contract.CancellationReason ?? "No reason provided";
                }
            }
            ViewBag.CancellationReason = cancellationReason;
            return View();
        }

        [AllowAnonymous]
        public ActionResult Login(string ReturnUrl, string reason = null)
        {
            // Reuse the current session if the user is already authenticated instead of
            // forcing a logout and rendering a stale anonymous anti-forgery token.
            if (Request.IsAuthenticated)
                return RedirectAuthenticatedUser(ReturnUrl);

            if (string.Equals(reason, "session-expired", StringComparison.OrdinalIgnoreCase))
                ViewBag.error = "Your session expired or changed. Please sign in again.";

            LoginVM loginVM = new LoginVM();
            loginVM.ReturnURL = ReturnUrl;
            return View(loginVM);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(StudentLoginVM loginVM)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    
                    var user = accountService.AuthenticateUser(loginVM.Email, loginVM.Password);
                    if (user != null)
                    {
                        if (user.IsStudent == true && !string.IsNullOrWhiteSpace(loginVM.DeviceToken))
                        {
                            studentDeviceService.RegisterDevice(
                                user.ID,
                                user.PersonId,
                                loginVM.DeviceToken,
                                loginVM.Platform,
                                loginVM.DeviceId);
                        }

                        EstablishWebSession(user, loginVM.Email, loginVM.RememberMe);

                        if (user.IsStudent == true)
                            return RedirectToAction("Index", "Home", new { area = "Student" });

                        if (!string.IsNullOrWhiteSpace(loginVM.ReturnURL))
                            return RedirectToAction("CompleteLoginRedirect", new { returnURL = loginVM.ReturnURL });
                        else
                            return RedirectToAction("Index", "Home");
                    }

                    ViewBag.error = "Invalid email or password";
                }
                else
                    ViewBag.error = "Email and Password are required.";
            }

            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }

            return View(loginVM);
        }

        /// <summary>
        /// Establishes a web session for a student who authenticated via the mobile API.
        /// The mobile app should open this URL in a WebView using redirectUrl from the Login API.
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public ActionResult StudentAppSignIn(string token, string returnUrl)
        {
            if (!StudentAuthTokenHelper.TryValidateToken(token, out var userId, out _, out _))
            {
                return RedirectToAction("Login", new { ReturnUrl = returnUrl });
            }

            var user = accountService.GetStudentUserForSession(userId);
            if (user == null)
            {
                return RedirectToAction("Login", new { ReturnUrl = returnUrl });
            }

            EstablishWebSession(user, user.Email, true);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home", new { area = "Student" });
        }

        public ActionResult FilterData(List<int> locationid, string returnURL,List<int> selItem)
        {
            Session["locationid"] = locationid;
            Session.Timeout = 525600;
            Session["locationid"] = locationid;

            return Redirect(returnURL);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOut(string returnURL = null)
        {
            try
            {
                // First we clean the authentication ticket like always
                //required NameSpace: using System.Web.Security;
                FormsAuthentication.SignOut();

                // Second we clear the principal to ensure the user does not retain any authentication
                //required NameSpace: using System.Security.Principal;
                HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);

                Session.Clear();
                System.Web.HttpContext.Current.Session.RemoveAll();

                // Redirect to Login; if returnURL provided (idle auto-logout), preserve as ReturnUrl
                if (!string.IsNullOrWhiteSpace(returnURL) && Url.IsLocalUrl(returnURL) && !returnURL.ToLower().StartsWith("/account/logout"))
                {
                    return RedirectToAction("Login", new { returnURL = returnURL });
                }
                return RedirectToAction("Login");
            }

            catch
            {
                throw;
            }
        }

        private void EstablishWebSession(User user, string signInEmail, bool rememberMe)
        {
            Session["User"] = user;
            HttpContext.Items["User"] = user;
            SignInRemember(signInEmail, rememberMe);
            HttpContext.User = new GenericPrincipal(new GenericIdentity(user.Name), null);

            if (user.AssignedLocations != null && user.AssignedLocations.Count == 1)
            {
                Session["locationcount"] = user.AssignedLocations;
                Session.Timeout = 525600;
            }
            else
            {
                try
                {
                    var lastLocation = setupService.GetLastLocation();
                    int selectedLocation = 0;
                    if (lastLocation.HasValue && lastLocation.Value > 0)
                        selectedLocation = lastLocation.Value;
                    else if (user.AssignedLocations != null && user.AssignedLocations.Count > 0)
                        selectedLocation = user.AssignedLocations.First();

                    if (selectedLocation > 0)
                    {
                        Session["locationid"] = new List<int> { selectedLocation };
                        Session["locationcount"] = new List<int> { selectedLocation };
                        Session.Timeout = 525600;
                        try { setupService.UpdateLastLocation(selectedLocation); } catch { }
                    }
                }
                catch { }
            }
        }

        //GET: SignInAsync
        private void SignInRemember(string userName, bool isPersistent = false)
        {
            // Clear any lingering authencation data
            FormsAuthentication.SignOut();

            int timeout = isPersistent ? 525600 : 30; // Timeout in minutes, 525600 = 365 days.

            // Write the authentication cookie
            //FormsAuthentication.SetAuthCookie(userName, isPersistent);

            var serializer = new JavaScriptSerializer();
            string userData = serializer.Serialize(PMS.Common.Globals.User);

            FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1,
                    userName,
                    DateTime.Now,
                    DateTime.Now.AddDays(365),
                    isPersistent,
                    userData,
                    FormsAuthentication.FormsCookiePath);

            // Encrypt the ticket.
            string encTicket = FormsAuthentication.Encrypt(ticket);

            // Create the cookie.
            HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
            if (isPersistent)
                cookie.Expires = System.DateTime.Now.AddMinutes(timeout);
            Response.Cookies.Add(cookie);
        }

        //GET: RedirectToLocal
        private ActionResult RedirectToLocal(string returnURL = "")
        {
            try
            {
                // If the return url starts with a slash "/" we assume it belongs to our site
                // so we will redirect to this "action"
                if (!string.IsNullOrWhiteSpace(returnURL) && Url.IsLocalUrl(returnURL))
                    return Redirect(returnURL);

                // If we cannot verify if the url is local to our host we redirect to a default location
                return RedirectToAction("Index", "Home");
            }

            catch
            {
                throw;
            }
        }

        private ActionResult RedirectAuthenticatedUser(string returnURL = null)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(returnURL) && Url.IsLocalUrl(returnURL))
                    return RedirectToAction("CompleteLoginRedirect", new { returnURL = returnURL });

                if (PMS.Common.Globals.User?.IsStudent == true)
                    return RedirectToAction("Index", "Home", new { area = "Student" });

                return RedirectToAction("Index", "Home");
            }
            catch
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string oldpassword,string ConfirmPassword)
        {
            if (oldpassword != null && ConfirmPassword != null)
            {
                var restul =   accountService.ChangePassword(oldpassword, ConfirmPassword);
                
                if (restul == true)
                {
                    ViewBag.success = "Password has Changed Successfully!";
                    return RedirectToAction("Login");

                }

                else
                {
                    ViewBag.error = "Old Password is not correct!";
                }
            }

            return View();
        }
        public ActionResult UpdateData(List<int> locationid, string returnURL, List<int> selItem)
        {
            Session["locationid"] = locationid;
            Session.Timeout = 525600;

            //Update LastLocation in database
            setupService.UpdateLastLocation(locationid[0]);

            return Redirect(returnURL);
        }

        // Ensure session location is set before redirecting to deep link
        public ActionResult CompleteLoginRedirect(string returnURL)
        {
            try
            {
                // If session location not set, apply last or first assigned
                if (Session["locationid"] == null)
                {
                    int selectedLocation = 0;
                    var lastLocation = setupService.GetLastLocation();
                    if (lastLocation.HasValue && lastLocation.Value > 0)
                        selectedLocation = lastLocation.Value;
                    else if (PMS.Common.Globals.User?.AssignedLocations != null && PMS.Common.Globals.User.AssignedLocations.Count > 0)
                        selectedLocation = PMS.Common.Globals.User.AssignedLocations.First();

                    if (selectedLocation > 0)
                    {
                        Session["locationid"] = new List<int> { selectedLocation };
                        Session["locationcount"] = new List<int> { selectedLocation };
                        Session.Timeout = 525600;
                        try { setupService.UpdateLastLocation(selectedLocation); } catch { }
                    }
                }

                if (!string.IsNullOrWhiteSpace(returnURL) && Url.IsLocalUrl(returnURL))
                    return Redirect(returnURL);

                return RedirectToAction("Index", "Home");
            }
            catch
            {
                return RedirectToAction("Index", "Home");
            }
        }

    }
}