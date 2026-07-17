using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels.TransportationViewModels;
using PMS.Services.Services.BusStop;
using PMS.Services.Services.Schedule;
using PMS.Services.Services.Setup;
using PMS.Services.Services.VehicleRoutes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Areas.Transportation.Controllers
{
    public class VehicleRoutesController : Controller
    {
        IVehicleRoutesService vehicleRoutesService;
        IScheduleService scheduleService;
        IBusStopService busStopService;
        ISetupService setupService;

        public VehicleRoutesController(IVehicleRoutesService _vehicleRoutesService,IScheduleService _scheduleService,IBusStopService _busStopService, ISetupService _setupService)
        {
            vehicleRoutesService = _vehicleRoutesService;
            scheduleService = _scheduleService;
            busStopService = _busStopService;
            setupService = _setupService;
        }
        // GET: Transportation/Routes
        [AuthorizeUser(Roles = AppUserRoles.view_routes)]
        public ActionResult GetAll()
        {
            var res = vehicleRoutesService.GetAll();
            ViewBag.routes = res;
            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];
            return View();
        }

        [AuthorizeUser(Roles = AppUserRoles.add_routes)]
        public ActionResult Add(int RouteID=0)
        {

            var response = vehicleRoutesService.GetRouteById(RouteID);
            if (response==null)
            {
                response = new AddRouteVM();
            }
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", response.LocationId);
            ViewBag.DepartureStopEdit = busStopService.GetAllStops();
            ViewBag.DepartureStopId = new SelectList(busStopService.GetAllStops(), "Id", "Name",response.DepartureStopId);
            return View(response);
        }
        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.add_routes)]

        public JsonResult Add(AddRouteVM addRouteVM)
        {
            if (addRouteVM.RouteID == 0)
            {
                bool res = vehicleRoutesService.Add(addRouteVM);
                if (res != false)
                {
                    TempData["success"] = "Route Added Successfully";
                }
                else
                {
                    TempData["error"] = "Something Went Wrong";
                }
            }
            else if (addRouteVM.RouteID > 0)
            {
                bool res1 = vehicleRoutesService.Update(addRouteVM);
                if (res1 != false)
                {
                    TempData["success"] = "Route Updated Successfully";
                }
                else
                {
                    TempData["error"] = "Something Went Wrong";
                }
            }
            return Json(new {status=true},JsonRequestBehavior.AllowGet);
        }
     
        [AuthorizeUser(Roles = AppUserRoles.delete_routes)]

        public ActionResult Delete(int RouteID)
        {
            bool res = vehicleRoutesService.Delete(RouteID);
            if(res!=false)
            {
                TempData["success"] = "Route Deleted Successfully";
            }
            else
            {
                TempData["error"] = "Something Went Wrong";
            }
            return RedirectToAction("GetAll");
        }
        public JsonResult GetStop()
        {
            var res = vehicleRoutesService.Getstops();
            return Json(new { Status = true, data = res }, JsonRequestBehavior.AllowGet);
        }
    }
}