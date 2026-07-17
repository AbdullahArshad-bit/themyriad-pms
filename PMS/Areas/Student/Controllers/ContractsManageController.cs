using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using PMS.Common.Filters;
using PMS.DTO.ViewModels.ContractViewModels;
using PMS.Services.Services.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;
using System.Text;
using PMS.Areas.Student.Classes;

namespace PMS.Areas.Student.Controllers
{
    [AuthorizeUser]
    [AllowUserFilter]
    public class ContractsManageController : Controller
    {
        private readonly IContractManageService ContractManageService;
        public ContractsManageController(IContractManageService _ContractMangeService)
        {
            ContractManageService = _ContractMangeService;
        }
        // GET: Student/ContractsManage
        public ActionResult StudentContracts()
        {
            var StdId = PMS.Common.Globals.User.PersonId;
            List<StudentConractsListVM> model = ContractManageService.GetSingleStudentContract(StdId);
            ViewBag.success = TempData["success"];
            ViewBag.error = TempData["error"];

            return View(model);
        }
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Accept_Contract_Details(StudentConractsVM studentConracts)
        {
            var signdocument = ContractManageService.SignContractDocument(studentConracts.Id, studentConracts.Signature, "By User Online");
            if (signdocument == true)
            {
                TempData["success"] = "Contract Signed Successfully";

            }
            else
            {
                TempData["error"] = "Something Went Wrong";
            }
            return RedirectToAction("StudentContracts");
        }
        [AllowAnonymous]
        [ValidateInput(false)]
        public FileResult ContractDownloadPdf(int id, string type)
        {
            var html = "";
            var FileName = "";
            var ContractNumber = "";
            if (type == "contract")
            {
                var contract = ContractManageService.GetContractById(id);
                FileName = contract.ContractName;
                ContractNumber = contract.ContractNumber;
                html = contract.ContractContent.ContentValue;

            }
            else
            {
                var contract = ContractManageService.GetStudentContractById(id);
                FileName = contract.ContractName;
                html = contract.Content;

            }
            using (MemoryStream stream = new System.IO.MemoryStream())
            {
                StringReader sr = new StringReader(html);

                Document pdfDoc = new Document(PageSize.LETTER, 15f, 15f, 10f, 0f);
                BaseFont bf = iTextSharp.text.pdf.BaseFont.CreateFont(BaseFont.TIMES_ROMAN, iTextSharp.text.pdf.BaseFont.CP1257, false);
                Font courier = new Font(Font.FontFamily.COURIER, 30, Font.BOLD, BaseColor.ORANGE);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();
                XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, sr);
                pdfDoc.Close();
                return File(stream.ToArray(), "application/pdf", FileName + " - " + ContractNumber + ".pdf");
            }

            //FontOverrider fontOverrider = new FontOverrider(Environment.GetEnvironmentVariable("windir") + @"\fonts\ARIAL.TTF");
            //using (MemoryStream stream = new MemoryStream())
            //{
            //    Document pdfDoc = new Document(PageSize.LETTER, 15f, 15f, 10f, 0f);
            //    PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
            //    pdfDoc.Open();
            //    using (var msCss = new MemoryStream(Encoding.UTF8.GetBytes("")))
            //    {
            //        using (var msHtml = new MemoryStream(Encoding.UTF8.GetBytes(html)))
            //        {
            //            XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, msHtml, msCss, Encoding.UTF8, fontOverrider);
            //        }
            //    }

            //    pdfDoc.Close();
            //    return File(stream.ToArray(), "application/pdf", FileName + " - " + ContractNumber + ".pdf");
            //}




        }
        public class FontOverrider : FontFactoryImp
        {
            private readonly BaseFont baseFont;
            public FontOverrider(string path, string encoding = BaseFont.IDENTITY_H, bool embedded = BaseFont.EMBEDDED)
            {
                if (!System.IO.File.Exists(path))
                {
                    throw new FileNotFoundException("Could not find the supplied font file", path);
                }

                baseFont = BaseFont.CreateFont(path, encoding, embedded);

            }

            public override Font GetFont(string fontname, string encoding, bool embedded, float size, int style, BaseColor color, bool cached)
            {
                return new Font(baseFont, size, style, color);
            }
        }
    }
}