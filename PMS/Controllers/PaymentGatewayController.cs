using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PMS.DTO;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.Notifications;
using PMS.Services.Services.Payment;
using PMS.Services.Services.PaymentGateway;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using static PMS.DTO.ViewModels.PaymentGatewayViewModel;

namespace PMS.Controllers
{
    public class PaymentGatewayController : Controller
    {
        private readonly IPaymentGatewayService paymentGatewayService;

        public PaymentGatewayController(IPaymentGatewayService _paymentGatewayService)
        {
            paymentGatewayService = _paymentGatewayService;
        }
        // GET: PaymentGateway
        public ActionResult Index()
        {
            return View();
        }
        [AllowAnonymous]
        [Route("PaymentGateway/response")]
        public ActionResult PaymentGatewayResponse()

        {
            var reference = Request.QueryString["ref"];
            if (string.IsNullOrWhiteSpace(reference))
            {
                ViewBag.error = "Payment reference not found.";
                return View();
            }

            var model = paymentGatewayService.PaymentResponse(reference.ToLower(), Request.QueryString["Respond"], true);
            if (model.Success)
            {
                ViewBag.success = model.Data.Reference;
                var response = paymentGatewayService.GetUserPayment(model.Data.Reference);
                return View(response.Data);
            }
            else
                ViewBag.error = model.Message;

            return View();
        }
        [AllowAnonymous]
        [Route("PaymentGateway/PayGatewayResponse")]
        public ActionResult PayGatewayResponse()
        {
            var reference = Request.QueryString["ref"];
            if (string.IsNullOrWhiteSpace(reference))
            {
                ViewBag.error = "Payment reference not found.";
                return View();
            }

            var model = paymentGatewayService.PaymentResponse(reference, "", true);

            if (model.Success)
            {
                ViewBag.success = model.Data.Reference;
                var res = paymentGatewayService.GetUserPayment(model.Data.Reference);
                return View(res.Data);
            }
            else
            {
                ViewBag.error = model.Message;
                return View();
            }
        }

        [AllowAnonymous]
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        [Route("PaymentGateway/ExitProcess")]
        public ActionResult ExitProcess()
        {
            var reference = ExtractReferenceFromRequest();
            if (string.IsNullOrWhiteSpace(reference))
            {
                return Json(new
                {
                    success = false,
                    message = "Payment reference not found in callback payload.",
                    query = Request.Url != null ? Request.Url.Query : ""
                }, JsonRequestBehavior.AllowGet);
            }

            var result = paymentGatewayService.PaymentResponse(reference, "", true);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                reference = reference
            }, JsonRequestBehavior.AllowGet);
        }

        private string ExtractReferenceFromRequest()
        {
            // Support direct redirects carrying ref in query string.
            var reference = Request.QueryString["ref"]
                ?? Request.QueryString["reference"]
                ?? Request.Form["ref"]
                ?? Request.Form["reference"];

            if (!string.IsNullOrWhiteSpace(reference))
                return reference;

            try
            {
                if (Request.InputStream == null || !Request.InputStream.CanRead)
                    return null;

                if (Request.InputStream.CanSeek)
                    Request.InputStream.Position = 0;

                using (var reader = new StreamReader(Request.InputStream))
                {
                    var payload = reader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(payload))
                        return null;

                    var token = JToken.Parse(payload);
                    return FindReferenceInJson(token);
                }
            }
            catch
            {
                return null;
            }
        }

        private string FindReferenceInJson(JToken token)
        {
            if (token == null)
                return null;

            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                foreach (var prop in obj.Properties())
                {
                    if ((prop.Name.Equals("reference", StringComparison.OrdinalIgnoreCase)
                        || prop.Name.Equals("ref", StringComparison.OrdinalIgnoreCase))
                        && !string.IsNullOrWhiteSpace(prop.Value.ToString()))
                    {
                        return prop.Value.ToString();
                    }

                    var nestedReference = FindReferenceInJson(prop.Value);
                    if (!string.IsNullOrWhiteSpace(nestedReference))
                        return nestedReference;
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var item in token.Children())
                {
                    var nestedReference = FindReferenceInJson(item);
                    if (!string.IsNullOrWhiteSpace(nestedReference))
                        return nestedReference;
                }
            }

            return null;
        }
    }
}
