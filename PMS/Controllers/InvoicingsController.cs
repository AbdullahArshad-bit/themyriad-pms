using PMS.Classes;
using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.EF;
using PMS.Services.Services.Person;
using PMS.Services.Services.Setup;
using PMS.Services.Services.Service;
using PMS.Services.Services.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using PMS.Services.Services.AuditLogs;
using System.Web.Mvc;
using PMS.Services.Services.Invoicings;
using System.Threading.Tasks;
using static PMS.Common.Classes.Enumeration;
using PMS.Services.Services.Tax;
using PMS.Services.Services.PaymentGateway;
using PMS.Services.Services.VehicleSubscription;
using PMS.Services.Services.Reporting;
using PMS.DTO.ViewModels.ReportingViewModels;
using PMS.Services.Services.PaymentTypes;
using PMS.Repository.UnitOfWork;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.DashboardViewModel;
using iTextSharp.text;
using PMS.Services.Services.CreditNote;
using PMS.Services.Services.LocationContext;
using PMS.Services.Services.Notifications;

namespace PMS.Controllers
{
    public class InvoicingsController : Controller
    {
        private readonly IVehicleSubscriptionService vehicleSubscriptionService;
        private readonly IPersonService personService;
        private readonly ISetupService setupService;
        private readonly IServicesService servicesService;
        private readonly IEmailService emailService;
        private readonly IAuditLogsService auditLogsService;
        private readonly IInvoicingService invoicingService;
        private readonly ITaxService taxService;
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IPaymentGatewayService paymentGatewayService;
        private readonly IReportingService reportingService;
        private readonly IPaymentTypesService paymentTypesservice;
        private readonly ICreditNoteService creditNoteService;
        private readonly ILocationContextService locationContextService;
        private readonly INotificationService notificationService;

        public InvoicingsController(
            IAuditLogsService _auditLogsService, IPersonService _personService,
            ISetupService _setupService, IServicesService _servicesService, IEmailService _emailservice
            , IInvoicingService _invoicingService, ITaxService _taxService, IPaymentGatewayService _paymentGatewayService,
            IReportingService _reportingService, IVehicleSubscriptionService _vehicleSubscriptionService, UnitOfWork<PMSEntities> _uow,
            IPaymentTypesService _paymentTypesService, ICreditNoteService _creditNoteService, ILocationContextService _locationContextService,
            INotificationService _notificationService)
        {
            vehicleSubscriptionService = _vehicleSubscriptionService;
            personService = _personService;
            setupService = _setupService;
            servicesService = _servicesService;
            emailService = _emailservice;
            auditLogsService = _auditLogsService;
            invoicingService = _invoicingService;
            taxService = _taxService;
            paymentGatewayService = _paymentGatewayService;
            reportingService = _reportingService;
            uow = _uow;
            paymentTypesservice = _paymentTypesService;
            creditNoteService = _creditNoteService;
            locationContextService = _locationContextService;
            notificationService = _notificationService;
        }

        // GET: Invoicings
        [AuthorizeUser(Roles = AppUserRoles.view_acc_Invoice)]
        public ActionResult Index(InvoicingBinding request)
        {
            if (request.FromDate == null || request.ToDate == null)
            {
                var today = DateTime.Now.Date;

                request.ToDate = today;
                request.FromDate = new DateTime(today.Year - 1, 9, 1);
            }

            ViewBag.FromDate = request.FromDate.HasValue ? request.FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = request.ToDate.HasValue ? request.ToDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.InvoiceTypeId = new SelectList(invoicingService.GetInvoicingTypes(), "Id", "InvoiceTypeName");

            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];


            return View();
        }

        public object loadInvoicingbyAjax(InvoicingBinding request)
        {
            try
            {
                var Invoicing = new InvoicingsResponse();
                Invoicing = invoicingService.GetAll(request, "", request.search.value, request.start, request.length, request.query, request.orderBy, request.orderDir, request.FromDate, request.ToDate, request.InvoiceTypeId);
                var result = Json(new { draw = request.draw, recordsFiltered = Invoicing.RecordsFiltered, recordsTotal = Invoicing.TotalRecords, data = Invoicing.InvoicingList });
                return result;
            }

            catch (Exception ex)
            {
                return Json(new { error = "An error occurred while retrieving data.", details = ex.Message });
            }
        }

        public async Task ExportInvoiceReport(DateTime? FromDate, DateTime? ToDate, int? InvoiceTypeId = null)
        {
            string QueryBy = "Excel";
            var invoice = await invoicingService.ExportInvoiceReportAsync(QueryBy, FromDate, ToDate, InvoiceTypeId);
            var report = invoice.InvoicingList;
            var data = report.Select(x => new
            {
                x.Location,
                InvoiceId = x.Code,
                MyriadID = x.MyriadID,
                FullName = x.FullName,
                InvoiceDate = x.InvoiceDate.ToString("dd/M/yyyy"),
                FromDate = x.FromDate.HasValue ? x.FromDate.Value.ToString("dd/M/yyyy") : null,
                ToDate = x.ToDate.HasValue ? x.ToDate.Value.ToString("dd/M/yyyy") : null,
                //DueDate = x.DueDate.HasValue ? x.DueDate.Value.ToString("dd/M/yyyy") : null,
                x.Remarks,
                ServiceName = x.ServiceName,
                x.NetAmount,
                x.PendingBalance,
                TotalBalance = x.TotalBalanceOfResident,
                Status = x.Status ? "Approved" : "Pending",
                Paid = x.isPaid == true && x.InvoiceTypeId == 2 && x.Refunded == true ? "Refunded" :
              x.isPaid == true ? "Paid" :
              x.Status && (x.ParentInvoiceId == null || x.ParentInvoiceId != null) && x.Refunded == true ? "Reversed" :
              "Unpaid",
                x.CreatedBy,
                x.ApprovedBy,
                CreatedDate = x.CreatedDate.ToString("dd/M/yyyy"),
            });
            ExcelHelper.ExportToExcel(Response, data, "Invoices Report - PMS");
            return;
        }

        // GET: Invoicings/Create
        [AuthorizeUser(Roles = AppUserRoles.add_acc_Invoice)]
        public ActionResult Create()
        {
            Invoicing model = new Invoicing();
            model.InvoiceDate = DateTime.Now;
            model.CreatedDate = DateTime.Now;

            ViewBag.StudentId = new SelectList("");
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            ViewBag.TaxIds = new SelectList(taxService.GetAll(), "TaxId", "TaxName");
            ViewBag.InvoiceTypeId = new SelectList(invoicingService.GetInvoicingTypes(), "Id", "InvoiceTypeName");
            ViewBag.serviceid = new SelectList(servicesService.GetServices().Where(x => x.IsActive == true), "ServiceId", "ServiceName");
            model.LocationId = Convert.ToInt32(setupService.GetLastLocation());
            return View(model);
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.add_acc_Invoice)]
        public ActionResult SaveInvoice(Invoicing invoicing, List<InvoicingDetail> list)
        {
            try
            {
                bool status = false;

                if (ModelState.IsValid)
                {
                    status = invoicingService.SaveInvoice(invoicing, list);
                }

                if (status == true)
                {
                    ViewBag.success = "Invoice Saved Successfully";
                    TempData["success"] = ViewBag.success;
                }

                else
                {
                    ViewBag.error = "Something Went Wrong";
                    TempData["error"] = ViewBag.error;
                }

                return Json(new { status = status, redirect = false, error = string.Empty }, JsonRequestBehavior.AllowGet);
            }

            catch (Exception ex)
            {
                string errorMessage = ex.Message;

                if (errorMessage != null)
                {
                    //TempData["error"] = errorMessage;

                    return Json(new { status = false, redirect = false, error = errorMessage }, JsonRequestBehavior.AllowGet);
                }

                else
                {
                    ViewBag.error = errorMessage;
                    TempData["error"] = ViewBag.error;

                    return Json(new { status = false, redirect = false, error = string.Empty }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        // GET: Invoicings/Edit/id
        [AuthorizeUser(Roles = AppUserRoles.add_acc_Invoice)]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var invoicing = invoicingService.GetById(id);

            if (invoicing == null)
            {
                return HttpNotFound();
            }

            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", invoicing.StudentId);
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", invoicing.LocationId);
            ViewBag.InvoiceTypeId = new SelectList(invoicingService.GetInvoicingTypes(), "Id", "InvoiceTypeName", invoicing.InvoiceTypeId);
            ViewBag.InvoicingDetail = invoicingService.GetInvoiceDetail(invoicing.Id);
            ViewBag.serviceid = new SelectList(servicesService.GetServices().Where(x => x.IsActive == true), "ServiceId", "ServiceName");
            ViewBag.SubTotal = invoicing.TotalPrice;
            ViewBag.NetAmount = invoicing.TotalPrice;
            ViewBag.TaxIds = new SelectList(taxService.GetAll(), "TaxId", "TaxName");
            invoicing.LocationId = Convert.ToInt32(setupService.GetLastLocation());


            return View(invoicing);
        }

        [AuthorizeUser(Roles = AppUserRoles.view_acc_InvoiceDetail)]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Invoicing invoicing = invoicingService.GetById(id);

            if (invoicing == null)
            {
                return HttpNotFound();
            }
            ViewBag.InvoicingDetail = invoicingService.GetInvoiceDetail(invoicing.Id);
            ViewBag.LocationSetting = setupService.GetLocationSettingsByLocationid(invoicing.LocationId);
            var receiptDetails = invoicingService.ReceiptDetail(id);
            ViewBag.ReceiptDetail = receiptDetails;
            var sum = receiptDetails.Sum(x => x.Amount);
            ViewBag.total = invoicing.NetAmount - sum;
            var invoicingsResponse = invoicingService.Getcalculation();
            ViewBag.PendingBalance = invoicingsResponse.InvoicingList.FirstOrDefault(x => x.Id == id)?.PendingBalance;
            ViewBag.TotalBalanceOfResident = invoicingsResponse.InvoicingList.FirstOrDefault(x => x.Id == id)?.TotalBalanceOfResident;

            return View(invoicing);
        }

        [AllowAnonymous]
        public ActionResult InvoiceHtml(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Invoicing invoicing = invoicingService.GetById(id);

            if (invoicing == null)
            {
                return HttpNotFound();
            }

            ViewBag.InvoicingDetail = invoicingService.GetInvoiceDetail(invoicing.Id);

            return View(invoicing);
        }

        public ActionResult GetTaxPercenageValueById(int Id)
        {
            var Tax = taxService.GetTaxById(Id);

            if (Tax == null)
            {
                return HttpNotFound();
            }

            return Json(new { status = true, Value = Tax.TaxPercentage }, JsonRequestBehavior.AllowGet);
        }


        [AuthorizeUser(Roles = AppUserRoles.add_acc_Invoice)]
        public ActionResult FeeAssessment(int? StudentId)
        {
            if (StudentId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var invoicing = invoicingService.GetLastInoviceByStudentIdId(StudentId);

            if (invoicing == null)
            {
                return HttpNotFound();
            }

            ViewBag.StudentId = new SelectList(personService.GetPersons(), "PersonID", "FullName", invoicing.StudentId);
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", invoicing.LocationId);
            ViewBag.InvoiceTypeId = new SelectList(invoicingService.GetInvoicingTypes(), "Id", "InvoiceTypeName", invoicing.InvoiceTypeId);
            ViewBag.InvoicingDetail = invoicingService.GetFeeAssessmentInvoiceDetail(invoicing.Id);
            ViewBag.SubTotal = invoicing.TotalPrice;
            ViewBag.NetAmount = invoicing.TotalPrice;
            ViewBag.TaxIds = new SelectList(taxService.GetAll(), "TaxId", "TaxName");

            return View(invoicing);
        }


        [HttpPost]
        public ActionResult GenerateInvoices(int[] personIds, DateTime StartDate, DateTime EndDate)
        {
            var (success, successMessage, errorMessage) = invoicingService.GenerateInvoices(personIds, StartDate, EndDate);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                ViewBag.error = errorMessage;
                TempData["error"] = ViewBag.error;
            }

            if (!string.IsNullOrEmpty(successMessage))
            {
                ViewBag.success = successMessage;
                TempData["success"] = ViewBag.success;
            }

            return Json(new { success = success, message = success ? "Invoice generation process completed" : "An error occurred while generating invoices" });
        }

        public ActionResult GetLocationBYId(int id, int InvoiceTypeId)
        {
            try
            {
                var Students = (InvoiceTypeId == (int)InvoiceTypes.Deposit || InvoiceTypeId == (int)InvoiceTypes.Miscellaneous) ?
                    personService.GetPersonsNotCheckedinYet(InvoiceTypeId).Where(x => x.LocationId == id).Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }).ToList() :
                    User.IsInRole(AppUserRoles.View_CheckedOut_Residents) == true ? personService.GetPersonsReservedCurrentlyOrCheckedOut().Where(x => x.LocationId == id).Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }).ToList() :
                    personService.GetPersonsReservedCurrently().Where(x => x.LocationId == id).Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }).ToList();
                Students = Students.Distinct().ToList();
                var Code = invoicingService.GetMaxInvoiceCodeString(id, InvoiceTypeId);

                return Json(new { Status = true, Students = Students, Code = Code }, JsonRequestBehavior.AllowGet);
            }

            catch (Exception EX)
            {
                return Json(new { Status = false, Message = EX }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetServicesList(int InvoicetypeId)
        {
            var services = servicesService.GetServices().Where(x => x.IsActive == true);
            if (InvoicetypeId == (int)InvoiceTypes.Rental)
                services = services.Where(x => x.ServiceTypeId == (int)InvoiceTypes.Rental);
            else if (InvoicetypeId == (int)InvoiceTypes.Deposit)
                services = services.Where(x => x.ServiceTypeId == (int)InvoiceTypes.Deposit);
            else
                services = services.Where(x => x.ServiceTypeId != (int)InvoiceTypes.Deposit && x.ServiceTypeId != (int)InvoiceTypes.Rental);

            var List = services.Select(x => new
            {
                value = x.ServiceName,
                data = x.ServiceId,
                price = x.ServiceAmount,
                servicetypeid = x.ServiceTypeId,
                taxId = x.TaxId
            }).ToList();

            return Json(new { suggestions = List }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetInvoicesByPersonId(int PersonId)
        {
            var response = invoicingService.GetByPersonId(PersonId);

            return Json(new { data = response }, JsonRequestBehavior.AllowGet);
        }

        // New endpoint: list unpaid invoices with due for payment allocation UI
        public ActionResult GetUnpaidInvoicesWithDueByPerson(int personId)
        {
            var response = invoicingService.GetUnpaidInvoicesWithDueByPerson(personId);
            return Json(new { data = response }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetEditInvoicesByInvoiceId(int InvoiceId)
        {
            var Invoices = invoicingService.GetEditInvoiceById(InvoiceId);

            return Json(new { data = Invoices }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetTaxes()
        {
            var TaxIds = new SelectList(taxService.GetAll(), "TaxId", "TaxName");

            return Json(new { data = TaxIds }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SendInvoiceByEmail(string subject, string body, bool isBodyHtml, string to, int locationId)
        {
            emailService.SendInvoiceAndPaymentEmail(subject, body, isBodyHtml, to, 0, locationId);

            return Json(new { status = true, Message = "Email Sent Successfully" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.view_acc_InvoiceDetail)]
        public async Task<ActionResult> SendInvoicePushNotification(int id)
        {
            var invoicing = invoicingService.GetById(id);
            if (invoicing == null)
            {
                return Json(new { success = false, message = "Invoice not found." }, JsonRequestBehavior.AllowGet);
            }

            if (invoicing.StudentId <= 0)
            {
                return Json(new { success = false, message = "Student not found for this invoice." }, JsonRequestBehavior.AllowGet);
            }

            var subject = "Invoice Reminder";
            var description = "Your invoice " + invoicing.Code + " is available. Net amount: " + invoicing.NetAmount;
            var redirectUrl = "/Student/Invoicings/InvoicingList";

            await notificationService.SendNotification(
                null,
                invoicing.StudentId,
                "Student",
                subject,
                description,
                redirectUrl,
                PMS.Common.Globals.User.Email);

            return Json(new { success = true, message = "Push notification sent to student." }, JsonRequestBehavior.AllowGet);
        }

        // GET: Invoicings/Delete/5
        [AuthorizeUser(Roles = AppUserRoles.delete_acc_Invoice)]

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Invoicing invoicing = invoicingService.GetById(id);

            if (invoicing == null)
            {
                return HttpNotFound();
            }

            return View(invoicing);
        }

        [AuthorizeUser(Roles = AppUserRoles.Approve_acc_Invoice)]
        public ActionResult ApproveInvoice(int id)
        {
            var result = invoicingService.Approve(id);

            return Json(new { status = result }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ResidentOccupancy(int serviceId, int studentId, int invoiceTypeId)
        {
            try
            {
                var response = invoicingService.GetStudentOccupancy(serviceId, studentId, invoiceTypeId);

                return Json(new { status = true, data = response }, JsonRequestBehavior.AllowGet);
            }

            catch (Exception ex)
            {
                return Json(new { status = true, data = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetDepositOptions(int serviceId, int studentId)
        {
            try
            {
                var options = invoicingService.GetAllDepositOptions(serviceId, studentId);

                if (options == null || options.Count == 0)
                    return Json(new { success = false, message = "No bookings found" }, JsonRequestBehavior.AllowGet);

                if (options.Count == 1)
                    return Json(new { success = true, multiple = false, data = options.First() }, JsonRequestBehavior.AllowGet);


                var list = options.Select(x => new {
                    label = x.Occupancy + (x.LocationID == (int)Enumeration.LocationEnum.Dubai ? " - AED " : " - RO ") + x.ServicePrice,
                    price = x.ServicePrice,
                    occupancyId = x.OccupancyId,
                    termId = x.TermId,
                    taxId = x.TaxId,
                    occupancy = x.Occupancy
                });

                return Json(new { success = true, multiple = true, data = list }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetPayableInvoice(int id)
        {
            var response = invoicingService.GetUnpaidInvoice(id);

            return Json(new { success = response.Success, data = response.Data }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CreatePaymentLink(int Id)
        {
            var response = paymentGatewayService.PayNow(Id, "/PaymentGateway/PayGatewayResponse?ref=");

            if (response.Success)
            {
                TempData["success"] = response.Message;
                return Json(new { success = response.Success, data = response.Data }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                TempData["error"] = response.Message;
                return Json(new { success = response.Success, data = response.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetPaymentLink(int Id)
        {
            var response = paymentGatewayService.PayNow(Id, "/PaymentGateway/response?Respond=");

            if (response.Success)
            {
                TempData["success"] = response.Message;

                return Json(new { success = response.Success, data = response.Data }, JsonRequestBehavior.AllowGet);
            }

            else
            {
                TempData["error"] = response.Message;

                return Json(new { success = response.Success, data = response.Message }, JsonRequestBehavior.AllowGet);
            }
            //return Json(new
            //{
            //    Success = true,
            //    PaymentLink = paymentLink,
            //});
        }

        public ActionResult ResidentPackage(int studentId)
        {
            try
            {
                var response = vehicleSubscriptionService.GetStudentPackage(studentId);

                return Json(new { status = true, data = response }, JsonRequestBehavior.AllowGet);
            }

            catch (Exception ex)
            {
                return Json(new { status = true, data = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetFrequencyById(int Id, int configId)
        {
            try
            {
                var response = invoicingService.GetFrequencyById(Id, configId);
                return Json(new { status = true, data = response }, JsonRequestBehavior.AllowGet);
            }

            catch (Exception ex)
            {
                return Json(new { status = true, data = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [AuthorizeUser(Roles = AppUserRoles.View_Deposit_Invoices)]

        public ActionResult GetDepositInvoices(int? StudentId = 0, int? InvoiceId = 0, int? Refunded = 0)
        {
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", StudentId);
            ViewBag.PaymentTypeId = new SelectList(paymentTypesservice.GetPayment().Where(x => x.IsActive == true), "PaymentId", "PaymentName");

            var model = reportingService.GetDepositInvoices(StudentId, InvoiceId, Refunded).ToList();

            ViewBag.Refunded = Refunded;
            ViewBag.success = TempData["success"];
            ViewBag.error = TempData["error"];

            return View(model);
        }

        public JsonResult DepositInvoices(int InvoiceId)
        {
            var res = invoicingService.GetDepositInvoices(InvoiceId);
            TempData["res"] = res;

            return Json(new { success = true, res = res }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.Refund_Deposit_Invoices)]
        public ActionResult CreateRefundInvoice(DepositInvoicesVM depositInvoicesVM, string source = "deposit")
        {
            bool res = invoicingService.CloneInvoice(depositInvoicesVM, source);

            try
            {
                if (res == true)
                {
                    TempData["success"] = "Invoice refund successfully.";
                }
                else
                {
                    TempData["error"] = "Invoice refund has already been processed or cannot be updated at this time. Please try again later.";
                }
            }

            catch (Exception ex)
            {

            }

            // Redirect based on source screen
            if (source == "partnerledger")
            {
                return RedirectToAction("PartnerLedger", "Payments", new { personId = depositInvoicesVM.personId });
            }

            return RedirectToAction("GetDepositInvoices");
        }

        //[HttpPost]
        //[AuthorizeUser(Roles = AppUserRoles.Payment_Cloned_Invoices)]
        //public ActionResult RefundedInvoicePayment(DepositInvoicesVM depositInvoicesVM)
        //{
        //    depositInvoicesVM.IsPaid = depositInvoicesVM.IsPaid ?? false; // Set default value to false if IsPaid is null

        //    bool res = invoicingService.SaveCloneInvoicePayment(depositInvoicesVM);

        //    try
        //    {
        //        if (res == true)
        //        {
        //            TempData["success"] = "Payment generated successfully.";
        //        }

        //        else
        //        {
        //            TempData["error"] = "Payment not updated. Please try again later.";
        //        }
        //    }

        //    catch (Exception ex)
        //    {
        //        // Handle exception if necessary
        //    }

        //    return RedirectToAction("GetDepositInvoices");
        //}

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.Payment_Cloned_Invoices)]
        public ActionResult RefundedInvoicePayment(DepositInvoicesVM depositInvoicesVM)
        {
            depositInvoicesVM.IsPaid = depositInvoicesVM.IsPaid ?? false;

            var result = invoicingService.ProcessRefundedInvoicePayment(depositInvoicesVM);
            if (result.Success)
                TempData["success"] = result.Message;
            else
                TempData["error"] = result.Message;
            return RedirectToAction("GetDepositInvoices");
        }

        private bool SaveCreditNote(DepositInvoicesVM vm)
        {
            var creditNoteVm = new StudentCreditNoteVm
            {
                LocationId = vm.LocationId,
                TypeId = (int)CrdNoteTypeLookup.Refund,
                Code = creditNoteService.GetCode(vm.LocationId, (int)CrdNoteTypeLookup.Refund),
                StudentId = vm.personId,
                Amount = vm.CreditNoteAmount,
                Reason = vm.CreditNoteRemarks,
                Status = (int)Enumeration.Status.Approved,
                IsUtilized = false,
                PaymentTypeId = null,
                CreatedDate = DateTime.Now,
                CreatedById = PMS.Common.Globals.User.ID
            };

            return creditNoteService.Add(creditNoteVm);
        }
        [HttpPost]
        public ActionResult ReverseInvoice(ReverseInvoiceModel reverseInvoiceData)
        {
            try
            {
                var invoicing = invoicingService.GetInoviceForReverse(reverseInvoiceData.Id);
                if (invoicing == null)
                {
                    return Json(new { success = false, message = "Invoice not found." });
                }

                var invoiceTypeId = reverseInvoiceData.InvoiceTypeId > 0
                    ? reverseInvoiceData.InvoiceTypeId
                    : invoicing.InvoiceTypeId;
                var locationId = reverseInvoiceData.LocationId ?? invoicing.LocationId;

                if (invoicing.Refunded == true)
                {
                    return Json(new { success = false, message = "This invoice has already been reversed." });
                }

                if (string.IsNullOrWhiteSpace(reverseInvoiceData.Remarks))
                {
                    return Json(new { success = false, message = "Please enter remarks." });
                }

                if (locationId == (int)LocationEnum.Dubai && invoiceTypeId == (int)InvoiceTypes.Rental)
                {
                    var studentId = reverseInvoiceData.StudentId > 0 ? reverseInvoiceData.StudentId : invoicing.StudentId;

                    var allInvoices = invoicingService.GetInvoicesByStudentId(studentId)
                        .Where(i => i.InvoiceTypeId == (int)InvoiceTypes.Rental && i.LocationId == (int)LocationEnum.Dubai)
                        .ToList();

                    var invoiceWithDates = allInvoices.Select(inv => new
                    {
                        Invoice = inv,
                        MaxToDate = inv.InvoicingDetails.Where(d => d.ToDate.HasValue).Select(d => d.ToDate.Value).DefaultIfEmpty(DateTime.MinValue).Max()
                    }).ToList();

                    var latestInvoice = invoiceWithDates.OrderByDescending(x => x.Invoice.InvoiceDate).ThenByDescending(x => x.MaxToDate).FirstOrDefault()?.Invoice;

                    if (latestInvoice == null || latestInvoice.Id != reverseInvoiceData.Id)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Only the latest invoice can be reversed. Please reverse the most recent invoice first."
                        });
                    }
                }

                if (locationId == (int)LocationEnum.Muscat)
                {
                    var studentId = invoicing.StudentId;

                    var allInvoices = invoicingService.GetInvoicesByStudentId(studentId);

                    var invoiceWithDates = allInvoices.Select(inv => new
                    {
                        Invoice = inv,
                        MaxToDate = inv.InvoicingDetails.Where(d => d.ToDate.HasValue).Select(d => d.ToDate.Value).DefaultIfEmpty(DateTime.MinValue).Max()
                    }).ToList();

                    var latestInvoice = invoiceWithDates.OrderByDescending(x => x.Invoice.InvoiceDate).ThenByDescending(x => x.MaxToDate).FirstOrDefault()?.Invoice;

                    if (latestInvoice == null || latestInvoice.Id != invoicing.Id)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Only the latest invoice can be reversed. Please reverse the most recent invoice first."
                        });
                    }
                }

                if (locationId == (int)LocationEnum.Muscat || (locationId == (int)LocationEnum.Dubai && invoiceTypeId == (int)InvoiceTypes.Miscellaneous))
                {
                    var inv = new Invoicing()
                    {
                        Code = "R-" + invoicing.Code,
                        CreatedDate = DateTime.Now,
                        InvoiceDate = DateTime.Today,
                        CreatedBy = Common.Globals.User.ID,
                        TermID = invoicing.TermID,
                        StudentId = invoicing.StudentId,
                        Remarks = reverseInvoiceData.Remarks.Trim(),
                        TotalPrice = invoicing.TotalPrice * (-1),
                        NetAmount = invoicing.NetAmount * (-1),
                        TaxAmount = invoicing.TaxAmount * (-1),
                        TaxIds = invoicing.TaxIds,
                        IsApproved = true,
                        LocationId = invoicing.LocationId,
                        ApprovedBy = Common.Globals.User.ID,
                        InvoiceTypeId = invoicing.InvoiceTypeId,
                        ParentInvoiceId = invoicing.Id,
                        TotalDiscountAmount = invoicing.TotalDiscountAmount,
                        Refunded = true
                    };

                    var invoicingDetails = invoicingService.GetInvoiceDetailsForReverse(invoicing.Id);

                    SaveReverseInvoice(inv, invoicingDetails);
                }

                if (locationId == (int)LocationEnum.Dubai && invoiceTypeId != (int)InvoiceTypes.Miscellaneous)
                {

                    if (invoicing != null)
                    {
                        var inv = new Invoicing()
                        {
                            Code = "R-" + invoicing.Code,
                            CreatedDate = DateTime.Now,
                            InvoiceDate = DateTime.Today,
                            //DueDate = DateTime.Today,
                            CreatedBy = Common.Globals.User.ID,
                            TermID = invoicing.TermID,
                            StudentId = invoicing.StudentId,
                            Remarks = reverseInvoiceData.Remarks.Trim(),
                            TotalPrice = reverseInvoiceData.SubTotal * (-1),
                            //NetAmount = invoicing.NetAmount * (-1),
                            NetAmount = reverseInvoiceData.NetTotalAmount * (-1),
                            //TaxAmount = invoicing.TaxAmount * (-1),
                            TaxAmount = reverseInvoiceData.TaxAmount * (-1),
                            TaxIds = invoicing.TaxIds,
                            IsApproved = true,
                            LocationId = invoicing.LocationId,
                            ApprovedBy = Common.Globals.User.ID,
                            InvoiceTypeId = invoicing.InvoiceTypeId,
                            ParentInvoiceId = invoicing.Id,
                            //TotalDiscountAmount = invoicing.TotalDiscountAmount,
                            TotalDiscountAmount = reverseInvoiceData.DiscountAmount,
                            Refunded = true
                        };

                        var invoicingDetails = invoicingService.GetInvoiceDetailsForReverse(invoicing.Id);

                        foreach (var dbDetail in invoicingDetails)
                        {
                            var updatedDetail = reverseInvoiceData.InvoiceDetails
                              .FirstOrDefault(x => x.Id == dbDetail.Id);

                            if (updatedDetail == null)
                                continue;

                            dbDetail.FromDate = updatedDetail.FromDate;
                            dbDetail.ToDate = updatedDetail.ToDate;

                            dbDetail.Price = -updatedDetail.Price;
                            dbDetail.TotalAmount = -updatedDetail.TotalAmount;
                        }

                        SaveReverseInvoice(inv, invoicingDetails);
                    }
                }

                return Json(new { success = true, message = "Invoices generated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while generating invoices" });
            }
        }


        public ActionResult GetReverseInvoiceData(int id)
        {
            try
            {
                var invoiceDetails = invoicingService.GetInvoiceDetail(id);
                if (invoiceDetails == null)
                {
                    return Json(new { success = false, message = "Invoice details not found" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = true, data = invoiceDetails }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult SaveReverseInvoice(Invoicing invoicing, List<InvoicingDetail> list)
        {
            try
            {
                bool status = invoicingService.SaveReverseInvoice(invoicing, list);

                if (status == true)
                {
                    ViewBag.success = "Invoice Reversed Successfully";
                    TempData["success"] = ViewBag.success;
                }
                else
                {
                    ViewBag.error = "Something Went Wrong";
                    TempData["error"] = ViewBag.error;
                }

                return Json(new { status = status, redirect = false, error = string.Empty }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;

                ViewBag.error = errorMessage;
                TempData["error"] = ViewBag.error;

                return Json(new { status = false, redirect = false, error = string.Empty }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetServiceTypeId(int invoiceDetailId)
        {
            try
            {
                var serviceTypeId = invoicingService.GetServiceType(invoiceDetailId);
                if (serviceTypeId == null)
                {
                    return Json(new { success = false, message = "Service type not found" }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { success = true, data = serviceTypeId }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [AuthorizeUser(Roles = AppUserRoles.view_UpComing_Invoices)]
        public ActionResult GetInvoicesTillNextWeek()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();
            var assignedLocationids = assignedLocationIds?.FirstOrDefault() ?? 0;

            var lists = uow.Context.GetInvoicesTillNextWeek(assignedLocationids).ToList();
            List<unpaidInvoice> unpaidInvoices = new List<unpaidInvoice>();
            foreach (var item in lists)
            {
                unpaidInvoice unpaid = new unpaidInvoice
                {
                    InvoiceId = item.InvoiceId,
                    StudentId = item.StudentId,
                    Personcode = item.PersonCode,
                    FullName = item.FullName,
                    InvoiceDate = item.InvoiceDate,
                    CheckOut = item.CheckOut,
                    TillDate = item.TillDate,
                    PaidStatus = item.PaidStatus,
                };

                unpaidInvoices.Add(unpaid);
            }

            ViewBag.unpaidinvoice = unpaidInvoices;
            if (TempData.ContainsKey("ShowMessage") && TempData.ContainsKey("MessageType"))
            {
                ViewBag.MessageType = TempData["MessageType"];
                ViewBag.ShowMessage = TempData["ShowMessage"];
            }
            return View();
        }

    }
}
