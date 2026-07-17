using PMS.Services.Services.PaymentGateway;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Areas.Student.Controllers
{
    public class PaymentGatewayController : Controller
    {
        private readonly IPaymentGatewayService paymentGatewayService;
        public PaymentGatewayController(IPaymentGatewayService _pyamentGatewayService)
        {
            paymentGatewayService = _pyamentGatewayService;
        }
        // GET: Student/PaymentGateway
        public ActionResult PaymentMethod(int id)
        {

            return View();
        }
        public JsonResult PayNow(int Id)
        {
            var response=paymentGatewayService.PayNow(Id, "/PaymentGateway/muscatpayment?Respond=", true);
            if(response.Success)
            return Json(new { success = response.Success, data = response.Data }, JsonRequestBehavior.AllowGet);
            else
            return Json(new { success = response.Success, data = response.Message }, JsonRequestBehavior.AllowGet);
        }
        [Route("PaymentGateway/muscatpayment")]
        public ActionResult PaymentGatewayResponse()
        {
            var model = paymentGatewayService.PaymentResponse(Request.QueryString["ref"].ToLower(), Request.QueryString["Respond"]);
            if (model.Success)
                TempData["success"] = model.Data.Reference;
            else

                TempData["error"] = model.Message;
          
            return RedirectToAction("InvoicingList", "Invoicings",new { area="Student"});
        }
        public JsonResult GetUserPayment(string referenceId)
        {
           var response= paymentGatewayService.GetUserPayment(referenceId);
         
            return Json(new { success = response.Success, data = response.Data }, JsonRequestBehavior.AllowGet);
        }
    }
}