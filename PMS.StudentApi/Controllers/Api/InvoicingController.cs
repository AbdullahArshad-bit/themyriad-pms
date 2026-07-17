//using PMS.Services.Services.Invoicing;
using PMS.Services.Services.Invoicings;
using PMS.StudentApi.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PMS.StudentApi.Controllers.Api
{
    [ApiAuthorize]
    public class InvoicingController : ApiController
    {
        private readonly IInvoicingService InvoicingService;
        public InvoicingController(IInvoicingService _InvoicingService)
        {
            InvoicingService = _InvoicingService;
        }
        [HttpGet]
        public HttpResponseMessage GetAll(int id)
        {
            
            var invoice = InvoicingService.GetInvoicesById(id);
            return Request.CreateResponse((HttpStatusCode)invoice.Code, invoice);
        }
    }
}
