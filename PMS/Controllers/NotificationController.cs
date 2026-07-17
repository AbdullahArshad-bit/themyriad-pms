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
    public class NotificationController : Controller
    {
        private readonly INotificationService notificationService;

        public NotificationController(
            INotificationService _notificationService)
        {
            notificationService = _notificationService;
        }

        // GET: Student/Notification
        public ActionResult Index()
        {
            var model = notificationService.GetAllNotification(PMS.Common.Globals.User.ID, (int)NotifiactionType.Admin);

            return View(model);
        }

        public JsonResult GetNotifications()
        {
            var notification = notificationService.SPGetNotification("Admin", PMS.Common.Globals.User.ID);

            return Json(new { notifications = notification }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult UpdateNotification()
        {
            var updateNotification = notificationService.UpdateNotification(PMS.Common.Globals.User.ID, 1);

            return Json(new { status = true }, JsonRequestBehavior.AllowGet);
        }
    }
}
