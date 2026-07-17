using PMS.Services.Services.Invoicings;
using PMS.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using PMS.Common.Filters;
using PMS.DTO.ViewModels.ReportingViewModels;
using PMS.Classes;
using System.Net.Http;
using System.IO;
using System.Net.Http.Headers;
using PMS.Services.Services.Reporting;
using PMS.Areas.Student.Classes;
using static PMS.DTO.ViewModels.PaymentGatewayViewModel;

namespace PMS.Areas.Student.Controllers
{
    [AuthorizeUser]
    [AllowUserFilter]
    public class InvoicingsController : Controller
    {
        // GET: Student/Invoicings
        private readonly IInvoicingService InvoicingService;
        private readonly IReportingService reportingService;
        public InvoicingsController(IInvoicingService _InvoicingService, IReportingService _reportingService)
        {
            InvoicingService = _InvoicingService;
            reportingService = _reportingService;
        }
        public ActionResult InvoicingList(bool ispaid = true)
        {
            var StdId = PMS.Common.Globals.User.PersonId;
            var invoice = InvoicingService.GetActiveInvoicesByPerson(StdId);
            //if (!ispaid)
            //    invoice = invoice.Where(x => x.Status != true).ToList();

            ViewBag.Invoicings = invoice;
            ViewBag.IsPaid=ispaid;
            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];
            return View();
        }
        public JsonResult GetPayableInvoice(int Id)
        {
            var response = InvoicingService.GetUnpaidInvoice(Id);
            return Json(new {success=response.Success, data = response.Data }, JsonRequestBehavior.AllowGet);
        }

    }
}