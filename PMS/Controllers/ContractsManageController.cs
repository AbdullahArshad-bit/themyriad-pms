using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using PMS.Common.Filters;
using PMS.Services.Services.Contracts;
using PMS.Services.Services.BedSpacePlace;
using PMS.Services.Services.Booking;
using PMS.Services.Services.Person;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using PMS.Common.Classes;
using System.Text;
using PMS.Services.Services.Correspondence;
using PMS.Common;
using PMS.Services.Services.Setup;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.Invoicings;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using PMS.DTO.ViewModels.ContractViewModels;
using static PMS.Common.Classes.Enumeration;

namespace PMS.Controllers
{
    [AuthorizeUser]
    public class ContractsManageController : BaseController
    {
        private readonly IContractManageService contractManageService;
        private readonly IBedSpacePlacementService placementService;
        private readonly IBookingService bookingService;
        private readonly IPersonService personService;
        private readonly ICorrespondenceService correspondenceService;
        private readonly ISetupService setupService;
        private readonly UnitOfWork<PMSEntities> uow;

        public ContractsManageController(IContractManageService _contractManageService, IBedSpacePlacementService _placementService,
            IBookingService _bookingService,
            IPersonService _personService, ICorrespondenceService _correspondenceService,
            ISetupService _setupService, UnitOfWork<PMSEntities> _uow)
        {
            contractManageService = _contractManageService;
            placementService = _placementService;
            bookingService = _bookingService;
            personService = _personService;
            correspondenceService = _correspondenceService;
            setupService = _setupService;
            uow = _uow;

        }
        [AuthorizeUser(Roles = AppUserRoles.view_contracts)]

        public ActionResult Contracts()
        {
            ViewBag.Contracts = contractManageService.GetContracts();

            return View();
        }
        [AuthorizeUser(Roles = AppUserRoles.Add_contracts)]

        public ActionResult AddContracts(int? id, bool nv = false)
        {
            AddContractVM model = new AddContractVM();
            model.Properties = new ContractProperties();
            model.Properties.IsActive = true;
            model.Properties.ContractVersion = 1;

            model.Content = new PMS.DTO.ViewModels.ContractViewModels.ContractContent();
            model.Email = new PMS.DTO.ViewModels.ContractViewModels.ContractEmail();
            model.Assertions = new List<ContractAssertions>();

            if (id > 0)
            {
                var contract = contractManageService.GetContractVMById(Convert.ToInt32(id));
                if (contract != null)
                {
                    model = contract;

                    if (nv)
                    {
                        model.OriginalContractID = contract.ContractID;
                        model.ContractID = 0;

                        model.Properties.ContractVersion = contract.Properties.ContractVersion + 1;
                        model.IsEnable = true;
                        model.Properties.IsActive = true;
                        model.IsPublish = false;
                    }
                    else if (model.IsPublish)
                    {
                        TempData["error"] = "This version is published and no changes can be made to it.";
                        return RedirectToAction("Contracts");
                    }
                }
                else
                {
                    if (nv)
                        TempData["error"] = "Contract not found to add new version.";
                    else
                        TempData["error"] = "Contract not found to edit.";

                    return RedirectToAction("Contracts");
                }
            }
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");

            model.Properties.ContractTypeList = contractManageService.GetContractTypesDD();
            model.Email.EmailMessageList = contractManageService.GetEmailMessagesDDList();

            return View(model);
        }
        [AuthorizeUser(Roles = AppUserRoles.Add_contracts)]
        [HttpPost, ValidateAntiForgeryToken, ValidateInput(false)]
        public ActionResult AddContracts(AddContractVM model)
        {
            bool IsActive = (Request.Form["IsActive"] != null);
            model.Properties.IsActive = IsActive;

            bool IsPublish = (Request.Form["Publish"] != null);
            model.IsPublish = IsPublish;

            if (ModelState.IsValid)
            {
                try
                {
                    if (model.ContractID == 0)
                    {
                        model.CreatedDate = DateTime.Now;
                        model.CreatedBy = Common.Globals.User.Email;

                        model.UpdatedDate = DateTime.Now;
                        model.UpdatedBy = Common.Globals.User.Email;

                        model = contractManageService.AddContract(model);
                        if (model.ContractID > 0)
                        {
                            TempData["success"] = "Contract saved successfully.";
                            return RedirectToAction("AddContracts", new { id = 0, nv = false });
                        }
                        else
                        {
                            ViewBag.error = "Something went wrong, unable to save contract.";
                        }
                    }
                    else
                    {
                        model.UpdatedDate = DateTime.Now;
                        model.UpdatedBy = Common.Globals.User.Email;

                        contractManageService.UpdateContract(model);

                        TempData["success"] = "Contract updated successfully.";

                        return RedirectToAction("Contracts");
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.error = ex.Message;
                }
            }
            else
            {
                ViewBag.error = "Model error.";
            }

            model.Properties.ContractTypeList = contractManageService.GetContractTypesDD();
            model.Email.EmailMessageList = contractManageService.GetEmailMessagesDDList();
            if (model.Assertions == null)
                model.Assertions = new List<ContractAssertions>();

            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.Delete_contracts)]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DeleteContract(int id)
        {
            if (contractManageService.DeleteContract(id))
                TempData["success"] = "Contract deleted successfully.";
            else
                TempData["error"] = "Error : Unable to delete contract at this moment.";

            return RedirectToAction("Contracts");
        }
        [AuthorizeUser(Roles = AppUserRoles.view_contractTypes)]

        public ActionResult ContractTypes()
        {
            ViewBag.ContractTypes = contractManageService.GetContractTypes();

            return View();
        }
        [AuthorizeUser(Roles = AppUserRoles.Add_contractTypes)]

        public ActionResult AddContractType(int? id)
        {
            ContractTypesVM model = new ContractTypesVM();
            if (id > 0)
            {
                var type = contractManageService.GetContractTypeById(Convert.ToInt32(id));
                if (type != null)
                {
                    model = type;
                }
            }
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult AddContractType(ContractTypesVM model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (model.ContractTypeID == 0)
                    {
                        model.CreatedBy = PMS.Common.Globals.User.Email;
                        model.CreatedDate = DateTime.Now;
                        if (contractManageService.AddContractType(model).ContractTypeID > 0)
                        {
                            TempData["success"] = "Contract type saved successfully.";
                            return RedirectToAction("ContractTypes");
                        }
                        else
                        {
                            ViewBag.error = "Something went wrong. Contract type not saved";
                        }
                    }
                    else
                    {
                        model.UpdatedBy = PMS.Common.Globals.User.Email;
                        model.UpdatedDate = DateTime.Now;
                        contractManageService.UpdateContractType(model);
                        TempData["success"] = "Contract type updated successfully.";
                        return RedirectToAction("ContractTypes");
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.error = ex.Message;
                }
            }
            else
            {
                ViewBag.error = "Model error.";
            }
            return View(model);
        }


        [AllowAnonymous]
        [ValidateInput(false)]
        public FileResult ContractDownloadPdf(int id, string type)
        {
            string html = "";
            string fileName = "";
            string contractNumber = "";
            string signature = null;

            if (type == "contract")
            {
                var contract = contractManageService.GetContractById(id);
                fileName = contract.ContractName;
                contractNumber = contract.ContractNumber;
                html = contract.ContractContent.ContentValue;
            }
            else
            {
                var contract = contractManageService.GetStudentContractById(id);
                fileName = contract.ContractName;

                html = contract.Content;
                signature = contract.Signature;
            }

            // Replace [[ContractSignature]] with a placeholder

            using (MemoryStream stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.LETTER, 15f, 15f, 10f, 0f);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                // Parse and add the HTML content
                using (var stringReader = new StringReader(html))
                {
                    XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, stringReader);
                }

                // Handle the signature as base64 image if available
                if (!string.IsNullOrEmpty(signature) && signature.StartsWith("data:image"))
                {
                    string base64String = signature.Replace("data:image/png;base64,", "")
                                                   .Replace("data:image/jpeg;base64,", "");
                    byte[] imageBytes = Convert.FromBase64String(base64String);
                    iTextSharp.text.Image signatureImage = iTextSharp.text.Image.GetInstance(imageBytes);

                    // Scale the signature image
                    signatureImage.ScaleToFit(150f, 75f);

                    // Add the image to the PDF
                    pdfDoc.Add(signatureImage);
                }
                // Handle the signature as plain text
                if (!string.IsNullOrEmpty(signature) && !signature.StartsWith("data:image"))
                {
                    html = html.Replace("[[StudentSignature]]", signature);
                }

                pdfDoc.Close();

                return File(stream.ToArray(), "application/pdf", fileName + ".pdf");
            }
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

        [HttpGet]
        [AuthorizeUser(Roles = AppUserRoles.view_StudentContracts)]

        public ActionResult StudentContracts(int? PersonId, DateTime? FromDate, DateTime? ToDate, string Status)
        {
            if (FromDate == null || ToDate == null)
            {
                FromDate = DateTime.Now.AddDays(-7).Date;
                ToDate = DateTime.Now.Date;
            }

            ViewBag.FromDate = FromDate?.ToString("dd/MMM/yyyy");
            ViewBag.ToDate = ToDate?.ToString("dd/MMM/yyyy");
            ViewBag.SelectedStatus = Status;

            var contracts = contractManageService.GetStudentContracts(FromDate, ToDate);

            if (!string.IsNullOrEmpty(Status))
            {
                switch (Status)
                {
                    case "checkin":
                        contracts = contracts.Where(c => c.CheckIn != null && c.CheckOut == null).ToList();
                        break;
                    case "checkedout":
                        contracts = contracts.Where(c => c.CheckIn != null && c.CheckOut != null).ToList();
                        break;
                    case "pending":
                        contracts = contracts.Where(c => !c.IsSigned).ToList();
                        break;
                    case "signed":
                        contracts = contracts.Where(c => c.IsSigned).ToList();
                        break;
                    case "cancelled":
                        contracts = contracts.Where(c => c.IsCancel).ToList();
                        break;
                }
            }
            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];

            return View(contracts);
        }

        [AuthorizeUser(Roles = AppUserRoles.Cancel_contracts)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelContract(int Id, string CancellationReason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CancellationReason))
                {
                    return Json(new { status = false, redirect = false, error = "Cancellation reason is required." }, JsonRequestBehavior.AllowGet);
                }

                bool status = false;

                status = contractManageService.CancelContract(Id, CancellationReason);

                if (status == true)
                {
                    ViewBag.success = "Contract cancel successfully.";
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

                return Json(new { status = false, redirect = false, error = errorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        [AuthorizeUser(Roles = AppUserRoles.Delete_contracts + "," + AppUserRoles.Cancel_contracts)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkContractAsSigned(int Id)
        {
            try
            {
                bool status = contractManageService.MarkContractAsSigned(Id);

                if (status)
                {
                    ViewBag.success = "Contract status updated to signed successfully.";
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

                return Json(new { status = false, redirect = false, error = errorMessage }, JsonRequestBehavior.AllowGet);
            }
        }

        public FileResult ViewContractWithSignature(int id, bool view = true)
        {
            var contract = contractManageService.GetStudentContractById(id);
            string contractUrl = contract.ContractUrl;
            string signature = contract.Signature;
            string fileName = contract.ContractName;

            if (string.IsNullOrEmpty(contractUrl) || !contractUrl.StartsWith("http"))
                throw new Exception("Contract URL is invalid.");

            // Load existing PDF from URL
            using (var client = new WebClient())
            {
                byte[] pdfBytes = client.DownloadData(contractUrl);

                using (var reader = new PdfReader(pdfBytes))
                using (var outputStream = new MemoryStream())
                {
                    using (var stamper = new PdfStamper(reader, outputStream))
                    {
                        // If there's a signature, replace [[StudentSignature]] with the signature image
                        if (!string.IsNullOrEmpty(signature) && signature.StartsWith("data:image"))
                        {
                            // Convert base64 signature to image
                            var base64 = signature.Replace("data:image/png;base64,", "").Replace("data:image/jpeg;base64,", "");
                            byte[] imageBytes = Convert.FromBase64String(base64);
                            var signatureImage = iTextSharp.text.Image.GetInstance(imageBytes);
                            signatureImage.ScaleToFit(150f, 75f); // Adjust size of signature image
                            signatureImage.SetAbsolutePosition(400f, 100f); // Adjust position on the page

                            // Replace the [[StudentSignature]] in the HTML content with the image
                            var content = stamper.GetOverContent(reader.NumberOfPages);
                            content.AddImage(signatureImage);
                        }
                    }

                    string contentDisposition = view ? "inline" : "attachment";
                    Response.AppendHeader("Content-Disposition", $"{contentDisposition}; filename={fileName}.pdf");
                    return File(outputStream.ToArray(), "application/pdf");
                }
            }
        }


        public ActionResult Contractsigned(int? PersonId)
        {
            List<StudentConractsListVM> model = contractManageService.GetUnsignedContracts();

            return View(model);
        }

        [HttpPost]
        [ValidateInput(false)]
        [AuthorizeUser(Roles = AppUserRoles.view_GenerateStudentContracts)]
        public ActionResult GenerateStudentContract(StudentConractsVM studentConractsVM)
        {
            try
            {
                if (studentConractsVM.PlacementId > 0)
                {
                    // Optimized one-shot fetch for placement/booking/person hierarchy (via service)
                    var placementData = contractManageService.GetPlacementWithRelatedDataOptimized(studentConractsVM.PlacementId);
                    // Cache contract template & meta (via service)
                    var cachedTemplate = contractManageService.GetCachedContractTemplate(studentConractsVM.ContractId);

                    if (placementData == null || string.IsNullOrEmpty(cachedTemplate.EditorContent))
                    {
                        return HttpNotFound();
                    }
                    else
                    {
                        DateTime nowDateTime = DateTime.Now;
                        var currentDate = String.Format("{0:dd/MM/yyyy}", nowDateTime);
                        var contractduedate = String.Format("{0:dd/MM/yyyy}", nowDateTime.AddDays(3));

                        var processedHtml = contractManageService.ProcessContractContentOptimized(
                            cachedTemplate.EditorContent,
                            placementData,
                            studentConractsVM,
                            currentDate,
                            contractduedate);

                        studentConractsVM.BookingId = placementData.BookingId;
                        studentConractsVM.PersonId = placementData.PersonId;
                        studentConractsVM.Content = processedHtml;
                        studentConractsVM.PersonFullName = placementData.PersonFullName;
                        studentConractsVM.PersonCode = placementData.PersonCode;
                        studentConractsVM.LocationID = placementData.LocationId;
                        studentConractsVM.ContractName = placementData.PersonFullName + " - " + cachedTemplate.ContractName;

                        string message = "";
                        var generated = contractManageService.AddStudentContract(studentConractsVM, placementData.PersonEmail, cachedTemplate.EmailMessageId, out message);
                        var PersonLocationId = studentConractsVM.LocationID;
                        var locationSettings = setupService.GetLocationSettingsByLocationid(PersonLocationId ?? 0);

                        if (generated && studentConractsVM.LocationID != (int)LocationEnum.Muscat && locationSettings.PreCheckinDocumentationIsActive != false)
                        {
                            var preCheckInDocumentationVM = new PreCheckInDocumentationVM
                            {
                                PersonId = placementData.PersonId,
                                PlacementId = placementData.PlacementId,
                                BookingId = placementData.BookingId,
                                PersonCode = placementData.PersonCode,
                                PersonFullName = placementData.PersonFullName,
                                DocumentationName = placementData.PersonFullName + " - Pre CheckIn Documentation",
                                DocumentationContent = GenerateDocumentationContent(placementData.PersonLocationId ?? 0),
                                DocumentationKey = Guid.NewGuid().ToString(),
                                CreatedBy = Globals.User.ID,
                                CreatedOn = DateTime.Now,
                                IsSigned = false
                            };
                            var getDocumentationhtml = preCheckInDocumentationVM.DocumentationContent;
                            getDocumentationhtml = getDocumentationhtml.Replace("[[PersonFull_Name]]", preCheckInDocumentationVM.PersonFullName);
                            getDocumentationhtml = getDocumentationhtml.Replace("[[CheckIn_Date]]", placementData.CheckInDate.ToString("dd/MM/yyyy"));
                            getDocumentationhtml = getDocumentationhtml.Replace("[[PersonID]]", preCheckInDocumentationVM.PersonCode);
                            getDocumentationhtml = getDocumentationhtml.Replace("[[PersonPhone]]", placementData.PersonPhone);
                            preCheckInDocumentationVM.DocumentationContent = getDocumentationhtml;

                            var documentationSaved = contractManageService.AddPreCheckInDocumentation(preCheckInDocumentationVM, out message);

                            if (!documentationSaved)
                            {
                                return Json(new { status = false, message = "Documentation Save Failed: " + message }, JsonRequestBehavior.AllowGet);
                            }
                        }

                        if (generated)
                        {
                            return Json(new { status = true, data = generated, message }, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            return Json(new { status = false, message = "Something Went Wrong!" }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
            return Json(new { status = false, message = "Something Went Wrong!" }, JsonRequestBehavior.AllowGet);

        }

        //public ActionResult GenerateStudentContract(StudentConractsVM studentConractsVM)
        //{
        //    try
        //    {
        //        if (studentConractsVM.PlacementId > 0)
        //        {
        //            // Single query to get all related data at once
        //            var placementData = GetPlacementWithRelatedDataOptimized(studentConractsVM.PlacementId);
        //            var contractContent = contractManageService.GetContractVMById(studentConractsVM.ContractId);

        //            if (placementData == null || contractContent == null)
        //            {
        //                return HttpNotFound();
        //            }

        //            DateTime nowDateTime = DateTime.Now;
        //            var currentDate = nowDateTime.ToString("dd/MM/yyyy");
        //            var contractDueDate = nowDateTime.AddDays(3).ToString("dd/MM/yyyy");

        //            // Optimized string replacement using StringBuilder
        //            var gethtml = ProcessContractContentOptimized(contractContent.Content.EditorContent, placementData, studentConractsVM, currentDate, contractDueDate);

        //            // Prepare contract data
        //            studentConractsVM.BookingId = placementData.BookingId;
        //            studentConractsVM.PersonId = placementData.PersonId;
        //            studentConractsVM.Content = gethtml;
        //            studentConractsVM.PersonFullName = placementData.PersonFullName;
        //            studentConractsVM.PersonCode = placementData.PersonCode;
        //            studentConractsVM.LocationID = placementData.LocationId;
        //            studentConractsVM.ContractName = placementData.PersonFullName + " - " + contractContent.Properties.ContractName;

        //            string message = "";
        //            var generated = contractManageService.AddStudentContract(studentConractsVM, placementData.PersonEmail, contractContent.Email.EmailMessageID, out message);

        //            // Check if documentation is needed
        //            var locationSettings = setupService.GetLocationSettingsByLocationid(studentConractsVM.LocationID ?? 0);

        //            if (generated && studentConractsVM.LocationID != ((int)Enumeration.LocationEnum.Muscat) && locationSettings.PreCheckinDocumentationIsActive != false)
        //            {
        //                // Generate documentation
        //                var preCheckInDocumentationVM = CreatePreCheckInDocumentationVM(placementData, studentConractsVM);
        //                var documentationSaved = contractManageService.AddPreCheckInDocumentation(preCheckInDocumentationVM, out message);

        //                if (!documentationSaved)
        //                {
        //                    return Json(new { status = false, message = "Documentation Save Failed: " + message }, JsonRequestBehavior.AllowGet);
        //                }
        //            }

        //            if (generated)
        //            {
        //                return Json(new { status = true, data = generated, message }, JsonRequestBehavior.AllowGet);
        //            }
        //            else
        //            {
        //                return Json(new { status = false, message = "Something Went Wrong!" }, JsonRequestBehavior.AllowGet);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //    return Json(new { status = false, message = "Something Went Wrong!" }, JsonRequestBehavior.AllowGet);
        //}

        // Removed legacy optimized helpers to avoid duplicates

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser(Roles = AppUserRoles.Delete_contractTypes)]

        public ActionResult DeleteContractType(int id)
        {
            try
            {
                if (contractManageService.GetContracts().ToList().Where(x => x.ContractTypeID == id).ToList().Count > 0)
                {
                    TempData["error"] = "You can not delete Contract Type is in use.";
                    var error = TempData["error"];
                    return RedirectToAction("ContractTypes", "ContractsManage");
                }
                if (contractManageService.DeleteContractType(id))
                    TempData["Success"] = "Contract type deleted successfully.";
                else
                    TempData["error"] = "Error : Unable to delete contract type at this moment.";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }
            return RedirectToAction("ContractTypes");
        }

        public ActionResult GetContractVersions(string contractNumber)
        {
            var model = contractManageService.GetContracts(contractNumber);

            return PartialView("_PartialContractVersions", model);
        }

        [HttpGet]
        public ActionResult GetContractDetailByPlacement(int PlacementId)
        {
            var contract = contractManageService.GetContractDetailByPlacment(PlacementId);
            return Json(contract, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetOldContract(int? personId)


        {
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", personId);

            if (personId.HasValue)
            {
                var _allOldContracts = new List<FileInfo>();
                var _studentContracts = contractManageService.GetSingleStudentContractLatest(personId.Value);
                var _place = new DirectoryInfo(Server.MapPath("/Upload/Files/StudentContracts"));
                //foreach (var item in _studentContracts)
                //{
                if (_studentContracts != null)
                {
                    var _activeFileName = _studentContracts.ContractUrl.Split('/').Last();
                    var _fileName = _activeFileName.Split(new string[] { "--" }, StringSplitOptions.None).First();
                    //var url = contractManageService.GetUrl();
                    //var onlyname = last.Select(x => x.Split('-'));
                    //var items=Directory.EnumerateFiles(Server.MapPath("/Upload/Files/StudentContracts"));           
                    //var last = items.Select(x => x.Split('\\').Last().Split('-').Skip(1).Take(1));

                    var _allFiles = _place.GetFiles(_fileName + "*");
                    ViewBag.contract = /*_files =*/ _allFiles.ToList();
                    //}
                }
                var request = HttpContext.Request.Url;
                ViewBag.auth = request.Authority;
            }


            return View();
        }

        private string GenerateDocumentationContent(int? locationId)
        {
            var NotifyEmail = correspondenceService.GetEmailMessagesByActionId(((int)Enumeration.CorrespondenceAction.PreCheckInDocumentation), locationId ?? 0);
            var body = NotifyEmail.EmailMessageBody;
            return body;
        }

        [AllowAnonymous]
        [ValidateInput(false)]
        public FileResult PreCheckInDownloadPdf(int id)
        {
            var preCheckInDoc = contractManageService.GetPreCheckInDocumentationById(id);
            if (preCheckInDoc == null)
            {
                return null;
            }

            var fileName = preCheckInDoc.DocumentationName;
            var html = preCheckInDoc.DocumentationContent;
            var base64ImageString = preCheckInDoc.StudentSignature;

            using (MemoryStream stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.LETTER, 15f, 15f, 10f, 0f);
                PdfWriter writer = PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                // If there's a signature, replace [[Signature]] with an image placeholder
                if (!string.IsNullOrEmpty(base64ImageString))
                {
                    string base64String = base64ImageString.Replace("data:image/png;base64,", "");
                    byte[] imageBytes = Convert.FromBase64String(base64String);
                    iTextSharp.text.Image signatureImage = iTextSharp.text.Image.GetInstance(imageBytes);

                    // Scale the signature image
                    signatureImage.ScaleToFit(150f, 75f);

                    // Split the HTML content into parts before and after [[Signature]]
                    string[] htmlParts = html.Split(new string[] { "[[Signature]]" }, StringSplitOptions.None);
                    string htmlBeforeSignature = htmlParts[0];
                    string htmlAfterSignature = htmlParts.Length > 1 ? htmlParts[1] : string.Empty;

                    // Parse and add the part of HTML before [[Signature]]
                    using (var stringReader = new StringReader(htmlBeforeSignature))
                    {
                        XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, stringReader);
                    }

                    // Add the signature image at the current position
                    pdfDoc.Add(signatureImage);

                }
                else
                {
                    // If no signature, just parse the whole HTML content
                    using (var stringReader = new StringReader(html))
                    {
                        XMLWorkerHelper.GetInstance().ParseXHtml(writer, pdfDoc, stringReader);
                    }
                }

                pdfDoc.Close();

                return File(stream.ToArray(), "application/pdf", fileName + ".pdf");
            }
        }




    }
}