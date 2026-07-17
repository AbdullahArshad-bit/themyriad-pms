using Microsoft.AspNet.Identity;
using PMS.Classes;
using PMS.Common;
using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels.PersonManageViewModels;
using PMS.EF;
using PMS.Services.Services.Person;
using PMS.Services.Services.Setup;
using PMS.Services.Services.PaymentTypes;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using PMS.Repository.UnitOfWork;
using PMS.Common.Classes;
using PMS.Services.Services.AuditLogs;
using System.Collections.Generic;
using PMS.Services.Services.CreditNote;

using PMS.Services.Services.Notifications;
using PMS.DTO.ViewModels.ReportingViewModels;
using PMS.Services.Services.Payment;
using PMS.DTO.ViewModels.PaymentViewModels;
using iTextSharp.text;
using PMS.DTO.ViewModels;
using System.Web.Services.Description;
using static PMS.Common.Classes.Enumeration;
using PMS.Services.Services.Invoicings;
using PMS.Services.Services.Email;
using DevDefined.OAuth.Framework;
using PMS.Services.Services.VoucherSystem;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.Services.Services.Correspondence;
using System.Text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using System.IO;
using System.Windows;

namespace PMS.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly IPersonService personService;
        private readonly ISetupService setupService;
        private readonly IPaymentTypesService paymentTypesservice;
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IAuditLogsService auditLogsService;
        private readonly ICreditNoteService creditNoteService;
        private readonly INotificationService notificationService;
        private readonly IPaymentService paymentService;
        private readonly IEmailService emailService;
        private readonly IVoucherService voucherService;
        private readonly ICorrespondenceService correspondenceService;


        private readonly decimal? remaningAmount = 0;
        public PaymentsController(UnitOfWork<PMSEntities> _uow, IAuditLogsService _auditLogsService, IPersonService _personService, ISetupService _setupService, IPaymentTypesService _paymentTypesService,
            ICreditNoteService _creditNoteService, INotificationService _notificationService, IPaymentService _paymentService, IEmailService _emailservice, IVoucherService _voucherService,
            ICorrespondenceService _correspondenceService)
        {
            personService = _personService;
            setupService = _setupService;
            paymentTypesservice = _paymentTypesService;
            uow = _uow;
            auditLogsService = _auditLogsService;
            creditNoteService = _creditNoteService;
            notificationService = _notificationService;
            paymentService = _paymentService;
            emailService = _emailservice;
            voucherService = _voucherService;
            correspondenceService = _correspondenceService;
        }

        [AuthorizeUser(Roles = AppUserRoles.view_acc_PaymentTransection)]
        public ActionResult Index(DateTime? FromDate, DateTime? ToDate, int? StudentId = 0)
        {

            if (FromDate == null || ToDate == null)
            {
                var today = DateTime.Now.Date;

                ToDate = today;
                FromDate = new DateTime(today.Year - 1, 9, 1);

            }
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", StudentId);

            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];
            ViewBag.PaymentTypeId = new SelectList(paymentTypesservice.GetPayment().Where(x => x.IsActive == true), "PaymentId", "PaymentName");
            return View();
        }

        [AuthorizeUser(Roles = AppUserRoles.View_Refund_Payments)]
        public ActionResult Refunds(DateTime? FromDate, DateTime? ToDate, int? StudentId = 0)
        {
            if (FromDate == null || ToDate == null)
            {
                var today = DateTime.Now.Date;

                ToDate = today;
                FromDate = new DateTime(today.Year - 1, 9, 1);

            }
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", StudentId);

            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];
            return View();
        }

        public ActionResult loadPaymentbyAjax(PaymentBinding request)
        {
            try
            {
                var payment = new PaymentResponse();
                payment = paymentService.GetAll(request, "", request.search.value, request.start, request.length, request.orderBy, request.orderDir, request.query, request.FromDate, request.ToDate, request.StudentId);
                var result = Json(new { draw = request.draw, recordsFiltered = payment.RecordsFiltered, recordsTotal = payment.TotalRecords, data = payment.PaymentingList });
                return result;
            }

            catch (Exception ex)
            {
                return Json(new { error = "An error occurred while retrieving data." });
            };
        }

        public ActionResult GetChildPayments(int parentId)
        {
            try
            {
                var children = paymentService.GetChildPayments(parentId);
                return Json(new { data = children }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { error = "An error occurred while retrieving child payments." });
            }
        }

        public ActionResult loadRefundPaymentByAjax(PaymentBinding request)
        {
            try
            {
                var payment = paymentService.GetRefunds(request, "", request.search.value, request.start, request.length, request.query, request.orderBy, request.orderDir, request.FromDate, request.ToDate, request.StudentId);
                var result = Json(new { draw = request.draw, recordsFiltered = payment.RecordsFiltered, recordsTotal = payment.TotalRecords, data = payment.PaymentingList });
                return result;
            }
            catch (Exception)
            {
                return Json(new { error = "An error occurred while retrieving data." });
            }
        }

        public void ExportPaymentReport(DateTime? FromDate, DateTime? ToDate)
        {
            string QueryBy = "Excel";
            var payment = paymentService.ExportPaymentReport(QueryBy, FromDate, ToDate);
            var report = payment.PaymentingList;
            var data = report.Select(x => new
            {

                x.Location,
                x.TransactionCode,
                x.InvoiceCode,
                InvoiceDate = x.InvoiceDate.HasValue ? x.InvoiceDate.Value.ToString("dd/M/yyyy") : null,
                x.MyriadID,
                x.FullName,
                PaymentDate = x.PaymentDate.ToString("dd/M/yyyy"),
                x.Remarks,
                Amount = x.Amount,
                PaymentMethod = x.PaymentName,
                PaymentReference = x.PaymentReferenceNumber,
                Status = x.IsApproved ? "Approved" : "Pending",
                x.CreatedBy,
                x.ApprovedBy

            });
            ExcelHelper.ExportToExcel(Response, data, "Payment-Transactions - PMS");
            return;
        }

        public void ExportRefundPaymentReport(DateTime? FromDate, DateTime? ToDate)
        {
            string QueryBy = "Excel";
            var payment = paymentService.ExportRefundPaymentReport(QueryBy, FromDate, ToDate);

            var report = payment.PaymentingList;
            var data = report.Select(x => new
            {
                x.Location,
                x.TransactionCode,
                x.InvoiceCode,
                InvoiceDate = x.InvoiceDate.HasValue ? x.InvoiceDate.Value.ToString("dd/M/yyyy") : null,
                x.MyriadID,
                x.FullName,
                PaymentDate = x.PaymentDate.ToString("dd/M/yyyy"),
                x.Remarks,
                Amount = x.Amount,
                PaymentMethod = x.PaymentName,
                PaymentReference = x.PaymentReferenceNumber,
                Status = x.IsApproved ? "Approved" : "Pending",
                x.CreatedBy,
                x.ApprovedBy
            });
            ExcelHelper.ExportToExcel(Response, data, "Refund-Payment-Transactions - PMS");
            return;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser(Roles = AppUserRoles.Edit_Refund_Payment)]
        public ActionResult ReversePayment(int paymentId, decimal amount, int paymentTypeId, string remarks)
        {
            try
            {
                paymentService.ReversePayment(paymentId, amount, paymentTypeId, remarks);
                TempData["success"] = "Payment reversed successfully.";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        [AuthorizeUser(Roles = AppUserRoles.Add_acc_PaymentTransection)]
        public ActionResult Create()
        {
            var model = new StudentLedger();
            {
                model.CreatedDate = DateTime.Now;
                model.PaymentDate = DateTime.Now;
                model.CreatedBy = Common.Globals.User.ID;
            }
            ViewBag.InvoiceId = new SelectList("");
            ViewBag.StudentId = new SelectList("");
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            ViewBag.LastLocation = setupService.GetLastLocation();
            ViewBag.PaymentTypeId = new SelectList(paymentTypesservice.GetPayment().Where(x => x.IsActive == true), "PaymentId", "PaymentName");
            if (!string.IsNullOrEmpty(Convert.ToString(TempData["chkCreditAmount"])))
                TempData["chkCreditAmount"] = null;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser(Roles = AppUserRoles.Add_acc_PaymentTransection)]
        public ActionResult Create([Bind(Include = "Id,PaymentDate,Code,InvoiceId,StudentId,DebitAmount,CreditAmount,IsApproved,CreatedBy,CreatedDate,Remarks,PaymentTypeId,PaymentTypeName,PaymentReferenceNumber,LocationId,CreditNoteId")] StudentLedger studentLedger)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    paymentService.ProcessPayment(studentLedger);

                    ViewBag.success = "Payment Saved Successfully";
                    TempData["success"] = ViewBag.success;

                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;

                if (errorMessage == "Cannot pay rental invoice")
                {
                    ViewBag.error = errorMessage;
                    TempData["error"] = ViewBag.error;
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.error = errorMessage;
                    TempData["error"] = ViewBag.error;
                    return RedirectToAction("Index");
                }
            }

            return RedirectToAction("Index");
        }

        [AuthorizeUser(Roles = AppUserRoles.Edit_acc_PaymentTransection)]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            StudentLedger studentLedger = uow.GenericRepository<StudentLedger>().GetById(id);
            if (studentLedger == null)
            {
                return HttpNotFound();
            }

            var Invoices = uow.GenericRepository<Invoicing>().Table.Where(x => x.StudentId == studentLedger.StudentId && x.IsApproved == true && (x.IsPaid != true || x.Id == studentLedger.InvoiceId) && x.Refunded != false).Select(x => new
            {
                Id = x.Id,
                Value = x.Code + " - (" + x.NetAmount + ")",
                Amount = x.NetAmount
            }).ToList();
            ViewBag.InvoiceId = new SelectList(Invoices, "Id", "Value", studentLedger.InvoiceId);
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", studentLedger.StudentId);
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", studentLedger.LocationId);
            var paymentTypes = new SelectList(paymentTypesservice.GetPayment(), "PaymentId", "PaymentName", studentLedger.PaymentTypeId);
            ViewBag.PaymentTypeId = paymentTypes;
            return View(studentLedger);
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.Edit_acc_PaymentTransection)]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,PaymentDate,Code,InvoiceId,StudentId,DebitAmount,CreditAmount,IsApproved,CreatedBy,CreatedDate,Remarks,PaymentTypeId,PaymentTypeName,PaymentReferenceNumber,LocationId,CreditNoteId")] StudentLedger studentLedger)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    paymentService.ProcessPaymentUpdate(studentLedger);

                    ViewBag.success = "Updated Successfully";
                    TempData["success"] = ViewBag.success;

                    if (studentLedger.LookupId == (int)Enumeration.PaymentLookup.PaymentRefund)
                    {
                        return RedirectToAction("Refunds");
                    }
                    else
                    {
                        return RedirectToAction("Index");
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;

                if (errorMessage == "Please pay previous invoices first." || errorMessage == "Miscellaneous invoice pending for more than two months.")
                {
                    ViewBag.error = errorMessage;
                    TempData["error"] = ViewBag.error;
                    
                    if (studentLedger.LookupId == (int)Enumeration.PaymentLookup.PaymentRefund)
                    {
                        return RedirectToAction("Refunds");
                    }
                    else
                    {
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    ViewBag.error = errorMessage;
                    TempData["error"] = ViewBag.error;
                    
                    // Redirect based on payment type
                    if (studentLedger.LookupId == (int)Enumeration.PaymentLookup.PaymentRefund)
                    {
                        return RedirectToAction("Refunds");
                    }
                    else
                    {
                        return RedirectToAction("Index");
                    }
                }
            }

            return View(studentLedger);
        }




        private void UpdateInvoicePaymentStatus(int? invoiceId)
        {
            if (!invoiceId.HasValue)
            {
                return;
            }

            var TotalPaidAmount = uow.GenericRepository<StudentLedger>().Table.Where(x => x.InvoiceId == invoiceId.Value).ToList();
            var remainingAmount = TotalPaidAmount.Sum(x => x.DebitAmount) - TotalPaidAmount.Sum(x => x.CreditAmount);

            var Invoice = uow.GenericRepository<Invoicing>().GetById(invoiceId.Value);
            Invoice.IsPaid = remainingAmount == 0;
            uow.GenericRepository<Invoicing>().Update(Invoice);
            uow.SaveChanges();
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            StudentLedger studentLedger = uow.GenericRepository<StudentLedger>().GetById(id);
            if (studentLedger == null)
            {
                return HttpNotFound();
            }
            return View(studentLedger);
        }

        [AuthorizeUser(Roles = AppUserRoles.view_acc_PaymentTransectionDetail)]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            StudentLedger studentLedger = uow.GenericRepository<StudentLedger>().GetById(id);
            if (studentLedger == null)
            {
                return HttpNotFound();
            }
            ViewBag.LocationSetting = setupService.GetLocationSettingsByLocationid(studentLedger.LocationId ?? 0);
            ViewBag.Invoice = paymentTypesservice.GetInvoiceCode(id);
            return View(studentLedger);
        }

        [Route("Payment/checkpayables")]
        public ActionResult CheckRemainingPayablesByInvoiceId(int InvoiceId)
        {
            try
            {
                var totalPayable = RemainingPayablesByInvoiceId(InvoiceId);

                return Json(new { Status = true, Amount = totalPayable }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {

                return Json(new { Status = false, message = ex.Message }, JsonRequestBehavior.AllowGet);

            }

        }

        public decimal RemainingPayablesByInvoiceId(int InvoiceId)
        {
            var Invoiceamount = uow.GenericRepository<StudentLedger>().Table.Where(x => x.InvoiceId == InvoiceId && x.PaymentTypeName == "Invoice").FirstOrDefault().DebitAmount;
            var PaidAmount = uow.GenericRepository<StudentLedger>().Table.Where(x => x.InvoiceId == InvoiceId && x.PaymentTypeName != "Invoice").ToList();
            var totalPayable = PaidAmount.Count > 0 ? Invoiceamount - PaidAmount.Sum(x => x.CreditAmount) : Invoiceamount;
            return totalPayable ?? 0;
        }


        //[AuthorizeUser(Roles = AppUserRoles.view_acc_PartnetLedger)]
        //public ActionResult PartnerLedger(DateTime? FromDate, DateTime? ToDate, int PersonId, bool Status = true)
        //{
        //    var query = db.StudentLedgers
        //                  .Include(x => x.Invoicing.InvoicingDetails)
        //                  .Include(x => x.Location)
        //                  .Include(x => x.Person)
        //                  .Where(x => x.StudentId == PersonId);

        //    if (Status)
        //        query = query.Where(x => x.IsApproved == Status);

        //    // Retrieve all records and order by date for proper balance calculation
        //    var allRecords = query.OrderBy(x => x.PaymentDate).ToList();
        //    decimal openingBalance = 0;

        //    if (FromDate != null)
        //    {
        //        // Calculate opening balance from records before FromDate
        //        var prevRecords = allRecords.Where(x => x.PaymentDate.Date < FromDate.Value.Date).ToList();
        //        openingBalance = prevRecords.Sum(x => x.DebitAmount ?? 0) - prevRecords.Sum(x => x.CreditAmount ?? 0);
        //    }

        //    // Filter records for display based on date range
        //    var model = allRecords.Where(x =>
        //        (FromDate == null || x.PaymentDate.Date >= FromDate.Value.Date) &&
        //        (ToDate == null || x.PaymentDate.Date <= ToDate.Value.Date)
        //    ).ToList(); // Already ordered by PaymentDate from the original query

        //    ViewBag.PersonId = PersonId;
        //    ViewBag.Status = Status;
        //    ViewBag.openingBalance = openingBalance;
        //    ViewBag.FromDate = FromDate?.ToString("dd/MMM/yyyy");
        //    ViewBag.ToDate = ToDate?.ToString("dd/MMM/yyyy");

        //    return View(model);
        //}


        [AuthorizeUser(Roles = AppUserRoles.view_acc_PartnetLedger)]
        public ActionResult PartnerLedger(DateTime? FromDate, DateTime? ToDate, int PersonId, bool Status = true)
        {
            var model = paymentService.GetPartnerLedger(FromDate, ToDate, PersonId, Status);
            ViewBag.PaymentTypeId = new SelectList(paymentTypesservice.GetPayment().Where(x => x.IsActive == true), "PaymentId", "PaymentName");
            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];
            return View(model);
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.view_acc_PartnetLedger)]
        public ActionResult CreatePartnerLedgerPayment(decimal DebitAmount, int PaymentTypeId, string Remarks, int PersonId, int LocationId, decimal OutstandingBalance)
        {
            try
            {
                var payment = paymentService.CreatePartnerLedgerPayment(DebitAmount, PaymentTypeId, Remarks, PersonId, LocationId);

                TempData["success"] = "Payment created successfully!";
                return RedirectToAction("PartnerLedger", new { PersonId = PersonId });
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error creating payment: " + ex.Message;
                return RedirectToAction("PartnerLedger", new { PersonId = PersonId });
            }
        }


        [AuthorizeUser(Roles = AppUserRoles.Approve_acc_PaymentTransection)]
        public ActionResult ApprovePayment(int id)
        {
            var oldpayment = uow.GenericRepository<StudentLedger>().Table.AsNoTracking().Where(x => x.Id == id).FirstOrDefault();
            StudentLedger ledger = uow.GenericRepository<StudentLedger>().GetById(id);
            {
                ledger.IsApproved = true;
                ledger.ApprovedBy = Common.Globals.User.ID;

            }
            uow.GenericRepository<StudentLedger>().Update(ledger);
            uow.SaveChanges();

            //Insert Audit Log
            {
                var difference = Common.Classes.Common.DetailedCompare<EF.StudentLedger>(oldpayment, ledger);
                List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Update,
                    ActionId = (int)Enumeration.CorrespondenceAction.UpdatePayment,
                    PK = ledger.Id.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "StudentLedger - Approve",
                    Reference = ledger.Code,
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    PersonId = ledger.StudentId,
                    AuditLogDetails = difference
                };
                auditLogsService.AddAuditLog(auditLog);
            }

            return Json(new { status = true }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult GetLocationBYId(int id)
        {
            try
            {
                var Students = personService.GetPersons().Where(x => x.LocationId == id).Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }).ToList();

                var Code = GetMaxReceiptCodeString(id);

                return Json(new { Status = true, Students = Students, Code = Code }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {

                return Json(new { Status = false }, JsonRequestBehavior.AllowGet);

            }
        }

        //public static string GetMaxReceiptCodeString(int id)
        //{
        //    PMSEntities db1 = new PMSEntities();
        //    var data = db1.Locations.Find(id);
        //    var maxcode = GetMaxReceiptCode(id);
        //    string value = String.Format("{0:D4}", maxcode);
        //    var Code = "RCT-" + data.Prefix + "-" + value;
        //    return Code;
        //}
        public string GetMaxReceiptCodeString(int id)
        {
            lock (this)
            {
                PMSEntities db1 = new PMSEntities();
                var data = db1.Locations.Find(id);
                var maxcode = GetMaxReceiptCode(id);
                string value = String.Format("{0:D4}", maxcode);
                var Code = "RCT-" + data.Prefix + "-" + value;
                return Code;
            }
        }

        public static int GetMaxReceiptCode(int id)
        {
            PMSEntities db1 = new PMSEntities();
            int code = 0;

            if (db1.StudentLedgers.Where(x => x.CreditAmount != null && x.Code != null && x.LocationId == id).Count() != 0)
            {
                var nowithGRn = Convert.ToDecimal(db1.StudentLedgers.Where(x => x.CreditAmount != null && x.Code != null && x.LocationId == id).AsEnumerable().Select(x => new { Number = Convert.ToDecimal(x.Code.Split('-').Last()) }).Max(x => x.Number)) + 1;
                code = (int)nowithGRn;
            }
            else

            {
                code = 1;
            }
            return code;
        }

        public ActionResult GetOutStandingBalance(int id)
        {
            var res = uow.GenericRepository<EF.StudentLedger>().Table.Where(x => x.StudentId == id && x.IsApproved == true).Select(x => new ResidentTrialBalanceVM
            {
                DebitAmount = x.DebitAmount ?? 0,
                CreditAmount = x.CreditAmount ?? 0,
                locationID = x.LocationId ?? 0
            }).ToList();
            var Balance = res.Sum(x => x.DebitAmount) - res.Sum(x => x.CreditAmount);

            var locationID = res.FirstOrDefault()?.locationID ?? 0;
            var currencyName = uow.GenericRepository<EF.Currency>()
                .Table
                .Where(c => c.LocationId == locationID)
                .Select(c => c.Name)
                .FirstOrDefault() ?? "Currency";

            return Json(new { data = Balance, currency = currencyName }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetForPaymentById(int id)
        {
            var paymenttype = uow.GenericRepository<PaymentType>().Table.Where(x => x.Code == "Out-01").FirstOrDefault();
            var res = uow.GenericRepository<EF.StudentLedger>().Table.Where(x => x.StudentId == id && x.IsApproved == true).Select(x => new ResidentTrialBalanceVM
            {
                DebitAmount = x.DebitAmount ?? 0,
                CreditAmount = x.CreditAmount ?? 0,
                PaymentTypeId = paymenttype.PaymentId
            }).ToList();
            var Balance = res.Sum(x => x.DebitAmount) - res.Sum(x => x.CreditAmount);
            var PaymentTypeId = res.Select(x => x.PaymentTypeId).FirstOrDefault();
            return Json(new { data = Balance, data1 = PaymentTypeId }, JsonRequestBehavior.AllowGet);

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // uow is managed by dependency injection, no need to dispose here
            }
            base.Dispose(disposing);
        }

        [AllowAnonymous]
        public ActionResult PaymentHtml(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            StudentLedger studentLedger = uow.GenericRepository<StudentLedger>().GetById(id);

            if (studentLedger == null)
            {
                return HttpNotFound();
            }

            ViewBag.Invoice = paymentTypesservice.GetInvoiceCode(id);

            return View(studentLedger);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SendPaymentByEmail(string subject, string body, bool isBodyHtml, string to, int locationId)
        {
            emailService.SendInvoiceAndPaymentEmail(subject, body, isBodyHtml, to, 0, locationId);

            return Json(new { status = true, Message = "Email Sent Successfully" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SendPaymentReceiptEmail(int paymentId)
        {
            try
            {
                var studentLedger = uow.GenericRepository<StudentLedger>().GetById(paymentId);
                if (studentLedger == null)
                {
                    return Json(new { status = false, Message = "Payment not found" }, JsonRequestBehavior.AllowGet);
                }

                paymentService.SendPaymentReceiptEmail(studentLedger);

                return Json(new { status = true, Message = "Payment Receipt Email Sent Successfully" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = false, Message = "Error sending email: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.view_acc_PartnetLedger)]
        public ActionResult ValidateTransactionPassword(string password, int locationId)
        {
            try
            {
                if (string.IsNullOrEmpty(password))
                {
                    return Json(new { success = false, message = "Password is required" }, JsonRequestBehavior.AllowGet);
                }

                // Get transaction password for this location
                var transactionPassword = PMS.Services.Helpers.LocationAccountsCacheHelper.GetLocationTransactionPassword(locationId, uow);

                if (string.IsNullOrEmpty(transactionPassword))
                {
                    return Json(new { success = false, message = "Transaction password not configured for this location" }, JsonRequestBehavior.AllowGet);
                }

                // Decrypt the stored password and compare
                var decryptedPassword = PMS.Common.Security.StringCipher.Decrypt(transactionPassword);

                if (password == decryptedPassword)
                {
                    return Json(new { success = true, message = "Password validated successfully" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "Invalid transaction password" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error validating password: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
