using PMS.Services.Services.Notifications;
using PMS.Services.Services.Firebase;
using PMS.DTO.ViewModels;
using System.Threading.Tasks;
using System.Web.Mvc;
using static PMS.Common.Classes.Enumeration;
using PMS.Common.Classes;
using PMS.Common.Filters;

namespace PMS.Controllers
{
    public class PushNotificationController : Controller
    {
        private readonly IPushNotificationService pushNotificationService;

        public PushNotificationController(IPushNotificationService _pushNotificationService)
        {
            pushNotificationService = _pushNotificationService;
        }

        [AuthorizeUser(Roles = AppUserRoles.Push_Notification)]
        [HttpGet]
        public ActionResult PushNotification()
        {
            var model = new PushNotificationPageViewModel
            {
                Logs = pushNotificationService.GetPushNotificationLogs(100)
            };

            if (TempData["StatusMessage"] != null)
            {
                ViewBag.StatusMessage = TempData["StatusMessage"];
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser(Roles = AppUserRoles.Push_Notification)]
        public async Task<ActionResult> PushNotification(string title, string body)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                TempData["StatusMessage"] = "Title and Message are required!";
                return RedirectToAction("PushNotification");
            }

            await pushNotificationService.SendPushNotificationAsync(
                title,
                body,
                PMS.Common.Globals.User.ID,
                PMS.Common.Globals.User.Name,
                PMS.Common.Globals.User.Email);

            TempData["StatusMessage"] = "Notification sent successfully to SecurityAlert topic!";
            return RedirectToAction("PushNotification");
        }
    }
}
