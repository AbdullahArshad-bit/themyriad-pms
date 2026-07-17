using PMS.Services.Services.Contracts;
using PMS.StudentApi.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;

namespace PMS.StudentApi.Controllers.Api
{
    [ApiAuthorize]
    public class ContractManageController : ApiController
    {
        public readonly IContractManageService ContractManageService;
        public ContractManageController(IContractManageService _contractManageService)
        {
            ContractManageService = _contractManageService;
        }
        public HttpResponseMessage GetAll(int id)
        {
            var contract = ContractManageService.GetAllById(id);
            return Request.CreateResponse((HttpStatusCode)contract.Code, contract);
        }
        public HttpResponseMessage GetPdfContract(int id)
        {
            var html = "";
            var FileName = "";
            var ContractNumber = "";
            var contract1 = ContractManageService.GetStudentContractById(id);
            HttpResponseMessage response=Request.CreateResponse(HttpStatusCode.OK, contract1);
            FileName = contract1.ContractName;
            html = contract1.Content;
            var stream= PdfHelper.GetPdfByHtml(html, "ARIAL.TTF");
            response.Content = new ByteArrayContent(stream.ToArray());
            response.Content.Headers.ContentLength = stream.ToArray().LongLength;
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            response.Content.Headers.ContentDisposition.FileName = FileName+".pdf";

            //Set the File Content Type.
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            return response;
        }
    }
}
