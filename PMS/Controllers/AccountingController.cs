using PMS.Common.Filters;
using PMS.DTO.ViewModels.COAViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PMS.Services.Services.ChartOfAccounts;
using PMS.Common.Classes;
using PMS.Services.Services.Setup;


namespace PMS.Controllers
{
    [AuthorizeUser]
    public class AccountingController : Controller
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IChartOfAccountsService ChartOfAccountsService;
        private readonly ISetupService setupService;

        public AccountingController( UnitOfWork<PMSEntities> _uow, IChartOfAccountsService _chartOfAccountsService, ISetupService _setupService)
        {
            uow = _uow;
            ChartOfAccountsService = _chartOfAccountsService;
            setupService = _setupService;
        }
        // GET: Accounting
        [AuthorizeUser(Roles = AppUserRoles.view_acc_COA)]
        public ActionResult Index()
        {
            ViewBag.ChartOfAccounts = ChartOfAccountsService.GetChartOfAccounts();
            return View();
        }
        [AuthorizeUser(Roles = AppUserRoles.add_acc_COA)]
        public ActionResult CreateOrUpdateCOA(int? id)
        {
            AddCOAVM model = new AddCOAVM();
            model.Status = true;

            if (id > 0)
            {
                //edit
                model = ChartOfAccountsService.GetCOAById(Convert.ToInt32(id));
                if (model == null)
                {
                    TempData["error"] = "Something went wrong, Service not found to update.";
                    return RedirectToAction("Index");
                }
            }
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            ViewBag.AccountTypeId = new SelectList(uow.GenericRepository<EF.AccountType>().Table.ToList(), "Id", "TypeName", model.AccountTypeId);
            return View(model);
        }
        [HttpPost]
        public ActionResult CreateOrUpdateCOA(AddCOAVM model)
        {
            try
            {
                bool IsActive = (Request.Form["Status"] != null);
                model.Status = IsActive;

                if (model.Id == 0)
                {
                    if (ChartOfAccountsService.GetChartOfAccounts().Where(x =>x.Code.ToLower() ==  model.Code.ToLower()).FirstOrDefault() != null)
                    {
                        ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", model.LocationId);
                        ViewBag.AccountTypeId = new SelectList(uow.GenericRepository<EF.AccountType>().Table.ToList(), "Id", "TypeName", model.AccountTypeId);
                        ModelState.AddModelError("Code", "Code Already Exit.");
                        return View(model);
                    } 
                    if (ChartOfAccountsService.GetChartOfAccounts().Where(x =>x.Name.ToLower() ==  model.Name.ToLower()).FirstOrDefault() != null)
                    {
                        ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", model.LocationId);
                        ViewBag.AccountTypeId = new SelectList(uow.GenericRepository<EF.AccountType>().Table.ToList(), "Id", "TypeName", model.AccountTypeId);
                        ModelState.AddModelError("Name", "Account Already Exist with same Name.");
                        return View(model);
                    }

                    //add

                    var result = ChartOfAccountsService.AddCOA(model);
                    if (result == true)
                    {
                        TempData["success"] = "Chart of account added succesfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", model.LocationId);
                        ViewBag.AccountTypeId = new SelectList(uow.GenericRepository<EF.AccountType>().Table.ToList(), "Id", "TypeName", model.AccountTypeId);
                        ViewBag.error = "Something went wrong, COA not added.";
                    }
                }
                else
                {
                    if (ChartOfAccountsService.GetCOAById(model.Id).Code.ToLower() != model.Code.ToLower())
                    {

                        if (ChartOfAccountsService.GetChartOfAccounts().Where(x => x.Code.ToLower() == model.Code.ToLower()).FirstOrDefault() != null)
                        {
                            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", model.LocationId);
                            ViewBag.AccountTypeId = new SelectList(uow.GenericRepository<EF.AccountType>().Table.ToList(), "Id", "TypeName", model.AccountTypeId);
                            ModelState.AddModelError("Code", "Code Already Exit.");
                            return View(model);
                        }
                    }

                    if (ChartOfAccountsService.GetCOAById(model.Id).Name.ToLower() != model.Name.ToLower())
                    {

                        if (ChartOfAccountsService.GetChartOfAccounts().Where(x => x.Name.ToLower() == model.Name.ToLower()).FirstOrDefault() != null)
                        {
                            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", model.LocationId);
                            ViewBag.AccountTypeId = new SelectList(uow.GenericRepository<EF.AccountType>().Table.ToList(), "Id", "TypeName", model.AccountTypeId);
                            ModelState.AddModelError("Name", "Account Already Exist with same Name.");
                            return View(model);
                        }
                    }


                    //update

                    var result = ChartOfAccountsService.UpdateCOA(model);
                    if (result == true)
                    {
                        TempData["success"] = "Service updated succesfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", model.LocationId);
                        ViewBag.AccountTypeId = new SelectList(uow.GenericRepository<EF.AccountType>().Table.ToList(), "Id", "TypeName", model.AccountTypeId);
                        ViewBag.error = "Something went wrong, service not updated";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", model.LocationId);
                ViewBag.AccountTypeId = new SelectList(uow.GenericRepository<EF.AccountType>().Table.ToList(), "Id", "TypeName", model.AccountTypeId);
                ViewBag.error = ex.Message;
            }
            return View(model);
        }
        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.delete_acc_COA)]
        public ActionResult DeleteChatOfAccount(int id)
        {
            try
            {
                var db = uow.Context;
                //if (db.Services.Where(x => x.AccountId == id).FirstOrDefault() != null)
                //{
                //    TempData["error"] = "You can not Delete this Account Already is in use with service.";


                //}
                //if (db.Taxes.Where(x => x.AccountId == id).FirstOrDefault() != null)
                //{
                //    TempData["error"] = "You can not Delete this Account Already is in use with Tax.";

                //}

                if (id != 0)
                {
                    ChartOfAccountsService.DeleteAccount(id);
                    TempData["success"] = "Account has Deleted Successfully.";

                }
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {

                TempData["error"] = "You can not Delete this Account Already is in use";
                return RedirectToAction("Index");


            }
        }


    }
}