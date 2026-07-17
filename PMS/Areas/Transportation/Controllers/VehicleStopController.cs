using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels;
using PMS.Services.Services.BusStop;
using PMS.Services.Services.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Areas.Transportation.Controllers
{
    public class VehicleStopController : Controller
    {
        IBusStopService busStopService;
        ISetupService setupService;
        public VehicleStopController(IBusStopService _busStopService, ISetupService _setupService)
        {
            busStopService = _busStopService;
            setupService = _setupService;
        }
        // GET: Transportation/BusStop
        [AuthorizeUser(Roles = AppUserRoles.view_stops)]
        public ActionResult GetAll()
        {
            var res = busStopService.GetAllStops();
            ViewBag.stops = res;
            ViewBag.success = TempData["success"];
            ViewBag.error = TempData["error"];
            return View();
        }
        [AuthorizeUser(Roles = AppUserRoles.add_stops)]
        public ActionResult Add(int StopID=0)
        {
            var response = busStopService.GetStopById(StopID);
            if(response==null)
            {
                response = new AddBusStopViewModel();
            }
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", response.LocationId);

            return View(response);
        }
        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.add_stops)]

        public ActionResult Add(AddBusStopViewModel addBusStopViewModel)
        {
            if (addBusStopViewModel.Id == 0)
            {
                bool res = busStopService.Add(addBusStopViewModel);
                if(res!=false)
                {
                    TempData["success"] = "Stop Added Succesfully";
                }
                else
                {
                    TempData["error"] = "Something Went Wrong";
                }
            }
            else if (addBusStopViewModel.Id>0)
            {
                bool res1 = busStopService.Update(addBusStopViewModel);
                if(res1!=false)
                {
                    TempData["success"] = "Stop Updated Successfully";
                }
                else
                {
                    TempData["error"] = "Something went Wrong";
                }
            }
            return RedirectToAction("GetAll");
        }
       
        [AuthorizeUser(Roles = AppUserRoles.delete_stops)]

        public ActionResult Delete(int StopID)
        {
            bool res = busStopService.Delete(StopID);
            if(res!=false)
            {
                TempData["success"] = "Stop Deleted Successfully";
            }
            else
            {
                TempData["error"] = "Something went wrong";
            }
                
            return RedirectToAction("GetAll");
        }
    }
}