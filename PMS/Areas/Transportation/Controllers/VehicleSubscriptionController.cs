using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels.TransportationViewModels;
using PMS.Services.Services.Person;
using PMS.Services.Services.Setup;
using PMS.Services.Services.UserManage;
using PMS.Services.Services.VehiclePrice;
using PMS.Services.Services.VehicleSubscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Areas.Transportation.Controllers
{
    public class VehicleSubscriptionController : Controller
    {
        private IVehicleSubscriptionService vehicleSubscriptionService;
        private IPersonService personService;
        private IVehiclePriceService vehiclePriceService;
        private IUserManageService userService;
        private ISetupService setupService;

        public VehicleSubscriptionController(IVehicleSubscriptionService _vehicleSubscriptionService, IPersonService _personService,
            IVehiclePriceService _vehiclePriceService, IUserManageService _userService, ISetupService _setupService)
        {
            vehicleSubscriptionService = _vehicleSubscriptionService;
            personService = _personService;
            vehiclePriceService = _vehiclePriceService;
            userService = _userService;
            setupService = _setupService;
        }
        // GET: Transportation/VehicleSubscription
        [AuthorizeUser(Roles = AppUserRoles.view_subscription)]
        public ActionResult Index()
        {
            var res = vehicleSubscriptionService.GetAll();
            ViewBag.success = TempData["success"];
            ViewBag.error = TempData["error"];
            ViewBag.Subscription = res;
            return View();
        }
        [AuthorizeUser(Roles = AppUserRoles.add_subscription)]
        public ActionResult Add(int? SubscriptionID)
        {
            var vehicleSubscription = vehicleSubscriptionService.GetById(Convert.ToInt32(SubscriptionID));
            if (vehicleSubscription == null)
            {
                vehicleSubscription = new VehicleSubscriptionVM();
            }
            if (SubscriptionID > 0)
            {
                ViewBag.VehiclePriceID = new SelectList(vehiclePriceService.GetPrice(vehicleSubscription.FrequencyId), "VehiclePriceId", "PriceName", vehicleSubscription.VehiclePriceID);
            }
            else if (SubscriptionID == null)
            {
                ViewBag.VehiclePriceID = new SelectList("");
            }
            ViewBag.StudentID = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName",vehicleSubscription.StudentID);
            ViewBag.FrequencyId = new SelectList(vehiclePriceService.GetActivePrices(), "Id", "Name",vehicleSubscription.FrequencyId);
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", vehicleSubscription.LocationId);

            return View(vehicleSubscription);
        }
        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.add_subscription)]

        public ActionResult Add(VehicleSubscriptionVM vehicleSubscriptionVM)
        {
            try
            {
                if (vehicleSubscriptionVM.SubscriptionId == 0)
                {
                    bool res = vehicleSubscriptionService.Add(vehicleSubscriptionVM);
                    if (res == true)
                    {
                        TempData["success"] = "Subscription Added Successfully";
                    }
                    else
                    {
                        TempData["error"] = "Something Went Wronge";
                    }
                }
                else if (vehicleSubscriptionVM.SubscriptionId > 0)
                {
                    bool res1 = vehicleSubscriptionService.Update(vehicleSubscriptionVM);
                    if (res1 == true)
                    {
                        TempData["success"] = "Subscription Updated Successfully";
                    }
                    else
                    {
                        TempData["error"] = "Something Went Wronge";
                    }
                }
            }
            catch(Exception ex)
            {
                TempData["error"] = ex.Message;            }
            return RedirectToAction("Index");
        }
        [AuthorizeUser(Roles = AppUserRoles.delete_subscription)]

        public ActionResult Delete(int SubscriptionID)
        {
            bool res = vehicleSubscriptionService.Delete(SubscriptionID);
            if(res==true)
            {
                TempData["success"] = "Subscription Deleted Successfully";
            }
            else
            {
                TempData["error"] = "Something Went Wronge";
            }
            return RedirectToAction("Index");
        }
        [HttpGet]
        public ActionResult GetVehiclePricesNameByFrequencyId(int Id)
        {
            var prices = vehicleSubscriptionService.GetPriceNameByFrequency(Id);

            return Json(new { success = true, prices = prices }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult GetVehiclePricesByPriceId(int priceID)
        {
            var prices = vehicleSubscriptionService.GetPricesByPriceId(priceID);
            return Json(new { success = true, prices = prices }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.approve_subscription)]

        public ActionResult Approve(int subscriptionid)
        {
            bool res = vehicleSubscriptionService.Approve(subscriptionid);
            if (res == true)
            {
                TempData["success"] = "Subscription Approved Successfully";
            }
            else
            {
                TempData["error"] = "Something Went Wronge";
            }
            return RedirectToAction("Index");

        }
        [AuthorizeUser(Roles = AppUserRoles.suspend_subscription)]

        public ActionResult Suspend(int subscriptionid)
        {
            bool res = vehicleSubscriptionService.Suspend(subscriptionid);
            if (res == true)
            {
                TempData["success"] = "Subscription Suspended Successfully";
            }
            else
            {
                TempData["error"] = "Something Went Wronge";
            }
            return RedirectToAction("Index");

        }
        [AuthorizeUser(Roles = AppUserRoles.end_subscription)]

        public ActionResult End(int subscriptionid)
        {
            bool res = vehicleSubscriptionService.End(subscriptionid);
            if (res == true)
            {
                TempData["success"] = "Subscription Ended Successfully";
            }
            else
            {
                TempData["error"] = "Something Went Wronge";
            }
            return RedirectToAction("Index");

        }
    }
}