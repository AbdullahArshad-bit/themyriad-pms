using PMS.Services.Services.Schedule;
using PMS.Services.Services.VehicleRoutes;
using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PMS.Services.Services.Vehicle;
using PMS.Services.Services.VehicleSubscription;
using PMS.Services.Services.Reporting;

namespace PMS.Areas.Student.Controllers
{
    public class TransportController : Controller
    {
       private readonly IVehicleRoutesService vehicleRoutesService;
       private readonly IScheduleService scheduleService;
       private readonly IVehicleService vehicleService;
       private readonly IVehicleSubscriptionService vehicleSubscriptionService;
       private readonly IReportingService reportingService;
        public TransportController(IVehicleRoutesService _vehicleRoutesService, IScheduleService _scheduleService, IVehicleService _vehicleService, IVehicleSubscriptionService _vehicleSubscriptionService,IReportingService _reportingService)
        {
            vehicleRoutesService = _vehicleRoutesService;
            scheduleService = _scheduleService;
            vehicleService = _vehicleService;
            vehicleSubscriptionService = _vehicleSubscriptionService;
            reportingService = _reportingService;
        }
        // GET: Student/Transport
        public ActionResult Routes(DateTime? date)
        {
            if (date == null)
            {
                date = DateTime.Now.Date;
            }
            var res = scheduleService.GetRoutes(date);
            ViewBag.routes = res;
            ViewBag.date = date.HasValue ? date.Value.ToString("dd/MMM/yyyy") : null;
            var personid = PMS.Common.Globals.User.PersonId;
            var res1 = vehicleSubscriptionService.GetSubscription(personid);
            ViewBag.data = res1;
            ViewBag.success = TempData["success"];
            ViewBag.error = TempData["error"];
            return View();
        }
        public JsonResult GetStops(int RouteId)
        {
            var res = vehicleRoutesService.Stops(RouteId);
            return Json(new { success = true, res = res }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult GetSeats(int busid, int scheduleid)
        {
            try
            {
                bool res = scheduleService.BookSeats(busid, scheduleid);

                if (res == true)
                {
                    TempData["success"] = "Seat reserved successfully. Please check your email.";
                }
                else
                {
                    TempData["error"] = "Booking not updated. Please try again later.";
                }
            }
            catch (Exception ex)
            {

                TempData["error"] = ex.Message;
            }
            return RedirectToAction("Routes");

        }
        public ActionResult GetStudentBookingReport()
        {
            var personid = PMS.Common.Globals.User.PersonId;
            var model = reportingService.GetStudentTransportationBookingReport(personid);
            ViewBag.success = TempData["success"];
            ViewBag.error = TempData["error"];
            return View(model);
        }
        public ActionResult CancelSeat(int id)
        {
            bool res = reportingService.CancelStudentSeat(id);
            if(res==true)
            {
                TempData["success"] = "Your Seat Reservation has been canceled";
            }
            else
            {
                TempData["error"] = "Something Went Wronge";
            }
            return RedirectToAction("GetStudentBookingReport");
        }


    }
}