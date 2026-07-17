using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PMS.DTO.ViewModels.PaymentViewModels;
using PMS.Common.Filters;
using PMS.Services.Services.PaymentTypes;
using PMS.Common.Classes;
using PMS.Services.Services.ChartOfAccounts;
using PMS.Services.Services.Setup;

namespace PMS.Controllers
{
    [AuthorizeUser]

    public class PaymentTypesController : Controller
    {
        private readonly IPaymentTypesService paymentTypesService;
        private readonly IChartOfAccountsService ChartOfAccountsService;
        private readonly ISetupService setupService;
        public PaymentTypesController(IPaymentTypesService _paymentTypesService, IChartOfAccountsService _chartOfAccountsService, ISetupService _setupService)
        {
            paymentTypesService = _paymentTypesService;
            ChartOfAccountsService = _chartOfAccountsService;
            setupService = _setupService;
        }

        // GET: PaymentTypes
        [AuthorizeUser(Roles = AppUserRoles.view_acc_PaymentMethod)]
        public ActionResult PaymentList()
        {
            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];
            ViewBag.Payment = paymentTypesService.GetPayment();
            return View();
        }
        [AuthorizeUser(Roles = AppUserRoles.add_acc_PaymentMethod)]

        public ActionResult AddPayment(int? id)
        {
            AddPaymentTypeVM model = new AddPaymentTypeVM();
            model.IsActive = true;
            if (id > 0)
            {
                //edit
                model = paymentTypesService.GetPaymentById(Convert.ToInt32(id));
                if (model == null)
                {
                    TempData["error"] = "Something went wrong, Payment not found to insert.";
                    return RedirectToAction("PaymentList");
                }
            }

            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            ViewBag.AccountId = new SelectList(ChartOfAccountsService.GetAssetAccounts(), "Id", "Name", model.AccountId);

            return View(model);
        }

        [HttpPost]
        public ActionResult AddPayment(AddPaymentTypeVM model)
        {
            try
            {
                bool IsActive = (Request.Form["IsActive"] != null);
                model.IsActive = IsActive;
                if (model.PaymentId == 0)
                {
                    //add
                    model.CreatedDate = DateTime.Now;
                    model.CreatedBy = Common.Globals.User.Email;
                    var result = paymentTypesService.AddPayment(model);
                    if (result == true)
                    {
                        TempData["success"] = "Payment added succesfully";
                        return RedirectToAction("PaymentList");
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, Payment not added.";
                    }
                }
                else
                {
                    //update
                    model.UpdatedDate = DateTime.Now;
                    model.UpdatedBy = Common.Globals.User.Email;
                    var result = paymentTypesService.UpdatePayment(model);
                    if (result == true)
                    {
                        TempData["success"] = "Payment updated succesfully";
                        return RedirectToAction("PaymentList");
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, Payment not updated";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }

            ViewBag.AccountId = new SelectList(ChartOfAccountsService.GetAssetAccounts(), "Id", "Name", model.AccountId);

            return View(model);
        }


        [AuthorizeUser(Roles = AppUserRoles.delete_acc_PaymentMethod)]
        public ActionResult DeletePayment(int id)
        {
            try
            {
                if (paymentTypesService.DeletePayment(id))
                {
                    TempData["success"] = "Payment deleted successfully.";
                }
                else
                {
                    TempData["error"] = "Sorry something went wrong, Payment not deleted.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }
            return RedirectToAction("PaymentList");
        }
    }
}