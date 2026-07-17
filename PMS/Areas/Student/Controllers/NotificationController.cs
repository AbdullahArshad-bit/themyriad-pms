using PMS.Services.Services.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static PMS.Common.Classes.Enumeration;

namespace PMS.Areas.Student.Controllers
{
    public class NotificationController : Controller
    {
        private readonly INotificationService notificationService;
        public NotificationController(
           INotificationService _notificationService
           )
        {
            notificationService = _notificationService;

        }
        // GET: Student/Notification
        public ActionResult Index()
        {
            var model = notificationService.GetAllNotification(PMS.Common.Globals.User.PersonId,(int)NotifiactionType.Student);

            return View(model);
        }
        public JsonResult GetNotifications()
        {

            var notification = notificationService.SPGetNotification("Student", PMS.Common.Globals.User.PersonId);

            return Json(new {notifications = notification}, JsonRequestBehavior.AllowGet);

        }
        public JsonResult UpdateNotification()
        {

            var updateNotification = notificationService.UpdateNotification(PMS.Common.Globals.User.PersonId, (int)NotifiactionType.Student);

            return Json(new { status  = true }, JsonRequestBehavior.AllowGet);

        }
    }
}