using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels.TransportationViewModels;
using PMS.Services.Services.Setup;
using PMS.Services.Services.Vehicle;
using PMS.Services.Services.VehiclePrice;
using PMS.Services.Services.VehicleRoutes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Areas.Transportation.Controllers
{
    public class VehiclePriceController : Controller
    {
        private IVehiclePriceService vehiclePriceService;
        private IVehicleService vehicleService;
        private IVehicleRoutesService routeService;
        private ISetupService setupService;
        public VehiclePriceController(IVehiclePriceService _vehiclePriceService, IVehicleService _vehicleService,
            IVehicleRoutesService _routeService, ISetupService _setupService)
        {
            vehiclePriceService = _vehiclePriceService;
            vehicleService = _vehicleService;
            routeService = _routeService;
            setupService = _setupService;
        }
        // GET: Transportation/VehiclePrice
        [AuthorizeUser(Roles = AppUserRoles.view_prices)]
        public ActionResult Index()
        {
            ViewBag.VehiclePrices = vehiclePriceService.GetAll();
            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];
            return View();
        }
        [AuthorizeUser(Roles = AppUserRoles.add_prices)]
        public ActionResult Add(int? id)
        {
            var model = new VehiclePriceVM();
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            ViewBag.RouteId = new SelectList(routeService.GetAll(), "RouteID", "Routename");
            ViewBag.FrequencyId = new SelectList(vehiclePriceService.GetActivePrices(), "Id", "Name");
            if (id != null)
            {
                var vehiclePrice = vehiclePriceService.GetById(Convert.ToInt32(id));
                if (vehiclePrice != null)
                {
                    model = vehiclePrice;
                }
                ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", model.LocationId);
                ViewBag.RouteId = new SelectList(routeService.GetAll(), "RouteID", "Routename", model.RouteId);
                ViewBag.FrequencyId = new SelectList(vehiclePriceService.GetActivePrices(), "Id", "Name", model.FrequencyId);
            }
          
            return View(model);
        }
        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.add_prices)]
        public ActionResult Add(VehiclePriceVM vehiclePriceVM)
        {
            try
            {
                bool IsActive = (Request.Form["IsActive"] != null);
                vehiclePriceVM.IsActive = IsActive;

                if (vehiclePriceVM.VehiclePriceId == 0)
                {
                    //add

                    var result = vehiclePriceService.AddVehiclePrice(vehiclePriceVM);
                    if (result == true)
                    {
                        TempData["success"] = "Vehicle Price saved successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, Vehicle Price not added.";
                    }
                }
                else
                {
                    //update
                    var result = (vehiclePriceService.UpdateVehiclePrice(vehiclePriceVM).VehiclePriceId > 0);
                    if (result == true)
                    {
                        TempData["success"] = "Vehicle Price updated succesfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, Vehicle Price not updated";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }
            return View(vehiclePriceVM);
        }
        [AuthorizeUser(Roles = AppUserRoles.delete_prices)]
        public ActionResult DeleteVehicle(int id)
        {
            try
            {
                if (vehiclePriceService.DeleteVehiclePrice(id))
                    TempData["success"] = "Vehicle Price deleted successfully.";
                else

                    TempData["error"] = "Something went wrong. Vehicle Price not deleted.";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

    }
}