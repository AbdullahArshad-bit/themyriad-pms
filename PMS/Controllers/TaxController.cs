using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels.TaxViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.ChartOfAccounts;
using PMS.Services.Services.Setup;
using PMS.Services.Services.Tax;
using PMS.Services.Services.Tex;

namespace PMS.Controllers
{
    [AuthorizeUser]
    public class TaxController : BaseController
    {
        private readonly ITaxService taxService;
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IChartOfAccountsService ChartOfAccountsService;
        private readonly ISetupService setupService;

        public TaxController(ITaxService _taxService, UnitOfWork<PMSEntities> _uow, IChartOfAccountsService _chartOfAccountsService, ISetupService _setupService)
        {
            taxService = _taxService;
            uow = _uow;
            ChartOfAccountsService = _chartOfAccountsService;
            setupService = _setupService;

        }
        [AuthorizeUser(Roles = AppUserRoles.view_acc_TaxType)]
        public ActionResult TaxList()
        {

            ViewBag.Tax = taxService.GetTax();
            return View();
        }
        [AuthorizeUser(Roles = AppUserRoles.add_acc_TaxType)]

        public ActionResult AddTax(int? id)
        {
            AddTaxVM model = new AddTaxVM();
            model.IsActive = true;

            if (id > 0)
            {
                //edit
                model = taxService.GetTaxById(Convert.ToInt32(id));
                if (model == null)
                {
                    TempData["error"] = "Something went wrong, Tax not found to update.";
                    return RedirectToAction("TaxList");
                }
            }
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            ViewBag.AccountId = new SelectList(uow.GenericRepository<EF.ChartOfAccount>().Table.ToList().Where(x=>x.Status==true), "Id", "Name", model.AccountId);
            return View(model);
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.add_acc_TaxType)]

        public ActionResult AddTax(AddTaxVM model)
        {
            try
            {
                bool IsActive = (Request.Form["IsActive"] != null);
                model.IsActive = IsActive;
                if (model.TaxId == 0)
                {
                    if (taxService.GetTax().Where(x => x.Code.ToLower() == model.Code.ToLower()).FirstOrDefault() != null)
                    {
                        ViewBag.AccountId = new SelectList(uow.GenericRepository<EF.ChartOfAccount>().Table.ToList(), "Id", "Name", model.AccountId);
                        ModelState.AddModelError("Code", "Tax Code Already Exist.");
                        return View(model);
                    }
                    if (taxService.GetTax().Where(x => x.TaxName.ToLower() == model.TaxName.ToLower()).FirstOrDefault() != null)
                    {
                        ViewBag.AccountId = new SelectList(uow.GenericRepository<EF.ChartOfAccount>().Table.ToList(), "Id", "Name", model.AccountId);
                        ModelState.AddModelError("TaxName", "Tax Type Already Exist with same Name.");
                        return View(model);
                    }

                    //add
                    model.CreatedDate = DateTime.Now;
                    model.CreatedBy = Common.Globals.User.Email;

                    var result = taxService.AddTax(model);
                    if (result == true)
                    {
                        TempData["success"] = "Tax added succesfully";
                        return RedirectToAction("TaxList");
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, Tax not added.";
                    }
                }
                else
                {
                    if (taxService.GetTaxById(model.TaxId).Code.ToLower() != model.Code.ToLower())
                    {
                        if (taxService.GetTax().Where(x => x.Code.ToLower() == model.Code.ToLower()).FirstOrDefault() != null)
                        {
                            ViewBag.AccountId = new SelectList(uow.GenericRepository<EF.ChartOfAccount>().Table.ToList(), "Id", "Name", model.AccountId);
                            ModelState.AddModelError("Code", "Tax Code Already Exist.");
                            return View(model);
                        }

                    }
                    if (taxService.GetTaxById(model.TaxId).TaxName.ToLower() != model.TaxName.ToLower())
                    {
                        if (taxService.GetTax().Where(x => x.TaxName.ToLower() == model.TaxName.ToLower()).FirstOrDefault() != null)
                        {
                            ViewBag.AccountId = new SelectList(uow.GenericRepository<EF.ChartOfAccount>().Table.ToList(), "Id", "Name", model.AccountId);
                            ModelState.AddModelError("TaxName", "Tax Type Already Exist with same Name.");
                            return View(model);
                        }

                    }

                    //update

                    model.UpdatedDate = DateTime.Now;
                    model.UpdatedBy = Common.Globals.User.Email;

                    var result = taxService.UpdateTax(model);
                    if (result == true)
                    {
                        TempData["success"] = "Tax updated succesfully";
                        return RedirectToAction("TaxList");
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, Tax not updated";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }
            return View(model);
        }
        [AuthorizeUser(Roles = AppUserRoles.delete_acc_TaxType)]

        public ActionResult DeleteTax(int id)
        {
            try
            {
                if (taxService.DeleteTax(id))
                {
                    TempData["success"] = "Tax deleted successfully.";
                }
                else
                {
                    TempData["error"] = "Sorry something went wrong, Tax not deleted.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }
            return RedirectToAction("TaxList");
        }
    }
}