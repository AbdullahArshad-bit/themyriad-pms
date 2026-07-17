using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.VehicleViewModel;
using PMS.Services.Services.Setup;
using PMS.Services.Services.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace PMS.Areas.Transportation.Controllers
{
    public class VehicleController : Controller
    {
        private readonly IVehicleService vehicleService;
        private readonly ISetupService setupService;
        public VehicleController(IVehicleService _vehicleService, ISetupService _setupService)
        {
            vehicleService = _vehicleService;
            setupService = _setupService;
        }
        [AuthorizeUser(Roles = AppUserRoles.view_vehicles)]
        public ActionResult Index()
        {
            ViewBag.Vehicles = vehicleService.GetVehicles();
            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];
            return View();
        }
        [AuthorizeUser(Roles = AppUserRoles.add_vehicles)]
        public ActionResult Add(int? id)
        {
            var vehicleVM = new VehicleViewModel();

            if (id != null)
            {
                var vehicle = vehicleService.GetById(Convert.ToInt32(id));
                if (vehicle != null)
                {
                    vehicleVM = vehicle;
                }
            }
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName",vehicleVM.LocationId);

            return View(vehicleVM);
        }
        [AuthorizeUser(Roles = AppUserRoles.add_vehicles)]
        [HttpPost]
        public ActionResult Add(VehicleViewModel vehicleVM, HttpPostedFileBase file)
        {
            try
            {
                bool IsActive = (Request.Form["IsActive"] != null);
                vehicleVM.IsActive = IsActive;

                if (vehicleVM.BusId == 0)
                {
                    //add

                    var result = vehicleService.AddVehicle(vehicleVM, file);
                    if (result == true)
                    {
                        TempData["success"] = "Vehicle saved successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, Vehicle not added.";
                    }
                }
                else
                {
                    //update
                    var result = (vehicleService.UpdateVehicle(vehicleVM, file).BusId > 0);
                    if (result == true)
                    {
                        TempData["success"] = "Vehicle updated succesfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, Vehicle not updated";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }
            return View(vehicleVM);
        }
        [AuthorizeUser(Roles = AppUserRoles.delete_vehicles)]

        public ActionResult DeleteVehicle(int id)
        {
            try
            {
                if (vehicleService.DeleteVehicle(id))
                    TempData["success"] = "Vehicle deleted successfully.";
                else

                    TempData["error"] = "Something went wrong. Vehicle not deleted.";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }
        [AuthorizeUser(Roles = AppUserRoles.view_seats)]

        public ActionResult Seats(int busId)
        {
            if (busId == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var seats = vehicleService.GetVehicleSeatsById(busId);
            if (seats == null)
            {
                return HttpNotFound();
            }
            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];
            ViewBag.busId = busId;
            return View(seats);
        }
        [AuthorizeUser(Roles = AppUserRoles.add_seats)]

        [HttpPost]
        public ActionResult AddVehicleSeat(VehicleSeatsViewModel model)
        {
            try
            {
                bool IsActive = (Request.Form["IsActive"] != null);
                model.IsActive = IsActive;


                if (model.Id == 0)
                {
                    //add
                    var result = vehicleService.AddSeat(model);
                    if (result == true)
                    {
                        TempData["success"] = "Seat saved successfully";

                    }
                    else
                    {
                        TempData["error"] = "Something went wrong, Seat not added.";
                    }
                }
                else
                {
                    //update
                    var result = (vehicleService.UpdateSeat(model));

                    if (result == true)
                    {
                        TempData["success"] = "Seat updated succesfully";
                    }
                    else
                    {
                        TempData["error"] = "Something went wrong, Seat not updated";
                    }
                }
            }

            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }
            return RedirectToAction("Seats", new { busId = model.VehicleId });
        }

        public JsonResult GetSeatDetail(int busId)
        {
            var response = vehicleService.GetSeatDetailById(busId);
            return Json(new { success = response.Success, data = response.Data, message = response.Message }, JsonRequestBehavior.AllowGet);
        }
        [AuthorizeUser(Roles = AppUserRoles.delete_seats)]

        public ActionResult DeleteSeat(int id, int busId)
        {
            try
            {
                if (vehicleService.DeleteSeat(id))
                    TempData["success"] = "Seat deleted successfully.";
                else

                    TempData["error"] = "Something went wrong. Seat not deleted.";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("Seats", new { busId = busId });
        }
    }
}