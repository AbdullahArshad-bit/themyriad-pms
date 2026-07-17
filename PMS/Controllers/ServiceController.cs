using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels.ServiceViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.ChartOfAccounts;
using PMS.Services.Services.Service;
using PMS.Services.Services.Setup;
using PMS.Services.Services.Tax;

namespace PMS.Controllers
{
    [AuthorizeUser]
    public class ServiceController : BaseController
    {
        private readonly IServicesService servicesService;
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IChartOfAccountsService ChartOfAccountsService;
        private readonly ITaxService taxService;
        private readonly ISetupService setupService;
        public ServiceController(IServicesService _servicesService, UnitOfWork<PMSEntities> _uow, IChartOfAccountsService _chartOfAccountsService,
            ITaxService _taxService, ISetupService _setupService)
        {
            servicesService = _servicesService;
            uow = _uow;
            ChartOfAccountsService = _chartOfAccountsService;
            taxService = _taxService;
            setupService = _setupService;
        }

        // GET: Service
        [AuthorizeUser(Roles = AppUserRoles.view_Services)]
        public ActionResult ServicesList()
        {
            ViewBag.Services = servicesService.GetServices();
            return View();
        }
        [AuthorizeUser(Roles = AppUserRoles.add_acc_Services)]
        public ActionResult AddService(int? id)
        {
            AddServiceVM model = new AddServiceVM();
            model.IsActive = true;

            if (id > 0)
            {
                //edit
                model = servicesService.GetServicesById(Convert.ToInt32(id));
                if (model == null)
                {
                    TempData["error"] = "Something went wrong, Service not found to update.";
                    return RedirectToAction("ServicesList");
                }
            }
            var accounts = ChartOfAccountsService.GetChartOfAccounts().Where(x => x.Status == true).ToList();

            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            ViewBag.AccountId = new SelectList(accounts, "Id", "Name", model.AccountId);
            ViewBag.ServiceType = servicesService.GetServiceType();
            ViewBag.TaxId = new SelectList(taxService.GetAll(), "TaxId", "TaxName", model.TaxId);

            return View(model);
        }

        [HttpPost]
        public JsonResult GetAccountsByServiceType(int serviceTypeId, int? selectedAccountId = null)
        {
            var accounts = ChartOfAccountsService.GetAccountsByServiceType(serviceTypeId).Select(x => new {Id = x.Id,  Name = x.Name,
                                                   Selected = selectedAccountId.HasValue && x.Id == selectedAccountId.Value}).ToList();
            return Json(accounts);
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.add_acc_Services)]
        public ActionResult AddService(AddServiceVM model)
        {
            try
            {
                bool IsActive = (Request.Form["IsActive"] != null);
                model.IsActive = IsActive;

                bool IsDeposit = (Request.Form["IsPrePlacementService"] != null);
                model.IsPrePlacementService = IsDeposit;

                if (model.serviceId == 0)
                {
                    if (servicesService.GetServices().Where(x => x.ServiceName.ToLower() == model.ServiceName.ToLower()).FirstOrDefault() != null)
                    {
                        ViewBag.AccountId = new SelectList(uow.GenericRepository<EF.ChartOfAccount>().Table.ToList(), "Id", "Name", model.AccountId);
                        ModelState.AddModelError("ServiceName", "Service Already Exist with Same Name.");
                        return View(model);
                    }

                    //add
                    model.CreatedDate = DateTime.Now;
                    model.CreatedBy = Common.Globals.User.Email;

                    var result = servicesService.AddService(model);
                    if (result == true)
                    {
                        TempData["success"] = "Service added succesfully";
                        return RedirectToAction("ServicesList");
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, Service not added.";
                    }
                }
                else
                {

                    if (servicesService.GetServicesById(model.serviceId).ServiceName.ToLower() != model.ServiceName.ToLower())
                    {
                        if (servicesService.GetServices().Where(x => x.ServiceName.ToLower() == model.ServiceName.ToLower()).FirstOrDefault() != null)
                        {
                            ViewBag.AccountId = new SelectList(uow.GenericRepository<EF.ChartOfAccount>().Table.ToList(), "Id", "Name", model.AccountId);
                            ModelState.AddModelError("ServiceName", "Service Already Exist with Same Name.");
                            return View(model);
                        }
                    }
                    //update

                    model.UpdatedDate = DateTime.Now;
                    model.UpdatedBy = Common.Globals.User.Email;

                    var result = servicesService.UpdateService(model);
                    if (result == true)
                    {
                        TempData["success"] = "Service updated succesfully";
                        return RedirectToAction("ServicesList");
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, service not updated";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }
            return View(model);
        }
        [AuthorizeUser(Roles = AppUserRoles.delete_acc_Services)]

        public ActionResult DeleteService(int id)
        {
            try
            {
                var db = uow.Context;
                if (db.InvoicingDetails.Where(x => x.ServiceId == id).FirstOrDefault() != null)
                {
                    TempData["error"] = "You can not delete this service already in use.";
                    return RedirectToAction("ServicesList");
                }

                if (servicesService.DeleteService(id))
                {
                    TempData["success"] = "service deleted successfully.";
                }
                else
                {
                    TempData["error"] = "Sorry something went wrong, service not deleted.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }
            return RedirectToAction("ServicesList");
        }
    }
}