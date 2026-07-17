using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels.TransportationViewModels;
using PMS.Services.Services.Schedule;
using PMS.Services.Services.Setup;
using PMS.Services.Services.Vehicle;
using PMS.Services.Services.VehicleRoutes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Areas.Transportation.Controllers
{
    public class ScheduleController : Controller
    {
        private IScheduleService scheduleService;
        private IVehicleService vehicleService;
        private IVehicleRoutesService routeService;
        private ISetupService setupService;

        public ScheduleController(IScheduleService _scheduleService, IVehicleService _vehicleService,
            IVehicleRoutesService _routeService, ISetupService _setupService)
        {
            scheduleService = _scheduleService;
            vehicleService = _vehicleService;
            routeService = _routeService;
            setupService = _setupService;
        }
        // GET: Transportation/Schedule
        [AuthorizeUser(Roles = AppUserRoles.view_schedules)]
        public ActionResult Index()
        {
            ViewBag.Schedules = scheduleService.GetAll();
            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];
            return View();
        }
        [AuthorizeUser(Roles = AppUserRoles.add_schedules)]
        public ActionResult Add(int? id)
        {
            var model = new AddScheduleVM();

            model.FromDate = DateTime.Today;
            model.ToDate = DateTime.Today;
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            ViewBag.VehicleId = new SelectList("");
            ViewBag.DepartureTimeId = new SelectList(scheduleService.GetActiveTimes(), "Id", "Name");
            ViewBag.RouteId = new SelectList("");
            return View(model);
        }
        [AuthorizeUser(Roles = AppUserRoles.add_schedules)]
        [HttpPost]
        public ActionResult Add(AddScheduleVM Schedule)
        {
            try
            {
                if (Schedule.Id == 0)
                {
                    //add
                    var result = scheduleService.Add(Schedule);
                    if (result == true)
                    {
                        TempData["success"] = "Schedule saved successfully";
                        //return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, Schedule not added.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }
            return Json(new { status = true, data = "Schedule saved successfully." }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetBusesBYLocationId(int id)
        {
            try
            {
                var Buses = vehicleService.GetList().Where(x => x.LocationId == id).Select(x => new { x.BusId, BusName = (x.BusName.ToString())}).ToList();
                var Routes = routeService.GetRouteList().Where(x => x.LocationId == id).Select(x => new { x.RouteID, Routename = (x.Routename.ToString())}).ToList();

                return Json(new { Status = true, Buses = Buses, Routes = Routes }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Status = false, error = ex.StackTrace }, JsonRequestBehavior.AllowGet);

            }
        }
        [AuthorizeUser(Roles = AppUserRoles.edit_schedules)]
        public ActionResult Edit(int? id)
        {
            var model = new UpdateScheduleVM();
            if (id != null)
            {
                var schedule = scheduleService.GetById(Convert.ToInt32(id));
                if (schedule != null)
                {
                    model = schedule;
                }
                ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", model.LocationId);
                ViewBag.VehicleId = new SelectList(vehicleService.GetList(), "BusId", "BusName", model.VehicleId);
                ViewBag.DepartureTimeId = new SelectList(scheduleService.GetActiveTimes(), "Id", "Name", model.DepartureTimeId);
                ViewBag.RouteId = new SelectList(routeService.GetAll(), "RouteID", "Routename", model.RouteId);
            }
            return View(model);
        }
        [AuthorizeUser(Roles = AppUserRoles.edit_schedules)]
        [HttpPost]
        public ActionResult Edit(UpdateScheduleVM Schedule)
        {
            try
            {
                if (Schedule.Id > 0)
                {
                    //add
                    var result = scheduleService.Update(Schedule);
                    if (result == true)
                    {
                        TempData["success"] = "Schedule updated successfully";
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, Schedule not added.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }
            return Json(new { status = true, data = "Schedule updated successfully." }, JsonRequestBehavior.AllowGet);
        }
        //public ActionResult GetAll()
        //{
        //    var Schedules = scheduleService.GetAll();
        //    ViewBag.Schedule = Schedules;
        //    ViewBag.success = TempData["success"];
        //    ViewBag.error = TempData["error"];
        //    return View();
        //}
        [AuthorizeUser(Roles = AppUserRoles.delete_schedules)]
        public ActionResult Delete(int ScheduleID)
        {
            bool res = scheduleService.RemoveSchedule(ScheduleID);
            if (res != false)
            {
                TempData["success"] = "Schedule deleted successfully";
                return Redirect("Index");
            }
            else
            {
                TempData["error"] = "Something Went Wrong";
                return RedirectToAction("Index");
            }
        }
    }
}