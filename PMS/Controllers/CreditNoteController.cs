using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels;
using PMS.Services.Services.CreditNote;
using PMS.Services.Services.PaymentTypes;
using PMS.Services.Services.Person;
using PMS.Services.Services.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Controllers
{
    [AuthorizeUser]
    public class CreditNoteController : Controller
    {
        private readonly ISetupService setupService;
        private readonly ICreditNoteService creditNoteService;
        private readonly IPersonService personService;
        private readonly IPaymentTypesService paymentTypesservice;

        public CreditNoteController(ISetupService _setupService, ICreditNoteService _creditNoteService, IPersonService _personService, IPaymentTypesService _paymentTypesService)
        {
            setupService = _setupService;
            creditNoteService = _creditNoteService;
            personService = _personService;
            paymentTypesservice = _paymentTypesService;
        }
        // GET: CreditNote
        [AuthorizeUser(Roles = AppUserRoles.view_acc_CreditNote)]
        public ActionResult Index()
        {
           var model= creditNoteService.GetAll();
            return View(model);
        }
        [AuthorizeUser(Roles = AppUserRoles.add_acc_CreditNote)]
        public ActionResult Add(int id=0)
        {
            var model = new StudentCreditNoteVm();
            if (id != 0)
                model = creditNoteService.GetById(id);

            ViewBag.TypeId = new SelectList(creditNoteService.GetTypes(), "Id", "Name",model.TypeId);
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName",model.LocationId);
            ViewBag.PaymentTypeId = new SelectList(paymentTypesservice.GetPayment().Where(x => x.IsActive == true), "PaymentId", "PaymentName", model.PaymentTypeId);
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new {x.PersonID,FullName = x.Code + ": " + x.FullName}),"PersonID","FullName",model.StudentId);

            return View(model);
        }

        [HttpGet]
        public JsonResult GetCode(int LocationId, int TypeId)
        {
            var code = creditNoteService.GetCode(LocationId, TypeId);
            var students = personService.GetPersons()
                .Where(x => x.LocationId == LocationId)
                .Select(x => new { StudentId = x.PersonID, FullName = x.Code + ": " + x.FullName })
                .ToList();

            return Json(new { students = students, code = code }, JsonRequestBehavior.AllowGet);
        }


        [AuthorizeUser(Roles = AppUserRoles.add_acc_CreditNote)]
        [HttpPost]
        public ActionResult Add(StudentCreditNoteVm model)
        {
            var response = false;
            if (model.Id != 0)
               response= creditNoteService.Edit(model);
            else
               response =creditNoteService.Add(model);

            return Json(new {status=response },JsonRequestBehavior.AllowGet);
        }
        [AuthorizeUser(Roles = AppUserRoles.approve_acc_CreditNote)]
        public ActionResult ApproveById(int Id)
        {
            try
            {
               var response= creditNoteService.ApprovedById(Id);
                return Json(new { status = response, message = "Successfuly Approved!" }, JsonRequestBehavior.AllowGet);
                
            }catch(Exception ex)
            {
                return Json(new { status = false, message = ex.Message}, JsonRequestBehavior.AllowGet);
            }
            
        }
        public JsonResult GetAllStudentCreditNote(int id)
        {
            var response= creditNoteService.GetStudentCreditNote(id);
            return Json(new { data = response }, JsonRequestBehavior.AllowGet);
        }
        [AuthorizeUser(Roles = AppUserRoles.view_acc_CreditNote)]
        public ActionResult Details(int Id)
        {
            return View();
        }
        public JsonResult GetForPaymentById(int id)
        {
           var response= creditNoteService.GetForPaymentById(id);
            return Json(new { data = response }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ReferralCreditNotes()
        {
            var res = creditNoteService.GetReferralCreditNotes();
            return View(res);
        }
    }
}