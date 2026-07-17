using PMS.Areas.Student.Classes;
using PMS.Common.Filters;
using PMS.Services.Services.Payment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Areas.Student.Controllers
{
    [AuthorizeUser]
    [AllowUserFilter]
    public class PaymentController : Controller
    {
        // GET: Student/Payment
        private readonly IPaymentService PaymentService;
        public PaymentController(IPaymentService _PaymentService)
        {
            PaymentService = _PaymentService;
        }
        public ActionResult PaymentList()
        {
            var PersonId = PMS.Common.Globals.User.PersonId;
            var payment = PaymentService.GetPayment(PersonId);
            ViewBag.payments = payment;
            return View();
        }
        public JsonResult GetInvoicePayableAmount(int Id)
        {
            var response = PaymentService.InvoicePayableAmount(Id);
           return Json(new { data =response },JsonRequestBehavior.AllowGet);
        }
    }
}